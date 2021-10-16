using System;
using System.Collections.Generic;
using System.Text;

namespace WolfpackBot.Data.Models
{
    public class ExcelSheetEventMappingModel
    {       
        public int Id { get; set; }       
        public ulong EventId { get; set; }
        public string Sheetname { get; set; }
        public bool IsRoundsSheet { get; set; }
        public virtual Event Event { get; set; }
    }
}
