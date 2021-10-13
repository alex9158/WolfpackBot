
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace WolfpackBot.Models
{

    public class EventsWithCount : Event
    {
        public int ParticipantCount { get; set; }

        [NotMapped]
        public bool Full
        {
            get
            {
                if (!SpaceLimited)
                {
                    return false;
                }

                return ParticipantCount >= Capacity;
            }
        }
    }
}
