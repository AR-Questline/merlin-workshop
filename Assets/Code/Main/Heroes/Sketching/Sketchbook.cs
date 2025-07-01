using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.MVC;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Sketching {
    public class Sketchbook {
        const string SketchesFolder = "Sketches";
        static readonly int BaseColorMap = Shader.PropertyToID("_SketchMap");
        static readonly int Intensity = Shader.PropertyToID("_Transition");

        static ItemTemplate SketchItemTemplate => GameConstants.Get.SketchItemTemplate;

        readonly CharacterSketchbook _characterSketchbook;
        bool _isSketchVisible;

        public Sketchbook(CharacterSketchbook characterSketchbook) {
            _characterSketchbook = characterSketchbook;
        }

        public bool TryDrawSketch(float delay, float drawTime) {
            if (Sketch.IsGlobalCountLimited) {
                int sketchesCount = World.Services.Get<LargeFilesStorage>().GetFileCountInFolder(SketchesFolder);
                if (sketchesCount >= Sketch.GlobalCountLimit) {
                    return false;
                }
            }
            
            DrawSketch(delay, drawTime).Forget();
            return true;
        }
        
        async UniTaskVoid DrawSketch(float delay, float drawTime) {
            await UniTask.WaitForEndOfFrame();
            var startTime = Time.time;
            var sketchTexture = await HeroSketches.TakeScreenshotSketch(Hero.Current.ParentTransform);
            delay = math.max(0, delay - (Time.time - startTime));
            int sketchIndex;
            World.Services.Get<LargeFilesStorage>().SaveFile(SketchesFolder, sketchTexture, out sketchIndex);
            AddSketchItem(sketchIndex);
            DelayedShowTexture(sketchTexture, delay, drawTime).Forget();
        }

        public void HideSketchedDrawing() {
            HideTexture();
        }
        
        // Sketchbook

        async UniTaskVoid DelayedShowTexture(Texture2D texture, float delay, float drawTime) {
            if (texture == null || _isSketchVisible) {
                return;
            }
            _isSketchVisible = true;
            if (!await AsyncUtil.DelayTime(_characterSketchbook, delay)) {
                return;
            }
            ShowTexture(texture, drawTime);
        }

        void ShowTexture(Texture2D texture, float drawTime) {
            var material = _characterSketchbook.Material;
            if (material == null) {
                return;
            }
            material.SetTexture(BaseColorMap, texture);
            SmoothShowTexture(material, drawTime).Forget();
        }

        async UniTaskVoid SmoothShowTexture(Material material, float lerpTime) {
            float timeModifier = 1 / lerpTime;
            for (float i = 0; i < 1; i += timeModifier * Time.deltaTime) {
                material.SetFloat(Intensity, i);
                if (!await AsyncUtil.DelayFrame(_characterSketchbook)) {
                    return;
                }
            } 
        }

        void HideTexture() {
            var material = _characterSketchbook.Material;
            if (material == null || !_isSketchVisible) {
                return;
            }
            _isSketchVisible = false;
            
            material.SetTexture(BaseColorMap, null);
        }
        
        // Sketch Item

        void AddSketchItem(int index) {
            var item = new Item(SketchItemTemplate);
            item.AddElement(new Sketch(index));
            _characterSketchbook.Owner.Inventory.Add(item);
        }
    }
}
