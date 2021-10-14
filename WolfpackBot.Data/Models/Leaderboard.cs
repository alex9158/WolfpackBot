using System;
using System.Collections.Generic;
using System.Text;

namespace WolfpackBot.Data.Models
{
    public class Leaderboard
    {
        public int Id { get; set; }
        public string GuildId { get; set; }
        public string ChannelId { get; set; }
        public string Game { get; set; }
        public string Description { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public bool Active { get; set; }
        public virtual ICollection<LeaderboardEntry> LeaderboardEntries { get; set; }

    }
}
