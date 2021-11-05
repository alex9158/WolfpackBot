
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Data.DataAccess
{
    public class ChampionshipResults : IChampionshipResults
    {
        private readonly WolfpackDbContext _db;

        public ChampionshipResults(WolfpackDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(List<ChampionshipResultsModel> championshipResults)
        {
            _db.AddRange(championshipResults.Select(resultsModel => new ChampionshipResultsModel()
            {
                EventId = resultsModel.EventId,
                Pos = resultsModel.Pos,
                Driver = resultsModel.Driver,
                Number = resultsModel.Number,
                Car = resultsModel.Car,
                Points = resultsModel.Points,
                Diff = resultsModel.Diff,
                ExcelSheetEventMappingId = resultsModel.ExcelSheetEventMappingId
            }));
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAllGuildEvents(string guildId)
        {
            var events = _db.Events.Where(e => e.GuildId == guildId).Include(e => e.ChampionshipResults);
            foreach (var @event in events)
            {
                @event.ChampionshipResults.Clear();
            }
            await _db.SaveChangesAsync();
        }

        public async Task<List<ChampionshipResultsModel>> GetChampionshipResultsByIdAsync(int eventId)
        {

            return await _db.ChampionshipResults.Where(res => res.EventId == eventId).ToListAsync();
        }

        public async Task<string[]> GetEventsWithResultsAsync()
        {
            return (await _db.Events.Where(e => e.ChampionshipResults.Any()).Select(e => e.ShortName).Distinct().ToListAsync()).ToArray();
        }
    }
}
