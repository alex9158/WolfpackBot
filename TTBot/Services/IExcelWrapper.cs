using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;
using WolfpackBot.Models;

namespace WolfpackBot.Services
{
    public interface IExcelWrapper
    {
        public Task<List<ExcelDriverDataModel>> GetExcelDriverData(Attachment attachment);
        public Task<List<ExcelChampionshipRoundModel>> GetChampionshipRoundsData(Attachment attachment); 
    }
}