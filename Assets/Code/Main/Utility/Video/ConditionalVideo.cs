using System;
using Awaken.TG.Main.Utility.Tags;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Video {
    [Serializable]
    public class ConditionalVideo {
        [SerializeField] FlagLogic[] requirements = Array.Empty<FlagLogic>();
        [SerializeField] LoadingHandle video;

        public LoadingHandle Video => video;

        public bool ShouldPlay() {
            for (int i = 0; i < requirements.Length; i++) {
                if (!requirements[i].Get(true)) {
                    return false;
                }
            }
            return true;
        }
    }
}