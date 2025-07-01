using System;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.RemoteEvents {
    public abstract class DefaultEventSpec : ScriptableObject, IRemoteEventSpec {
        // -- Base data
        public abstract SaveSlot SaveSlot { get; }
        public string remoteKey = "TheMessage";
        [LocStringCategory(Category.Event)]
        public LocString title, description, rewardTitle;
        public bool allowSelectClass;
        [ShowIf(nameof(allowSelectClass))]
        public bool allowSelectCorruption = true;
        // -- Score and rewards
        [ValueDropdown(nameof(Leaderboards))]
        public string leaderboardName;
        public Sprite rewardSprite;
        public Sprite scenarioSprite;
        // --- UI
        [FilePath(ParentFolder = "Assets/Resources")]
        public string remoteEventUIPrefab;
        // -- Gameplay
        public virtual int MinimumScoreForReward { get; } = 1;
        
        // === IRemoteEventSpec
        public string RemoteKey => remoteKey;
        public string Name => title;
        public string RewardTitle => string.IsNullOrWhiteSpace(rewardTitle) ? LocTerms.DefaultEventRewardTitle.Translate() : rewardTitle;
        public string Leaderboard => leaderboardName;

        public abstract IRemoteEvent CreateEvent(DateTime endTime);

        public virtual IRemoteEventData CreateEventData(DateTime endTime) {
            return new DefaultEventData(RemoteKey, title, description, leaderboardName, RewardTitle, endTime, rewardSprite, scenarioSprite,
                Subtitle, allowSelectClass, allowSelectCorruption, MinimumScoreForReward, remoteEventUIPrefab, SaveSlot);
        }

        public virtual void GrantRewards() { }

        // === Defaults
        protected virtual string Subtitle(int leaderboardValue) {
            return string.Empty;
        }
        
        // === Editor helpers values
        static string[] Leaderboards => LeaderboardLibrary.GetAllStaticLeaderboards().ToArray();
    }
}