using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WolfpackBot.Data;
using WolfpackBot.Data.DataAccess;
using WolfpackBot.Data.Models;
using WolfpackBot.DataAccess;
using WolfpackBot.Services;
using WolfpackBot.Utilities;

namespace WolfpackBot.Commands
{
    [Group("championships")]
    [Alias("c", "champ", "champs", "championship")]
    public class ChampionshipsModule : ModuleBase<SocketCommandContext>
    {
        private readonly IChampionshipResults _results;
        private readonly IPermissionService _permissionService;
        private readonly IExcelService _excelService;
        private readonly IEvents _events;
        private readonly IEventAliasMapping _eventAliasMapping;
        private readonly IExcelSheetEventMapping _excelSheetEventMapping;
        private readonly ITwitterIntegrationService _twitterIntegrationService;
        private readonly WolfpackDbContext _db;

        public ChampionshipsModule(
            IChampionshipResults results,
            IPermissionService permissionService,
            IExcelService excelService,
            IEvents events,
            IEventAliasMapping eventAliasMapping,
            IExcelSheetEventMapping excelSheetEventMapping,
            ITwitterIntegrationService twitterIntegrationService,
            WolfpackDbContext db)
        {
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _eventAliasMapping = eventAliasMapping ?? throw new ArgumentNullException(nameof(eventAliasMapping));
            _excelSheetEventMapping = excelSheetEventMapping ?? throw new ArgumentNullException(nameof(excelSheetEventMapping));
            _twitterIntegrationService = twitterIntegrationService ?? throw new ArgumentNullException(nameof(twitterIntegrationService));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }
    

        [Command("import")]
        public async Task Standings()
        {
            var sb = new StringBuilder();

            List<string> listOfUnknownChampionships = new List<string>();
            List<string> listOfSuccessfulyUploadedChampionships = new List<string>();

            try
            {
                var author = Context.Message.Author as SocketGuildUser;
                if (!await _permissionService.UserIsModeratorAsync(Context, author))
                {
                    await Context.Channel.SendMessageAsync("You dont have permission to create events");
                    return;
                }

                var guildId = Context.Guild.Id;

                // clear out our results DB table first
                await _results.DeleteAllGuildEvents(guildId.ToString());

                var attachment = Context.Message.Attachments.First();
                var excelDriverDataModels = await _excelService.ReadResultsDataFromAttachment(attachment);

                List<ChampionshipResultsModel> championshipResults = new List<ChampionshipResultsModel>();

                foreach (ExcelDriverDataModel excelDriverDataModel in excelDriverDataModels)
                {
                    var e = await _events.GetActiveEvent(excelDriverDataModel.Championship, guildId);

                    if (e == null || e.Id == 0)
                    {
                        if (!listOfUnknownChampionships.Contains(excelDriverDataModel.Championship))
                        {
                            listOfUnknownChampionships.Add(excelDriverDataModel.Championship);
                        }
                        continue;
                    }
                    else
                    {
                        if (!listOfSuccessfulyUploadedChampionships.Contains(excelDriverDataModel.Championship))
                        {
                            listOfSuccessfulyUploadedChampionships.Add(excelDriverDataModel.Championship);
                        }
                    }

                    var eventId = e.Id;

                    // clear the Round column for each championship imported
                    // (it will be populated later and we want to avoid stale data)
                    e.Round = 0;
                    await _db.SaveChangesAsync();

                    ChampionshipResultsModel championshipResult = new ChampionshipResultsModel()
                    {
                        EventId = eventId,
                        Pos = excelDriverDataModel.Pos,
                        Driver = excelDriverDataModel.Driver,
                        Number = excelDriverDataModel.Number,
                        Car = excelDriverDataModel.Car,
                        Points = excelDriverDataModel.Points,
                        Diff = excelDriverDataModel.Diff,
                        ExcelSheetEventMappingId = excelDriverDataModel.ExcelSheetEventMappingId
                    };

                    championshipResults.Add(championshipResult);

                }

                await _results.AddAsync(championshipResults);

                var derivedRoundsForChampionships = await _excelService.DeriveRoundsFromAttachment(attachment);
                foreach (var c in derivedRoundsForChampionships)
                {
                    var e = await _events.GetActiveEvent(c.Championship, guildId);

                    if (e == null || e.Id == 0)
                    {
                        continue;
                    }

                    e.Round = c.Round;
                    e.LastRoundTrack = c.LastRoundTrack;
                    e.LastRoundDate = c.LastRoundDate;
                    e.NextRoundTrack = c.NextRoundTrack;
                    e.NextRoundDate = c.NextRoundDate;
                    await _db.SaveChangesAsync();
                }


            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error when doing import: {ex.Message}");
            }

            if (listOfSuccessfulyUploadedChampionships.Count > 0)
            {
                sb.AppendLine($"Data has been imported for the following championships: {string.Join(", ", listOfSuccessfulyUploadedChampionships)}");
            }

            if (listOfUnknownChampionships.Count > 0)
            {
                sb.AppendLine($"The following championship shortnames were not found in events: {string.Join(", ", listOfUnknownChampionships)}");
            }

            await postStandings();
            await ReplyAsync(sb.ToString());
        }

