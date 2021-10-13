using System;
using System.Drawing;
using System.Threading.Tasks;

namespace WolfpaackBot.Services
{
    public interface ITwitterIntegrationService
    {
        Task PostImage(Bitmap image, string message);
    }
}
