using System;
using System.Collections.Generic;
using System.Text;

namespace WolfpackBot.Models
{
    public class ExcelChampionshipRoundModel
    {
        public string Championship { get; set; }
        public int Round { get; set; }
        public string LastRoundDate { get; set; }
        public string LastRoundTrack { get; set; }
        public DateTime NextRoundDate { get; set; }
        public string NextRoundTrack { get; set; }
    }
}
