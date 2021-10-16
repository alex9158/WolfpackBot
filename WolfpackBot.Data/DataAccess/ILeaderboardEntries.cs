using System;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Data.DataAccess
{
    public interface ILeaderboardEntries
    {
        Task<LeaderboardEntry> GetBestEntryForUser(int leaderboardId, ulong userId);    
    }
}