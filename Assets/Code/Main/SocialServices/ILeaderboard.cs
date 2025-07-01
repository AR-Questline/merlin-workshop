using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Awaken.TG.Main.SocialServices {
    public interface ILeaderboard {
        bool IsInitialized { get; }
        SocialService Service { get; }
        ILeaderboardEntry PlayerEntry { get; }
        SortingType SortingType { get; }
        DisplayType DisplayType { get; }
        ConnectionState ConnectionState { get; }
        void AfterInitialized(Action<ILeaderboard> callback);
        void DownloadPlayerScore(Action<ILeaderboardEntry> callback = null, Action onFailure = null);
        void UploadPlayerScore(int score, bool overrideScore);
        [UnityEngine.Scripting.Preserve] void GetEntries(LeaderboardType type, int from, int to, [NotNull] Action<IEnumerable<ILeaderboardEntry>> callback);
    }

    public enum ConnectionState {
        Connecting,
        Connected,
        NotConnected,
    }
}