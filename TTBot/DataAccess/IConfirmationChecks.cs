using System.Threading.Tasks;
using WolfpaackBot.Models;

namespace WolfpaackBot.DataAccess
{
    public interface IConfirmationChecks
    {
        Task<ConfirmationCheck> GetConfirmationCheckByMessageId(ulong messageId);
        Task SaveAsync(ConfirmationCheck confirmationCheck);
        Task<ConfirmationCheck> GetMostRecentConfirmationCheckForEventAsync(int eventId);
    }
}