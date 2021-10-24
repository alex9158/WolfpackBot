using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace WolfpackBot.Data
{
    public class WolfpackContextFactory : IDesignTimeDbContextFactory<WolfpackDbContext>
    {
        public WolfpackDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", true)
                  .AddEnvironmentVariables("TTBot_")
                  .AddCommandLine(args).Build();

            var connectionString = config.GetValue<string>("CONNECTION_STRING");
            var optionsBuilder = new DbContextOptionsBuilder<WolfpackDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new WolfpackDbContext(optionsBuilder.Options);
        }
    }
}
