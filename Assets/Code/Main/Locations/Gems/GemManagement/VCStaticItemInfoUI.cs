using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips.Components;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems.GemManagement {
    public abstract class VCStaticItemInfoUI<TModel, TView> : ViewComponent<TModel> where TModel : IModel where TView : View {
        const float FadeDuration = 0.2f;
        
        [SerializeField] CanvasGroup backgroundResult;
        [SerializeField] CanvasGroup resultCanvasGroup;
        
        [SerializeField] protected ItemTooltipHeaderComponent header;
        [SerializeField] protected ItemTooltipBodyComponent body;
        [SerializeField] protected ItemTooltipDescriptionsEffectsComponent effects;
        [SerializeField] protected ItemTooltipDescriptionsRequirementsComponent requirements;
        [SerializeField] protected ItemTooltipDescriptionsGemComponent gem;
        [SerializeField] protected ItemTooltipDescriptionsBuffComponent buff;
        [SerializeField] protected ItemTooltipFooterComponent footer;
        
        Sequence _fadeSequence;
        
        protected virtual bool InstantFading => false;
        protected virtual IItemTooltipComponent[] AllSections => new IItemTooltipComponent[] { body, effects, requirements, gem, buff, footer };

        protected abstract void Initialize();
        
        protected override void OnAttach() {
            Initialize();
            backgroundResult.alpha = 0f;
            resultCanvasGroup.alpha = 0f;
        }
        
        public void SetupSections(View view) {
            foreach (var tooltipSection in AllSections) {
                tooltipSection.SetupComponent(view);
            }
        }

        protected void ItemRefreshed(Item item) {
            if (item == null) {
                Hide();
                return;
            }
            RefreshContent(new ExistingItemDescriptor(item));
            Show();
        }

        public void Show() => SetVisibility(1f);

        public void Hide() => SetVisibility(0f);

        void RefreshContent(IItemDescriptor descriptor) {
            if (descriptor != null) {
                var view = Target.View<TView>();
                foreach (var tooltipSection in AllSections) {
                    tooltipSection.Refresh(descriptor, null, view);
                }
            }
        }

        void SetVisibility(float targetAlpha) {
            if (InstantFading) {
                backgroundResult.alpha = targetAlpha;
                resultCanvasGroup.alpha = targetAlpha;
            } else {
                Fade(targetAlpha);
            }
        }

        void Fade(float targetAlpha) {
            _fadeSequence.Kill(true);
            _fadeSequence = DOTween.Sequence().SetUpdate(true)
                .Append(resultCanvasGroup.DOFade(targetAlpha, FadeDuration))
                .Join(backgroundResult.DOFade(targetAlpha, FadeDuration / 2));
        }
    }
}