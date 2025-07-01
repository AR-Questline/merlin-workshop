using System;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Data {
    public class DataViewSharedData : ScriptableObject {
        const string Guid = "4c7a3b6aa11c9d548b495e06963b3338";
        public static DataViewSharedData Get() => AssetDatabase.LoadAssetAtPath<DataViewSharedData>(AssetDatabase.GUIDToAssetPath(Guid));
        
        public DataViewTab[] tabs = Array.Empty<DataViewTab>();
    }
}