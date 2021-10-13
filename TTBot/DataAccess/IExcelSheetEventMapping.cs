﻿using System.Collections.Generic;
using System.Threading.Tasks;
using WolfpaackBot.Models;

namespace WolfpaackBot.DataAccess
{
    public interface IExcelSheetEventMapping
    {
        Task AddAsync(ulong eventId, string sheet, bool isRoundsSheet = false);
        Task RemoveAsync(int id);
        Task<string> GetEventShortnameFromSheetNameAsync(string sheet, bool isRoundsSheet = false);
        Task<Event> GetActiveEventFromWorksheetAsync(string sheet);
        Task<bool> ActiveEventExistsAsync(string worksheet);
        Task<int> GetWorksheetMappingIdAsync(string worksheet);
        Task<List<ExcelSheetEventMappingModel>> GetAllActiveWorksheetMappings();
    }
}
