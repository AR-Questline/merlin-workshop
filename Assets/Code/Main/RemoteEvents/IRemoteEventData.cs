using System;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.RemoteEvents {
    public interface IRemoteEventData : IModel {
        string Key { get; }
        string Name { get; }
        string Description { get; }
        string Leaderboard { get; }
        string RewardTitle { get; }
        DateTime EndTime { get; }
        Sprite RewardSprite { get; }
        Sprite ScenarioSprite { get; }
        [UnityEngine.Scripting.Preserve] string Subtitle(int leaderboardScore);
        bool AllowSelectClass { get; }
        bool AllowSelectCorruption { get; }
        int MinimumScoreForReward { get; }
        string RemoteEventUIPrefab { get; }
        SaveSlot SaveSlot { get; }
    }
}