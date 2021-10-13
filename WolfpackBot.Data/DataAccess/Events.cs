using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Models;

namespace WolfpackBot.Data.DataAccess
{
    public class Events: IEvents
    {
        private readonly WolfpackDbContext _db;

        public Events(WolfpackDbContext db)
        {
            _db = db;
        }

        public async Task<EventsWithCount> GetActiveEvent(string name, ulong guildId)
        {
            return await _db.EventsWithCount.SingleAsync(ev => (ev.Name.ToLower() == name.ToLower() || (ev.ShortName != null && ev.ShortName.ToLower() == name.ToLower())) && ev.GuildId == guildId.ToString() && !ev.Closed);
        }

        public async Task<EventsWithCount> GetActiveEvent(int eventId)
        {
            return await _db.EventsWithCount.SingleAsync(ev => ev.Id == eventId && !ev.Closed);
        }

        public async Task<List<EventsWithCount>> GetActiveEvents(ulong guildId)
        {
            return await _db.EventsWithCount.Where(ev => !ev.Closed && ev.GuildId == guildId.ToString()).ToListAsync();
        }

        public async Task<EventsWithCount> GetEventByMessageIdAsync(ulong messageId)
        {
            return await _db.EventsWithCount.SingleAsync<EventsWithCount>(e => e.MessageId == messageId.ToString());
        }   
    }
}
