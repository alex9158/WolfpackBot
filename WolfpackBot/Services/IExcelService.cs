using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Services
{
    public interface IExcelService
    {
        Task<List<ExcelDriverDataModel>> ReadResultsDataFromAttachment(Attachment attachment);
        Task<List<ExcelChampionshipRoundModel>> DeriveRoundsFromAttachment(Attachment attachment);
    }
}
