using System;
using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.TG.Main.UIToolkit.PresenterData.Notifications {
    [Serializable]
    public struct PObjectiveNotificationData : IPresenterNotificationData {
        [field: SerializeField] public PBaseData BaseData { get; private set; }
        [field: SerializeField, ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true)] public ShareableSpriteReference DefaultQuestIcon { get; private set; }
        [field: SerializeField] public float VisibilityDuration { get; private set; }
        [field: SerializeField] public float FadeDuration { get; private set; }
        [field: SerializeField] public float InitialXOffset { get; private set; }
        [field: SerializeField] public float MoveDuration { get; private set; }
        
    }
}