using System;
using UnityEngine;

namespace Awaken.TG.Main.UIToolkit.PresenterData.Notifications {
    [Serializable]
    public struct PLevelUpNotificationData : IPresenterNotificationData {
        [field: SerializeField] public PBaseData BaseData { get; private set; }
        [field: SerializeField] public float VisibilityDuration { get; private set; }
        [field: SerializeField] public float FadeDuration { get; private set; }
        [field: SerializeField] public float BgFadeDuration { get; private set; }
    }
}