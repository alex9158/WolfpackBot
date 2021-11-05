
using System;
using System.Collections.Generic;
using System.Text;

namespace WolfpackBot.Data.Models
{

    public class ChampionshipResultsModel
    {

        public int Id { get; set; }


        public int EventId { get; set; }
        public int Pos { get; set; }
        public string Driver { get; set; }
        public string Number { get; set; }
        public string Car { get; set; }
        public string Points { get; set; }
        public string Diff { get; set; }
        public int ExcelSheetEventMappingId { get; set; }

        public virtual Event Event { get; set; }

    }
}
