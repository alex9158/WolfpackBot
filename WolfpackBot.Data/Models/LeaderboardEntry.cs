
using System;
using System.Collections.Generic;
using System.Text;

namespace WolfpackBot.Data.Models
{

    public class LeaderboardEntry
    {    
        public int Id { get; set; }
        public int LeaderboardId { get; set; }
        public string SubmittedById { get; set; }
        public string ProofUrl { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime SubmittedDate { get; set; }
        public bool Invalidated { get; set; }
        public virtual Leaderboard Leaderboard { get; set; }
    }
}
