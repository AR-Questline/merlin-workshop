using System;
using Awaken.TG.Main.Memories;
using UnityEngine;

namespace Awaken.TG.Main.SocialServices.DebugServices {
    public class DebugLeaderboardEntry : ILeaderboardEntry {
        public string Name => "DebugUser";
        public int Rank => 1;
        public int Score => PrefMemory.GetInt($"{_leaderboardId}_score");
        public bool IsPlayer => true;

        readonly string _leaderboardId;
        
        public DebugLeaderboardEntry(string leaderboardId) {
            _leaderboardId = leaderboardId;
        }
        
        public void GetAvatar(Action<Texture2D, Vector3> callback) {
            var texture = new Texture2D(100, 100, TextureFormat.RGBA32, false, false) {name = "Runtime_LeaderboardAvatar"};
            callback?.Invoke(texture, Vector3.one);
        }
    }
}
