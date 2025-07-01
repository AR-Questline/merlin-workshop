using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.SocialServices {
    [Serializable]
    public struct DlcId {
#if UNITY_PS5 || UNITY_EDITOR
        [LabelText("PS5 Id")]
        public Optional<string> ps5Id;
#endif

#if UNITY_GAMECORE || UNITY_EDITOR
        public Optional<string> xboxStoreId;
#endif

#if UNITY_STANDALONE || UNITY_EDITOR
        public Optional<uint> steamId;
        public Optional<ulong> gogId;
#endif

        [Serializable, InlineProperty]
        public struct Optional<T> {
            [HorizontalGroup(Width = 0.1f), SerializeField, HideLabel]
            bool hasValue;

            [HorizontalGroup(Width = 0.89f), SerializeField, HideLabel, ShowIf(nameof(hasValue))]
            T value;

            public bool HasValue => hasValue;

            public T Value => value;

            public bool TryGet(out T value) {
                value = this.value;
                return hasValue;
            }
        }
    }
}