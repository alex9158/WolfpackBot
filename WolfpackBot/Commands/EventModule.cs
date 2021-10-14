using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Data;
using WolfpackBot.Data.DataAccess;
using WolfpackBot.Data.Models;
using WolfpackBot.DataAccess;
using WolfpackBot.Extensions;

using WolfpackBot.Services;

namespace WolfpackBot.Commands
{
    [Group("event")]
    [Alias("events")]
    public class EventModule : ModuleBase<SocketCommandContext>
    {
        private readonly IEvents _events;
        private readonly IPermissionService _permissionService;
        private readonly IEventParticipantService _eventParticipantService;
        private readonly WolfpackDbContext _db;

        public EventModule(IEvents events, IPermissionService permissionService, IEventParticipantService eventParticipantService, WolfpackDbContext db)
        {
            _events = events;
            _permissionService = permissionService;
            _eventParticipantService = eventParticipantService;
            _db = db;
        }

        [Command("create")]
        [Alias("add")]
        public async Task Create(string eventName, string shortName, int? capacity = null)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to create events");
                return;
            }

            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id);
            var existingEventWithAlias = await _events.GetActiveEvent(shortName, Context.Guild.Id);
            if (existingEvent != null || existingEventWithAlias != null)
            {
                await Context.Channel.SendMessageAsync("There is already an active event with that name or short name. Event names must be unique!");
                return;
            }

            string roleId = "";
            try
            {
                /* Create a new role for this event. The role is not visible in the sidebar and can be menitoned.
                 * It is possible that the shortName is already used for a role, but that's not forbidden by Discord.
                 * It might be a consideration for the future to check for duplicates and assign a unique name.
                 */

                var role = await author.Guild.CreateRoleAsync(shortName + " notify", null, null, false, true);
                roleId = role.Id.ToString();
            }
            catch (Discord.Net.HttpException) { /* ignore forbidden exception */ }


            var @event = new Event
            {
                ChannelId = Context.Channel.Id.ToString(),
                GuildId = Context.Guild.Id.ToString(),
                RoleId = roleId,
                ShortName = shortName,
                Closed = false,
                Name = eventName,
                Capacity = capacity
            };
            _db.Add(@event);
            await _db.SaveChangesAsync();
            existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id);
            await Context.Channel.SendMessageAsync($"{Context.Message.Author.Mention} has created the event {eventName}! React to the message below to sign up to the event. If you can no longer attend, simply remove your reaction!");
            await _eventParticipantService.CreateAndPinParticipantMessage(Context.Channel, existingEvent);
        }

        [Command("close")]
        [Alias("delete")]
        public async Task Close([Remainder] string eventName)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to create events");
                return;
            }

            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }

            var role = Context.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == existingEvent.RoleId);

            /* the event could have already been deleted by a mod, null-check required */

            if (role != null)
            {
                try
                {
                    await role.DeleteAsync();
                }
                catch (Discord.Net.HttpException) { /* ignore forbidden exception */ }
            }

            await _eventParticipantService.UnpinEventMessage(Context.Channel, existingEvent);
            existingEvent.Closed = true;
            await _db.SaveChangesAsync();

            await Context.Channel.SendMessageAsync($"{existingEvent.Name} is now closed!");
        }

        [Command("active")]
        [Alias("current", "open")]
        public async Task ActiveEvents()
        {
            var activeEvents = await _events.GetActiveEvents(Context.Guild.Id);
            if (!activeEvents.Any())
            {
                await Context.Channel.SendMessageAsync($"There's no events currently running for {Discord.MentionUtils.MentionChannel(Context.Channel.Id)}.");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Currently active events:{Environment.NewLine}{string.Join(Environment.NewLine, activeEvents.Select(ev => $"{ev.Name}{(ev.SpaceLimited ? $" - {ev.EventSignups.Count}/{ev.Capacity} participants" : "")}"))}");
                await Context.Channel.SendMessageAsync($"Join any active event with the command `!event signup event name`");
            }
        }

        [Command("signup")]
        [Alias("sign", "join")]
        public async Task SignUp([Remainder] string eventName)
        {
            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }
            if (existingEvent.EventSignups.Any(sign => sign.UserId == Context.Message.Author.Id.ToString()))
            {
                await Context.Message.Author.SendMessageAsync($"You're already signed up to {eventName}");
                return;
            }

            if (existingEvent.SpaceLimited && existingEvent.EventSignups.Count >= existingEvent.Capacity)
            {
                await Context.Message.Author.SendMessageAsync($"Sorry, but {eventName} is already full! Keep an eye out in-case someone pulls out.");
                return;
            }


            string nickName = Context.Message.Author.Username;

            if (Context.Message.Author is IGuildUser guildUser)
            {
                var role = guildUser.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == existingEvent.RoleId);
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


            existingEvent.EventSignups.Add(new EventSignup()
            {
                UserId = Context.Message.Author.Id.ToString()
            });
            await _db.SaveChangesAsync();
            await Context.Guild.Owner.SendMessageAsync($"{nickName} signed up to {existingEvent.Name}");
            await Context.Message.Author.SendMessageAsync($"Thanks {Context.Message.Author.Mention}! You've been signed up to {existingEvent.Name}. You can check the pinned messages in the event's channel to see the list of participants.");
            await _eventParticipantService.UpdatePinnedMessageForEvent(Context.Channel, existingEvent);
        }

        [Command("unsign")]
        [Alias("unsignup")]
        public async Task Unsign([Remainder] string eventName)
        {
            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }
            var existingSignup = existingEvent.EventSignups.FirstOrDefault(sign => sign.UserId == Context.Message.Author.Id.ToString());
            if (existingSignup == null)
            {
                await Context.Channel.SendMessageAsync($"You're not currently signed up to {eventName}");
                return;
            }


            string nickName = Context.Message.Author.Username;

            if (Context.Message.Author is IGuildUser guildUser)
            {
                var role = guildUser.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == existingEvent.RoleId);
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

            existingEvent.EventSignups.Remove(existingSignup);
            await _db.SaveChangesAsync();
            await Task.WhenAll(Context.Guild.Owner.SendMessageAsync($"{nickName} signed out of {existingEvent.Name}"),
                               Context.Channel.SendMessageAsync($"Thanks { Context.Message.Author.Mention}! You're no longer signed up to {existingEvent.Name}."),                            
                               _eventParticipantService.UpdatePinnedMessageForEvent(Context.Channel, existingEvent));
        }

        [Command("signups")]
        [Alias("participants")]
        public async Task GetSignups([Remainder] string eventName)
        {
            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }


            var messageText = await _eventParticipantService.GetParticipantsMessageBody(Context.Channel, existingEvent, showJoinPrompt: false);
            await Context.Channel.SendMessageAsync(messageText);
        }

        [Command("bulkadd", ignoreExtraArgs: true)]
        [Alias("bulksign")]
        public async Task BulkAdd(string eventName)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to bulk add");
                return;
            }

            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }

            foreach (var mentionedUser in Context.Message.MentionedUsers)
            {
                existingEvent.EventSignups.Add(new EventSignup()
                {
                    UserId = mentionedUser.Id.ToString()
                });
            }

            await _db.SaveChangesAsync();
            await _eventParticipantService.UpdatePinnedMessageForEvent(Context.Channel, existingEvent);
        }

        [Command("remove", ignoreExtraArgs: true)]
        public async Task Remove(string eventName)
        {
            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }

            foreach (var user in Context.Message.MentionedUsers)
            {
                var signup = existingEvent.EventSignups.FirstOrDefault(signup => signup.UserId == user.Id.ToString());
                if(signup != null)
                {
                    existingEvent.EventSignups.Remove(signup);
                }          
            }
            await _db.SaveChangesAsync();

            await Context.Channel.SendMessageAsync($"Removed {string.Join(' ', Context.Message.MentionedUsers.Select(user => user.Username))} from {eventName}");

            await _eventParticipantService.UpdatePinnedMessageForEvent(Context.Channel, existingEvent);
        }

        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("Use `!events active` to see a list of all active events. To join an event use the `!event join` command with the name of the event. " +
                "For example `!event join ACC Championship`. To unsign from an event, use the `!event unsign` command with the name of the event. For example, `!event unsign ACC Championship`.");
        }

    }
}