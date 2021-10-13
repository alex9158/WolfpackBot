﻿using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WolfpaackBot.Models;

namespace WolfpaackBot.Services
{
    public interface IExcelService
    {
        Task<List<ExcelDriverDataModel>> ReadResultsDataFromAttachment(Attachment attachment);
        Task<List<ExcelChampionshipRoundModel>> DeriveRoundsFromAttachment(Attachment attachment);
    }
}
