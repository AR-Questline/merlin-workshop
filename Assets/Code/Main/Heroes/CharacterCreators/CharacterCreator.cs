using System;
using System.Linq;
using Awaken.TG.Graphics;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterCreators.Data;
using Awaken.TG.Main.Heroes.CharacterCreators.Difficulty;
using Awaken.TG.Main.Heroes.CharacterCreators.Parts;
using Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.HeroCreator;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators {
    public partial class CharacterCreator : Model, CharacterCreatorTabs.ITabParent, IPromptHost, IUIStateSource {
        public sealed override bool IsNotSaved => true;

        public static bool applyingBuildPreset;
        
        public override Domain DefaultDomain => Domain.Gameplay;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden).WithPauseTime();

        public const int BlendShapesCount = HeadShapes + BodyShapes + GenderShapes;
        const int HeadShapes = CharacterHeadShapes.Count;
        const int BodyShapes = 3;
        const int GenderShapes = 1;

        readonly CharacterCreatorTemplate _template;
        readonly BodyFeatures _features;
        [UnityEngine.Scripting.Preserve] readonly MapScene _mapScene;

        readonly BlendShape[] _shapes = new BlendShape[BlendShapesCount];

        bool _transitToCameraOnDiscard;
        bool _changingLock;
        int? _presetIndex;
        CharacterPresetData _presetData;

        public CharacterCreatorTemplate Template => _template;
        public Prompts Prompts => Element<Prompts>();
        public HeroRenderer HeroRenderer => Element<HeroRenderer>();
        public BodyFeatures DisplayFeatures => HeroRenderer.BodyFeatures();

        public Transform TabButtonsHost => View.TabButtonsHost;
        public Transform ContentHost => View.ContentHost;
        public Transform PromptsHost => View.PromptsHost;
        
        public CharacterCreatorTabType CurrentType { get; set; }
        public Tabs<CharacterCreator, VCharacterCreatorTabs, CharacterCreatorTabType, ICharacterCreatorTab> TabsController { get; set; }

        public CharacterBuildPreset BuildPreset { get; }
        
        VCharacterCreator View { get; set; }

        Prompt _acceptPopupPrompt;
        Prompt _cancelPopupPrompt;
        Prompt _gridSelectPrompt;
        Prompt _increasePrompt;
        Prompt _decreasePrompt;

        public ICCPromptSource PromptInvoker { get; private set; }

        public new static class Events {
            public static readonly Event<CharacterCreator, CharacterCreator> AppearanceChanged = new(nameof(AppearanceChanged));
            public static readonly Event<CharacterCreator, CharacterCreator> CharacterCreated = new(nameof(CharacterCreated));
        }
        
        public CharacterCreator(BodyFeatures features, MapScene mapScene, CharacterBuildPreset preset, CharacterCreatorTemplate template = null, bool transitToCameraOnDiscard = false) {
            _template = template ?? Services.Get<TemplatesProvider>().GetAllOfType<CharacterCreatorTemplate>().First();
            _features = features;
            _mapScene = mapScene;
            BuildPreset = preset;
            _transitToCameraOnDiscard = transitToCameraOnDiscard;

            World.EventSystem.LimitedListenTo(
                EventSelector.AnySource,
                SceneLifetimeEvents.Events.AfterSceneStoriesExecuted,
                Hero.Current,
                _ => {
                    applyingBuildPreset = true;
                    BuildPreset.Apply();
                    applyingBuildPreset = false;
                    AutoSaveWhenMapIsInteractive().Forget();
                },
                1);
        }

        protected override void OnInitialize() {
            World.Services.Get<FpsLimiter>().RegisterLimit(this, FpsLimiter.DefaultUIFpsLimit);
        }

        protected override void OnFullyInitialized() {
            AddElement(new HeroRenderer(_features, false));
            View = World.SpawnView<VCharacterCreator>(this, true);
            SetPresetIndex(0);
            
            World.SpawnView<VRotator>(HeroRenderer, false, true, View.HeroRender.Slot);
            
            AddElement(new Prompts(this));
            AddElement(new CharacterCreatorTabs());
            InitPrompts();
        }

        void InitPrompts() {
            _gridSelectPrompt = Prompt.Tap(KeyBindings.UI.Items.SelectItem, LocTerms.Select.Translate(), () => (PromptInvoker as CCGridSelectOption)?.Select(), controllers: ControlSchemeFlag.Gamepad);
            _decreasePrompt = Prompt.Tap(KeyBindings.Gamepad.DPad_Left, LocTerms.SettingsPreviousOption.Translate(), () => (PromptInvoker as CCSlider)?.Decrease(), controllers: ControlSchemeFlag.Gamepad);
            _increasePrompt = Prompt.Tap(KeyBindings.Gamepad.DPad_Right, LocTerms.SettingsNextOption.Translate(), () => (PromptInvoker as CCSlider)?.Increase(), controllers: ControlSchemeFlag.Gamepad);

            Prompts.AddPrompt(_gridSelectPrompt, this, visible: false);
            Prompts.AddPrompt(_decreasePrompt, this, visible: false);
            Prompts.AddPrompt(_increasePrompt, this, visible: false);
        }

        void RefreshPrompts() {
            _gridSelectPrompt.SetVisible(PromptInvoker is CCGridSelectOption);
            _decreasePrompt.SetVisible(PromptInvoker is CCSlider);
            _increasePrompt.SetVisible(PromptInvoker is CCSlider);
        }

        public void SetPromptInvoker(ICCPromptSource invoker) {
            if (PromptInvoker == invoker) return;
            PromptInvoker = invoker;
            RefreshPrompts();
        }

        public void ResetPrompts(bool refresh = false) {
            PromptInvoker = null;

            if (refresh) {
                RefreshPrompts();
            }
        }

        async UniTaskVoid AutoSaveWhenMapIsInteractive() {
            var uiStateStack = UIStateStack.Instance;
            if (await AsyncUtil.WaitUntil(Hero.Current, () => uiStateStack.State.IsMapInteractive)) {
                if (LoadSave.Get.CanAutoSave()) {
                    LoadSave.Get.Save(SaveSlot.GetAutoSave());
                }
            }
        }

        public Gender GetGender() {
            return CharacterCreatorTemplate.GenderOfIndex(GetGenderIndex());
        }

        public void SaveAndClose() {
            Reference<PopupUI> popup = new();
            
            if (string.IsNullOrWhiteSpace(Hero.Current.Name)) {
                DisplayPopup(LocTerms.CharacterCreatorEmptyName.Translate());
                return;
            }
            
            if (PlatformUtils.IsPlatformWithLanguageRestrictions() && ForbiddenTerms.terms.Any(t => Hero.Current.Name.ToLower().Contains(t, StringComparison.InvariantCultureIgnoreCase))) {
                DisplayPopup(LocTerms.CharacterCreatorForbiddenName.Translate());
                return;
            }

            DisplayPopup(LocTerms.CharacterCreatorApplyChanges.Translate(), () => ChooseDifficulty(popup));
            return;

            void DisplayPopup(string contentText, Action confirmAction = null) {
                confirmAction ??= () => popup.item.Discard();
                _acceptPopupPrompt = PopupUI.AcceptTapPrompt(confirmAction);
                _cancelPopupPrompt = PopupUI.CancelTapPrompt(() => popup.item.Discard());
                
                popup.item = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                    contentText,
                    _acceptPopupPrompt,
                    _cancelPopupPrompt,
                    LocTerms.Confirm.Translate()
                );
            }
        }
        
        // choose difficulty as a fullscreen view insert between save Creator and scene start
        // assume that player can't go back to the character creator
        void ChooseDifficulty(Reference<PopupUI> popup) {
            // hide confirm popup
            _acceptPopupPrompt.SetActive(false);
            _cancelPopupPrompt.SetActive(false);
            popup.item.Discard();
            
            // disable all Character Creator prompts to avoid any wrong interaction with ChooseDifficulty UI
            foreach (var prompt in Prompts.Elements<Prompt>().GetManagedEnumerator()) {
                prompt.SetActive(false);
            }
            
            // disable view to proper auto navigation
            View.TrySetActiveOptimized(false);
            
            var chooseDifficulty = World.Add(new ChooseDifficulty());
            chooseDifficulty.ListenTo(Difficulty.ChooseDifficulty.Events.DifficultyChooseConfirmed, model => ActualSaveAndClose(model).Forget(), this);
        }

        async UniTaskVoid ActualSaveAndClose(ChooseDifficulty chooseDifficulty) {
            Gender gender = GetGender();
            SetAudioContainer(gender);
            Hero.LoadGenderSoundBanks(gender);
            CommonReferences.RefreshLocsGender(gender);
            
            // DO NOT REMOVE, the blackscreen is used to mask transition to scene start. init story should fade the transition out if necessary. This is needed for animations being able to fade in the scene.
            var transition = World.Services.Get<TransitionService>();
            await transition.ToBlack(TransitionService.QuickFadeIn);
            
            HeroRenderer.RemoveClothesSpawnedFromPrefab();
            
            _features.CopyFrom(HeroRenderer.BodyFeatures());
            if (gender == Gender.Female) {
                _features.Beard = null;
            }

            await Hero.Current.VHeroController.TryReloadBodyWithEquips();
            
            this.Trigger(Events.CharacterCreated, this);
                
            if (_transitToCameraOnDiscard) {
                World.Services.Get<TransitionService>().ToCamera(2f).Forget();
            }
            
            if (await AsyncUtil.WaitWhile(this, () => HeroRenderer.IsLoading)) {
                Discard();
                chooseDifficulty.Discard();
            }
        }

        public void SetPresetIndex(int index) {
            if (_changingLock || (_presetIndex == index && !_presetData.random)) return;
            _presetIndex = index;
            _presetData = _template.Preset(index).data;
            
            if (_presetData.random) {
                _presetData.Randomize(_template);
            }
            
            RefreshPreset().Forget();
        }

        public void RandomizePreset() {
            if (_changingLock) return;
            _presetData.Randomize(_template);
            RefreshPreset().Forget();
        }

        public void SetGenderIndex(int index) {
            SetIndex(index, ref _presetData.gender, () => {
                CharacterPresetData preset = new() {
                    gender = _presetData.gender,
                    skinColor = _presetData.skinColor,
                    headShape = _presetData.headShape,
                    faceSkin = 0,
                    hair = 1,
                    beard = _presetData.beard,
                    hairColor = _presetData.hairColor,
                    beardColor = _presetData.beardColor,
                    bodyNormals = _presetData.bodyNormals,
                    eyeColor = _presetData.eyeColor,
                    eyebrow = 0,
                    bodyTattoo = _presetData.bodyTattoo,
                    bodyTattooColor = _presetData.bodyTattooColor,
                    faceTattoo = _presetData.faceTattoo,
                    faceTattooColor = _presetData.faceTattooColor,
                };
                
                _presetData = preset;
                RefreshPreset().Forget();
            });
        }

        public void SetHeadShapeIndex(int index) {
            SetIndex(index, ref _presetData.headShape, RefreshShapes);
        }
        
        public void SetFaceSkinIndex(int index) {
            SetIndex(index, ref _presetData.faceSkin,
                () => {
                    AsyncRefreshWithMutableLock(DelayRefreshFaceSkin()).Forget();
                });
        }

        public void SetSkinColorIndex(int index) {
            SetIndex(index, ref _presetData.skinColor, RefreshSkinColor);
        }

        public void SetHairIndex(int index) {
            SetIndex(index, ref _presetData.hair, () => {
                AsyncRefreshWithMutableLock(DelayRefreshHair()).Forget();
            });
        }

        public void SetBeardIndex(int index) {
            SetIndex(index, ref _presetData.beard, () => {
                AsyncRefreshWithMutableLock(DelayRefreshBeard()).Forget();
            });
        }

        public void SetHairColorIndex(int index) {
            SetIndex(index, ref _presetData.hairColor, () => {
                RefreshHairColor();
                RefreshEyebrow();
            });
        }
        
        public void SetBeardColorIndex(int index) {
            SetIndex(index, ref _presetData.beardColor, RefreshBeardColor);
        }

        public void SetBodyNormalsIndex(int index) {
            SetIndex(index, ref _presetData.bodyNormals, RefreshBodyNormals);
        }
        
        public void SetEyeColorIndex(int index) {
            SetIndex(index, ref _presetData.eyeColor, RefreshEyeColor);
        }
        
        public void SetEyebrowIndex(int index) {
            SetIndex(index, ref _presetData.eyebrow, RefreshEyebrow);
        }
        
        public void SetBodyTattooIndex(int index) {
            SetIndex(index, ref _presetData.bodyTattoo, RefreshBodyTattoo);
        }
        
        public void SetBodyTattooColorIndex(int index) {
            SetIndex(index, ref _presetData.bodyTattooColor, RefreshBodyTattoo);
        }
        
        public void SetFaceTattooIndex(int index) {
            SetIndex(index, ref _presetData.faceTattoo, RefreshFaceTattoo);
        }   
        
        public void SetFaceTattooColorIndex(int index) {
            SetIndex(index, ref _presetData.faceTattooColor, RefreshFaceTattoo);
        }
        
        void SetIndex(int index, ref int field, Action refresh) {
            if (_changingLock || field == index) return;
            _presetIndex = null;
            field = index;
            refresh();
            RefreshAppearance();
        }

        public int GetPresetIndex() => _presetIndex ?? -1;
        public int GetGenderIndex() => _presetData.gender;
        public int GetSkinColorIndex() => _presetData.skinColor;
        public int GetHeadShapeIndex() => _presetData.headShape;
        public int GetFaceSkinIndex() => _presetData.faceSkin;
        public int GetHairIndex() => _presetData.hair;
        public int GetBeardIndex() => _presetData.beard;
        public int GetHairColorIndex() => _presetData.hairColor;
        public int GetBeardColorIndex() => _presetData.beardColor;
        public int GetBodyNormalsIndex() => _presetData.bodyNormals;
        public int GetEyeColorIndex() => _presetData.eyeColor;
        public int GetEyebrowIndex() => _presetData.eyebrow;
        public int GetBodyTattooIndex() => _presetData.bodyTattoo;
        public int GetBodyTattooColorIndex() => _presetData.bodyTattooColor;
        public int GetFaceTattooIndex() => _presetData.faceTattoo;
        public int GetFaceTattooColorIndex() => _presetData.faceTattooColor;

        async UniTaskVoid RefreshPreset() {
            HeroRenderer.FadeForegroundQuad(1f, 0.4f, 0.1f);
            _changingLock = true;
            var features = DisplayFeatures;
            
            try {
                await RefreshGenderAsync();
                RefreshShapes();

                features.MutableSetterAsyncLock = true;
                {
                    UniTask[] tasks = new UniTask[3];
                    tasks[0] = DelayRefreshFaceSkin();
                    tasks[1] = DelayRefreshHair();
                    tasks[2] = DelayRefreshBeard();
                    await UniTask.WhenAll(tasks);
                }
                features.MutableSetterAsyncLock = false;

                RefreshSkinColor();
                RefreshHairColor();
                RefreshBeardColor();
                RefreshBodyNormals();
                RefreshEyebrow();
                RefreshEyeColor();
                RefreshBodyTattoo();
                RefreshFaceTattoo();
                HeroRenderer.ShowBody();
            } finally {
                _changingLock = false;
                features.MutableSetterAsyncLock = false;
            }
            RefreshAppearance();
            HeroRenderer.FadeForegroundQuad(0f, 0.4f, 0.1f);
        }
        
        async UniTaskVoid AsyncRefreshWithMutableLock(UniTask refresh) {
            var features = DisplayFeatures;
            features.MutableSetterAsyncLock = true;
            await refresh;
            features.MutableSetterAsyncLock = false;
        }
        
        async UniTask RefreshGenderAsync() {
            var gender = CharacterCreatorTemplate.GenderOfIndex(_presetData.gender);
            if (gender == DisplayFeatures.Gender) {
                Element<HeroRenderer>().HideBody();
                return;
            }
            DisplayFeatures.Gender = gender;
            await Element<HeroRenderer>().RefreshGender();
        }

        void SetAudioContainer(Gender gender) {
            ICharacter character = Element<HeroRenderer>().Character;
            if (character != null) {
                character.RemoveElementsOfType<AliveAudio>();
                var aliveAudio = new HeroAliveAudio(View.AudioContainer(gender));
                character.AddElement(aliveAudio);
            }
        }

        void RefreshShapes() {
            _shapes[0] = _presetData.Gender(_template);
            _presetData.HeadPreset(_template).FillShapesContinuously(_shapes, 4);
            DisplayFeatures.ShapesFeature = new BlendShapesFeature(_shapes);
        }
        
        async UniTask DelayRefreshFaceSkin() {
            var features = DisplayFeatures;
            features.FaceSkin = await features.ChangeMutableFeatureAsync(features.FaceSkin, new FaceSkinTexturesFeature(_presetData.FaceSkin(_template)));
        }

        void RefreshSkinColor() {
            DisplayFeatures.SkinColor = new SkinColorFeature(_presetData.SkinColor(_template).tint);
        }

        async UniTask DelayRefreshHair() {
            BodyFeatures features = DisplayFeatures;
            var hairAsset = _presetData.Hair(_template).Asset;

            features.Hair = await features.ChangeMutableFeatureAsync(features.Hair, hairAsset != null ? new MeshFeature(hairAsset) : null);
            features.ShapesFeature.Spawn();
        }
        
        async UniTask DelayRefreshBeard() {
            BodyFeatures features = DisplayFeatures;
            var beardAsset = _presetData.Beard(_template).Asset;
            
            features.Beard = await features.ChangeMutableFeatureAsync(features.Beard, beardAsset != null ? new MeshFeature(beardAsset) : null);
            features.ShapesFeature.Spawn();
        }

        void RefreshHairColor() {
            var hairColor = _presetData.HairColor(_template);
            DisplayFeatures.ChangeHairColor(hairColor.config);
        }
        
        void RefreshBeardColor() {
            var beardColor = _presetData.BeardColor(_template);
            DisplayFeatures.ChangeBeardColor(beardColor.config);
        }

        void RefreshBodyNormals() {
            var bodyNormals = _presetData.BodyNormal(_template);
            DisplayFeatures.Normals = new BodyNormalFeature(bodyNormals);
        }
        
        void RefreshEyeColor() {
            var color = _presetData.EyeColor(_template);
            DisplayFeatures.Eyes = new EyeColorFeature(color.tint);
        }
        
        void RefreshEyebrow() {
            var eyebrow = _presetData.Eyebrow(_template);
            DisplayFeatures.Eyebrows = new EyebrowFeature(eyebrow.Asset);
        }
        
        void RefreshBodyTattoo() {
            var tattoo = _presetData.BodyTattoo(_template);
            var tattooColor = _presetData.BodyTattooColor(_template);

            if (tattoo.data != null) {
                var config = new TattooConfig(tattoo.data, tattooColor.tint);
                DisplayFeatures.BodyTattoo = new BodyTattooFeature(config);
            } else {
                DisplayFeatures.BodyTattoo = null;
            }
        }
        
        void RefreshFaceTattoo() {
            var tattoo = _presetData.FaceTattoo(_template);
            var tattooColor = _presetData.FaceTattooColor(_template);

            if (tattoo.data != null) {
                var config = new TattooConfig(tattoo.data, tattooColor.tint);
                DisplayFeatures.FaceTattoo = new FaceTattooFeature(config);
            } else {
                DisplayFeatures.FaceTattoo = null;
            }
        }

        void RefreshAppearance() {
            this.Trigger(Events.AppearanceChanged, this);
        }
    }
}