﻿using Discord;
using Discord.WebSocket;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WolfpaackBot.Models;

namespace WolfpaackBot.DataAccess
{
    public class EventSignups : IEventSignups
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public EventSignups(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task SaveAsync(EventSignup eventSignUp)
        {
            using (var con = _dbConnectionFactory.Open())
            {
                await con.SaveAsync(eventSignUp);
            }
        }

        public async Task AddUserToEvent(Event @event, IUser user)
        {
            var signUp = new EventSignup()
            {
                EventId = @event.Id,
                UserId = user.Id.ToString()
            };
            await SaveAsync(signUp);
        }

        public async Task DeleteAsync(EventSignup signup)
        {
            using (var con = _dbConnectionFactory.Open())
            {
                await con.DeleteAsync<EventSignup>(signup);
            }
        }

        public async Task<EventSignup> GetSignupAsync(Event @event, IUser user)
        {
            using (var con = _dbConnectionFactory.Open())
            {
                return await con.SingleAsync<EventSignup>(esup => esup.EventId == @event.Id && esup.UserId == user.Id.ToString());
            }
        }

        public async Task<List<EventSignup>> GetAllSignupsForEvent(Event @event)
        {
            using (var con = _dbConnectionFactory.Open())
            {
                return await con.SelectAsync<EventSignup>(sup => sup.EventId == @event.Id);
            }
        }
    }
}
