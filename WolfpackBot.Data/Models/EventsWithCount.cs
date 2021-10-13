﻿using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace WolfpackBot.Models
{
    [Alias("EventsWithCount")]
    public class EventsWithCount : Event
    {
        public int ParticipantCount { get; set; }

        [Ignore]
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
