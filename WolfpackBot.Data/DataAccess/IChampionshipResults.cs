using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Data.DataAccess
{
    public interface IChampionshipResults
    {
        Task AddAsync(List<ChampionshipResultsModel> championshipResults);
        Task<List<ChampionshipResultsModel>> GetChampionshipResultsByIdAsync(int championshipId);
        Task DeleteAllGuildEvents(string guildId);
        Task<string[]> GetEventsWithResultsAsync();
    }
}
