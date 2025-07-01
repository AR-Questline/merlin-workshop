namespace Awaken.Utility.Slack {
    public class SlackThread {
        public string ThreadID { get; }

        public SlackThread(string threadID) {
            ThreadID = threadID;
        }
    }
}