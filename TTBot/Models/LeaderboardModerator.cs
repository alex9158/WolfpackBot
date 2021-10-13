
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace WolfpaackBot.Models
{
    [Alias("LeaderboardModerators")]
    public class LeaderboardModerator
    {
        public string RoleId { get; set; }
        public string GuildId { get; set; }
    }
}
