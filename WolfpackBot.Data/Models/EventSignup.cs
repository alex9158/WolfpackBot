
using System;
using System.Collections.Generic;
using System.Text;

namespace WolfpackBot.Models
{
    public class EventSignup
    {
        public int Id { get; set; }    
        public int EventId { get; set; }
        public string UserId { get; set; }
        public virtual Event @event { get; set; }
    }
}
