using System;
using Awaken.TG.Main.Saving.SaveSlots;
using JetBrains.Annotations;

namespace Awaken.TG.Main.RemoteEvents {
    public interface IRemoteEventSpec {
        SaveSlot SaveSlot { get; }
        string Name { get; }
        string RewardTitle { get; }
        string RemoteKey { get; }
        [UnityEngine.Scripting.Preserve] string Leaderboard { get; }
        IRemoteEvent CreateEvent(DateTime endTime);
        IRemoteEventData CreateEventData(DateTime endTime);
        void GrantRewards();
    }
}