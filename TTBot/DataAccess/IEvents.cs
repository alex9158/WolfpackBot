using System.Collections.Generic;
using System.Threading.Tasks;
using WolfpackBot.Models;

namespace WolfpackBot.DataAccess
{
    public interface IEvents
    {
        Task<List<EventsWithCount>> GetActiveEvents(ulong guildId);
        Task<EventsWithCount> GetActiveEvent(string name, ulong guildId);
        Task SaveAsync(Event @event);
        Task<EventsWithCount> GetActiveEvent(int eventId);
        Task<EventsWithCount> GetEventByMessageIdAsync(ulong messageId);
    }
}