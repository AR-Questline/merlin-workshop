using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    public class VCCharacterPointsAvailable : ViewComponent<Hero> {
        const float FadeDuration = 1f;
        const float FadeStayDuration = 60f;

        bool _bonfireAcquired;
        IEventListener _bonfireAcquiredListener;
        bool _whispersVisible;
        Sequence _fadeTween;

        [SerializeField] TextMeshProUGUI pointsText;
        [SerializeField] TextMeshProUGUI memoriesText;
        [SerializeField] TextMeshProUGUI goToBonfireText;
        [SerializeField] CanvasGroup canvasGroup;

        static StatType BaseStatType => CharacterStatType.BaseStatPoints;
        static StatType TalentStatType => CharacterStatType.TalentPoints;
        static StatType ShardsStatType => HeroStatType.WyrdMemoryShards;
        static StatType WyrdWhispersStatType => HeroStatType.WyrdWhispers;
        bool HasBonfire => Target.HeroItems.HasItem(CommonReferences.Get.Bonfire.ToRuntimeData(Target));

        protected override void OnAttach() {
            canvasGroup.alpha = 0;
            pointsText.SetText(LocTerms.PointsAvailable.Translate());
            memoriesText.SetText(LocTerms.ArthurMemories.Translate());
            goToBonfireText.SetText(LocTerms.GoToBonfire.Translate());
            InitListeners();
        }

        void InitListeners() {
            Target.ListenTo(Stat.Events.StatChanged(BaseStatType), _ => UpdateVisual(), this);
            Target.ListenTo(Stat.Events.StatChanged(TalentStatType), _ => UpdateVisual(), this);
            Target.ListenTo(Stat.Events.StatChanged(ShardsStatType), _ => UpdateVisual(), this);
            Target.ListenTo(Stat.Events.StatChangedBy(WyrdWhispersStatType), TryShowWhispers, this);
            Target.ListenTo(WyrdRepellingFireplaceUI.Events.TalkedWithArthurAtCamp, _ => HideWhispers(), this);
            Target.ListenTo(ICharacter.Events.CombatExited, _ => UpdateVisual(), this);
            Target.ListenTo(Hero.Events.ArthurMemoryReminded, _ => UpdateVisual(), this);
            Target.ListenTo(StoryFlags.Events.UniqueFlagChanged(TutorialMaster.BonfireTutorialFlag), _ => UpdateVisual(), this);
            
            Target.AfterFullyInitialized(() => {
                _bonfireAcquired = HasBonfire;
                if (!_bonfireAcquired) {
                    _bonfireAcquiredListener = Target.HeroItems.ListenTo(ICharacterInventory.Events.PickedUpItem, item => {
                        _bonfireAcquired = item.Template == CommonReferences.Get.Bonfire.ItemTemplate(Target);
                        if (_bonfireAcquired) {
                            World.EventSystem.RemoveListener(_bonfireAcquiredListener);
                            _bonfireAcquiredListener = null;
                        }
                    }, this);
                }
                UpdateVisual();
            });
        }

        void TryShowWhispers(Stat.StatChange change) {
            if (change.value > 0 && GetStatValue(WyrdWhispersStatType) > 0 && !_whispersVisible) {
                _whispersVisible = true;
                UpdateVisual();
            }
        }

        public void HideWhispers() {
            if (_whispersVisible) {
                _whispersVisible = false;
                UpdateVisual();
            }
        }

        void UpdateVisual() {
            if (!_bonfireAcquired) {
                _fadeTween.Kill();
                canvasGroup.alpha = 0;
                canvasGroup.TrySetActiveOptimized(false);
                return;
            }
            
            bool pointsVisible = GetStatValue(BaseStatType) > 0 ||
                                 GetStatValue(TalentStatType) > 0 ||
                                 GetStatValue(ShardsStatType) > 0;
            
            pointsText.transform.parent.gameObject.SetActive(pointsVisible);
            memoriesText.transform.parent.gameObject.SetActive(_whispersVisible);
            
            if (!pointsVisible && !_whispersVisible) {
                _fadeTween.Kill(true);
                return;
            }

            canvasGroup.TrySetActiveOptimized(true);
            _fadeTween.Kill();
            _fadeTween = DOTween.Sequence()
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, FadeDuration))
                .AppendInterval(FadeStayDuration)
                .Append(canvasGroup.DOFade(0f, FadeDuration))
                .OnComplete(() => canvasGroup.TrySetActiveOptimized(false));
        }

        static int GetStatValue(StatType statType) {
            return statType == null ? 0 : Hero.Current.Stat(statType).ModifiedInt;
        }

        protected override void OnDestroy() {
            _fadeTween.Kill();
        }
    }
}