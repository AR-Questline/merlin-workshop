using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Crosshair {
    public abstract partial class CrosshairPart : Element<HeroCrosshair> {
        public sealed override bool IsNotSaved => true;

        public HeroCrosshair Crosshair => ParentModel;
        public Hero Hero => Crosshair.Hero;
        
        public abstract CrosshairLayer Layer { get; }
        public abstract int Priority { get; }
        public virtual bool SpawnAsLast => false;

        public void SetActive(bool active) {
            MainView.gameObject.SetActive(active);
        }
    }

    [SpawnsView(typeof(VDefaultCrosshairPart))]
    public partial class DefaultCrosshairPart : CrosshairPart {
        public override CrosshairLayer Layer => CrosshairLayer.OverridingLayer0;
        public override int Priority => 0;
    }

    [SpawnsView(typeof(VCrouchCrosshairPart))]
    public partial class CrouchCrosshairPart : CrosshairPart {
        public override CrosshairLayer Layer => CrosshairLayer.OverridingLayer1;
        public override int Priority => 1;
    }

    [SpawnsView(typeof(VMeleeCrosshairPart))]
    public partial class MeleeCrosshairPart : CrosshairPart {
        public override CrosshairLayer Layer => CrosshairLayer.OverridingLayer2;
        public override int Priority => 0;
        public override bool SpawnAsLast => true;
    }

    [SpawnsView(typeof(VBowCrosshairPart))]
    public partial class BowCrosshairPart : CrosshairPart {
        public override CrosshairLayer Layer => CrosshairLayer.OverridingLayer0;
        public override int Priority => 1;
        public override bool SpawnAsLast => true;
    }

    [SpawnsView(typeof(VCustomCrosshairPart))]
    public partial class CustomCrosshairPart : CrosshairPart {
        public SpriteReference SpriteReference { get; }
        public override CrosshairLayer Layer { get; }
        public override int Priority => 100;

        public CustomCrosshairPart(SpriteReference sprite, CrosshairLayer layer) {
            SpriteReference = sprite;
            Layer = layer;
        }
    }

    [Flags]
    public enum CrosshairLayer : byte {
        [UnityEngine.Scripting.Preserve] AdditiveLayer = 0,
        
        [UnityEngine.Scripting.Preserve] OverridingLayer0 = 1 << 0,
        [UnityEngine.Scripting.Preserve] OverridingLayer1 = 1 << 1,
        [UnityEngine.Scripting.Preserve] OverridingLayer2 = 1 << 2,
    }
}