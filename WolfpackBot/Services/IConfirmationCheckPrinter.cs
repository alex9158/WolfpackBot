using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System.Threading.Tasks;
using WolfpackBot.Data.Models;
using WolfpackBot.Models;

namespace WolfpackBot.Services
{
    public interface IConfirmationCheckPrinter
    {
        Task WriteMessage(ISocketMessageChannel channel, IUserMessage message, Event @event);
    }
}