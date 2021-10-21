using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using WolfpackBot.DataAccess;
using WolfpackBot.Services;

namespace WolfpackBot.Commands
{
    [Group("fuel")]
    public class FuelModule : ModuleBase<SocketCommandContext>
    {
        struct RaceFuel
        {
            public int RaceTime;
            public int LapTimeMs;
            public double FuelUsage;

            public int ReserveLaps;

            public int RaceLaps;
            public double fuelPerMinute;

            public double fuel;
            public double fuelSave;



            /// <summary>
            /// Create an embed which visualised this struct
            /// </summary>
            /// <param name="bot">optional for Author header</param>
            /// <param name="user">optional for Footer text</param>
            /// <returns></returns>
            public Embed ToEmbed(SocketSelfUser bot = null,  SocketUser user = null)
            {
                /* this cannot be used in anonymous expressions */
                RaceFuel data = this;

                var builder = new EmbedBuilder()
                {
                    Color = Color.Blue,
                    Title = "Fuel Calculation",
                };      

                builder.AddField(x =>
                {
                    x.Name = "Racetime";
                    x.Value = TimeSpan.FromSeconds(data.RaceTime).ToString(@"hh\:mm");
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Laptime";
                    x.Value = TimeSpan.FromMilliseconds(data.LapTimeMs).ToString(@"mm\:ss\.fff");
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Fuel per Lap";
                    x.Value = data.FuelUsage;
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Racelaps";
                    x.Value = data.RaceLaps;
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Fuel per Minute (est.)";
                    x.Value = data.fuelPerMinute.ToString("0.00");
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Minimum Fuel Required";
                    x.Value = data.fuel.ToString("0.00") + "l";
                    x.IsInline = false;
                });

                double reserve_laps_perc = data.ReserveLaps / (double)data.RaceLaps * 100;
                builder.AddField(x =>
                {
                    x.Name = $"Safe Fuel (+{data.ReserveLaps} laps / +{(int)reserve_laps_perc}%)";
                    x.Value = data.fuelSave.ToString("0.00") + "l";
                    x.IsInline = false;
                });

                if (bot != null)
                {
                    builder.WithAuthor(a =>
                    {
                        a.Name = bot.Username;
                        a.IconUrl = $"https://cdn.discordapp.com/avatars/{bot.Id}/{bot.AvatarId}.png";
                    });
                }

                if (user is SocketGuildUser gUser)
                {
                    string uName = (gUser.Nickname != null) ? gUser.Nickname : gUser.Username;

                    builder.WithFooter(f =>
                    {
                        f.Text = $"Calculation requested by {uName}";
                        f.IconUrl = $"https://cdn.discordapp.com/avatars/{gUser.Id}/{gUser.AvatarId}.png";
                    });
                }

                return builder.Build();
            }

        };

        /// <summary>
        /// Convert a string to TimeSpan
        /// Convert priority:
        ///     1. hh:mm
        ///     2. h:mm
        ///     3. [minutes]
        ///     4. return 0 seconds
        /// 
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns>0 TimeSpan on failure</returns>
        private TimeSpan ParseRaceLen(string timestamp)
        {
            TimeSpan ts;


            if (TimeSpan.TryParseExact(timestamp, @"hh\:mm", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"h\:mm", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }


            try
            {
                return TimeSpan.FromMinutes(double.Parse(timestamp, CultureInfo.InvariantCulture));
            }
            catch
            {
                return new TimeSpan();
            }
        }



        /// <summary>
        /// Convert a string to TimeSpan
        /// Convert priority:
        ///     1. m:ss.ffff
        ///     2. m:ss.fff
        ///     3. m:ss.ff
        ///     4. m:ss.f
        ///     5. m:ss
        ///     6. mm:ss
        ///     3. [total seconds]
        ///     4. return 0 seconds
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns>0 TimeSpan on failure</returns>
        private TimeSpan ParseLaptime(string timestamp)
        {
            TimeSpan ts;

            /* parsing TimeSpan is quiet annoying */


            if (TimeSpan.TryParseExact(timestamp, @"m\:ss\.ffff", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"m\:ss\.fff", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"m\:ss\.ff", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"m\:ss\.f", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"m\:ss", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"mm\:ss", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }


            try
            {
                return TimeSpan.FromSeconds(double.Parse(timestamp, CultureInfo.InvariantCulture));
            }
            catch
            {
                return new TimeSpan();
            }
        }




        private RaceFuel GetFuelUsage(RaceFuel data)
        {

            data.fuelPerMinute = data.FuelUsage * 1000 / data.LapTimeMs * 60;

            data.fuel = data.RaceLaps * data.FuelUsage;
            data.fuelSave = data.fuel + (data.ReserveLaps * data.FuelUsage);

            return data;
        }



        [Command("")]
        [Alias("time")]
        [Summary("Calculate fuel usage by time")]
        public async Task FuelTime(string raceLength, string lapTime, string fuelUsage, int reserveLaps = 3)
        {

            RaceFuel data = new RaceFuel
            {
                RaceTime = (int)ParseRaceLen(raceLength).TotalSeconds,
                LapTimeMs = (int)ParseLaptime(lapTime).TotalMilliseconds,
                FuelUsage = double.Parse(fuelUsage, CultureInfo.InvariantCulture),
                ReserveLaps = reserveLaps
            };


            /* calculating race laps here, as called function is generic and requires this value */

            double raceLaps = data.RaceTime * 1000 / (double)data.LapTimeMs;
            data.RaceLaps = (int)Math.Ceiling(raceLaps);
            data = GetFuelUsage(data);

            await Context.Channel.SendMessageAsync(embed: data.ToEmbed(Context.Client.CurrentUser, Context.User));            
        }

        
        [Command("laps")]
        [Summary("Calculate fuel usage by laps")]
        public async Task FuelLaps(int raceLaps, string lapTime, string fuelUsage, int reserveLaps = 3)
        {
            RaceFuel data = new RaceFuel
            {
                RaceLaps = raceLaps,
                LapTimeMs = (int)ParseLaptime(lapTime).TotalMilliseconds,
                FuelUsage = double.Parse(fuelUsage, CultureInfo.InvariantCulture),
                ReserveLaps = reserveLaps
            };
            /* calculating race time here, as called function is generic and requires this value */

            data.RaceTime = data.RaceLaps * data.LapTimeMs / 1000;


            data = GetFuelUsage(data);
            await Context.Channel.SendMessageAsync(embed: data.ToEmbed(Context.Client.CurrentUser, Context.User));
        }

        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("You can calculate an estimate for your fuel usage. use `!fuel <race length> <laptime> <fuel per lap>` to trigger a calculation. If your race is distance limited, use `!fuel laps <laps> <laptime> <fuel pre lap>`");
            await Context.Channel.SendMessageAsync("You can specify the number of save-laps as an optional 4th argument");
        }
    }


}
