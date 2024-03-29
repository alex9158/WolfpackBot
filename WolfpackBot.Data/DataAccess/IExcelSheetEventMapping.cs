﻿using System.Collections.Generic;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;

namespace WolfpackBot.DataAccess
{
    public interface IExcelSheetEventMapping
    {
        Task AddAsync(int eventId, string sheet, bool isRoundsSheet = false);
        Task RemoveAsync(int id);
        Task<string> GetEventShortnameFromSheetNameAsync(string sheet, bool isRoundsSheet = false);
        Task<Event> GetActiveEventFromWorksheetAsync(string sheet);
        Task<bool> ActiveEventExistsAsync(string worksheet);
        Task<int> GetWorksheetMappingIdAsync(string worksheet);
        Task<string> GetWorksheetNameFromIdAsync(int id);
        Task<List<ExcelSheetEventMappingModel>> GetAllActiveWorksheetMappings();
    }
}
