using System.Text;
using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.CustomControls;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.SpecialItem {
    public class PSpecialItemNotification : PAdvancedNotification<SpecialItemNotification, PSpecialItemNotificationData>, IPromptListener, IPresenterWithAccessibilityBackground {
        const int CharactersCount = 200;
        
        ItemRead _itemRead;
        Prompt _readPrompt;
        BetterOutlinedLabel _itemName;
        BetterOutlinedLabel _itemInfo;
        VisualElement _itemIcon;
        VisualPresenterKeyIcon _keyIcon;
        bool _isReadable;
        StringBuilder _stringBuilder = new();
        
        VisualElement IPresenterWithAccessibilityBackground.Host => Content;
        
        public PSpecialItemNotification(VisualElement parent) : base(parent) { }
        
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _itemName = contentRoot.Q<BetterOutlinedLabel>("item-name");
            _itemInfo = contentRoot.Q<BetterOutlinedLabel>("item-info");
            _itemIcon = contentRoot.Q<VisualElement>("item-icon");
            _keyIcon = new VisualPresenterKeyIcon(contentRoot.Q<VisualElement>("key-icon"));
            
            var prompts = TargetModel.AddElement(new Prompts(null));
            _readPrompt = Prompt.Tap(KeyBindings.UI.HUD.OpenInventoryItemRead, LocTerms.Read.Translate(), RunItemAction);
            
            World.BindPresenter(TargetModel, _keyIcon, () => {
                _keyIcon.Setup(_readPrompt);
                prompts.AddPrompt(_readPrompt, TargetModel, this, false, false);
            });
        }
        
        // --- IPromptListener
        public void SetName(string name) { }
        public void SetActive(bool active) { }

        public void SetVisible(bool visible) {
            if (visible) {
                _keyIcon.Content.ShowAndSetActiveOptimized();
            } else {
                _keyIcon.Content.HideAndSetActiveOptimized();
            }
        }

        protected override PSpecialItemNotificationData GetNotificationData() {
            return PresenterDataProvider.specialItemNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(SpecialItemNotification notification) {
            return CommonReferences.Get.AudioConfig.SpecialItemAudio;
        }

        protected override void OnBeforeShow(SpecialItemNotification itemNotification) {
            Content.transform.position = Vector3.right * Data.InitialXOffset;
            
            _itemName.SetTextColor(itemNotification.item.Quality.NameColor);
            _itemName.text = itemNotification.displayName;
            _isReadable = itemNotification.isReadable;
            _itemRead = _isReadable ? itemNotification.item.Element<ItemRead>() : null;
            _itemInfo.text = HandleDescription(itemNotification);

            if (itemNotification.itemIcon is { IsSet: true } iconRef) {
                iconRef.RegisterAndSetup(this, _itemIcon);
            }
            
            _readPrompt.SetupState(_isReadable, _isReadable);
        }

        protected override Sequence ShowSequence() {
            float fadeDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastFade : Data.FadeDuration;
            float moveDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastMove : Data.MoveDuration;
            float visibilityDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastVisibility : Data.VisibilityDuration;
            
            return DOTween.Sequence().SetUpdate(IsIndependentUpdate).SetDelay(Data.ShowDelayDuration)
                .Append(Content.DoFade(1f, fadeDuration))
                .Join(Content.DoMove(Vector3.zero, moveDuration))
                .AppendInterval(visibilityDuration)
                .Append(Content.DoFade(0f, fadeDuration));
        }

        protected override void OnAfterHide() {
            _itemRead = null;
            _readPrompt.SetupState(false, false);
            ReleaseReleasable();
        }

        string HandleDescription(SpecialItemNotification itemNotification) {
            _stringBuilder.Clear();
            _stringBuilder.Append(
                _isReadable
                    ? _itemRead.StoryText.Trim()
                    : itemNotification.item.DescriptionFor(Hero.Current).Trim()
            );

            _stringBuilder.Length = Mathf.Min(CharactersCount, _stringBuilder.Length);
            _stringBuilder
                .Replace("<b>", "")
                .Replace("</b>", "")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\t", " ");
            return _stringBuilder.ToString();
        }
        
        void RunItemAction() {
            _itemRead?.Submit();
        }
    }
}