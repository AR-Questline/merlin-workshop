using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Awaken.TG.Assets;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.Utility.Graphics;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Serialization;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace Awaken.TG.Main.Saving.SaveSlots {
    public partial class SaveSlot : Model {
        public override ushort TypeForSerialization => SavedModels.SaveSlot;

        public const string SlotIdPrefix = "Slot";
        const string QuickSaveIdPrefix = "QuickSave";
        const string AutoSaveIdPrefix = "AutoSave";
        const string IdSlotPattern = "[0-9]+";
        const int MaxQuickAutoSlots = 3;
        
        public override Domain DefaultDomain => Domain.SaveSlotMetaData(ID);

        public Action screenShotTaken;

        // === Properties
        [Saved] public UnicodeString Name { get; private set; }
        [Saved] public bool IsCustomName { get; private set; }
        [Saved] public SceneReference SceneRef { get; private set; }
        [Saved] public SceneReference AdditiveSceneRef { get; private set; }
        [Saved] public DateTime? LastSavedTime { get; private set; }
        [Saved] public ARTimeSpan PlayRealTime { get; private set; }
        [Saved] public UnicodeString HeroLocation { get; private set; }
        [Saved] public int HeroLevel { get; private set; }
        [Saved] public UnicodeString HeroName { get; private set; }
        [Saved] public Guid HeroId { get; private set; }
        [Saved] public bool Hardcore { get; private set; }
        [Saved] public UnicodeString ActiveQuestName { get; private set; }
        [Saved] byte[] _screenshotBytes;
        [Saved] public List<ItemSpawningData> ItemsToModifyOnLoad { get; [RequiredMember] private set; } = new();
        [Saved] public UnsafeBitmask usedLargeFilesIndices;
        [Saved] public int SavedDomainCount { get; private set; } = -1;
        
        public uint SlotIndex { get; private set; }
        public bool ChangedID { get; private set; }
        
        /// <summary>
        /// Used during folder renames due to ID change or invalid folder name due to user changes
        /// </summary>
        string _slotFilesToDelete = null;
        
        string IDNumber => Regex.Match(ID, IdSlotPattern).Value;
        public UnicodeString DisplayName => IsCustomName ? Name : $"{Name} {IDNumber}";
        public bool IsQuickSave => ID.Contains(QuickSaveIdPrefix);
        public bool IsAutoSave => ID.Contains(AutoSaveIdPrefix);

        // === Static Creators
        public static SaveSlot GetQuickSave(bool allowCreate = true, bool getNewest = true) => GetWithId(QuickSaveIdPrefix, allowCreate, LocTerms.QuickSave.Translate(), getNewest);
        public static SaveSlot GetAutoSave(bool allowCreate = true, bool getNewest = false) => GetWithId(AutoSaveIdPrefix, allowCreate, LocTerms.AutoSave.Translate(), getNewest);

        public static SaveSlot LastSaveSlot => LastSaveSlotOfHero(null);
        public static SaveSlot LastSaveSlotOfCurrentHero => LastSaveSlotOfHero(Hero.Current);

        static SaveSlot LastSaveSlotOfHero(Hero hero) => World.All<SaveSlot>()
            .Where(s => SaveSlotBelongsToHero(s, hero) && s.CanLoad())
            .MaxBy(s => s.LastSavedTime ?? DateTime.UnixEpoch, true);

        static bool SaveSlotBelongsToHero(SaveSlot saveSlot, Hero hero) => hero == null || saveSlot.HeroId == hero.HeroID;
        public static bool SaveSlotBelongsToCurrentHero(SaveSlot saveSlot) => saveSlot.HeroId == Hero.Current.HeroID;

        static SaveSlot GetWithId(string idPrefix, bool allowCreate, string defaultName, bool getNewest) {
            var saveSlotsWithPrefix = World.All<SaveSlot>()
                .Where(s => s.HeroId == Hero.Current.HeroID)
                .Where(s => s.ID.Contains(idPrefix))
                .OrderByDescending(s => s.LastSavedTime)
                .ToList();

            SaveSlot save = getNewest ? saveSlotsWithPrefix.FirstOrDefault() : saveSlotsWithPrefix.LastOrDefault();

            if (allowCreate && saveSlotsWithPrefix.Count < MaxQuickAutoSlots) {
                save = new SaveSlot(defaultName);
                save.AssignID(save.GenerateIDAndAssignIndex(idPrefix));
                World.Add(save);
            }

            return save;
        }

        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        SaveSlot() { }

        SaveSlot(string name, bool isCustomName = false) {
            AssignID(GenerateIDAndAssignIndex(SlotIdPrefix));
            Name = name;
            IsCustomName = isCustomName;
        }

        public static SaveSlot GetAndSave(string name, bool isCustomName = false) {
            var saveSlot = new SaveSlot(name, isCustomName);
            World.Add(saveSlot);
            LoadSave.Get.Save(saveSlot);
            return saveSlot;
        }

        public static SaveSlot OverrideAndSave(SaveSlot original) {
            bool isCustomName = original.IsCustomName;
            string name = isCustomName ? original.Name : SlotIdPrefix;
            
            var newSlot = new SaveSlot(name, isCustomName);

            uint oSlotIndex = original.SlotIndex;
            var lfsIndices = original.usedLargeFilesIndices;


            original.Discard();
            World.Add(newSlot);
            LoadSave.Get.Save(newSlot);

            return newSlot;
        }

        // === Operations
        public bool CanLoad() => !LoadingScreenUI.IsLoading && LoadSave.Get.LoadAllowedInMenu();

        public void LoadingStarted() {
            World.EventSystem.LimitedListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.AfterSceneStoriesExecuted, this, 
                _ => ItemUtils.ApplyItemsToModifyOnLoad(ItemsToModifyOnLoad, this), 
                1);
        }

        public void Rename(string name) {
            Name = name;
            IsCustomName = true;
            ApplyChanges();
        }

        public void ApplyChanges() {
            LoadSave.Get.SaveMetadataDomainSynchronous(CurrentDomain);
            TriggerChange();
        }

        public UniTask CaptureSlotInfo(int domainsCount) {
            SceneService sceneService = Services.Get<SceneService>();
            SceneRef = sceneService.MainSceneRef;
            AdditiveSceneRef = sceneService.AdditiveSceneRef;
            LastSavedTime = DateTime.Now;
            PlayRealTime = World.Only<GameRealTime>().PlayRealTime;
            ActiveQuestName = World.Only<QuestTracker>().ActiveQuest?.DisplayName;

            var hero = Hero.Current;
            HeroLevel = hero.CharacterStats.Level.BaseInt;
            HeroName = hero.Name;
            HeroId = hero.HeroID;
            Hardcore = World.Only<DifficultySetting>().Difficulty.SaveRestriction.HasFlagFast(SaveRestriction.Hardcore);
            HeroLocation = sceneService.ActiveSceneDisplayName;
            
            SavedDomainCount = domainsCount;

            return TakeGameplayScreenshot();
        }

        public void SetUsedLargeFilesIndices(ref UnsafeBitmask newUsedLargeFilesIndices) {
            usedLargeFilesIndices.DisposeIfCreated();
            if (newUsedLargeFilesIndices.ElementsLength == 0) {
                newUsedLargeFilesIndices.Dispose();
                return;
            }

            usedLargeFilesIndices = newUsedLargeFilesIndices;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void DisposeUsedLargeFilesIndices() {
            usedLargeFilesIndices.Dispose();
        }

        public string GetDirectory() {
            return Path.Combine(Domain.SaveSlot.ConstructSavePath(this), ID);
        }

        async UniTask TakeGameplayScreenshot() {
            var camera = World.Only<GameCamera>().MainCamera;
            var screenshot = await TextureUtils.CreateTexture2DFromCameraPreview(camera, 320, 180, 16, TextureFormat.RGB24, RenderTextureFormat.ARGB32, Hero.Current.MainView);
            _screenshotBytes = screenshot.EncodeToJPG(18);
            screenShotTaken?.Invoke();
            screenShotTaken = null;
            await LoadSave.Get.SaveMetadataDomainAsync(CurrentDomain);
        }
        
        public Texture2D RecreateGameplayScreenshot() {
            if (_screenshotBytes == null) {
                return null;
            }
            var texture = new Texture2D(1, 1, TextureFormat.RGB24, 1, false, true);
            texture.LoadImage(_screenshotBytes);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>
        /// <inheritdoc cref="ItemUtils.ApplyItemsToModifyOnLoad"/>
        /// </summary>
        public void AddItemToModifyOnLoad(ItemSpawningData item, bool shouldApplyChanges = true) {
            ItemsToModifyOnLoad.Add(item);
            if (shouldApplyChanges) {
                ApplyChanges();
            }
        }

        // === Validation
        internal override void EnsureIdIsValid() {
            SlotIndex = uint.Parse(IDNumber);
            foreach (var slot in World.All<SaveSlot>()) {
                if (slot.SlotIndex == SlotIndex) {
                    AssignNewId();
                    break;
                }
            }
        }
        
        public void DiscardAfterFolderRename(string oldFolderName) {
            _slotFilesToDelete = oldFolderName;
            Discard();
        }

        public bool ValidateDomainAmount(SaveResult saveResult, out string errorMessage) {
            if (saveResult.SupportsFileCounting == false || SavedDomainCount == -1) {
                errorMessage = null;
                return true;
            }

            int domainsCount = saveResult.FileCount - 1; // Don't count the metadata file
            if (domainsCount != SavedDomainCount) {
                errorMessage = $"Save slot {ID}: domain count ({SavedDomainCount}) doesn't equal domain files inside save folder ({domainsCount})\n" +
                                       $"Please load the previous save slot.";
                return false;
            }
            errorMessage = null;
            return true;
        }

        // === Discard
        
        protected override void OnDiscard(bool fromDomainDrop) {
            CleanLfs(SlotIndex, usedLargeFilesIndices, fromDomainDrop);
            if (!fromDomainDrop) {
                Log.Marking?.Warning($"Deleting save slot files for {LogUtils.GetDebugName(this)}");
                if (_slotFilesToDelete != null) {
                    LoadSave.Get.DeleteSlotFiles(_slotFilesToDelete);
                } else {
                    LoadSave.Get.DeleteSlotFiles(this);
                }
            }
        }

        static void CleanLfs(uint slotIndex, UnsafeBitmask lfsIndices, bool fromDomainDrop) {
            if (lfsIndices.IsCreated) {
                if (!fromDomainDrop) {
                    World.Services.Get<LargeFilesStorage>().RemoveSaveSlotUsedFilesData(slotIndex, lfsIndices);
                }

                lfsIndices.Dispose();
            }
        }

        // === Custom Generate ID
        protected override string GenerateID(Services services, StringBuilder idBuilder) {
            throw new InvalidOperationException("Static creators should be used to create SaveSlot with custom ID, this method should never be called");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AssignNewId() {
            AssignID(GenerateIDAndAssignIndex(SlotIdPrefix));
            ChangedID = true;
        }

        string GenerateIDAndAssignIndex(string idPrefix) {
            SlotIndex = GetNewSlotIndex();
            return $"{idPrefix}_{SlotIndex}";
        }
        
        uint GetNewSlotIndex() {
            uint maxId = 0;
            foreach (var slot in World.All<SaveSlot>()) {
                if (slot.SlotIndex > maxId) {
                    maxId = slot.SlotIndex;
                }
            }
            return ++maxId;
        }

        // === Operators/ToString
        public static implicit operator string(SaveSlot saveSlot) => saveSlot?.ID;
        public override string ToString() => ID;
        
        [UnityEngine.Scripting.Preserve]
        public struct OverrideCacheData {
            [UnityEngine.Scripting.Preserve] public readonly uint prevIndex;
            [UnityEngine.Scripting.Preserve] public UnsafeBitmask prevUsedLargeFilesIndices;
            public OverrideCacheData(uint prevIndex, UnsafeBitmask prevUsedLargeFilesIndices) {
                this.prevIndex = prevIndex;
                this.prevUsedLargeFilesIndices = prevUsedLargeFilesIndices;
            }
        }
    }
}