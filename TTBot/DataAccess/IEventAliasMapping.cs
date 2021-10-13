using System.Collections.Generic;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;
using WolfpackBot.Models;

namespace WolfpackBot.DataAccess
{
    public interface IEventAliasMapping
    {
        Task AddAsync(ulong eventId, string alias);
        Task RemoveAsync(int id);
        Task<Event> GetActiveEventFromAliasAsync(string alias, ulong guildId);
        Task<int> GetAliasIdAsync(string alias, ulong guildId);
        Task<bool> ActiveEventExistsAsync(string alias, ulong guildId);
        Task<List<EventAliasMappingModel>> GetAllActiveAliases();
    }
}
