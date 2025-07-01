using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.AddressableAssets;

namespace Awaken.Utility.Assets {
    public static class AssetReferenceUtils {
        public static bool Equals(AssetReference lhs, AssetReference rhs) {
            return lhs.AssetGUID == rhs.AssetGUID && lhs.SubObjectName == rhs.SubObjectName;
        }

        public static int HashCode(AssetReference reference) {
            return reference.AssetGUID.GetHashCode() ^ reference.SubObjectName.GetHashCode();
        }

        public static AssetReference Copy(AssetReference reference) {
            return new AssetReference(reference.AssetGUID) {
                SubObjectName = reference.SubObjectName,
            };
        }
        
        public class EqualityComparer : IEqualityComparer<AssetReference> {
            public static readonly EqualityComparer Instance = new();

            bool IEqualityComparer<AssetReference>.Equals(AssetReference x, AssetReference y) {
                return AssetReferenceUtils.Equals(x, y);
            }

            int IEqualityComparer<AssetReference>.GetHashCode(AssetReference obj) {
                return AssetReferenceUtils.HashCode(obj);
            }
        }
        
#if UNITY_EDITOR
        public static T EditorLoad<T>(this AssetReference assetReference) where T : UnityEngine.Object {
            var path = AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID);
            var subobject = assetReference.SubObjectName;
            return EditorLoadAt<T>(path, subobject);
        }
        
        public static T EditorLoadAt<T>(string path, string subObject) where T : class {
            if (string.IsNullOrWhiteSpace(subObject)) {
                return AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
            } else {
                return AssetDatabase.LoadAllAssetsAtPath(path).FirstOrDefault(asset => asset != null && asset is T && asset.name.Equals(subObject)) as T;
            }
        }
#endif
    }
}