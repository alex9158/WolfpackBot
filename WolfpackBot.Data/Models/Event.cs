using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace WolfpackBot.Data.Models
{
    public class Event
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string GuildId { get; set; }
        public string ChannelId { get; set; }
        public string RoleId { get; set; }
        public bool Closed { get; set; }
        public int? Capacity { get; set; }
        [NotMapped]
        public bool SpaceLimited => Capacity.HasValue;
        [NotMapped]
        public string DisplayName => ShortName ?? Name;
        public string MessageId { get; set; }
        public int? Round { get; set; }
        public string LastRoundDate { get; set; }
        public string LastRoundTrack { get; set; }
        public string[] StandingsMessageIds { get; set; }
        public DateTime? NextRoundDate { get; set; }
        public string NextRoundTrack { get; set; }
        public ulong? NextTrackMessageId { get; set; }
        public string TwitterMessage { get; set; }

        [NotMapped]
        public bool Full
        {
            get
            {
                if (!SpaceLimited)
                {
                    return false;
                }

                if (EventSignups == null)
                {
                    return true;
                }

                return EventSignups.Count >= Capacity;
            }
        }

        public virtual ICollection<EventSignup> EventSignups { get; set; }

        public virtual ICollection<ChampionshipResultsModel> ChampionshipResults { get; set; }
        public virtual ICollection<EventAliasMappingModel> EventAliasMappings { get; set; }
        public virtual ICollection<ExcelSheetEventMappingModel> ExcelSheetEventMappings { get; set; }
    }
}
