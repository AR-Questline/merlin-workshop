using System.Collections.Generic;
using Awaken.Kandra;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.MVC;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    [RequireComponent(typeof(MeshRenderer))]
    public class HeroSkinColorApplier : StartDependentView<Model> {
        static readonly List<Material> MaterialsBuffer = new();
        
        MeshRenderer _renderer;
        
        protected override void OnAwake() {
            _renderer = GetComponent<MeshRenderer>();
            ModelUtils.DoForFirstModelOfType<Hero>(ApplySkinColor, this);
        }

        void ApplySkinColor(Hero hero) {
            hero.AfterFullyInitialized(() => {
                if (!hero.TryGetElement(out BodyFeatures bodyFeatures) || bodyFeatures.SkinColor == null) {
                    return;
                }
                
                using var buffer = new ReusableListBuffer<Material>(MaterialsBuffer);
                _renderer.GetMaterials(buffer);
                foreach (var material in buffer) {
                    material.SetColor(SkinColorFeature.TintID, bodyFeatures.SkinColor.Color);
                }
            });
        }
    }
}