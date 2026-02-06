using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    public abstract class VExpansionEntryUI : View<ExpansionEntryUI> {
        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        static readonly int MaskTex = Shader.PropertyToID("_MaskTex");
        static readonly int ScaleProperty = Shader.PropertyToID("_Scale");
        
        [SerializeField] protected ARButton button;
        [SerializeField, ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true)] ShareableSpriteReference dlcSpriteReference;
        [SerializeField] Material maskedImageMaterial;
        [SerializeField] Image bgImage;
        [SerializeField] TextMeshProUGUI expansionTypeText;
        [SerializeField] TextMeshProUGUI expansionTitleText;
        [SerializeField] TextMeshProUGUI timeToReleaseText;
        [SerializeField] float hoverDuration = 1f;
        [SerializeField] float hoverScale = 1.1f;
        
        Material _bgMaterial;
        Tween _hoverTween;
        
        string Type => Target.ExpansionEntryData.type;
        string Title => Target.ExpansionEntryData.title;
        DateTime ReleaseDate => Target.ExpansionEntryData.releaseDate;

        protected override void OnInitialize() {
            bgImage.enabled = false;
            expansionTypeText.SetText(Type);
            expansionTitleText.SetText(Title);
            timeToReleaseText.SetText(ExpansionUtils.GetTimeToReleaseText(ReleaseDate));
            button.OnClick += OpenExpansionOverview;
            button.OnHover += OnHover;
        }

        protected override void OnFullyInitialized() {
            if (dlcSpriteReference is { IsSet: true }) {
                dlcSpriteReference.RegisterAndSetup(this, bgImage, (img, sprite) => {
                    _bgMaterial = new Material(maskedImageMaterial);
                    _bgMaterial.SetTexture(MainTex, sprite.texture);
                    _bgMaterial.SetTexture(MaskTex, sprite.texture);
                    img.material = _bgMaterial;
                    bgImage.enabled = true;
                });
            } else {
                Log.Important?.Error("Dlc SpriteReference is not set !");
            }
        }

        protected void OpenExpansionOverview() => World.Add(new ExpansionOverviewUI(Target.ExpansionIndex));

        void OnHover(bool hover) {
            _hoverTween?.Kill();
            _hoverTween = _bgMaterial.DOFloat(hover ? hoverScale : 1f, ScaleProperty, hoverDuration).SetUpdate(true);
        }

        protected override IBackgroundTask OnDiscard() {
            bgImage.material = null;
            Object.Destroy(_bgMaterial);
            return base.OnDiscard();
        }
    }
}