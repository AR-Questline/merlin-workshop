﻿// ReSharper disable InconsistentNaming
namespace Awaken.Utility {
    public static class SavedTypes {
        public const ushort ARAssetReference = 1;
        public const ushort SpriteReference = 2;
        public const ushort SceneReference = 3;
        public const ushort ShareableARAssetReference = 4;
        public const ushort ShareableSpriteReference = 5;
        public const ushort MapMemory = 6;
        public const ushort SceneSpecCaches = 7;
        public const ushort ModelElements = 8;
        public const ushort ConeDamageParameters = 9;
        public const ushort SphereDamageParameters = 10;
        public const ushort RelationStore = 11;
        public const ushort StepExecution = 12;
        public const ushort PresenceData = 13;
        public const ushort CachedItem = 14;
        public const ushort NpcStatsWrapper = 15;
        public const ushort WeakModelRef = 16;
        public const ushort UIState = 17;
        public const ushort LocString = 18;
        public const ushort DecalsProjectorsList = 19;
        public const ushort OptionalLocString = 20;
        public const ushort LargeFileData = 21;
        public const ushort ActorRef = 22;
        public const ushort EyeColorFeature = 23;
        public const ushort BlendShape = 24;
        public const ushort SkinColorFeature = 25;
        public const ushort SteamCloudOrigin = 26;
        public const ushort BodyNormalFeature = 27;
        public const ushort BodyTattooFeature = 28;
        public const ushort EyebrowFeature = 29;
        public const ushort ItemSpawningDataRuntime = 30;
        public const ushort ItemSpawningData = 31;
        public const ushort BlendShapesFeature = 32;
        public const ushort FaceTattooFeature = 33;
        public const ushort VfxByHitSurface = 34;
        public const ushort SavedAnimatorParameter = 35;
        public const ushort PlantStage = 36;
        public const ushort GrowingPartData = 37;
        public const ushort MeshFeature = 38;
        public const ushort PlantedSeedData = 39;
        public const ushort FaceSkinTexturesFeature = 40;
        public const ushort TattooConfig = 41;
        public const ushort TeethFeature = 42;
        public const ushort BodySkinTexturesFeature = 43;
        public const ushort InvalidStat = 44;
        public const ushort VertexPath = 45;
        public const ushort RegrowData = 46;
        public const ushort TrialReactivateDeferredAction = 47;
        public const ushort StoryBookmark = 48;
        public const ushort AliveAudioContainer = 49;
        public const ushort ActionData = 50;
        public const ushort CombatMusicAudioSource = 51;
        public const ushort SpawnedLocation = 52;
        public const ushort ItemAudioContainer = 53;
        public const ushort SerializedCrimeOwners = 54;
        public const ushort CurrencyStat = 55;
        public const ushort ItemVfxContainer = 56;
        public const ushort CompoundStat = 57;
        public const ushort RandomRangeStatLimit = 58;
        public const ushort ItemRepresentationByNpc = 59;
        public const ushort Stat = 60;
        public const ushort ItemStat = 61;
        public const ushort LimitedStatLimit = 62;
        public const ushort DeferredActionWithStoryStep = 63;
        public const ushort DeferredActionWithLocationMatch = 64;
        public const ushort DeferredActionWithBookmark = 65;
        public const ushort DeferredLocationMatchCondition = 66;
        public const ushort DeferredLocationExistCondition = 67;
        public const ushort DeferredDistanceCondition = 68;
        public const ushort DeferredAnyLocationExistCondition = 69;
        public const ushort DeferredActionWithPresenceMatch = 70;
        public const ushort DeferredTimeCondition = 71;
        public const ushort VSDatumType = 72;
        public const ushort VSDatumValue = 73;
        public const ushort CrimeOwnerData = 74;
        public const ushort SpecId = 75;
        public const ushort TooltipConstructorTokenText = 76;
        public const ushort FactionToFactionAntagonismOverride = 77;
        public const ushort TokenText = 78;
        public const ushort TemplateReference = 79;
        public const ushort SceneLocationInitializer = 80;
        public const ushort RichLabelSet = 81;
        public const ushort RichLabelUsageEntry = 82;
        public const ushort FactionContainer = 83;
        public const ushort RichEnumReference = 84;
        public const ushort RuntimeLocationInitializer = 85;
        public const ushort Data = 86;
        public const ushort TriState = 87;
        public const ushort MatchByTag = 88;
        public const ushort AttachmentTracker = 89;
        public const ushort StatusSourceInfo = 90;
        public const ushort SkillVariable = 91;
        public const ushort FloatRange = 92;
        public const ushort HeroDamageTimestamp = 93;
        public const ushort BindingData = 94;
        public const ushort SkillReference = 95;
        public const ushort SkillVariablesOverride = 96;
        public const ushort JournalGuid = 97;
        public const ushort ARGuid = 98;
        public const ushort StringCollectionSelector = 99;
        public const ushort ContextualFacts = 100;
        public const ushort Memory = 101;
        public const ushort ChangeSceneInteraction = 102;
        public const ushort PatrolInteractionSavedData = 103;
        public const ushort BindingOverride = 104;
        public const ushort InteractionUniqueFinder = 105;
        public const ushort InteractionFakeDeathFinder = 106;
        public const ushort InteractionFollowLocationFinder = 107;
        public const ushort InteractionDefeatedDuelistFinder = 108;
        public const ushort InteractionBaseFinder = 109;
        public const ushort InteractionRoamFinder = 110;
        public const ushort InteractionStandFinder = 111;
        public const ushort RoamInteractionSavedData = 112;
        public const ushort IdlePosition = 113;
        public const ushort MultiTypeDictionary = 114;
        public const ushort MaskRange = 115;
        public const ushort UnsafePinnableList = 116;
        public const ushort FrugalList = 117;
        public const ushort DelayedAngle = 118;
        public const ushort DelayedValue = 119;
        public const ushort StructList = 120;
        public const ushort Hysteresis = 121;
        public const ushort ARDateTime = 122;
        public const ushort SerializableGuid = 123;
        public const ushort ARTimeSpan = 124;
        public const ushort FactionOverride = 125;
        public const ushort AliveStatsWrapper = 126;
        public const ushort HeroMultStatsWrapper = 127;
        public const ushort LimitedStat = 128;
        public const ushort RandomRangeStat = 129;
        public const ushort CrimeDataRuntime = 130;
        public const ushort StashedItemData = 131;
        public const ushort SteamOriginFile = 132;
        public const ushort MerchantStatsWrapper = 133;
        public const ushort SimpleValue = 134;
        public const ushort DeferredActionsBySceneData = 135;
        public const ushort AttachmentTrackerSavedData = 136;
        public const ushort HeroStatsWrapper = 137;
        public const ushort HostilityData = 138;
        public const ushort StatusStatsWrapper = 139;
        public const ushort LocationReference = 140;
        public const ushort SkillRichEnum = 141;
        public const ushort ProficiencyStatsWrapper = 142;
        public const ushort CharacterStatsWrapper = 143;
        public const ushort HeroRPGStatsWrapper = 144;
        public const ushort ItemElementsDataRuntime = 145;
        public const ushort SlotAndItem = 146;
        public const ushort ItemBasedLocationData = 147;
        public const ushort ItemRequirementsWrapper = 148;
        public const ushort ItemStatsWrapper = 149;
        public const ushort GemTemplateWithSkills = 150;
        public const ushort SkillTemplate = 151;
        public const ushort MatchByRichLabel = 152;
        public const ushort SkillDatum = 153;
        public const ushort SkillAssetReference = 154;
        public const ushort MatchByAllTags = 155;
        public const ushort Match = 156;
        public const ushort MatchByTemplates = 157;
        public const ushort MatchByActor = 158;
        public const ushort InteractionChangeSceneFinder = 159;
        public const ushort InteractionFallbackFinder = 160;
        public const ushort FaceFeature = 161;
        public const ushort StepExecution_ActivateNpcPresenceViaRichLabels = 162;
        public const ushort StepExecution_ChangeNpcFaction = 163;
        public const ushort StepExecution_DiscardSpawnedLocation = 164;
        public const ushort StepExecution_LocationChangeAttachments = 165;
        public const ushort StepExecution_LocationChangeInteractability = 166;
        public const ushort StepExecution_LocationClear = 167;
        public const ushort StepExecution_LocationDiscard = 168;
        public const ushort StepExecution_LocationMakeBusy = 169;
        public const ushort StepExecution_LocationRemoveBusy = 170;
        public const ushort StepExecution_LocationRemoveItem = 171;
        public const ushort StepExecution_NpcChangeCrimeOverride = 172;
        public const ushort StepExecution_NpcKill = 173;
        public const ushort StepExecution_NpcRefreshCurrentBehaviour = 174;
        public const ushort StepExecution_NpcTurnFriendly = 175;
        public const ushort StepExecution_NpcTurnFromGhost = 176;
        public const ushort StepExecution_NpcTurnHostileBase = 177;
        public const ushort StepExecution_NpcTurnIntoGhost = 178;
        public const ushort StepExecution_NpcTurnKillPrevention = 179;
        public const ushort StepExecution_PerformInteraction = 180;
        public const ushort StepExecution_RemoveAllInteractionOverrides = 181;
        public const ushort StepExecution_SetAnimatorParameter = 182;
        public const ushort StepExecution_StopInteraction = 183;
        public const ushort StepExecution_TriggerLocationSpawners = 184;
        public const ushort LargeFilesIndices = 185;
        public const ushort FurnitureVariant = 186;
        public const ushort FishEntry = 187;
        public const ushort RawDamageData = 188;
        public const ushort RuntimeDamageTypeData = 189;
        public const ushort DamageTypeData = 190;
        public const ushort DamageTypeDataPart = 191;
        public const ushort DamageParameters = 192;
    }
}