        [Command("tweet")]
        [Alias("t")]
        public async Task TweetStandings()
        {
            await postStandings(false);
        }

        [Command("twitter")]
        public async Task Twitter(string action = null, string eventShortName = null, string message = null)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to create aliases");
                return;
            }
            var sb = new StringBuilder();
            if (action == null)
            {
                sb.AppendLine("Action missing");
                await ReplyAsync(sb.ToString());
                return;
            }

            var guildId = Context.Guild.Id;

            if (action == "add" ||action == "remove")
            {
                if (eventShortName == null || message == null)
                {
                    sb.AppendLine("Details missing for the add");
                    await ReplyAsync(sb.ToString());
                    return;
                }
                var e = await _events.GetActiveEvent(eventShortName, guildId);
                if (e == null)
                {
                    sb.AppendLine($"Unknown event with shortname {eventShortName}");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                if (action == "add")
                {
                    e.TwitterMessage = message;
                    sb.AppendLine($"Twitter messaged added to {e.Name}");
                } else if (action == "remove")
                {
                    e.TwitterMessage = "";
                    sb.AppendLine($"Twitter messaged deleted for {e.Name}");
                }

                await _db.SaveChangesAsync();

            }
            else if (action == "list")
            {
                var allActiveEvents = await _events.GetActiveEvents(guildId);

                foreach (var e in allActiveEvents)
                {
                    if (e.TwitterMessage == "" || e.TwitterMessage == null)
                    {
                        continue;
                    }

                    sb.Append("***");
                    sb.Append(e.Name);
                    sb.Append("***");
                    sb.Append(": \"");
                    sb.Append(e.TwitterMessage);
                    sb.Append(": \"");
                    sb.AppendLine();
                }
                    
            }
            else
            {
                sb.AppendLine("Incorrect action - must be either add, remove or list");
            }

            await ReplyAsync(sb.ToString());
        }

