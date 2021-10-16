
using System;
using System.Collections.Generic;
using System.Text;

namespace WolfpackBot.Data.Models
{
   
    public class EventAliasMappingModel
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Alias { get; set; }
        public virtual Event Event { get; set; }
    }
}
