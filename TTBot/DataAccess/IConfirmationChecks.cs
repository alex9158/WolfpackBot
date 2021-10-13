using System.Threading.Tasks;
using WolfpackBot.Models;

namespace WolfpackBot.DataAccess
{
    public interface IConfirmationChecks
    {
        Task<ConfirmationCheck> GetConfirmationCheckByMessageId(ulong messageId);
        Task SaveAsync(ConfirmationCheck confirmationCheck);
        Task<ConfirmationCheck> GetMostRecentConfirmationCheckForEventAsync(int eventId);
    }
}