using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Localization;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.FastTravel {
    [Serializable]
    public struct MapData {
        public SerializedArrayDictionary<SceneReference, MapSceneData> byScene;
        public ScenesGroup[] sceneGroups;
        
        [Serializable]
        public struct ScenesGroup {
            #if UNITY_EDITOR
            [SerializeField] string name;
            #endif
            public SceneReference[] scenes;
        }
    }
    
    [Serializable]
    public class MapSceneData {
        [SerializeField] LocString name;
        [SerializeField, UIAssetReference(AddressableGroup.CampaignMapsTextures)] ShareableSpriteReference sprite;
        [SerializeField] Bounds bounds;
        [SerializeField] float aspectRatio;
        [SerializeField] bool hasFogOfWar;

        public LocString Name => name;
        public ShareableSpriteReference Sprite => sprite;
        public Bounds Bounds => bounds;
        public float AspectRatio => aspectRatio;
        public bool HasFogOfWar => hasFogOfWar;

#if UNITY_EDITOR
        [Button]
        void TakeFromOpenScene() {
            var groundBounds = Object.FindAnyObjectByType<GroundBounds>();
            bounds = groundBounds.CalculateGameBounds();
            var size = bounds.size;
            aspectRatio = size.x / size.z;
        }
        
        [Button]
        void AdjustToAspectRatio() {
            var texture = sprite.Get().arSpriteReference.EditorLoad<Texture2D>();
            aspectRatio = texture.width / (float) texture.height;
            var size = bounds.size;
            bounds.size = new Vector3(size.z * aspectRatio, size.y, size.z);
        }
#endif
    }
}