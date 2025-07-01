using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using System;
using Awaken.TG.Main.Utility.UI;
using Awaken.Utility.Debugging;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.EmptyContent {
    public class VCEmptyInfo : ViewComponent {
        const float EmptyContentAlpha = 0.2f;
        
        [SerializeField] GameObject emptyInfo;
        [SerializeField] CanvasGroup emptyInfoGroup;
        [SerializeField] TMP_Text[] emptyInfoLabels = Array.Empty<TMP_Text>();
        CanvasGroup[] _targetGroups;
        Tween[] _tweens;

        protected override void OnAttach() {
            emptyInfoGroup.alpha = 0;
            GenericTarget.ListenTo(IEmptyInfo.Events.OnEmptyStateChanged, SetContentActive);
        }

        public void Setup(CanvasGroup[] contentGroups, params string[] texts) {
            _targetGroups = contentGroups;
            _tweens = new Tween[_targetGroups.Length + 1];
            SetupLabels(texts);
        }
        
        public void SetupLabels(params string[] texts) {
            if (emptyInfoLabels.Length < texts.Length) {
                Log.Important?.Error($"Texts count ({texts.Length}) must be less or equal to emptyInfoLabels count ({emptyInfoLabels.Length})", gameObject);
                return;
            }
            
            for (int i = 0; i < texts.Length; i++) {
                var text = texts[i];
                if (string.IsNullOrEmpty(text)) {
                    continue;
                }

                emptyInfoLabels[i].text = text;
            }
        }
        
        public void SetContentActive(bool active) {
            if (HasBeenDiscarded || _tweens == null) return;
            
            for (int i = 0; i < _targetGroups.Length; i++) {
                if(_targetGroups[i] == null) continue;
                
                CanvasGroup group = _targetGroups[i];
                _tweens[i] = group.DOFade(active ? 1 : EmptyContentAlpha, UITweens.FadeDuration).SetUpdate(true);
                group.interactable = active;
                group.blocksRaycasts = active;
            }

            emptyInfo.SetActiveOptimized(!active);
            _tweens[^1] = emptyInfoGroup.DOFade(!active ? 1 : 0, UITweens.FadeDuration).SetUpdate(true);
        }

        protected override void OnDiscard() {
            for (int i = 0; i < _tweens.Length; i++) {
                UITweens.DiscardTween(ref _tweens[i]);
            }
            
            _tweens = null;
        }
    }
}
