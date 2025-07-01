using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Utility {
    public class KeyBindings : RichEnum {
        
        public class Gameplay : KeyBindings {
            public static readonly Gameplay
                Horizontal = new(nameof(Horizontal)),
                Vertical = new(nameof(Vertical)),
                MountHorizontal = new(nameof(MountHorizontal)),
                MountVertical = new(nameof(MountVertical)),
                CameraHorizontal = new(nameof(CameraHorizontal)),
                CameraVertical = new(nameof(CameraVertical)),

                Sprint = new(nameof(Sprint)),
                Jump = new(nameof(Jump)),
                Block = new(nameof(Block)),
                Crouch = new(nameof(Crouch)),
                Dash = new(nameof(Dash)),
                Walk = new(nameof(Walk)),
                Kick = new(nameof(Kick)),

                QuickSave = new(nameof(QuickSave)),
                QuickLoad = new(nameof(QuickLoad)),

                Interact = new(nameof(Interact)),
                Dismount = new(nameof(Dismount)),

                ToggleWeapon = new(nameof(ToggleWeapon)),
                ToggleCameraZoom = new(nameof(ToggleCameraZoom)),
                Attack = new(nameof(Attack)),
                AttackHeavy = new(nameof(AttackHeavy)),

                SkipDialogue = new(nameof(SkipDialogue)),
                UseWyrdSkillsSlot = new(nameof(UseWyrdSkillsSlot)),
                AlternativeUseWyrdSkillsSlot = new(nameof(AlternativeUseWyrdSkillsSlot)),
                
                ChangeHeroPerspective = new(nameof(ChangeHeroPerspective));
                
            Gameplay(string enumName) : base(enumName) { }
            
            public class Technical : KeyBindings {
                [UnityEngine.Scripting.Preserve]
                public static readonly Technical
                    Forward = new(nameof(Forward)),
                    Backward = new(nameof(Backward)),
                    Right = new(nameof(Right)),
                    Left = new(nameof(Left));
            
                Technical(string enumName) : base(enumName) { }
            }
            
            public class PhotoMode : KeyBindings {
                public static readonly PhotoMode
                    EnableCameraMovement = new(nameof(EnableCameraMovement)),
                    ToggleUI = new(nameof(ToggleUI)),
                    ToggleCamera = new(nameof(ToggleCamera)),
                    NextAnimation = new(nameof(NextAnimation)),
                    PreviousAnimation = new(nameof(PreviousAnimation)),
                    ZoomIn = new(nameof(ZoomIn)),
                    ZoomOut = new(nameof(ZoomOut));
            
                PhotoMode(string enumName) : base(enumName) { }
            }
        }

        public class HeroItems : KeyBindings {
            public static readonly HeroItems
                NextItem = new(nameof(NextItem)),
                PreviousItem = new(nameof(PreviousItem)),

                EquipFirstItem = new(nameof(EquipFirstItem)),
                EquipSecondItem = new(nameof(EquipSecondItem)),
                EquipThirdItem = new(nameof(EquipThirdItem)),
                EquipFourthItem = new(nameof(EquipFourthItem)),

                UseQuickSlot = new(nameof(UseQuickSlot)),
                NextQuickSlot = new(nameof(NextQuickSlot));
            
            HeroItems(string enumName) : base(enumName) { }
        }

        //TODO: remove input actions should not be specific to the controller
        public class Gamepad : KeyBindings {
            [UnityEngine.Scripting.Preserve]
            public static readonly Gamepad
                UI_Y = new(nameof(UI_Y)),
                UI_X = new(nameof(UI_X)),
                UI_A = new(nameof(UI_A)),
                UI_B = new(nameof(UI_B)),
                
                DPad_Up = new(nameof(DPad_Up)),
                DPad_Down = new(nameof(DPad_Down)),
                DPad_Right = new(nameof(DPad_Right)),
                DPad_Left = new(nameof(DPad_Left));
            
            Gamepad(string enumName) : base(enumName) { }
        }
        
        public class UI : KeyBindings {
            public class Generic : UI {
                public static readonly Generic
                    Previous = new(nameof(Previous)),
                    PreviousAlt = new(nameof(PreviousAlt)),
                    Next = new(nameof(Next)),
                    NextAlt = new(nameof(NextAlt)),
                    Accept = new(nameof(Accept)),
                    Confirm = new(nameof(Confirm)),
                    Exit = new(nameof(Exit)),
                    Cancel = new(nameof(Cancel)),
                    Menu = new(nameof(Menu)),
                    MarkAllAsSeen = new(nameof(MarkAllAsSeen)),
                    IncreaseValue = new(nameof(IncreaseValue)),
                    IncreaseValueAlt = new(nameof(IncreaseValueAlt)),
                    DecreaseValue = new(nameof(DecreaseValue)),
                    DecreaseValueAlt = new(nameof(DecreaseValueAlt)),
                    SecondaryAction = new(nameof(SecondaryAction)),
                    ScrollVertical = new(nameof(ScrollVertical)),
                    ScrollHorizontal = new(nameof(ScrollHorizontal)),
                    ReadMore = new(nameof(ReadMore));
                
                Generic(string enumName) : base(enumName) { }
            }
            
            public class QuickWheel : UI {
                [UnityEngine.Scripting.Preserve]
                public static readonly QuickWheel
                    QuickWheelAction1 = new(nameof(QuickWheelAction1)),
                    QuickWheelAction2 = new(nameof(QuickWheelAction2)),
                    QuickWheelAction3 = new(nameof(QuickWheelAction3)),
                    QuickWheelAction4 = new(nameof(QuickWheelAction4)),
                    QuickWheelUse = new(nameof(QuickWheelUse)),
                    QuickWheelSelect = new(nameof(QuickWheelSelect)),
                    QuickWheelRest = new(nameof(QuickWheelRest)),
                    QuickWheelQuickSave = new(nameof(QuickWheelQuickSave));
                
                QuickWheel(string enumName) : base(enumName) { }
            }

            public class Settings : UI {
                public static readonly Settings
                    ApplyChanges = new(nameof(ApplyChanges)),
                    RestoreDefaults = new(nameof(RestoreDefaults));
                
                Settings(string enumName) : base(enumName) { }
            }
            
            public class Crafting : UI {
                public static readonly Crafting
                    CraftOne = new(nameof(CraftOne)),
                    CraftMany = new(nameof(CraftMany));
                
                Crafting(string enumName) : base(enumName) { }
            }
            
            public class HUD : UI {
                [UnityEngine.Scripting.Preserve]
                public static readonly HUD
                    QuickUseWheel = new(nameof(QuickUseWheel)),
                    ToggleQuestTracker = new(nameof(ToggleQuestTracker)),
                    OpenSkillTree = new(nameof(OpenSkillTree)),
                    OpenInventoryItemRead = new(nameof(OpenInventoryItemRead)),
                    OpenRestPopup = new(nameof(OpenRestPopup)),
                    TrackNewQuest = new(nameof(TrackNewQuest));
                
                HUD(string enumName) : base(enumName) { }
            }
            
            public class Items : UI {
                public static readonly Items
                    TakeItem = new(nameof(TakeItem)),
                    TransferItems = new(nameof(TransferItems)),
                    SelectItem = new(nameof(SelectItem)),
                    UnequipItem = new(nameof(UnequipItem)),
                    DropItem = new(nameof(DropItem)),
                    SortItems = new(nameof(SortItems)),
                    FilterItems = new(nameof(FilterItems));
                
                Items(string enumName) : base(enumName) { }
            }

            public class Talents : UI {
                public static readonly Talents
                    AcquireTalent = new(nameof(AcquireTalent)),
                    ResetTalent = new(nameof(ResetTalent)),
                    ConfirmTalents = new(nameof(ConfirmTalents));
                
                Talents(string enumName) : base(enumName) { }
            }
            
            public class Map : UI {
                public static readonly Map
                    MapTranslate = new(nameof(MapTranslate)),
                    MapZoom = new(nameof(MapZoom)),
                    PlaceCustomMarker = new(nameof(PlaceCustomMarker));
                
                Map(string enumName) : base(enumName) { }
            }

            public class Saving : UI {
                public static readonly Saving
                    SaveSlotAction = new(nameof(SaveSlotAction)),
                    RenameSaveSlotInputFocused = new(nameof(RenameSaveSlotInputFocused)),
                    RenameSaveSlotInputUnfocused = new(nameof(RenameSaveSlotInputUnfocused)),
                    RenameSaveSlot = new(nameof(RenameSaveSlot)),
                    RemoveSaveSlot = new(nameof(RemoveSaveSlot));
                
                Saving(string enumName) : base(enumName) { }
            }

            public class CharacterSheets : UI {
                public static readonly CharacterSheets
                    CharacterSheet = new(nameof(CharacterSheet)),
                    Inventory = new(nameof(Inventory)),
                    QuestLog = new(nameof(QuestLog)),
                    Journal = new(nameof(Journal)),
                    ToggleMap = new(nameof(ToggleMap));
                
                CharacterSheets(string enumName) : base(enumName) { }
            }
            
            public class Housing : UI {
                public static readonly Housing
                    PreviewFurniture = new(nameof(PreviewFurniture));
                    
                Housing(string enumName) : base(enumName) { }
            }

            public class CloudConflict : UI {
                public static readonly CloudConflict
                    ChoseLocal = new(nameof(ChoseLocal)),
                    ChoseCloud = new(nameof(ChoseCloud));
                CloudConflict(string enumName) : base(enumName) { }
            }
            
            public class BugReport : UI {
                public static readonly BugReport
                    ConfirmAndSend = new(nameof(ConfirmAndSend));
                
                public BugReport(string enumName) : base(enumName) { }
            }
            
            UI(string enumName) : base(enumName) { }
        }

        public class Minigames : KeyBindings {
            public static readonly Minigames
                PickRotate = new(nameof(PickRotate)),
                LockOpenAxis = new(nameof(LockOpenAxis));
            
            Minigames(string enumName) : base(enumName) { }
        }

        public class Debug : KeyBindings {
            [UnityEngine.Scripting.Preserve]
            public static readonly Debug
                DebugDelete = new(nameof(DebugDelete)),
                DebugAddResourcesAndExp = new(nameof(DebugAddResourcesAndExp)),
                DebugSwitchCanvas = new(nameof(DebugSwitchCanvas)),
                DebugChangeLanguage = new(nameof(DebugChangeLanguage)),
                DebugModelsDebug = new(nameof(DebugModelsDebug)),
                DebugGodMode = new(nameof(DebugGodMode)),
                DebugSceneSelection = new(nameof(DebugSceneSelection)),
                DebugNoClipUp = new(nameof(DebugNoClipUp)),
                DebugNoClipDown = new(nameof(DebugNoClipDown)),
                DebugNoClipAccelerate = new(nameof(DebugNoClipAccelerate)),
                DebugNoClipBoost = new(nameof(DebugNoClipBoost)),
                DebugCallMount = new(nameof(DebugCallMount)),

                DebugHeroSkillKillEverything = new(nameof(DebugHeroSkillKillEverything)),
                DebugHeroSkillLogInfo = new(nameof(DebugHeroSkillLogInfo)),
                DebugHeroSkillRegenHPAndStamina = new(nameof(DebugHeroSkillRegenHPAndStamina)),
                DebugHeroSkillSuperJump = new(nameof(DebugHeroSkillSuperJump)),
                DebugHeroSkillSuperDash = new(nameof(DebugHeroSkillSuperDash)),
                DebugToggleGraphy = new(nameof(DebugToggleGraphy)),
                DebugSpawnWyrdnessEnemy = new(nameof(DebugSpawnWyrdnessEnemy)),
                DebugToggleGraphicsLevel = new(nameof(DebugToggleGraphicsLevel));
            
            Debug(string enumName) : base(enumName) { }
        }

        KeyBindings(string enumName, string inspectorCategory = "") : base(enumName, inspectorCategory) { }
        public static implicit operator string(KeyBindings binding) => binding.EnumName;
    }
}
