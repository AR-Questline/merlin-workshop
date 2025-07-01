using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using UnityEngine;

namespace Awaken.TG.Main.RemoteEvents {
    public partial class DefaultEventData : Model, IRemoteEventData {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        // === IRemoteEventData
        public string Key { get; }
        public string Name { get; }
        public string Description { get; }
        public string Leaderboard { get; }
        public string RewardTitle { get; }
        public DateTime EndTime { get; }
        public Sprite RewardSprite { get; }
        public Sprite ScenarioSprite { get; }
        public bool AllowSelectClass { get; }
        public bool AllowSelectCorruption { get; }
        public int MinimumScoreForReward { get; }
        public string RemoteEventUIPrefab { get; }
        public SaveSlot SaveSlot { get; }

        public string Subtitle(int leaderboardScore) => _subtitleFunc?.Invoke(leaderboardScore) ?? string.Empty;

        // === Fields
        Func<int, string> _subtitleFunc;

        // === Initialization
        public DefaultEventData(string key, string name, string description, string leaderBoard, string rewardTitle, DateTime endTime, 
            Sprite rewardSprite, Sprite scenarioSprite, Func<int, string> subtitleFunc, bool allowSelectClass, bool allowSelectCorruption, int minimumScoreForReward,
            string remoteEventUIPrefab, SaveSlot saveSlot) {
            Key = key;
            Name = name;
            Description = description;
            Leaderboard = leaderBoard;
            RewardTitle = rewardTitle;
            EndTime = endTime;
            RewardSprite = rewardSprite;
            ScenarioSprite = scenarioSprite;
            _subtitleFunc = subtitleFunc;
            AllowSelectClass = allowSelectClass;
            AllowSelectCorruption = allowSelectCorruption;
            MinimumScoreForReward = minimumScoreForReward;
            RemoteEventUIPrefab = remoteEventUIPrefab;
            SaveSlot = saveSlot;
        }
        
        protected override void OnInitialize() {
            Services.TryGet<SocialService>()?.LeaderboardAddToScore(Leaderboard, 0);
        }
    }
}