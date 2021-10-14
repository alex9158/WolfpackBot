
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WolfpackBot.Data.DataAccess
{
    public class Leaderboards : ILeaderboards
    {

        private readonly WolfpackDbContext _db;

        public Leaderboards(WolfpackDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Leaderboard>> GetAllAsync(ulong guildId)
        {
            return await _db.Leaderboards.Where(lb => lb.GuildId == guildId.ToString()).ToListAsync();
        }

        public async Task<IEnumerable<Leaderboard>> GetAllActiveAsync(ulong guildId)
        {
            return await _db.Leaderboards.Where(lb => lb.GuildId == guildId.ToString() && lb.Active).ToListAsync();
        }

        public async Task AddAsync(ulong guildId, ulong channelId, string game, DateTime? endDate = null, bool active = true)
        {
            _db.Leaderboards.Add(new Leaderboard()
            {
                GuildId = guildId.ToString(),
                Active = active,
                ChannelId = channelId.ToString(),
                Game = game,
                StartDateTime = DateTime.Now,
                EndDateTime = endDate
            });
            await _db.SaveChangesAsync();
        }

        public async Task<Leaderboard> GetActiveLeaderboardForChannelAsync(ulong guildId, ulong channelId)
        {
            return await _db.Leaderboards.FirstOrDefaultAsync(l => l.GuildId == guildId.ToString() && l.ChannelId == channelId.ToString() && l.Active);
        }

        public async Task<IEnumerable<LeaderboardEntry>> GetStandingsAsync(int leaderboardId)
        {
            return await _db.LeaderboardEntries.FromSqlRaw(@"   SELECT Id,LeaderboardId,SubmittedById,SubmittedDate,ProofUrl,Time,Invalidated FROM (
	                                                                    SELECT ROW_NUMBER() OVER (PARTITION BY  SubmittedById ORDER BY [Time] ASC) as RowNumber, * 
		                                                                FROM LeaderboardEntries
		                                                                WHERE LeaderboardId = {0}
                                                                        AND Invalidated = 0
	                                                                    ) X
                                                                    where RowNumber = 1
                                                                    ORDER BY [Time] ASC", leaderboardId).ToListAsync();
        }
    }
}
