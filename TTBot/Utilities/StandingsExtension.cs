using Discord;
using Discord.Commands;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTBot.DataAccess;
using TTBot.Models;
using TTBot.Extensions;
using System.Text.RegularExpressions;
using System.Globalization;

namespace TTBot.Utilities
{
    public static class StandingsExtension
    {
        public static Bitmap BuildImage(Event e, List<ChampionshipResultsModel> results)
        {
            var orderedResults = results.OrderBy(r => r.Pos);

            int posXStart = OperatingSystem.IsWindows() ? 100 : 110;
            int posYStart = 375;
            int championshipX = OperatingSystem.IsWindows() ? 245 : 252;
            int championshipY = OperatingSystem.IsWindows() ? 220 : 225;
            int roundX = 660;
            int roundY = 120;

            var driverX = posXStart + 100;
            var numberX = driverX + 400;
            int pointsX = numberX + 110;
            int diffX = pointsX + 155;

            int lastRowY = 0;

            string templateFilePath = @"Assets/StandingsTemplate.png";
            using (Bitmap image = (Bitmap)System.Drawing.Image.FromFile(templateFilePath))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                PrivateFontCollection fontCol = new PrivateFontCollection();
                fontCol.AddFontFile(@"Assets/Fonts/Formula1-Regular.otf");
                var formula1FontFamily = fontCol.Families[0];

                Font font, numberFont, longDriverFont, largerFont;
                if (OperatingSystem.IsWindows())
                {
                    font = new Font(formula1FontFamily, 8);
                    numberFont = new Font(formula1FontFamily, 7);
                    longDriverFont = new Font(formula1FontFamily, 5);
                    largerFont = new Font(formula1FontFamily, 10);
                }
                else
                {
                    font = new Font(formula1FontFamily.Name, 24);
                    numberFont = new Font(formula1FontFamily.Name, 20);
                    longDriverFont = new Font(formula1FontFamily.Name, 18);
                    largerFont = new Font(formula1FontFamily.Name, 28);
                }

                // write championship
                var championshipXMax = 370;
                var championshipYMax = OperatingSystem.IsWindows() ? 60 : 50;

                // For testing - uncomment to show rectangles
                /* Rectangle rect1 = new Rectangle(championshipX, championshipY, championshipXMax, championshipYMax);
                graphics.FillRectangle(
                    new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 0)), rect1);
                */

                Size championshipSize = new Size(championshipXMax, championshipYMax);
                graphics.DrawString(
                    e.Name,
                    graphics.GetAdjustedFont(e.Name, largerFont, championshipSize),
                    new SolidBrush(System.Drawing.Color.FromArgb(213, 213, 213)),
                    championshipX,
                    championshipY);

                // write round details (if available)
                if (e.Round != null && e.Round > 0)
                {
                    graphics.DrawString(
                        $"Round {e.Round}",
                        largerFont,
                        new SolidBrush(System.Drawing.Color.FromArgb(213, 213, 213)),
                        roundX,
                        roundY);

                    graphics.DrawString(
                        e.LastRoundDate,
                        largerFont,
                        new SolidBrush(System.Drawing.Color.FromArgb(213, 213, 213)),
                        roundX,
                        roundY + 50);

                    var trackXMax = 310;
                    var trackYMax = OperatingSystem.IsWindows() ? 60 : 40;
                    Size trackSize = new Size(trackXMax, trackYMax);

                    // For testing - uncomment to show rectangles
                    /*
                    Rectangle rect2 = new Rectangle(roundX, roundY + 100, trackXMax, trackYMax);
                    graphics.FillRectangle(
                        new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 0)), rect2);
                    */

                    graphics.DrawString(
                        e.LastRoundTrack,
                        graphics.GetAdjustedFont(e.LastRoundTrack, largerFont, trackSize),
                        new SolidBrush(System.Drawing.Color.FromArgb(213, 213, 213)),
                        roundX,
                        roundY + 100);
                }

                int y = OperatingSystem.IsWindows() ? posYStart - 1 : posYStart - 4;
                int driverPosition = 0;
                foreach (ChampionshipResultsModel r in orderedResults)
                {
                    graphics.FillRoundedRectangle(
                        Brushes.White,
                        OperatingSystem.IsWindows() ? posXStart + 7 : posXStart + 1,
                        OperatingSystem.IsWindows() ? y - 3 : y - 5,
                        50,
                        40,
                        4);

                    var posX = r.Pos <= 9
                        ? posXStart + 15
                        : posXStart + 5;

                    graphics.DrawString(r.Pos.ToString(), numberFont, Brushes.Black, posX, y);

                    var driverXMax = OperatingSystem.IsWindows() ? 400 : 380;
                    var driverYMax = OperatingSystem.IsWindows() ? 50 : 40;
                    Size driverSize = new Size(driverXMax, driverYMax);

                    // For testing - uncomment to show rectangles
                    /*
                    Rectangle rect3 = new Rectangle(driverX, y, driverXMax, driverYMax);
                    graphics.FillRectangle(
                        new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 0)), rect3);
                    */

                    graphics.DrawString(
                        r.Driver,
                        graphics.GetAdjustedFont(r.Driver, font, driverSize),
                        Brushes.White,
                        driverX,
                            r.Driver.Length <= 25 ? y : y + 6);
                    graphics.DrawString(r.Number, font, Brushes.White, numberX, y);
                    graphics.DrawString(r.Points, font, Brushes.White, pointsX, y);
                    graphics.DrawString(r.Diff, font, Brushes.White, diffX, y);

                    lastRowY = y;

                    y += 50;

                    driverPosition++;
                }

                if (lastRowY + 75 < image.Height)
                {
                    var imageCropRect = new Rectangle(0, 0, image.Width, lastRowY + 75);
                    return image.Clone(imageCropRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                }
                else
                {
                    return image;
                }
                
            }
        }

