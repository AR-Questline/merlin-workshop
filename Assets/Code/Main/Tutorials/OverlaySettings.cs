using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Tutorials.Views;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Graphics;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Tutorials {
    [Serializable]
    public class OverlaySettings {
        [InfoBox("Masks are taken from Prefabs/MapViews/UI/Tutorials/Masks/ folder")]
        [TableList]
        public HoleSpec[] holes = new HoleSpec[0];
        
        [TableList]
        public StickerSpec[] stickers = new StickerSpec[0];
        
        [Range(1, 30)]
        public int blurFactor;

        public VGreyOverlay Spawn(IModel model) {
            var overlay = World.SpawnView<VGreyOverlay>(model);
            Camera camera = World.Only<CameraStateStack>().MainCamera;
            Setup(overlay, camera);
            return overlay;
        }

        void Setup(VGreyOverlay overlay, Camera camera) {
            Texture2D texture;
            bool useColor = true;
            
            if (!holes.Any() && !stickers.Any() && blurFactor <= 1) {
                texture = TextureUtils.GetNotModifiableOverlay;
                useColor = false;
            } else {
                texture = TextureUtils.CreateFullScreenOverlay();
                
                // holes
                foreach (var hole in holes) {
                    hole.CreateHole(overlay, texture, camera);
                }

                // stickers
                foreach (var sticker in stickers) {
                    sticker.CreateSticker(overlay, texture, camera);
                }

                // blur
                if (blurFactor > 1) {
                    texture.ApplyBlur(blurFactor);
                }
            }

            overlay.SetOverlay(texture.ToSprite(), useColor);
        }
        
        // Editor
        [Button]
        [ShowIf("@holes.Length > 0")]
        void TestSpawnInternal() => TestSpawn();
        
        public VGreyOverlay TestSpawn() {
            if (!holes.Any()) {
                return null;
            }
            
            RectTransform highlightArea = holes.First().area;
            GameObject prefab = World.ExtractPrefab(typeof(VGreyOverlay), "Prefabs/MapViews");
            GameObject viewGob = Object.Instantiate(prefab, highlightArea.parent);
            VGreyOverlay overlay = viewGob.GetComponent<VGreyOverlay>();
            Camera camera = highlightArea.GetComponentInParent<Canvas>().worldCamera;

            Setup(overlay, camera);
            return overlay;
        }

        [Serializable]
        public class HoleSpec {
            [VerticalGroup("Area")]
            public RectTransform area;

            [VerticalGroup("Area")]
            public float scaleFactor = 1f;


            [VerticalGroup("Mask")] 
            public OverlayPreset preset = OverlayPreset.RedCircle;
            [VerticalGroup("Mask")]
            [HideIf("@preset != OverlayPreset.None")]
            [ValueDropdown(nameof(MaskPaths))]
            public string maskPath = "None";

            [VerticalGroup("Mask")]
            [HideIf("@preset != OverlayPreset.None")]
            [ShowIf("@maskPath == \"None\"")] 
            public Texture2D maskDirect;
            
            const string ResourcesPath = "Prefabs/MapViews/UI/Tutorials/Masks/";
            IEnumerable<string> MaskPaths => "None".Yield().Union(Resources.LoadAll<Texture2D>(ResourcesPath).Select(s => s.name));

            public static Texture2D DefaultMask => Resources.Load<Texture2D>($"{ResourcesPath}Brush_1");
            
            Texture2D Mask => maskDirect != null 
                ? maskDirect 
                : Resources.Load<Texture2D>($"{ResourcesPath}{maskPath}");

            public void CreateHole(VGreyOverlay overlay, Texture2D texture, Camera camera) {
                if (preset != OverlayPreset.None) {
                    OverlayPresets.SetupPreset(preset, overlay, texture, this, camera);
                } else {
                    texture.CutHole(area, camera, Mask, scaleFactor);
                }
            }
        }

        [Serializable]
        public class StickerSpec {
            [VerticalGroup("Area")]
            public RectTransform area;
            [VerticalGroup("Area")]
            public float scaleFactor = 1f;
            
            [VerticalGroup("Sticker")]
            [ValueDropdown(nameof(MaskPaths))]
            public string maskPath = "None";

            [VerticalGroup("Sticker")]
            [ShowIf("@maskPath == \"None\"")] 
            public Texture2D maskDirect;
            
            const string ResourcesPath = "Prefabs/MapViews/UI/Tutorials/Masks/";
            IEnumerable<string> MaskPaths => "None".Yield().Union(Resources.LoadAll<Texture2D>(ResourcesPath).Select(s => s.name));
            
            public static Texture2D DefaultMask => Resources.Load<Texture2D>($"{ResourcesPath}Sticker_1");
            public static Sprite RectMask => Resources.Load<Sprite>($"{ResourcesPath}Sticker_2");
            
            public Texture2D Mask => maskDirect != null
                ? maskDirect
                : Resources.Load<Texture2D>($"{ResourcesPath}{maskPath}");

            public void CreateSticker(VGreyOverlay overlay, Texture2D texture, Camera camera) {
                texture.Stick(area, camera, Mask, scaleFactor);
            }
        }
    }
}