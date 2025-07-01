using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.UI.Leaderboards;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.SocialServices.DebugServices {
    public class DebugLeaderboard : ILeaderboard {
        public bool IsInitialized => _initialized;
        public SocialService Service { get; private set; }
        public ILeaderboardEntry PlayerEntry => _playerEntry;

        string _id;
        bool _initialized;
        ILeaderboardEntry _playerEntry;
        Action<ILeaderboard> _afterInitializedCallback;
        public SortingType SortingType { get; }
        public DisplayType DisplayType { get; }
        public ConnectionState ConnectionState { get; private set; }

        public DebugLeaderboard(string id, DebugSocialService service,
            SortingType sortMethod = SortingType.Descending,
            DisplayType displayType = DisplayType.Numeric) {
            _id = id;
            Service = service;
            SortingType = sortMethod;
            DisplayType = displayType;
            Initialize().Forget();
        }

        async UniTaskVoid Initialize() {
            await Task.Delay(500);
            ConnectionState = ConnectionState.Connected;
            _initialized = true;
            _playerEntry = new DebugLeaderboardEntry(_id);
            _afterInitializedCallback?.Invoke(this);
            _afterInitializedCallback = null;
        }
        
        public void AfterInitialized(Action<ILeaderboard> callback) {
            if (IsInitialized) {
                callback.Invoke(this);
            } else {
                _afterInitializedCallback += callback;
            }
        }

        public async void DownloadPlayerScore(Action<ILeaderboardEntry> callback = null, Action onFailure = null) {
            await Task.Delay(500);
            callback?.Invoke(PlayerEntry);
        }

        public async void UploadPlayerScore(int score, bool overrideScore) {
            await Task.Delay(500);
            if (overrideScore) {
                if (PrefMemory.GetInt($"{_id}_score") < score) {
                    PrefMemory.Set($"{_id}_score", score, false);
                }
            } else {
                score += PrefMemory.GetInt($"{_id}_score");
                PrefMemory.Set($"{_id}_score", score, false);
            }
            foreach (var leaderboard in World.All<ILeaderboardUI>()) {
                leaderboard.Refresh();
            }
        }

        public async void GetEntries(LeaderboardType type, int @from, int to, Action<IEnumerable<ILeaderboardEntry>> callback) {
            await Task.Delay(500);
            callback.Invoke(PlayerEntry.Yield());
        }
    }
}
