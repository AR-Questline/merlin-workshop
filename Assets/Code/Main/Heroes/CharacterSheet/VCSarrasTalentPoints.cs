using System;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    public class VCSarrasTalentPoints : VCTalentPoints {
        static readonly int Grayscale = Shader.PropertyToID("_Grayscale");
        
        [SerializeField] Image[] grayscaleIcons = Array.Empty<Image>();
        [SerializeField] Image fillIcon;
        [SerializeField] GameObject lineObject;
        [SerializeField] Image goldLine;
        [SerializeField] Image goldLineGlow;
        [SerializeField] VCSyncImageFillWithMaterial fillSyncer;
        [SerializeField] float distributeFillDuration = 3f;
        [SerializeField] CanvasGroup pointsCanvasGroup;
        
        Material[] _grayscaleMaterials;
        Sequence _distributedSequence;
        
        CharacterStatType CharacterStatType { get; set; }

        protected override void OnAttach() {
            CharacterStatType = StatType as CharacterStatType;
            SetupLines();
            SetupIconsMaterial();
            SetupPointsState();
            World.EventSystem.ListenTo(EventSelector.AnySource, VCSarrasTalentTrinket.Events.PointsDistributionStarted, this, OnPointsDistributionStarted);
            World.EventSystem.ListenTo(EventSelector.AnySource, VCSarrasTalentTrinket.Events.PointsDistributionInProgress, this, OnPointsDistributionInProgress);
            World.EventSystem.ListenTo(EventSelector.AnySource, TalentTreeUI.Events.TreeZoomedIn, this, OnTreeZoomed);
            base.OnAttach();
        }

        void OnPointsDistributionStarted() {
            pointsCanvasGroup.alpha = 0f;
            fillIcon.fillAmount = 0f;
            goldLine.fillAmount = 0f;
            goldLineGlow.fillAmount = 0f;
            goldLineGlow.fillOrigin = 0;
            goldLine.fillOrigin = 0;
            fillSyncer.RegisterUpdate();
        }

        void OnPointsDistributionInProgress() {
            _distributedSequence.Kill();
            _distributedSequence = DOTween.Sequence().SetUpdate(true)
                .Append(DOVirtual.Float(0f, 1f, distributeFillDuration, x => {
                    goldLine.fillAmount = x;
                    goldLineGlow.fillAmount = x;
                    SetupPointsGrayscale(x);
                }))
                .AppendCallback(() => {
                    goldLineGlow.fillOrigin = 1;
                    goldLine.fillOrigin = 1;
                })
                .Append(DOVirtual.Float(1f, 0f, distributeFillDuration, x => {
                    goldLineGlow.fillAmount = x;
                    goldLine.fillAmount = x;
                    pointsCanvasGroup.alpha = 1f - x;
                    SetupPointsFill(1f - x);
            })).OnComplete(() => {
                    fillSyncer.UnregisterUpdate();
                });
        }

        void SetupLines() {
            goldLine.fillAmount = 0f;
            goldLineGlow.fillAmount = 0f;
        }

        void OnTreeZoomed(bool zoomed) {
            if (zoomed && (_distributedSequence?.IsPlaying() ?? false)) {
                _distributedSequence.Kill(true);
            }
            
            lineObject.SetActiveOptimized(!zoomed);
            transform.TrySetActiveOptimized(!zoomed);
            if (!zoomed) {
                SetupLines();
                SetupPointsState();
            }
        }

        void SetupIconsMaterial() {
            _grayscaleMaterials = new Material[grayscaleIcons.Length];
            for (int index = 0; index < grayscaleIcons.Length; index++) {
                Image grayscaleIcon = grayscaleIcons[index];
                _grayscaleMaterials[index] = new Material(grayscaleIcon.material);
                grayscaleIcon.material = _grayscaleMaterials[index];
            }
        }

        void SetupPointsState() {
            bool hasPoints = Hero.Current.Stat(CharacterStatType).ModifiedInt > 0;
            foreach (Material t in _grayscaleMaterials) {
                t.SetFloat(Grayscale, hasPoints ? 0f : 1f);
            }
            fillIcon.fillAmount = hasPoints ? 1f : 0f;
            pointsCanvasGroup.alpha = hasPoints ? 1f : 0f;
            fillSyncer.Sync();
        }

        void SetupPointsGrayscale(float value) {
            foreach (Material t in _grayscaleMaterials) {
                t.SetFloat(Grayscale, 1f - value);
            }
        }

        void SetupPointsFill(float value) {
            fillIcon.fillAmount = value;
        }

        protected override void OnDiscard() {
            for (int i = 0; i < grayscaleIcons.Length; i++) {
                grayscaleIcons[i].material = null;
                UnityEngine.Object.Destroy(_grayscaleMaterials[i]);
            }
            
            _grayscaleMaterials = null;
            fillSyncer.UnregisterUpdate();
        }
    }
}