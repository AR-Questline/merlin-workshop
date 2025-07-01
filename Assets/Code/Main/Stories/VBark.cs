using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.Main.UI.Stickers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Stories {
    [UsesPrefab("Story/VBark")]
    public class VBark : View<Story>, IVStoryPanel {
        const float DefaultHostHeight = 3f;
        const float AdditionalCharacterHeight = 0.75f;
        const float MaxDistanceForBarkVisibility = 30f;
        const float BarkFadeDistanceStart = 20f;
        
        [SerializeField] GameObject textPrefab;
        [SerializeField] Transform parent;
        [SerializeField] CanvasGroup parentCanvasGroup;
        
        Sticker _sticker;
        bool _updateAttached;
        
        public override Transform DetermineHost() {
            _sticker = Services.Get<MapStickerUI>().StickTo(Target.FocusedLocation, new StickerPositioning {
                pivot = new Vector2(0.5f, 0.5f),
                worldOffset = new Vector3(0, DetermineHostHeight(Target.FocusedLocation), 0),
                underneath = false
            });

            return _sticker;
        }
        
        protected override void OnInitialize() {
            Target.AddElement(new StoryOnTop(false));
            
            SubtitlesSetting subtitlesSetting = World.Only<SubtitlesSetting>();
            subtitlesSetting.ListenTo(Setting.Events.SettingChanged, SubtitlesSettingChanged, this);
            SubtitlesSettingChanged(subtitlesSetting);
        }

        void SubtitlesSettingChanged(Model model) {
            SubtitlesSetting subtitlesSetting = (SubtitlesSetting)model;
            if (subtitlesSetting.EnviroSubsEnabled && !_updateAttached) {
                Target.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
                _updateAttached = true;
            } else if (!subtitlesSetting.EnviroSubsEnabled && _updateAttached) {
                Target.GetTimeDependent()?.WithoutUpdate(OnUpdate);
                _updateAttached = false;
                parentCanvasGroup.alpha = 0;
            }
        }

        void UpdateStickerPosition(Location focusedLocation) {
            focusedLocation ??= Target.FocusedLocation;
            _sticker.anchor = focusedLocation.MainView.transform;
            _sticker.positioning.worldOffset = new Vector3(0, DetermineHostHeight(focusedLocation), 0);
        }

        void OnUpdate(float deltaTime) {
            Hero hero = Hero.Current;
            if (_sticker == null || hero == null) {
                return;
            }
            
            float3 directionToHero = _sticker.anchor.position - hero.Coords;
            float3 heroForward = hero.Rotation * math.forward();
            bool inViewCone = AIUtils.IsInHeroViewCone(math.dot(heroForward, math.normalize(directionToHero)));

            if (!inViewCone) {
                parentCanvasGroup.alpha = 0;
                return;
            }
            
            float distanceToHero = math.length(directionToHero);
            float alpha = 1;
            if (distanceToHero > BarkFadeDistanceStart) {
                alpha = distanceToHero.Remap(BarkFadeDistanceStart, MaxDistanceForBarkVisibility, 1, 0, true);
            }
            
            parentCanvasGroup.alpha = alpha;
        }

        public void Clear() {
            GameObjects.DestroyAllChildrenSafely(parent, textPrefab);
        }

        public void ClearText() {
            Clear();
        }

        public void ShowText(TextConfig textConfig) {
            Clear();
            UpdateStickerPosition(textConfig.Location);
            
            GameObject obj = Instantiate(textPrefab, parent, false);
            obj.SetActive(true);
            
            TextMeshProUGUI component = obj.GetComponentInChildren<TextMeshProUGUI>();
            component.text = StoryText.Format(Target, textConfig.Text, textConfig.Style);
        }

        public void SetArt(SpriteReference art) { }
        public void SetTitle(string title) { }
        public void ShowLastChoice(string textToDisplay, string iconName) { }
        public void ShowChange(Stat stat, int change) { }
        public void OfferChoice(ChoiceConfig choiceConfig) { }
        public void ToggleBg(bool bgEnabled) { }
        public void ToggleViewBackground(bool enabled) { }
        public void TogglePrompts(bool promptsEnabled) { }
        public Transform LastChoicesGroup() {
            throw new System.NotImplementedException();
        }
        public Transform StatsPreviewGroup() {
            throw new System.NotImplementedException();
        }
        public void SpawnContent(DynamicContent dynamicContent) { }
        public void LockChoiceAssetGate() { }
        public void UnlockChoiceAssetGate() { }

        // === Discarding
        protected override IBackgroundTask OnDiscard() {
            Target.GetTimeDependent()?.WithoutUpdate(OnUpdate);
            return base.OnDiscard();
        }
        
        // === Helpers
        float DetermineHostHeight(Location focusedLocation) {
            if (focusedLocation != null && focusedLocation.TryGetElement(out ICharacter character)) {
                return character.Height + AdditionalCharacterHeight;
            }
            return DefaultHostHeight;
        }
    }
}