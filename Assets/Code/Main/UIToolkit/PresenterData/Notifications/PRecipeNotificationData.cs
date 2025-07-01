using System;
using UnityEngine;

namespace Awaken.TG.Main.UIToolkit.PresenterData.Notifications {
    [Serializable]
    public struct PRecipeNotificationData : IPresenterNotificationDataWithHeight {
        [field: SerializeField] public PBaseData BaseData { get; private set; }
        [field: SerializeField] public float VisibilityDuration { get; private set; }
        [field: SerializeField] public float FadeDuration { get; private set; }
        [field: SerializeField] public float HeightDuration { get; private set; }
        [field: SerializeField] public float DefaultHeight { get; private set; }
    }
}