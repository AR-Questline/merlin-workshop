using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD.Summons {
    [UsesPrefab("HUD/" + nameof(VHeroSummonPreview))]
    public class VHeroSummonPreview : View<NpcHeroSummon> {
        [SerializeField] Image icon;
        [SerializeField] Image greyoutIcon;
        [SerializeField] SimpleBar durationBar;
        [SerializeField] GlowingBar hpBar;

        public override Transform DetermineHost() => Hero.Current.View<VHeroHUD>().heroSummonsParent;
        NpcElement NpcElement => Target.ParentModel;
        Material _greyoutMaterial;

        protected override void OnInitialize() {
            _greyoutMaterial = new Material(greyoutIcon.material);
            _greyoutMaterial.SetFloat("_Grayscale", 1f);
            greyoutIcon.material = _greyoutMaterial;
            
            NpcElement.NpcIcon.RegisterAndSetup(this, icon);
            NpcElement.NpcIcon.RegisterAndSetup(this, greyoutIcon);
            
            hpBar.SetPercentInstant(0);
            durationBar.SetPercentInstant(0);
            UpdateBars(NpcElement.Health.Percentage, Target.DurationLeftNormalized);
        }

        void Update() {
            UpdateBars(NpcElement.Health.Percentage, Target.DurationLeftNormalized);
        }

        public void UpdateBars(float healthPercentage, float durationLeftNormalized) {
            hpBar.SetPercent(healthPercentage);
            durationBar.SetPercent(durationLeftNormalized);
        }

        protected override IBackgroundTask OnDiscard() {
            Destroy(_greyoutMaterial);
            return base.OnDiscard();
        }
    }
}