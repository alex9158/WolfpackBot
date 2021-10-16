using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;
using WolfpackBot.DataAccess;

namespace WolfpackBot.Data.DataAccess
{
    public class ExcelSheetEventMapping : IExcelSheetEventMapping
    {
        private readonly WolfpackDbContext _db;

        public ExcelSheetEventMapping(WolfpackDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(ulong eventId, string sheet, bool isRoundsSheet = false)
        {
            _db.Add(new ExcelSheetEventMappingModel()
            {
                EventId = eventId,
                Sheetname = sheet,
                IsRoundsSheet = isRoundsSheet
            });
            await _db.SaveChangesAsync();
        }

        public async Task RemoveAsync(int id)
        {
            _db.ExcelSheetEventMapping.Remove(new ExcelSheetEventMappingModel
            {
                Id = id
            });

            await _db.SaveChangesAsync();
        }

        public async Task<string> GetEventShortnameFromSheetNameAsync(string sheet, bool isRoundsSheet = false)
        {
            var @event = await _db
                .Events
                .FirstOrDefaultAsync(e => !e.Closed && e.ExcelSheetEventMappings.Any(em => em.Sheetname == sheet && em.IsRoundsSheet == isRoundsSheet));

            return @event?.ShortName;
        }

        public async Task<bool> ActiveEventExistsAsync(string worksheet)
        {
            return await _db.ExcelSheetEventMapping.Include(em => em.Event).AnyAsync(em => em.Sheetname == worksheet && !em.Event.Closed);
        }

        public async Task<Event> GetActiveEventFromWorksheetAsync(string worksheet)
        {
            return await _db.Events.FirstOrDefaultAsync(e => !e.Closed && e.ExcelSheetEventMappings.Any(em => em.Sheetname == worksheet));
        }

        public async Task<Event> GetEventFromSheetNameAsync(string sheet, bool isRoundsSheet = false)
        {
            return await _db
                .Events
                .Include(e => e.EventAliasMappings)
                .FirstOrDefaultAsync(e => !e.Closed && e.ExcelSheetEventMappings.Any(em => em.Sheetname == sheet && em.IsRoundsSheet == isRoundsSheet));
        }

        public async Task<int> GetWorksheetMappingIdAsync(string worksheet)
        {
            return (await _db.ExcelSheetEventMapping.FirstAsync(em => em.Sheetname == worksheet && !em.Event.Closed)).Id;
        }


        public async Task<List<ExcelSheetEventMappingModel>> GetAllActiveWorksheetMappings()
        {
            return await _db.ExcelSheetEventMapping.OrderBy(em => em.EventId).ToListAsync();
        }
    }
}