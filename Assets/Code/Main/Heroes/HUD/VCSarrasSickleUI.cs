using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD {
    /// <summary>
    /// The logic assumes that hero can have one and only one sickle in the inventory and it cannot be dropped
    /// </summary>
    public class VCSarrasSickleUI : ViewComponent<Hero> {
        [SerializeField] GameObject sickleObject;
        [SerializeField] Image sickleChargeFill;
        [SerializeField] Image sickleChargeFillGlow;
        [SerializeField] TextMeshProUGUI sickleCharges;
        [SerializeField] float fillChargeDuration = 1f;
        [SerializeField] float feedbackDuration = 0.25f;
        [SerializeField] float scaleFeedback = 1.05f;
        [SerializeField] float sickleChargesScaleFeedback = 1.2f;
        [SerializeField] float rotationFeedback = 1.25f;

        Sequence _chargeSequence;
        IEventListener _pickedUpEvent;
        Transform _sickleTransform;
        int _currentSickleCharges;
        bool _hasSickleItem;
        bool _isSarrasScene;
        
        bool IsVisible => _hasSickleItem && _isSarrasScene;
        
        protected override void OnAttach() {
            Target.AfterFullyInitialized(AfterFullyInitialized);
        }

        void AfterFullyInitialized() {
            var heroItems = Target.HeroItems;

            _sickleTransform = sickleObject.transform;
            sickleObject.SetActiveOptimized(false);
            Item sickle = heroItems.Items.FirstOrDefault(item => item.IsSickle);
            TrySetupSickle(sickle);
            
            if (!_hasSickleItem) {
                _pickedUpEvent = heroItems.ListenTo(ICharacterInventory.Events.PickedUpItem, TrySetupSickle, this);
            }
            
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, OnSceneLoaded);
            OnSceneLoaded();
        }

        void OnSceneLoaded() {
            SceneReference mainSceneRef = Services.Get<SceneService>().MainSceneRef;
            _isSarrasScene = World.Services.Get<CommonReferences>().SceneConfigs.GetSceneConfig(mainSceneRef).IsSarrasDlcScene;
            sickleObject.SetActiveOptimized(IsVisible);
        }

        void TrySetupSickle(Item item) {
            if (item is not {IsSickle: true}) {
                return;
            }

            _hasSickleItem = true;
            var sickleTool = item.Element<SarrasSickle>();
            sickleObject.SetActiveOptimized(IsVisible);
            sickleTool.ListenTo(SarrasSickle.Events.SickleStateUpdated, OnSickleStateUpdated, this);
            OnSickleStateUpdated(sickleTool);
            World.EventSystem.TryDisposeListener(ref _pickedUpEvent);
        }

        void OnSickleStateUpdated(SarrasSickle sickleTool) {
            float chargeProgress = sickleTool.ChargeProgress;
            float fraction = chargeProgress - math.floor(chargeProgress);
            chargeProgress = fraction >= 0.99f ? 1f : chargeProgress;
            
            _chargeSequence.Kill();
            _chargeSequence = DOTween.Sequence().SetUpdate(true)
                .Append(_sickleTransform.DOShakeRotation(feedbackDuration, new Vector3(0, 0, rotationFeedback), 2))
                .Join(_sickleTransform.DOScale(scaleFeedback, feedbackDuration).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutCubic));

            if (_currentSickleCharges < sickleTool.Charges) {
                HandleSickleCharges(sickleTool);
            } else {
                _chargeSequence.JoinCallback(() =>  SetupSickleCharges(sickleTool));
            }
            
            _chargeSequence
                .Append(sickleChargeFill.DOFillAmount(chargeProgress, fillChargeDuration).SetEase(Ease.OutExpo))
                .Join(sickleChargeFillGlow.DOFillAmount(chargeProgress, fillChargeDuration).SetEase(Ease.OutExpo));
        }

        void HandleSickleCharges(SarrasSickle sickleTool) {
            _chargeSequence
                .Append(sickleChargeFill.DOFillAmount(1f, fillChargeDuration).SetEase(Ease.OutExpo))
                .Join(sickleChargeFillGlow.DOFillAmount(1f, fillChargeDuration).SetEase(Ease.OutExpo))
                .AppendCallback(() => {
                    sickleChargeFill.fillClockwise = false;
                    sickleChargeFillGlow.fillClockwise = false;
                })
                .Join(sickleChargeFill.DOFillAmount(0f, fillChargeDuration).SetEase(Ease.OutExpo))
                .Join(sickleChargeFillGlow.DOFillAmount(0f, fillChargeDuration).SetEase(Ease.OutExpo))
                .JoinCallback(() => SetupSickleCharges(sickleTool))
                // .Join(sickleCharges.DOScale(sickleChargesScaleFeedback, feedbackDuration).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutCubic))
                .AppendCallback(() => {
                    sickleChargeFill.fillClockwise = true;
                    sickleChargeFillGlow.fillClockwise = true;
                });
        }

        void SetupSickleCharges(SarrasSickle sickleTool) {
            if (sickleTool.Charges > _currentSickleCharges) {
                FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.SarrasSickleChargedSound);
            }
            _currentSickleCharges = sickleTool.Charges;
            sickleCharges.SetText($"{_currentSickleCharges}/{sickleTool.MaxCharges}");
        }

        protected override void OnDiscard() {
            _chargeSequence.Kill();
            World.EventSystem.TryDisposeListener(ref _pickedUpEvent);
        }
    }
}