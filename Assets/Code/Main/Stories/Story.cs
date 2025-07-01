using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Choices;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Debugging;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.MVC.Utils;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Awaken.TG.Main.Stories {
    /// <summary>
    /// Represents a running Story - an interaction between a hero and a location or threat.
    /// The whole interaction is based on a StoryTemplate to go through.
    /// </summary>
    public partial class Story : Model, ITagged, IUIStateSource, IUIAware {
        public const int MaxFramesToWaitForVOBanksLoad = 30;

        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;  
        
        // === State
        public bool InvolveAI { get; private set; }
        public bool ManualInterruptRequested { get; set; }
        public bool WasInterrupted { get; private set; }
        public bool IsEnding { get; private set; }
        public bool IsDebugPaused { get; set; }
        
        public Hero Hero {
            get => _heroRef.Get();
            set => _heroRef = value ? new WeakModelRef<Hero>(value) : WeakModelRef<Hero>.Empty;
        }

        public Location OwnerLocation { get; private set; }
        public Location FocusedLocation { get; private set; }
        public Item Item { get; private set; }

        WeakModelRef<Story> ParentStoryRef { get; set; }
        WeakModelRef<Story> ChildStoryRef { get; set; }
        WeakModelRef<Hero> _heroRef;

        List<WeakModelRef<Location>> _locations = new();

        StepSequenceRunner _runner;
        StoryGraphRuntime _graph;
#if UNITY_EDITOR
        public StoryGraph EDITOR_Graph { get; private set; }
#endif

        //UnsafeList<Bank> _loadingBanks;
        IVStoryPanel _view;
        bool _initInvolveHero;
        StructList<ISTextModifer> _sTextModifiers = new StructList<ISTextModifer>(0);

        
        // === Properties
        //public UnsafeList<Bank>.ReadOnly LoadingBanks => _loadingBanks.AsReadOnly();
        public UIState UIState => UIState.TransparentState;

        public override string ContextID => Guid;

        public IMemory Memory => Services.Get<GameplayMemory>();
        public IMemory ShortMemory { get; } = new PrivateMemory();
        
        public bool InvolveHero => _initInvolveHero || HasElement<IHeroInvolvement>();
        public bool IsSharedBetweenMultipleNPCs => _graph.sharedBetweenMultipleNPCs;
        public IEnumerable<WeakModelRef<Location>> Locations => _locations;
        public ICollection<string> Tags => Graph.tags;
        public IEnumerable<VariableDefine> Variables => Graph.variables;
        public IEnumerable<VariableReferenceDefine> VariableReferences => Graph.variableReferences;
        public ref readonly StructList<ISTextModifer> STextModifiers => ref _sTextModifiers;

        public ref readonly StoryGraphRuntime Graph => ref _graph;
        public string Guid => _graph.guid;

        IVStoryPanel View {
            get {
                if (MainView != null && !ReferenceEquals(MainView, _view)) {
                    _view = MainView as IVStoryPanel;
                }

                return _view;
            }
        }

        public new static class Events {
            public static readonly Event<Story, Location> FocusedLocationChanged = new(nameof(FocusedLocationChanged));
        }

        Story(Hero hero) {
            Hero = hero;
            _runner = new StepSequenceRunner(this);
        }
        
        // === Creation
        public static Story StartStory(StoryConfig config) {
            if (TryStartStoryDeferred(config, out var story)) {
                FinishStartStoryDeferred(config, story);
            }
            return story;
        }

        public static bool TryStartStoryDeferred(StoryConfig config, out Story story) {
            story = new Story(config.hero);
            if (story.TrySetupFrom(config.bookmark) == false) {
                story = null;
                return false;
            }
            return true;
        }

        public static bool IsStorySubMenuEmpty(StoryConfig config) {
            var story = new Story(config.hero);
            var guid = config.bookmark.story.GUID;
            var graph = StoryGraphRuntime.Get(guid);
            if (!graph.HasValue) {
                return false;
            }
            story._graph = graph.Value;
            
            var startingChapter = story._graph.InitialStoryChapter;
            if (story._graph.TryGetStart(config.bookmark, out _, out var chapter)) {
                startingChapter = chapter.continuation;
            }
            //story._runner.ChangeChapter(chapter);
            
            var subMenu = startingChapter?.steps?.FirstOrDefault() as SChoiceSubmenu;
            if (subMenu == null) {
                return false;
            }

            World.Add(story);

            bool isStorySubMenuEmpty = !subMenu.ShouldBeAvailable(story);
            story.Discard();
            return isStorySubMenuEmpty;
        }
        
        public static void FinishStartStoryDeferred(StoryConfig config, Story story) {
            World.Add(story);
            story.Item = config.item;
            if (config.parentStory.TryGet(out Story parentStory)) {
                story.ParentStoryRef = config.parentStory;
                parentStory.ChildStoryRef = story;
            }
            foreach (var loc in config.locations) {
                story.SetupLocation(loc, story.InvolveHero, story.InvolveAI, true, story.InvolveHero).Forget();
                story.OwnerLocation ??= loc;
            }

            story.ChangeFocusedLocation(story._locations.FirstOrDefault());
            InitStory(story, config.bookmark, config.viewType);
        }

        void ChangeGraph(StoryBookmark bookmark) {
            var guid = bookmark.story.GUID;
            if (_graph.guid == guid) {
                return;
            }
            ReleaseBanks();
            _graph.Dispose();
            TrySetupFrom(bookmark);
        }
        
        bool TrySetupFrom(StoryBookmark bookmark) {
            var guid = bookmark.story.GUID;
            var graph = StoryGraphRuntime.Get(guid);
            if (!graph.HasValue) {
                return false;
            }
            _graph = graph.Value;
#if UNITY_EDITOR
            EDITOR_Graph = UnityEditor.AssetDatabase.LoadAssetAtPath<StoryGraph>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
#endif
            
            //FmodRuntimeManagerUtils.LoadSoundBanks(_graph.usedSoundBanksNames, ref _loadingBanks, ARAlloc.Persistent);
            
            if (_graph.TryGetStart(bookmark, out var settings, out var chapter)) {
                _initInvolveHero = settings.involveHero;
                InvolveAI = settings.involveAI;
                _runner.ChangeChapter(chapter);
                return true;
            } else {
                return false;
            }
        }

        void ReleaseBanks() {
            // if (_loadingBanks.IsCreated) {
            //     _loadingBanks.Clear();
            // }
            FmodRuntimeManagerUtils.UnloadSoundBanks(_graph.usedSoundBanksNames);
        }
        
        static void InitStory(Story story, StoryBookmark bookmark, Type viewType) {
            story.WasInterrupted = false;

            InitView(story, viewType);
            story.SetDefaultTitle();

            // involve hero
            if (story.InvolveHero) {
                story.AddElement(new HeroDialogueInvolvement());
            }

            // debug start story window
            if (DebugReferences.DebugStoryStart) {
                World.Add(new DebugStartStory(story));
            }

            if (!story.HasBeenDiscarded) {
                World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<HeroDialogueInvolvement>(), story, story.HandleStoryInvolveHeroStarted);
            }
        }

        void HandleStoryInvolveHeroStarted(Model heroInvolvement) {
            if (InvolveHero) {
                return;
            }
            // If any other story with hero involve started, discard this one
            if (heroInvolvement is HeroDialogueInvolvement i && i.ParentModel != this && !i.ParentModel.IsAnyParent(this)) {
                FinishStory(true);
            }
        }

        static void InitView(Story story, Type viewType) {
            // spawn view if available
            bool isFirstStepDiscardUI = story.Graph.InitialStoryChapter?.steps?.FirstOrDefault() is SDiscardUI;
            bool spawnView = viewType != null && !isFirstStepDiscardUI;
            
            if (spawnView) {
                if (!viewType.GetInterfaces().Contains(typeof(IVStoryPanel))) {
                    throw new ArgumentException("Story view type must implement IVStoryPanel!");
                }
                
                if (story.InvolveHero) {
                    World.SpawnView(story, typeof(VModalBlocker));
                }

                story._view = World.SpawnView(story, viewType, true) as IVStoryPanel;
            }
        }

        // === Initialization/deinitialization
        protected override string GenerateID(Services services, StringBuilder idBuilder) {
            idBuilder.Append("Story:");
            idBuilder.Append(_graph.guid);
            idBuilder.Append(':');
            idBuilder.Append(services.Get<IdStorage>().NextIdFor(this, typeof(Story), true));
            return idBuilder.ToString();
        }

        protected override void OnInitialize() {
            if (Hero != null && !Hero.WasDiscarded) {
                Hero.Skills.UpdateContext();
                this.ListenTo(Model.Events.AfterDiscarded, () => {
                    if (Hero) Hero.Skills.UpdateContext();
                }, null);
            }

            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, this));
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _runner.Stop();
            this.Trigger(StoryEvents.StoryEnded, this);

            ReleaseBanks();
            _graph.Dispose();
            // if (_loadingBanks.IsCreated) {
            //     _loadingBanks.Dispose();
            // }
        }

        // === Story Advance
        public void ProcessEndFrame() {
            if (IsDebugPaused) {
                return;
            }
            _runner.Advance();
            if (_runner.RunningStep == null && View == null) {
                FinishStory();
            } else if (!HasBeenDiscarded) {
                TryGetElement<StoryOnTop>()?.ProcessEndFrame();
            }
        }

        // === Story API
        public bool StoryEndRequiresInteraction => View != null && (View is not VDialogue and not VBark);

        public void DropChildren() {
            if (HasBeenDiscarded) return;
            if (ChildStoryRef.TryGet(out Story childStory)) {
                childStory.WasInterrupted = WasInterrupted;
                childStory.FinishStory();
            }
        }

        public bool IsAnyParent(Story story) {
            if (!ParentStoryRef.TryGet(out Story parentStory)) return false;
            if (parentStory == story) return true;

            return parentStory.IsAnyParent(story);
        }

        /// <summary> Should only be called through StoryUtils only </summary>
        public void SetIsEnding(bool isEnding) {
            IsEnding = isEnding;
        }

        /// <summary>
        /// Should only be called through StoryUtils unless you want to interrupt without finishing the story
        /// </summary>
        public void SetInterrupted() {
            WasInterrupted = true;
        }

        // delegate UI-related API calls to the view
        public void Clear() {
            View?.Clear();
        }

        public void RemoveView() {
            MainView?.Discard();
            _view = null;
        }

        public void SetArt(SpriteReference art) => View?.SetArt(art);
        public void SetTitle(string title) => View?.SetTitle(title);
        public void ClearText() => View?.ClearText();
        public void ShowText(TextConfig textConfig) => View?.ShowText(textConfig);
        public void ShowLastChoice(string textToDisplay, string iconName) => View?.ShowLastChoice(textToDisplay, iconName);
        public void ShowChange(Stat stat, int change) => View?.ShowChange(stat, change);
        public void OfferChoice(ChoiceConfig choiceConfig) => View?.OfferChoice(choiceConfig);
        public void ToggleBg(bool enabled) => View?.ToggleBg(enabled);
        public void ToggleViewBackground(bool enabled) => View?.ToggleViewBackground(enabled);

        public Transform LastChoicesGroup() => View?.LastChoicesGroup();
        public Transform StatsPreviewGroup() => View?.StatsPreviewGroup();

        // -- Story State API calls 
        public void ChangeFocusedLocation(Location location) {
            if (location == null || location.HasBeenDiscarded) {
                return;
            }

            if (!_locations.Contains(location)) {
                SetupLocation(location, false, false, true, false).Forget();
            }

            if (FocusedLocation != location) {
                FocusedLocation = location;
                SetDefaultTitle();
                this.Trigger(Events.FocusedLocationChanged, location);
            }
        }

        public async UniTask SetupLocation(Location location, bool invulnerability, bool involve, bool rotReturnToInteraction,  bool rotToHero, bool forceExitInteraction = false) {
            if (location == null || location.HasBeenDiscarded) {
                return;
            }

            if (!_locations.Contains(location)) {
                _locations.Add(location);
            }

            await SetupNpc(location.TryGetElement<NpcElement>(), invulnerability, involve, rotReturnToInteraction, rotToHero, forceExitInteraction);
        }

        public async UniTask SetupNpc(NpcElement npc, bool invulnerability, bool involve, bool rotReturnToInteraction, bool rotToHero, bool forceExitInteraction = false) {
            if (npc == null || npc.HasBeenDiscarded) {
                return;
            }
            
            if (!involve) {
                var involvement = NpcInvolvement.GetFor(this, npc);
                if (involvement != null) {
                    await involvement.EndTalk(rotReturnToInteraction: rotReturnToInteraction);
                }
            } else {
                var involvement = await NpcInvolvement.GetOrCreateFor(this, npc, invulnerability);
                await involvement.DropToAnchor();
                await involvement.StartTalk(rotToHero, forceExitInteraction);
            }
        }

        public void JumpTo(StoryChapter chapter) {
            if (chapter == null) {
                StoryUtils.EndStory(this);
                return;
            }
            _runner.ChangeChapter(chapter);
        }

        public void JumpToDifferentGraph(StoryBookmark bookmark) {
            if (bookmark.story.GUID == _graph.guid) {
                JumpTo(string.IsNullOrEmpty(bookmark.chapterName) ? Graph.InitialStoryChapter : Graph.BookmarkedChapter(bookmark.chapterName));
            } else {
                ChangeGraph(bookmark);
            }
        }

        void SetDefaultTitle() {
            if (!string.IsNullOrWhiteSpace(FocusedLocation?.DisplayName)) {
                SetTitle(FocusedLocation.DisplayName);
            }
        }

        public void FinishStory(bool wasInterrupted = false) {
            StoryUtils.EndStory(this, withInterrupt: wasInterrupted);
        }

        public void SpawnContent(DynamicContent contentElement) { }

        public void LockChoiceAssetGate() {
            View.LockChoiceAssetGate();
        }

        public void UnlockChoiceAssetGate() {
            View.UnlockChoiceAssetGate();
        }

        public UIResult Handle(UIEvent evt) {
            if (evt is UICancelAction) {
                var choices = Elements<Choice>().ToArraySlow();
                if (choices.Any()) {
                    World.Only<Focus>().Select(choices.Last().View<VChoice>().DefaultFocus);
                }
            }

            if (evt is UIKeyDownAction uiDownAction) {
                var action = uiDownAction.Data.actionName;

                if (action == KeyBindings.UI.CharacterSheets.CharacterSheet) {
                    CharacterSheetUI.ToggleCharacterSheet();
                } else if (action == KeyBindings.UI.CharacterSheets.Inventory) {
                    CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Inventory);
                } else {
                    return UIResult.Ignore;
                }

                // if we got here, we did something with the key
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }
        
        // === Modifiers
        public void AddSTextModifier(ISTextModifer modifer) {
            _sTextModifiers.Add(modifer);
        }
        
        public void RemoveSTextModifier(ISTextModifer modifer) {
            _sTextModifiers.Remove(modifer);
        }
    }
}