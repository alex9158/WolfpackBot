using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using WolfpackBot.DataAccess;
using WolfpackBot.Models;
using WolfpackBot.Services;
using WolfpackBot.Utilities;

namespace WolfpackBot.Commands
{
    [Group("leaderboard")]
    [Alias("l")]
    public class LeaderboardModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILeaderboards _leaderboards;
        private readonly ILeaderboardEntries _leaderboardEntries;
        private readonly IPermissionService _permissionService;
        private readonly IEvents _events;

        public LeaderboardModule(ILeaderboards leaderboards, ILeaderboardEntries leaderboardEntries, IPermissionService permissionService, IEvents events)
        {
            _leaderboards = leaderboards;
            _leaderboardEntries = leaderboardEntries;
            _permissionService = permissionService;
            _events = events;
        }


        [Command("create", ignoreExtraArgs: true)]
        [Alias("add")]
        [Summary("Create a new leaderboard for a game")]
        public async Task Create([Remainder] string gameName)
        {
            if (!await _permissionService.UserIsModeratorAsync(Context, Context.User as SocketGuildUser))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to run this command");
                return;
            }

            var currentlyActiveLeaderboard = await _leaderboards.GetActiveLeaderboardForChannelAsync(Context.Guild.Id, Context.Channel.Id);
            if (currentlyActiveLeaderboard != null)
            {
                await Context.Channel.SendMessageAsync("There is already an active leaderboard for this channel. You must close the active leaderboard before creating a new one.");
                return;
            }
            await _leaderboards.AddAsync(this.Context.Guild.Id, this.Context.Channel.Id, gameName);
            await Context.Channel.SendMessageAsync($"Created new leaderboard for {gameName}. Submit a time to the leaderboard by using the command `!leaderboard submit` with your time. For example `!leaderboard submit 01:45.002`. All submission messages must have an attached image as proof of your time!{Environment.NewLine}Get racing!");
        }

        [Command("active")]
        [Alias("current")]
        public async Task Active()
        {
            var leaderboard = await _leaderboards.GetActiveLeaderboardForChannelAsync(this.Context.Guild.Id, this.Context.Channel.Id);
            if (leaderboard == null)
            {
                await Context.Channel.SendMessageAsync($"There is no active leaderboard for {Context.Channel.Name}");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Currently active leaderboard: {leaderboard.Game}");

            }
        }

        [Command("close")]
        public async Task Close()
        {
            if (!await _permissionService.UserIsModeratorAsync(Context, Context.User as SocketGuildUser))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to run this command");
                return;
            }

            var leaderboard = await _leaderboards.GetActiveLeaderboardForChannelAsync(Context.Guild.Id, Context.Channel.Id);
            if (leaderboard == null)
            {
                await Context.Channel.SendMessageAsync($"There isn't an active leaderboard for ${Context.Channel.Name}");
            }

            await Standings();

            leaderboard.Active = false;
            await _leaderboards.UpdateAsync(leaderboard);

            await Context.Channel.SendMessageAsync($"{leaderboard.Game} leaderboard is now closed");
        }

        [Command("submit")]
        public async Task Submit(string time)
        {
            var leaderboard = await _leaderboards.GetActiveLeaderboardForChannelAsync(Context.Guild.Id, Context.Channel.Id);
            if (leaderboard == null)
            {
                await Context.Channel.SendMessageAsync($"There aren't any leaderboards active for {Context.Channel.Name}");
                await this.Active();
                return;
            }
            if (!Context.Message.Attachments.Any())
            {
                await Context.Channel.SendMessageAsync($"Please attach a screenshot of your time as proof!");
                return;
            }
            var regex = @"([0-9]{1,2})([.:,])([0-9]{1,2})(?>([.:,])([0-9]{0,3}))?";
            var matchRes = Regex.Match(time.Trim(), regex);
            if (!matchRes.Success)
            {
                await Context.Channel.SendMessageAsync("Unable to parse submitted time format. Try using the format 02:23.123");
                return;
            }

            var numGroups = matchRes.Groups.Count;

            var minsMatch = matchRes.Groups[1];
            var numberOfMinsDigits = minsMatch.Length;

            var secondsMatch = matchRes.Groups[3];
            var numberOfSecondsDigits = secondsMatch.Length;

            var hasMilliseconds = numGroups == 6;
            var numOfMillisecondsDigits = 0;
            if (hasMilliseconds)
            {
                var msMatch = matchRes.Groups[5];
                numOfMillisecondsDigits = msMatch.Length;
            }
            var formatStringBuilder = new StringBuilder();

            for (int i = 0; i < numberOfMinsDigits; i++)
            {
                formatStringBuilder.Append("m");
            }
            formatStringBuilder.Append(@$"\{matchRes.Groups[2].Value}"); //seperator

            for (int i = 0; i < numberOfSecondsDigits; i++)
            {
                formatStringBuilder.Append("s");
            }

            formatStringBuilder.Append(@$"\{matchRes.Groups[4].Value}"); //seperator
            for (int i = 0; i < numOfMillisecondsDigits; i++)
            {
                formatStringBuilder.Append("f");
            }

            if (!TimeSpan.TryParseExact(time, formatStringBuilder.ToString(), CultureInfo.InvariantCulture, out var timespan))
            {
                await Context.Channel.SendMessageAsync("Unable to parse submitted time format. Try using the format 02:23.123");
                return;
            }

            await _leaderboardEntries.AddAsync(leaderboard.Id, timespan, Context.User.Id, Context.Message.Attachments.First().Url);
            await Context.Channel.SendMessageAsync($"Thanks {Context.User.Mention}! Your entry has been saved.");
            await Standings();
        }

        [Command("standings")]
        public async Task Standings([Remainder] string parameters = null)
        {
            var leaderboard = await _leaderboards.GetActiveLeaderboardForChannelAsync(Context.Guild.Id, Context.Channel.Id);
            if (leaderboard == null)
            {
                await Context.Channel.SendMessageAsync($"There aren't any leaderboards active for {Context.Channel.Name}");
                return;
            }
            var standings = (await _leaderboards.GetStandingsAsync(leaderboard.Id)).ToList(); //handle identical times here

            if (!standings.Any())
            {
                await Context.Channel.SendMessageAsync($"No times posted yet!");
                return;
            }

            var image = StandingsExtension.BuildImage(Context, leaderboard, standings);

            using (MemoryStream memoryStream = new MemoryStream())
            {

                image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                memoryStream.Position = 0;

                await Context.Channel.SendFileAsync
                    (memoryStream, $"{leaderboard.Description}-standings-{DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss")}.png");


            }
        }

        [Command("invalidate", ignoreExtraArgs: true)]
        public async Task Invalidate()
        {
            if (!await _permissionService.UserIsModeratorAsync(Context, Context.User as SocketGuildUser))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to run this command");
                return;
            }

            var leaderboard = await _leaderboards.GetActiveLeaderboardForChannelAsync(Context.Guild.Id, Context.Channel.Id);
            if (leaderboard == null)
            {
                await Context.Channel.SendMessageAsync($"There aren't any leaderboards active for {Context.Channel.Name}");
                return;
            }

            foreach (var mentionedUser in Context.Message.MentionedUsers)
            {
                var entry = await _leaderboardEntries.GetBestEntryForUser(leaderboard.Id, mentionedUser.Id);
                if (entry == null)
                {
                    await Context.Channel.SendMessageAsync("No entry submitted for user " + mentionedUser.Username);
                }

                entry.Invalidated = true;
                await _leaderboardEntries.Update(entry);
                await Context.Channel.SendMessageAsync("Best time by " + mentionedUser.Username + " has been invalidated");

            }
            await Standings();
        }

        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("You can submit times to an active leadboard by using the command `!leaderboard submit` with your time. For example `!leaderboard submit 01:45.123`. All submission messages must include an attached image as proof of your time.");
        }
    }


}
