namespace Awaken.Utility.Slack {
    public struct SlackUploadFileResponse {
        public bool Ok { get; [UnityEngine.Scripting.Preserve] set; }
        public string Error { get; [UnityEngine.Scripting.Preserve] set; }
        public SlackFile File { get; [UnityEngine.Scripting.Preserve] set; }
    }
}