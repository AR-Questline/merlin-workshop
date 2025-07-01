using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.Utility.Slack {
    public class SlackMessenger {
        const string BaseURL = "https://slack.com/api/";
        const string FilesUploadURL = BaseURL + "files.upload";
        const string PostMessageURL = BaseURL + "chat.postMessage";
        
        static string s_slackToken;
        static readonly HttpClient Client = new();
        static string SlackTokenPath => @$"{Application.persistentDataPath}\slackToken.txt";

        readonly string _channel;
        SlackThread _slackThread;

        public SlackMessenger(string channel) {
            _channel = channel;
            TryToReadSlackToken();
        }

        void TryUsingExistingThread(MultipartFormDataContent multiForm, bool useExistingThread = true) {
            if (useExistingThread && _slackThread != null) {
                multiForm.Add(new StringContent(_slackThread.ThreadID), "thread_ts");
            }
        }

        static void TryToReadSlackToken() {
            if (!File.Exists(SlackTokenPath)) {
                Log.Important?.Error("Slack token file not found!");
            }

            s_slackToken = File.ReadAllText(SlackTokenPath);
            if (string.IsNullOrEmpty(s_slackToken)) {
                Log.Important?.Error("Token is empty!");
            }
        }

        public async Task StartThread(string initialThreadMessage) {
            SlackPostMessageResponse response = await PostMessage(initialThreadMessage);
            if (response.Ok) {
                _slackThread = new SlackThread(response.Ts);
                Log.Important?.Info($"Started thread: {_slackThread.ThreadID}");
            } else {
                Log.Important?.Error($"Failed to start thread: {response.Error}");
            }
        }
        
        public void EndThread() {
            _slackThread = null;
        }

        public async Task<SlackUploadFileResponse> UploadFile(string path, bool useExistingThread = true) {
            // we need to send a request with multipart/form-data
            var multiForm = new MultipartFormDataContent();
            
            // add API method parameters
            multiForm.Add(new StringContent(s_slackToken), "token");
            multiForm.Add(new StringContent(_channel), "channels");
            
            // add file and directly upload it
            await using FileStream fs = File.OpenRead(path);
            multiForm.Add(new StreamContent(fs), "file", Path.GetFileName(path));
            
            TryUsingExistingThread(multiForm, useExistingThread);

            // send request to API
            var response = await Client.PostAsync(FilesUploadURL, multiForm);

            // fetch response from API
            var responseJson = await response.Content.ReadAsStringAsync();

            // convert JSON response to object
            SlackUploadFileResponse uploadFileResponse = JsonConvert.DeserializeObject<SlackUploadFileResponse>(responseJson);

            if (uploadFileResponse.Ok) {
                Log.Important?.Info($"Uploaded new file {uploadFileResponse.File.Name} with id: {uploadFileResponse.File.ID}");
            } else {
                Log.Important?.Error($"Failed to upload message: {uploadFileResponse.Error}");
            }

            return uploadFileResponse;
        }

        public async Task<SlackPostMessageResponse> PostMessage(string message, bool useExistingThread = true) {
            var multiForm = new MultipartFormDataContent();
            multiForm.Add(new StringContent(s_slackToken), "token");
            multiForm.Add(new StringContent(_channel), "channel");
            multiForm.Add(new StringContent(message), "text");
            
            TryUsingExistingThread(multiForm, useExistingThread);

            var response = await Client.PostAsync(PostMessageURL, multiForm);
            var responseJson = await response.Content.ReadAsStringAsync();
            SlackPostMessageResponse slackResponse = JsonConvert.DeserializeObject<SlackPostMessageResponse>(responseJson);

            if (slackResponse.Ok) {
                Log.Important?.Info($"Posted new message: {message}");
            } else {
                Log.Important?.Error($"Failed to post message: {message}. Error: {slackResponse.Error}");
            }

            return slackResponse;
        }

        public string GetMachineName() {
            string machineName = System.Environment.MachineName;
            
            if (machineName.ToLower().Contains("jenkins")) {
                return $"{machineName} :jenkins:";
            }

            if (machineName.ToLower().Contains("ziemniak")) {
                return $"{machineName} :potato:";
            }

            if (machineName.ToLower().Contains("frytka")) {
                return $"{machineName} :fries:";
            }
            
            return $"{machineName} :sadge:";
        }
    }
}