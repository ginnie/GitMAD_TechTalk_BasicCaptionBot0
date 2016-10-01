using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

using System.IO;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

namespace Bot_Application3
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                // Get Response from CogSvc
                string message = await GetBotResponse(activity);
                Activity reply = activity.CreateReply(message);

                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        // Methods for creating the bot response
        private async Task<string> GetBotResponse(Activity activity)
        {
            string message;

            try
            {
                message = await this.GetCaptionAsync(activity);
            }
            catch (ArgumentException e)
            {
                message = "Did you upload an image? I'm more of a visual person. " +
                    "Try sending me an image or an image URL";
            }
            catch (Exception e)
            {
                message = "Oops! Something went wrong. Try again later.";
            }

            return message;
        }

        private async Task<string> GetCaptionAsync(Activity activity)
        {
            Attachment imageAttachment = activity.Attachments?.FirstOrDefault(a => a.ContentType.Contains("image"));
            if (imageAttachment != null)
            {
                WebRequest req = WebRequest.Create(imageAttachment.ContentUrl);
                using (Stream stream = req.GetResponse().GetResponseStream())
                {
                    return await this.GetCaptionAsync(stream);
                }
            }
            else if (Uri.IsWellFormedUriString(activity.Text, UriKind.Absolute))
            {
                return await this.GetCaptionAsync(activity.Text);
            }

            // Activity is neither an image attachment nor an image URL.
            throw new ArgumentException("The activity doesn't contain a valid image attachment or an image URL.");
        }

        // Cognitive Service interaction:-
        private static readonly string ApiKey = "";
        private static readonly VisualFeature[] VisualFeatures = { VisualFeature.Description };

        private async Task<string> GetCaptionAsync(string url)
        {
            VisionServiceClient client = new VisionServiceClient(ApiKey);
            AnalysisResult result = await client.AnalyzeImageAsync(url, VisualFeatures);
            return ProcessAnalysisResult(result);
        }

        private async Task<string> GetCaptionAsync(Stream stream)
        {
            VisionServiceClient client = new VisionServiceClient(ApiKey);
            AnalysisResult result = await client.AnalyzeImageAsync(stream, VisualFeatures);
            return ProcessAnalysisResult(result);
        }

        private static string ProcessAnalysisResult(AnalysisResult result)
        {
            string message = result?.Description?.Captions.FirstOrDefault()?.Text;
            return string.IsNullOrEmpty(message) ?
                        "Couldn't find a caption for this one" :
                        "I think it's " + message;
        }

    private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}