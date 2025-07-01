using Awaken.TG.Assets;
using System;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.MovementStyles {
    public class NpcMovementStyle : ScriptableObject {
        [SerializeField, AnimancerAnimationsAssetReference] [UnityEngine.Scripting.Preserve] ShareableARAssetReference[] animations = Array.Empty<ShareableARAssetReference>();
    }
}