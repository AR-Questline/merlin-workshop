using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility {
    [Serializable]
    public abstract class EmbedExplicitWrapper<T1, T2> where T1 : IContainerAsset<T2> {
        [SerializeField]
        public AssetType assetType = AssetType.Embed;

        [ShowIf(nameof(ShowAsset)), SerializeField, InlineProperty, LabelWidth(90)]
        T1 dataAsset;
        [HideIf(nameof(ShowAsset)), SerializeField, InlineProperty, LabelWidth(90)]
        T2 data;

        static T2 _dummyEmbeddedData;
        public T2 Data {
            get {
                if (assetType == AssetType.Explicit && dataAsset != null) {
                    return dataAsset.Container;
                }
                return data;
            }
        }

        public ref T2 EmbeddedDataRef {
            get {
                if (assetType == AssetType.Explicit && dataAsset != null) {
                    _dummyEmbeddedData = default;
                    return ref _dummyEmbeddedData;
                }
                return ref data;
            }
        }

        // === Helpers
        bool ShowAsset => assetType == AssetType.Explicit;

        public enum AssetType {
            Explicit = 0,
            Embed = 1
        }
    }

    public interface IContainerAsset<out T> {
        T Container { get; }
    }
}
