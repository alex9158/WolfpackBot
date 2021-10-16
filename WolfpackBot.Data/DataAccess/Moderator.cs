using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Data;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Data.DataAccess
{
    public class Moderator : IModerator
    {
        private readonly WolfpackDbContext _db;

        public Moderator(WolfpackDbContext db)
        {
            _db = db;
        }

        public async Task AddRoleAsModerator(ulong guildId, ulong roleId)
        {
            if (await GetLeaderboardModeratorAsync(guildId, roleId) == null)
            {
                _db.LeaderboardModerators.Add(new LeaderboardModerator
                {
                    GuildId = guildId.ToString(),
                    RoleId = roleId.ToString()
                });
                await _db.SaveChangesAsync();
            }
        }

        public async Task RemoveRoleAsModeratorAsync(ulong guildId, ulong roleId)
        {
            var moderator = _db.LeaderboardModerators.FirstOrDefault(mod => mod.GuildId == guildId.ToString() && mod.RoleId == roleId.ToString());
            if (moderator != null)
            {
                _db.LeaderboardModerators.Remove(moderator);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<LeaderboardModerator> GetLeaderboardModeratorAsync(ulong guildId, ulong roleId)
        {
            return await _db.LeaderboardModerators.FirstOrDefaultAsync(mod => mod.GuildId == guildId.ToString() && mod.RoleId == roleId.ToString());
        }

        public async Task<List<LeaderboardModerator>> GetLeaderboardModeratorsAsync(ulong guildId)
        {
            return await _db.LeaderboardModerators.Where(mod => mod.GuildId == guildId.ToString()).ToListAsync();
        }
    }
}
