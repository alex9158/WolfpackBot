using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Services
{
    public interface IEventParticipantService
    {
        Task<IMessage> CreateAndPinParticipantMessage(ISocketMessageChannel channel, Event @event);
        Task<Embed> GetParticipantsEmbed(ISocketMessageChannel channel, Event @event, bool showJoinPrompt = true);
        Task<string> GetParticipantsMessageBody(ISocketMessageChannel channel, Event @event,  bool showJoinPrompt = true);
        Task UnpinEventMessage(ISocketMessageChannel channel, Event @event);
        Task UpdatePinnedMessageForEvent(ISocketMessageChannel channel, Event @event);
        Task UpdatePinnedMessageForEvent(ISocketMessageChannel channel, Event @event, IUserMessage message);
    }
}