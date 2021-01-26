﻿using System.Collections.Generic;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface IEvents
    {
        Task<List<EventsWithCount>> GetActiveEvents(ulong guildId, ulong channelId);
        Task<EventsWithCount> GetActiveEvent(string name, ulong guildId);
        Task SaveAsync(Event @event);
        Task<EventsWithCount> GetActiveEvent(int eventId);
        Task<EventsWithCount> GetEventByMessageIdAsync(ulong messageId);
        Task<EventsWithCount> GetEventByShortname(ulong guildId, string shortname);
    }
}