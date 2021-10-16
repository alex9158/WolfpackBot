
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;
using Microsoft.EntityFrameworkCore;
namespace WolfpackBot.Data.DataAccess
{
    public class LeaderboardEntries : ILeaderboardEntries
    {
        private readonly WolfpackDbContext _db;

        public LeaderboardEntries(WolfpackDbContext db)
        {
            _db = db;
        }


        public async Task<LeaderboardEntry> GetBestEntryForUser(int leaderboardId, ulong userId)
        {
            return await _db.LeaderboardEntries.FromSqlRaw(@" SELECT Id,LeaderboardId,SubmittedById,SubmittedDate,ProofUrl,Time,Invalidated FROM (
	                                                                    SELECT ROW_NUMBER() OVER (PARTITION BY  SubmittedById ORDER BY [Time] ASC) as RowNumber, * 
		                                                                FROM LeaderboardEntries
		                                                                WHERE LeaderboardId = {0}
                                                                        AND Invalidated = 0
	                                                                    ) X
                                                                    where RowNumber = 1
                                                                    AND SubmittedById = {1}", leaderboardId, userId).FirstOrDefaultAsync();
        }       
    }
}
