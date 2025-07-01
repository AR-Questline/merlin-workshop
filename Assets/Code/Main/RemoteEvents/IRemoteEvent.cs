using System;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.RemoteEvents {
    public interface IRemoteEvent : IModel {
        string Name { get; }
        [UnityEngine.Scripting.Preserve] string LeaderboardName { get; }
        [UnityEngine.Scripting.Preserve] DateTime EndTime { get; }
    }
}