using System;
using System.Threading.Tasks;
using WolfpackBot.Models;

namespace WolfpackBot.DataAccess
{
    public interface ILeaderboardEntries
    {
        Task AddAsync(int leaderboardId, TimeSpan time, ulong userId, string proofUrl);
        Task<LeaderboardEntry> GetBestEntryForUser(int leaderboardId, ulong userId);
        Task Update(LeaderboardEntry entry);
    }
}