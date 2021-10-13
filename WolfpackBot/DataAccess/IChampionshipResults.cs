using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Models;

namespace WolfpackBot.DataAccess
{
    public interface IChampionshipResults
    {
        Task AddAsync(List<ChampionshipResultsModel> championshipResults);
        Task<List<ChampionshipResultsModel>> GetChampionshipResultsByIdAsync(int championshipId);
        Task DeleteAllGuildEvents<ChampionshipResultsModel>(string guildId);
        Task<string[]> GetEventsWithResultsAsync();
    }
}
