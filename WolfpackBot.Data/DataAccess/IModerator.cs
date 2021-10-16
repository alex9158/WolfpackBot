using System.Collections.Generic;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Data.DataAccess
{
    public interface IModerator
    {
        Task AddRoleAsModerator(ulong guildId, ulong roleId);
        Task<LeaderboardModerator> GetLeaderboardModeratorAsync(ulong guildId, ulong userId);
        Task RemoveRoleAsModeratorAsync(ulong guildId, ulong roleId);
        Task<List<LeaderboardModerator>> GetLeaderboardModeratorsAsync(ulong guildId);
    }
}