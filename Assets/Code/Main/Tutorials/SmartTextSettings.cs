using System;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Tutorials.Steps.Composer.Helpers;
using Awaken.TG.Main.Tutorials.Views;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Tutorials {
    [Serializable]
    public class SmartTextSettings {
        public GameObject prefab;
        public PreDefinedTarget definedTarget = PreDefinedTarget.Custom;
        [ShowIf("@definedTarget==PreDefinedTarget.Custom")]
        public Transform target;
        public LocString text = new LocString();
        public FontSize fontSize = FontSize.Normal;
        public TextWidth textWidth = TextWidth.Normal;
        public Vector3 offset;
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        public SmartTextHelper helper;

        bool HasImage => helper?.HasImage ?? false;
        bool HasVideo => helper?.HasVideo ?? false;

        public VSmartText Spawn(IModel model) {
            var smartText = World.SpawnViewFromPrefab<VSmartText>(model, prefab);
            SetupSmartText(smartText);
            return smartText;
        }

        void SetupSmartText(VSmartText smartText) {
            smartText.textMesh.fontSize = (int)fontSize;
            smartText.Fill(text.ToString());
            smartText.SetPivot(pivot);
            smartText.SetSize(textWidth);
            smartText.SetTarget(DetermineTarget(false), offset);
            if (HasVideo) {
                smartText.SetVideo(helper.clipReference);
            } else if (HasImage) {
                smartText.SetImage(helper.spriteReference);
            }
        }

        public VSmartText TestSpawn() {
            Transform realTarget = DetermineTarget(false);
            if (realTarget == null) {
                return null;
            }

            GameObject viewGob = Object.Instantiate(prefab, realTarget.parent);
            VSmartText smartText = viewGob.GetComponent<VSmartText>();
            
            SetupSmartText(smartText);
            return smartText;
        }
        
        Transform DetermineTarget(bool isFight) {
            return definedTarget switch {
                PreDefinedTarget.Custom => target,
                PreDefinedTarget.Right when isFight && (HasImage || HasVideo) => World.Services?.Get<ViewHosting>().OnMainCanvas("Tutorials/RightMedia"),
                PreDefinedTarget.Right when isFight => World.Services?.Get<ViewHosting>().OnMainCanvas("Tutorials/Right"),
                PreDefinedTarget.Right => World.Services?.Get<ViewHosting>().OnMainCanvas("Tutorials/RightMap"),
                PreDefinedTarget.Middle => World.Services?.Get<ViewHosting>().OnMainCanvas("Tutorials/Middle"),
                _ => null,
            };
        }

        public enum FontSize {
            [UnityEngine.Scripting.Preserve] Small = 14,
            [UnityEngine.Scripting.Preserve] Normal = 18,
            [UnityEngine.Scripting.Preserve] Huge = 24,
        }

        public enum TextWidth {
            [UnityEngine.Scripting.Preserve] Small = 200,
            [UnityEngine.Scripting.Preserve] Normal = 300,
            [UnityEngine.Scripting.Preserve] Big = 500,
            [UnityEngine.Scripting.Preserve] WholeScreen = 1000,
        }
    }
    
    public enum PreDefinedTarget {
        Custom = 0,
        Right = 1,
        Middle = 2,
    }
}