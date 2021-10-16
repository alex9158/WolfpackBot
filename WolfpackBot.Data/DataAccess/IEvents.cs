using System.Collections.Generic;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Data.DataAccess
{
    public interface IEvents
    {
        Task<Event> GetActiveEvent(string name, ulong guildId);
        Task<Event> GetActiveEvent(int eventId);
        Task<List<Event>> GetActiveEvents(ulong guildId);
        Task<Event> GetEventByMessageIdAsync(ulong messageId);
    }
}