        [Command("list")]
        [Alias("l")]
        public async Task GetChampionships()
        {
            var sb = new StringBuilder();

            try
            {
                var events = await _results.GetEventsWithResultsAsync();

                if (events.Length == 0)
                {
                    sb.AppendLine("No standings currently available");
                }
                else
                {
                    sb.AppendLine("Standings currently available for: ");
                    sb.AppendLine("");

                    foreach (string e in events)
                    {
                        sb.AppendLine($" - {e}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error when doing list: {ex.Message}");
            }

            await ReplyAsync(sb.ToString());
        }

        private async Task postStandings(bool toDiscord = true)
        {
            var guildId = Context.Guild.Id;

            var championships = await _events.GetActiveEvents(guildId);
            foreach (var c in championships)
            {
                var alias = c.ShortName ?? c.Name;
                await writeStandingsForChampionship(alias, guildId, toDiscord);
            }

        }

        private async Task writeStandingsForChampionship(string alias, ulong guildId, bool toDiscord)
        {
            var sb = new StringBuilder();
            try
            {
                var aliasEvent = await _eventAliasMapping.GetActiveEventFromAliasAsync(alias, guildId);
                var activeEvent = await _events.GetActiveEvent(alias, guildId);
                if (aliasEvent == null && activeEvent == null)
                {
                    sb.AppendLine($"Championship alias {alias} not found");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var eventId = aliasEvent != null ? aliasEvent.Id : activeEvent.Id;

                var e = await _events.GetActiveEvent(eventId);
                if (e == null || e.Id == 0)
                {
                    sb.AppendLine($"Championship {eventId} not found");
                    await ReplyAsync(sb.ToString());
                }
                else
                {
                    var championship = Regex.Replace(e.Name, "championship", "", RegexOptions.IgnoreCase);
                    var results = await _results.GetChampionshipResultsByIdAsync(eventId);
                    if (results.Count == 0)
                    {
                        return;
                    }

                    var excelMappingId = results.GroupBy(r => r.ExcelSheetEventMappingId)
                        .Select(group => group.Key)
                        .ToList();

                    var channel = Context.Guild.GetChannel(e.StandingsChannelId) as IMessageChannel;
                    if (channel == null)
                    {
                        sb.AppendLine($"No sign-up channel found for event {e.ShortName} to post standings to");
                        await ReplyAsync(sb.ToString());
                        return;
                    }
                    if (e.StandingsMessageIds?.Length > 0 && toDiscord)
                    {
                        foreach (var messageId in e.StandingsMessageIds)
                        {
                            try
                            {
                                e.StandingsMessageIds = e.StandingsMessageIds.Where(id => id != messageId).ToArray();
                                await channel.DeleteMessageAsync(ulong.Parse(messageId));
                            }
                            catch
                            {
                            }
                        }
                    }

                    foreach (var emId in excelMappingId)
                    {
                        var sheetName = await _excelSheetEventMapping.GetWorksheetNameFromIdAsync(emId);
                        var image = StandingsExtension.BuildImage(
                            e,
                            results.Where(r => r.ExcelSheetEventMappingId == emId).ToList(),
                            Regex.Replace(sheetName, "championship", "", RegexOptions.IgnoreCase));

                        if (toDiscord)
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                                memoryStream.Position = 0;

                                var standingsMessage = await channel.SendFileAsync
                                    (memoryStream, $"{e.Name}-standings-{DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss")}.png");


                                e.StandingsMessageIds = e.StandingsMessageIds != null
                                    ? e.StandingsMessageIds.Concat(new string[] { standingsMessage.Id.ToString() }).ToArray()
                                    : new string[] { standingsMessage.Id.ToString() };
                                await PostNextRoundAsync(e, channel);
                                await _db.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            await _twitterIntegrationService.PostImage(image, e.TwitterMessage);
                            sb.AppendLine($"Championship {e.Name} standings tweeted!");
                            await ReplyAsync(sb.ToString());
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error when getting standings: {ex.Message}");
                await ReplyAsync(sb.ToString());
            }
        }

        private async Task PostNextRoundAsync(Event e, IMessageChannel channel )
        {
            var sb = new StringBuilder();
            var zone = NodaTime.TimeZones.TzdbDateTimeZoneSource.Default.ForId("Europe/London");
            var nextDateNoda = Instant.FromDateTimeOffset(e.NextRoundDate.Value);
            var nextDate = new ZonedDateTime(nextDateNoda, zone);

            var daylight = nextDateNoda.InZone(zone).IsDaylightSavingTime();
            var UKtimezone = daylight ? "BST" : "GMT";

            var unixTimeWithOffset = nextDate.ToDateTimeOffset().ToUnixTimeSeconds();

            var description = e.NextRoundTrack != ""
                ? $"{e.NextRoundTrack} @ {e.NextRoundDate.Value.ToString("dd MMMM yyyy HH:mm")} {UKtimezone}{Environment.NewLine}" +
                $"<t:{unixTimeWithOffset}> (local) <t:{unixTimeWithOffset}:R>"
                : "Season completed. Stay tuned for news of further seasons!";

            var builder = new EmbedBuilder()
                .WithTitle("Next Round")
                .WithDescription(description);

            if (e.NextTrackMessageId > 0)
            {
                try
                {
                    await channel.DeleteMessageAsync(e.NextTrackMessageId.Value);
                }
                catch { }
            }

            try
            {
                var nextRaceMessage = await channel.SendMessageAsync(embed: builder.Build());
                e.NextTrackMessageId = nextRaceMessage.Id;
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error sending image to sign-up channel: {ex.Message}");
                await ReplyAsync(sb.ToString());
            }

        }

        [Command("alias")]
        [Alias("a")]
        public async Task Alias(string action = null, string aliases = null, string eventShortName = null)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to create aliases");
                return;
            }
            var sb = new StringBuilder();
            if (action == null)
            {
                sb.AppendLine("Action missing");
                await ReplyAsync(sb.ToString());
                return;
            }

            var guildId = Context.Guild.Id;

            if (action == "add")
            {
                if (eventShortName == null || aliases == null)
                {
                    sb.AppendLine("Details missing for the add");
                    await ReplyAsync(sb.ToString());
                    return;
                }
                var e = await _events.GetActiveEvent(eventShortName, guildId);
                if (e == null)
                {
                    sb.AppendLine($"Unknown event with shortname {eventShortName}");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var aliasesList = aliases.Split(',').Select(p => p.Trim()).ToList();

                foreach (var alias in aliasesList)
                {
                    if (await _eventAliasMapping.ActiveEventExistsAsync(alias, guildId))
                    {
                        sb.AppendLine($"Alias {alias} already exists on active event");
                    }
                    else
                    {
                        await _eventAliasMapping.AddAsync(e.Id, alias);
                        sb.AppendLine($"Alias {alias} added for event {eventShortName}");
                    }
                }
            }
            else if (action == "remove")
            {
                if (aliases == null)
                {
                    sb.AppendLine("Aliases missing for the remove");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var aliasesList = aliases.Split(',').Select(p => p.Trim()).ToList();

                foreach (var alias in aliasesList)
                {
                    if (!await _eventAliasMapping.ActiveEventExistsAsync(alias, guildId))
                    {
                        sb.AppendLine($"Alias {alias} does not exist on an active event");
                    }
                    else
                    {
                        var activeEvent = await _eventAliasMapping.GetActiveEventFromAliasAsync(alias, guildId);
                        var aliasMappingId = await _eventAliasMapping.GetAliasIdAsync(alias, guildId);
                        await _eventAliasMapping.RemoveAsync(aliasMappingId);
                        sb.AppendLine($"Alias {alias} removed for active event {activeEvent.ShortName}");
                    }

                }
            }
            else if (action == "list")
            {
                var allActiveAliases = await _eventAliasMapping.GetAllActiveAliases();
                var allActiveEvents = await _events.GetActiveEvents(guildId);
                var eId = 0;

                foreach (var item in allActiveAliases.Select((value, i) => (value, i)))
                {
                    var em = item.value;
                    var index = item.i;

                    // only list active events
                    if (allActiveEvents.Where<Event>
                            (e => e.Id == em.EventId).Select(e => e.ShortName).FirstOrDefault() == null)
                    {
                        continue;
                    }

                    if (eId != em.EventId)
                    {
                        if (eId > 0)
                        {
                            sb.Append('.');
                            sb.AppendLine();
                        }
                        sb.Append("***");
                        sb.Append(allActiveEvents.Where<Event>
                            (e => e.Id == em.EventId).Select(e => e.Name).FirstOrDefault());
                        sb.Append("***");
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    sb.Append($"{em.Alias}");
                    eId = em.EventId;
                }
            } else
            {
                sb.AppendLine("Incorrect action - must be either add, remove or list");
            }

            await ReplyAsync(sb.ToString());
        }


        [Command("worksheets")]
        [Alias("w", "sheet", "sheets", "worksheet")]
        public async Task Sheets(string action = null, string worksheet = null, string eventShortName = null, bool isRoundsSheet = false)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to link worksheets");
                return;
            }
            var sb = new StringBuilder();
            if (action == null)
            {
                sb.AppendLine("Action missing");
                await ReplyAsync(sb.ToString());
                return;
            }

            var guildId = Context.Guild.Id;

            if (action == "add")
            {
                if (eventShortName == null || worksheet == null)
                {
                    sb.AppendLine("Details missing for the add");
                    await ReplyAsync(sb.ToString());
                    return;
                }
                var e = await _events.GetActiveEvent(eventShortName, guildId);
                if (e == null)
                {
                    sb.AppendLine($"Unknown event with shortname {eventShortName}");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                if (await _excelSheetEventMapping.ActiveEventExistsAsync(worksheet))
                {
                    sb.AppendLine($"Sheetname {worksheet} already exists on active event");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                await _excelSheetEventMapping.AddAsync(e.Id, worksheet, isRoundsSheet);
                sb.AppendLine($"Worksheet {worksheet} added for event {eventShortName}");
                await ReplyAsync(sb.ToString());
                return;
            }
            else if (action == "remove")
            {
                if (worksheet == null)
                {
                    sb.AppendLine("Alias missing for the remove");
                    await ReplyAsync(sb.ToString());
                    return;
                }
                if (!await _excelSheetEventMapping.ActiveEventExistsAsync(worksheet))
                {
                    sb.AppendLine($"Worksheet {worksheet} does not exist on an active event");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var activeEvent = await _excelSheetEventMapping.GetActiveEventFromWorksheetAsync(worksheet);
                var worksheetMappingId = await _excelSheetEventMapping.GetWorksheetMappingIdAsync(worksheet);

                await _excelSheetEventMapping.RemoveAsync(worksheetMappingId);
                sb.AppendLine($"Worksheet {worksheet} removed for active event {activeEvent.ShortName}");
                await ReplyAsync(sb.ToString());
                return;
            }
            else if (action == "list")
            {
                var allActiveWorksheets = await _excelSheetEventMapping.GetAllActiveWorksheetMappings();
                var allActiveEvents = await _events.GetActiveEvents(guildId);
                var eId = 0;

                foreach (var item in allActiveWorksheets.Select((value, i) => (value, i)))
                {
                    var w = item.value;
                    var index = item.i;

                    // only list active events
                    if (allActiveEvents.Where<Event>
                            (e => e.Id == w.EventId).Select(e => e.ShortName).FirstOrDefault() == null)
                    {
                        continue;
                    }

                    if (eId != w.EventId)
                    {
                        if (eId > 0)
                        {
                            sb.Append('.');
                            sb.AppendLine();
                        }
                        sb.Append("***");
                        sb.Append(allActiveEvents.Where<Event>
                            (e => e.Id == w.EventId).Select(e => e.Name).FirstOrDefault());
                        sb.Append("***");
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    sb.Append($"{w.Sheetname}");
                    var sheetType = w.IsRoundsSheet ? "R" : "S";
                    sb.Append($" *({sheetType})*");
                    eId = w.EventId;
                }
                await ReplyAsync(sb.ToString());
                return;
            } else
            {
                sb.AppendLine("Incorrect action - must be either add, remove or list");
            }

            await ReplyAsync(sb.ToString());
        }

        [Command("channel")]
        [Alias("c")]
        public async Task AddStandingsChannelForEvent(string eventShortName = null, SocketGuildChannel taggedChannel = null)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to add standings channel for an event");
                return;
            }
            var sb = new StringBuilder();

            var guildId = Context.Guild.Id;

            // if no params provided, list active channels
            if (eventShortName == null && taggedChannel == null)
            {
                var events = await _events.GetActiveEvents(guildId);
                var hasStandingsChannels = false;
                foreach (var ev in events)
                {
                    {
                        if (ev.StandingsChannelId > 0)
                        {
                            var sC = Context.Guild.GetChannel(ev.StandingsChannelId);
                            sb.AppendLine($"{ev.ShortName}, standings channel: {sC.Name}");
                            hasStandingsChannels = true;
                        }
                    }
                }

                if (!hasStandingsChannels)
                {
                    sb.AppendLine("No standings channels for active events");
                }

                await ReplyAsync(sb.ToString());
                return;
            }
            // if there is no channel name then clear the event's current one
            if (eventShortName != null && taggedChannel == null)
            {
                var e = await _events.GetActiveEvent(eventShortName, guildId);

                if (e == null)
                {
                    sb.AppendLine($"No active event called {eventShortName}");
                }
                else if (e.StandingsChannelId == 0)
                {
                    sb.AppendLine($"No standings channels for event {eventShortName}");
                }
                else
                {
                    sb.AppendLine($"Removed standings channel for event {eventShortName}");
                    e.StandingsChannelId = 0;
                    await _db.SaveChangesAsync();
                }

                await ReplyAsync(sb.ToString());
                return;
            }
            else
            {
                var e = await _events.GetActiveEvent(eventShortName, guildId);
                if (e == null)
                {
                    sb.AppendLine($"Unknown event with shortname {eventShortName}");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                e.StandingsChannelId = taggedChannel.Id;
                await _db.SaveChangesAsync();
                sb.AppendLine($"Standings channel {taggedChannel} added for event {eventShortName}");

                await ReplyAsync(sb.ToString());
                return;
            }

        }

        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("Use `!championships list` to see a list of all championships with standings. To get the standings for a specific championship, " +
                "use `!championships standings` command with the name of the championship. For example `!championships standings MX5`.");
        }
    }


}
