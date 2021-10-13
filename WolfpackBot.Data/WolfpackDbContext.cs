using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace WolfpackBot.Data
{
    public class WolfpackDbContext: DbContext
    {

        public WolfpackDbContext(DbContextOptions<WolfpackDbContext> options):base(options)
        {

        }
    }
}
