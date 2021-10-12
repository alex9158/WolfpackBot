using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace TTBot.Services
{
    class TwitterIntegrationService : ITwitterIntegrationService
    {
        static readonly HttpClient _client = new HttpClient();
        private readonly IConfiguration _config;

        public TwitterIntegrationService(IConfiguration config)
        {
            _config = config;
        }

        public async Task PostImage(Bitmap image, string message)
        {
            var consumerKey = _config.GetValue<string>("CONSUMER_KEY");
            var consumerSecret = _config.GetValue<string>("CONSUMER_SECRET");
            var accessToken = _config.GetValue<string>("ACCESS_KEY");
            var accessSecret = _config.GetValue<string>("ACCESS_SECRET");
            var userCredentials = new TwitterCredentials(consumerKey, consumerSecret, accessToken, accessSecret);
            var userClient = new TwitterClient(userCredentials);

            ImageConverter converter = new ImageConverter();
            var imageBytes = (byte[])converter.ConvertTo(image, typeof(byte[]));
            var media = await userClient.Upload.UploadBinaryAsync(imageBytes);

            if (media == null || media.Id == null || !media.HasBeenUploaded)
            {
                throw new OperationCanceledException("The tweet cannot be published as some of the medias could not be published!");
            }

            await userClient.Tweets.PublishTweetAsync(new PublishTweetParameters()
            {
                Medias = { media },
                Text = message ?? null
            });
        }

    }
}
