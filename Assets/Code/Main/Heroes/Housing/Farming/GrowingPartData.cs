using System;
using Awaken.TG.Assets;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing.Farming {
    [Serializable]
    public partial struct GrowingPartData {
        public ushort TypeForSerialization => SavedTypes.GrowingPartData;

        public ARAssetReference arAssetReference;
        public GameObject spawnedObject;
        [Saved] public ARDateTime growingStartTime;
    }
}