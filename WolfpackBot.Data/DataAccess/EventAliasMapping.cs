
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Data.DataAccess
{
    public class EventAliasMapping : IEventAliasMapping
    {
        private readonly WolfpackDbContext _db;

        public EventAliasMapping(WolfpackDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(int eventId, string alias)
        {
            _db.EventAliasMapping.Add(new EventAliasMappingModel()
            {
                EventId = eventId,
                Alias = alias
            });
            await _db.SaveChangesAsync();
        }

        public async Task RemoveAsync(int id)
        {
            _db.EventAliasMapping.Remove(new EventAliasMappingModel { Id = id });
            await _db.SaveChangesAsync();
        }

        public async Task<Event> GetActiveEventFromAliasAsync(string alias, ulong guildId)
        {

            var events = await _db.Events.Where(e => !e.Closed && e.GuildId == guildId.ToString()).Include(e => e.EventAliasMappings).ToListAsync();
            var trimmedAlias = GetLowerTrimmedText(alias);
            return events.FirstOrDefault(e => e.EventAliasMappings.Any(mapping => GetLowerTrimmedText(mapping.Alias) == trimmedAlias));
        }

        public async Task<int> GetAliasIdAsync(string alias, ulong guildId)
        {
            return (await GetActiveEventFromAliasAsync(alias, guildId)).Id;
        }

        private string GetLowerTrimmedText(string s)
        {
            return s?.Replace(" ", "").ToLower();
        }


        public async Task<bool> ActiveEventExistsAsync(string alias, ulong guildId)
        {
            return (await GetActiveEventFromAliasAsync(alias, guildId)) != null;
        }

        public async Task<List<EventAliasMappingModel>> GetAllActiveAliases()
        {
            return await _db.EventAliasMapping.OrderBy(mapping => mapping.EventId).ToListAsync();
        }
    }
}
