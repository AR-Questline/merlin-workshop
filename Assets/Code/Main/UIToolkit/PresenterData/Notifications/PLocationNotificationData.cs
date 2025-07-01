using System;
using UnityEngine;

namespace Awaken.TG.Main.UIToolkit.PresenterData.Notifications {
    [Serializable]
    public struct PLocationNotificationData : IPresenterNotificationData {
        [field: SerializeField] public PBaseData BaseData { get; private set; }
        [field: SerializeField] public float VisibilityDuration { get; private set; }
        [field: SerializeField] public float FadeDuration { get; private set; }
        [field: SerializeField] public float InitialXOffset { get; private set; }
        [field: SerializeField] public float MoveDuration { get; private set; }
    }
}