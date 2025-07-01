using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.SocialServices.DebugServices {
    public class DebugSocialService : SocialService {
        Dictionary<string, ILeaderboard> _leaderboards = new();

        public override void SetAchievement(string id, Action onSuccess = null) {
            onSuccess?.Invoke();
        }
        public override void SetAchievementProgress(string id, int value) { }

        public override ILeaderboard GetLeaderboard(string id) {
            if (_leaderboards.TryGetValue(id, out var stored)) {
                return stored;
            } else {
                LeaderboardLibrary leaderboard = RichEnum.AllValuesOfType<LeaderboardLibrary>().FirstOrDefault(library => id.Contains(library.LeaderboardName));
                var created = new DebugLeaderboard(id, this, leaderboard?.SortingType ?? SortingType.Descending, leaderboard?.DisplayType ?? DisplayType.Numeric);
                _leaderboards[id] = created;
                return created;
            }
        }

        public override void LeaderboardAddToScore(string id, int value) {
            GetLeaderboard(id).UploadPlayerScore(value, false);
        }

        public override void LeaderboardUpdateScore(string id, int value) {
            GetLeaderboard(id).UploadPlayerScore(value, true);
        }

        public override void GetLeaderboardScore(string id, Action<int> callback, Action onFailure) {
            var leaderboard = GetLeaderboard(id);
            leaderboard.AfterInitialized(_ => {
                callback.Invoke(leaderboard.PlayerEntry.Score);
            });
        }

        protected override bool HasDlc_Internal(DlcId dlcId) => true;
    }
}
