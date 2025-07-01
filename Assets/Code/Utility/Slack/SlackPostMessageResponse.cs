namespace Awaken.Utility.Slack {
    public struct SlackPostMessageResponse {
        public bool Ok { get; [UnityEngine.Scripting.Preserve] set; }
        public string Error { get; [UnityEngine.Scripting.Preserve] set; }
        public string Ts { get; [UnityEngine.Scripting.Preserve] set; } //used for threads
    }
}