        public static Bitmap BuildImage(SocketCommandContext context, Leaderboard leaderboard, List<LeaderboardEntry> results)
        {
            var orderedResults = results.OrderBy(r => r.Time);

            int posXStart = OperatingSystem.IsWindows() ? 100 : 110;
            int posYStart = 375;
            int descriptionX = OperatingSystem.IsWindows() ? 245 : 252;
            int descriptionY = OperatingSystem.IsWindows() ? 220 : 225;

            var driverX = posXStart + 60;
            var timeX = driverX + 605;

            int lastRowY = 0;

            Bitmap imageToReturn = null;

            string templateFilePath = @"Assets/TimeTrialStandingsTemplate.png";
            using (Bitmap image = (Bitmap)System.Drawing.Image.FromFile(templateFilePath))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                PrivateFontCollection fontCol = new PrivateFontCollection();
                fontCol.AddFontFile(@"Assets/Fonts/Formula1-Regular.otf");
                var formula1FontFamily = fontCol.Families[0];

                Font font, numberFont, longDriverFont, largerFont;
                if (OperatingSystem.IsWindows())
                {
                    font = new Font(formula1FontFamily, 8);
                    numberFont = new Font(formula1FontFamily, 7);
                    longDriverFont = new Font(formula1FontFamily, 5);
                    largerFont = new Font(formula1FontFamily, 10);
                }
                else
                {
                    font = new Font(formula1FontFamily.Name, 24);
                    numberFont = new Font(formula1FontFamily.Name, 20);
                    longDriverFont = new Font(formula1FontFamily.Name, 18);
                    largerFont = new Font(formula1FontFamily.Name, 28);
                }

                // write championship
                var descriptionXMax = 370;
                var descriptionYMax = OperatingSystem.IsWindows() ? 60 : 50;

                // For testing - uncomment to show rectangles
                /* Rectangle rect1 = new Rectangle(championshipX, championshipY, championshipXMax, championshipYMax);
                graphics.FillRectangle(
                    new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 0)), rect1);
                */

                Size championshipSize = new Size(descriptionXMax, descriptionYMax);
                graphics.DrawString(
                    leaderboard.Description,
                    graphics.GetAdjustedFont(leaderboard.Description, largerFont, championshipSize),
                    new SolidBrush(System.Drawing.Color.FromArgb(213, 213, 213)),
                    descriptionX,
                    descriptionY);

                int y = OperatingSystem.IsWindows() ? posYStart - 1 : posYStart - 4;
                int driverPosition = 0;

                foreach (var item in results.Select((value, i) => new { i, value }))
                {
                    graphics.FillRoundedRectangle(
                        Brushes.White,
                        OperatingSystem.IsWindows() ? posXStart + 7 : posXStart + 1,
                        OperatingSystem.IsWindows() ? y - 3 : y - 5,
                        50,
                        40,
                        4);

                    var posX = item.i <= 9
                        ? posXStart + 15
                        : posXStart + 5;

                    graphics.DrawString((item.i + 1).ToString(), numberFont, Brushes.Black, posX, y);

                    var driverXMax = OperatingSystem.IsWindows() ? 400 : 380;
                    var driverYMax = OperatingSystem.IsWindows() ? 50 : 40;
                    Size driverSize = new Size(driverXMax, driverYMax);

                    // For testing - uncomment to show rectangles
                    /*
                    Rectangle rect3 = new Rectangle(driverX, y, driverXMax, driverYMax);
                    graphics.FillRectangle(
                        new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 0)), rect3);
                    */

                    var userId = Convert.ToUInt64(item.value.SubmittedById);
                    var user = context.Guild.GetUser(userId).GetDisplayName();
                    var userDisplay = Regex.Replace(user, @"\p{Cs}", String.Empty);

                    graphics.DrawString(
                        userDisplay,
                        graphics.GetAdjustedFont(userDisplay, font, driverSize),
                        Brushes.White,
                        driverX,
                            user.Length <= 25 ? y : y + 6);

                    var time = item.value.Time.ToString(@"mm\:ss\.fff");
                    graphics.DrawString(time, font, Brushes.White, timeX, y);

                    lastRowY = y;

                    y += 50;

                    driverPosition++;
                }

                if (lastRowY + 75 < image.Height)
                {
                    var imageCropRect = new Rectangle(0, 0, image.Width, lastRowY + 75);
                    imageToReturn = image.Clone(imageCropRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                }
                else
                {
                    imageToReturn = image;
                }

            }

            return imageToReturn;
        }
    }
}
