using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Data.DataAccess
{
    public class Events: IEvents
    {
        private readonly WolfpackDbContext _db;

        public Events(WolfpackDbContext db)
        {
            _db = db;
        }

        public async Task<Event> GetActiveEvent(string name, ulong guildId)
        {
            return await _db.Events.SingleOrDefaultAsync(ev => (ev.Name.ToLower() == name.ToLower() || (ev.ShortName != null && ev.ShortName.ToLower() == name.ToLower())) && ev.GuildId == guildId.ToString() && !ev.Closed);
        }

        public async Task<Event> GetActiveEvent(int eventId)
        {
            return await _db.Events.SingleOrDefaultAsync(ev => ev.Id == eventId && !ev.Closed);
        }

        public async Task<List<Event>> GetActiveEvents(ulong guildId)
        {
            return await _db.Events.Where(ev => !ev.Closed && ev.GuildId == guildId.ToString()).ToListAsync();
        }

        public async Task<Event> GetEventByMessageIdAsync(ulong messageId)
        {
            return await _db.Events.SingleOrDefaultAsync(e => e.MessageId == messageId.ToString());
        }   
    }
}
