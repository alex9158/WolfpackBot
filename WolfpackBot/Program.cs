using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Linq;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using WolfpackBot.DataAccess;
using WolfpackBot.Services;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using WolfpackBot.Exceptions;
using WolfpackBot.Data;
using WolfpackBot.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using WolfpackBot.Data.Models;

namespace WolfpackBot
{
    class Program
    {
        private CommandService _commandService;
        private DiscordSocketClient _client;
        private ServiceProvider _serviceProvider;
        private IConfiguration _configuration;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Bot..");
            new Program().MainAsync(args).GetAwaiter().GetResult();
        }

        public Program()
        {
            _commandService = new CommandService();
            _client = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true, MessageCacheSize = 100, GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.Guilds | GatewayIntents.GuildMessageReactions });
        }

        private async Task MainAsync(string[] args)
        {
            var services = new ServiceCollection();

            ConfigureServices(services, args);
            CreateDataDirectory();
            _serviceProvider = services.BuildServiceProvider();

            await InitCommands();

            _client.MessageReceived += MessageReceived;
            _client.ReactionAdded += OnReactionAdd;
            _client.ReactionRemoved += OnReactionRemove;

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, _configuration.GetValue<string>("Token"));
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task OnReactionRemove(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var events = _serviceProvider.GetRequiredService<IEvents>();
            var eventParticipantSets = _serviceProvider.GetRequiredService<IEventParticipantService>();
            var db = _serviceProvider.GetRequiredService<WolfpackDbContext>();
            var message = await cacheableMessage.GetOrDownloadAsync();

            if (!reaction.User.IsSpecified)
                return;

            if (message.Author.Id != _client.CurrentUser.Id)
                return;

            var @event = await events.GetEventByMessageIdAsync(cacheableMessage.Id);
            if (@event == null || @event.Closed)
            {
                return;
            }

            var existingSignup = @event.EventSignups.FirstOrDefault(sign => sign.UserId == reaction.UserId.ToString());
            if (existingSignup == null)
            {
                return;
            }

            var noOfReactionsForUser = 0;
            foreach (var r in message.Reactions)
            {
                var reactors = await message.GetReactionUsersAsync(r.Key, 999).FlattenAsync();
                if (reactors.Any(r => r.Id == reaction.UserId))
                {
                    noOfReactionsForUser++;
                }
            }
            if (noOfReactionsForUser >= 1)
            {
                return;
            }


            string nickName = reaction.User.Value.Username;

            if (reaction.User.Value is IGuildUser guildUser)
            {
                var role = guildUser.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == @event.RoleId);
                if (role != null)
                {
                    /* no effect if user doesn' have the role anymore */
                    try
                    {
                        await guildUser.RemoveRoleAsync(role);
                    }
                    catch (Discord.Net.HttpException) { /* ignore forbidden exception */ }

                }

                nickName = string.IsNullOrEmpty(guildUser.Nickname) ? nickName : guildUser.Nickname;
            }


            /*   if (channel is SocketTextChannel textChannel)
               {
                   await textChannel.Guild.Owner.SendMessageAsync($"{nickName} signed out of {@event.Name}");
               }*/
            @event.EventSignups.Remove(existingSignup);
            await db.SaveChangesAsync();
            await eventParticipantSets.UpdatePinnedMessageForEvent(channel, @event, message);
            await NotifyUser(reaction, $"Thanks! You've been removed from {@event.Name}.");
        }

        private async Task NotifyUser(SocketReaction reaction, string message)
        {
            try
            {
                await reaction.User.Value.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to send message '{message}' " +
                    $"to user '{reaction.User.Value.Username}' " +
                    $"Error: '{ex.Message}'");
            }
        }

        private async Task OnReactionAdd(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var events = _serviceProvider.GetRequiredService<IEvents>();
            var db = _serviceProvider.GetRequiredService<WolfpackDbContext>();
            var eventParticipantSets = _serviceProvider.GetRequiredService<IEventParticipantService>();
            var message = await cacheableMessage.GetOrDownloadAsync();

            async Task CancelSignup(string reason)
            {
                await NotifyUser(reaction, reason);
                await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                return;
            }

            if (!reaction.User.IsSpecified)
            {
                return;
            }

            if (message.Author.Id != _client.CurrentUser.Id)
                return;

            var @event = await events.GetEventByMessageIdAsync(cacheableMessage.Id);
            if (@event == null || @event.Closed)
            {
                return;
            }


            if (reaction.User.Value is IGuildUser user)
            {
                var moderatorService = _serviceProvider.GetRequiredService<IModerator>();
                var moderators = await moderatorService.GetLeaderboardModeratorsAsync(user.GuildId);
                if ((user.GuildPermissions.ManageGuild || moderators.Exists(m => user.RoleIds.Any(r => r.ToString() == m.RoleId)))
                    && reaction.Emote.Name == "❌")
                {
                    var eventParticipantService = _serviceProvider.GetRequiredService<IEventParticipantService>();
                    await eventParticipantService.UnpinEventMessage(channel, @event);
                    @event.Closed = true;

                    await db.SaveChangesAsync();
                    await channel.SendMessageAsync($"{@event.Name} is now closed!");
                    return;
                }
            }

            var existingSignup = @event.EventSignups.FirstOrDefault(sign => sign.UserId == reaction.User.Value.ToString());

            if (existingSignup != null)
            {
                var noOfReactionsForUser = 0;
                foreach (var r in message.Reactions) //hack to handle events signed up to with command..
                {
                    var reactors = await message.GetReactionUsersAsync(r.Key, 999).FlattenAsync();
                    if (reactors.Any(r => r.Id == reaction.UserId))
                    {
                        noOfReactionsForUser++;
                    }
                }
                if (noOfReactionsForUser > 1)
                {
                    await CancelSignup($"You are already signed for this event {reaction.User.Value.Mention}");
                }

                return;
            }

            if (@event.SpaceLimited && @event.Full)
            {
                await CancelSignup($"Sorry, {reaction.User.Value.Mention} this event is currently full!");
                return;
            }


            var nickName = reaction.User.Value.Username;

            if (reaction.User.Value is IGuildUser guildUser)
            {
                var role = guildUser.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == @event.RoleId);
                if (role != null)
                {
                    try
                    {
                        await guildUser.AddRoleAsync(role);
                    }
                    catch (Discord.Net.HttpException) { /* ignore forbidden exception */ }

                }

                nickName = string.IsNullOrEmpty(guildUser.Nickname) ? nickName : guildUser.Nickname;
            }

            @event.EventSignups.Add(new EventSignup
            {
                UserId = reaction.UserId.ToString()
            });
            await db.SaveChangesAsync();
            await eventParticipantSets.UpdatePinnedMessageForEvent(channel, @event, message);

            /* if (channel is SocketTextChannel textChannel)
             {
                 await textChannel.Guild.Owner.SendMessageAsync($"{nickName} signed up to {@event.Name}");
             }*/

            await NotifyUser(reaction, $"Thanks! You've been signed up to {@event.Name}. " +
                $"If you can no longer attend just remove your reaction from the signup message!");
        }

        private string GetDataDirectory()
        {
            return _configuration.GetValue<string>("DATA_DIRECTORY", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TTBot"));
        }

        private void CreateDataDirectory()
        {
            var path = GetDataDirectory();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }


        private void ConfigureServices(ServiceCollection services, string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables("TTBot_")
                .AddCommandLine(args);

            services.AddSingleton(this._configuration = builder.Build());
            if (!_configuration.GetValue<bool>("DISABLE_TWITTER", false))
            {
                if (string.IsNullOrEmpty(_configuration.GetValue<string>("CONSUMER_KEY")) ||
                    string.IsNullOrEmpty(_configuration.GetValue<string>("CONSUMER_SECRET")) ||
                    string.IsNullOrEmpty(_configuration.GetValue<string>("ACCESS_KEY")) ||
                   string.IsNullOrEmpty(_configuration.GetValue<string>("ACCESS_SECRET")))
                {
                    throw new InvalidConfigException();
                }
            }
            var connectionString = _configuration.GetValue<string>("CONNECTION_STRING");
         
            Console.Write("Ef Con: " + connectionString);
            services.AddDbContext<WolfpackDbContext>(options =>
            {
                options.UseSqlite(connectionString);
            });
            services.AddScoped<IModerator, Moderator>();
            services.AddScoped<ILeaderboards, Leaderboards>();
            services.AddScoped<ILeaderboardEntries, LeaderboardEntries>();

            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IEvents, Events>();
            services.AddScoped<IEventParticipantService, EventParticipantService>();
            services.AddScoped<IChampionshipResults, ChampionshipResults>();
            services.AddScoped<IExcelService, ExcelService>();
            services.AddScoped<IExcelWrapper, ExcelWrapper>();
            services.AddScoped<IEventAliasMapping, EventAliasMapping>();
            services.AddScoped<IExcelSheetEventMapping, ExcelSheetEventMapping>();
            services.AddScoped<ITwitterIntegrationService, TwitterIntegrationService>();
            services.AddSingleton(_client);
        }

        private async Task InitCommands()
        {
            await _commandService.AddModulesAsync(typeof(Program).Assembly, _serviceProvider);
        }

        private async Task MessageReceived(SocketMessage socketMessage)
        {
            var message = socketMessage as SocketUserMessage;
            int argPos = 0;

            if (message == null || !message.HasCharPrefix('!', ref argPos) || message.Author.IsBot)
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);

            var commandResult = await _commandService.ExecuteAsync(context, argPos, _serviceProvider);

            if (!commandResult.IsSuccess)
            {
                Console.WriteLine("Error: " + commandResult.ErrorReason);
                Console.WriteLine(commandResult.ToString());

                if (commandResult.Error == CommandError.BadArgCount || commandResult.Error == CommandError.ParseFailed)
                {
                    await socketMessage.Channel.SendMessageAsync("Error running command. Try wrapping command parameters in \"quotes\"");
                }
            }
        }
        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
