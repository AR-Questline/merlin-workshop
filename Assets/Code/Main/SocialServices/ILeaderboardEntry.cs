using System;
using UnityEngine;

namespace Awaken.TG.Main.SocialServices {
    public interface ILeaderboardEntry {
        [UnityEngine.Scripting.Preserve] string Name { get; }
        [UnityEngine.Scripting.Preserve] int Rank { get; }
        [UnityEngine.Scripting.Preserve] int Score { get; }
        [UnityEngine.Scripting.Preserve] bool IsPlayer { get; }
        [UnityEngine.Scripting.Preserve] void GetAvatar(Action<Texture2D, Vector3> callback);
    }
}