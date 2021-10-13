using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Data
{
    public class WolfpackDbContext : DbContext
    {

        public DbSet<Event> Events { get; set; }
        public WolfpackDbContext(DbContextOptions<WolfpackDbContext> options) : base(options)
        {

        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Event>(opt =>
                {
                    opt.HasMany(e => e.EventSignups)
                       .WithOne(e => e.@event);
                    opt.Navigation(opt => opt.EventSignups).AutoInclude();
                });
        }
    }
}
