using System;
using UnityEngine.Localization;
using Object = UnityEngine.Object;

namespace UnityEditor.Localization
{
    static class AssetUtility
    {
        public static string GetPathFromAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            if (AssetAddress.IsSubAsset(address))
                return AssetDatabase.GUIDToAssetPath(AssetAddress.GetGuid(address));
            return AssetDatabase.GUIDToAssetPath(address);
        }

        public static Object LoadAssetFromAddress(string address, Type type)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            if (type == null)
                type = typeof(Object);

            Object asset = null;
            var path = GetPathFromAddress(address);
            if (AssetAddress.IsSubAsset(address))
            {
                var subAssetName = AssetAddress.GetSubAssetName(address);
                foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                {
                    if (subAsset.name == subAssetName && type.IsAssignableFrom(subAsset.GetType()))
                    {
                        asset = subAsset;
                        continue;
                    }
                }
            }
            else
            {
                asset = AssetDatabase.LoadAssetAtPath(path, type);
            }
            return asset;
        }

        public static bool IsBuiltInResource(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return IsBuiltInResource(path);
        }

        public static bool IsBuiltInResource(string resPath)
        {
            return string.Equals(resPath, "Library/unity editor resources", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(resPath, "resources/unity_builtin_extra", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(resPath, "library/unity default resources", StringComparison.OrdinalIgnoreCase);
        }
    }
}
