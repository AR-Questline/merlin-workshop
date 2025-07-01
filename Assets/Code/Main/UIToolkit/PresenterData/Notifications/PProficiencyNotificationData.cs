using System;
using UnityEngine;

namespace Awaken.TG.Main.UIToolkit.PresenterData.Notifications {
    [Serializable]
    public struct PProficiencyNotificationData : IPresenterNotificationData {
        [field: SerializeField] public PBaseData BaseData { get; private set; }
        [field: SerializeField] public float VisibilityDuration { get; private set; }
        [field: SerializeField] public float FadeDuration { get; private set; }
        [field: SerializeField] public float InitialXOffset { get; private set; }
        [field: SerializeField] public float MoveDuration { get; private set; }
        [field: SerializeField] public float LevelInitialScale { get; private set; }
        [field: SerializeField] public float LevelScaleDuration { get; private set; }
        [field: SerializeField] public float LevelFadeDuration { get; private set; }
        [field: SerializeField] public float ProgressBarDuration { get; private set; }
        
    }
}