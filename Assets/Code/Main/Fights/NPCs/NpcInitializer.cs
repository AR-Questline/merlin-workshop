using Awaken.TG.Fights.NPCs;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public class NpcInitializer {
        public NpcElement Npc { get; }

        bool _npcVisualLoaded;
        bool _npcCompletelyInitialized;
        
        NpcVisualLoadedDelegate _onNpcVisualLoaded;
        NpcCompletelyInitialized _onNpcCompletelyInitialized;
        
        public Location Location => Npc.ParentModel;
        public NpcController Controller => Npc.Controller;
        
        public bool HasVisualLoaded => _npcVisualLoaded;
        public bool HasCompletelyInitialized => _npcCompletelyInitialized;
        
        public NpcInitializer(NpcElement npc) {
            Npc = npc;
        }
        
        // === Initialization
        
        public void Initialize() {
            AddNotSavedElementsOnInitialize();
            AddSavedElementsOnInitialize();
        }

        public void Restore() {
            AddNotSavedElementsOnInitialize();
        }

        public void FullyInitialize() {
            AddNotSavedElementsOnFullyInitialize();
        }

        void AddNotSavedElementsOnInitialize() {
            Npc.AddElement(new HealthElement());
            Npc.AddElement(new DeathElement());
            Npc.AddElement(new CharacterDealingDamage());
            Npc.AddElement(new NpcInteractor());
            Npc.AddElement(new NpcHandOwner());
            Npc.AddElement(new NpcTargetGrounded());
            Npc.AddElement(new NpcCrimeReactions());
            Npc.AddElement(new NpcHealthRegeneration());
            
            if (Npc.Actor.HasBarks) {
                Npc.AddElement(new BarkElement());
            }
        }

        void AddSavedElementsOnInitialize() {
            AliveStats.Create(Npc);
            CharacterStats.Create(Npc);
            StatusStats.Create(Npc);
            NpcStats.CreateFromNpcTemplate(Npc);
            
            Location.AddElement(new NpcItems());
            Npc.AddElement(new CharacterStatuses());
            Npc.AddElement(new CharacterSkills());
            Npc.AddElement(new BodyFeatures());
            Npc.AddElement(new IdleBehaviours());
            Location.AddElement(new SearchAction(Npc.Template.Loot));
        }

        void AddNotSavedElementsOnFullyInitialize() {
            Location.AddElement(new PickpocketAction());
        }

        // === Visual Loaded
        
        public void NotifyVisualLoaded() {
            var npcAccessor = new NpcElement.InitializationAccessor(Npc);
            
            InitAnimations();
            InitWeaponsHandler(npcAccessor);
            InitClothesAndBodyFeatures(npcAccessor);
            InitEnemyBaseClass();
            InitWyrdConversion(npcAccessor);
            InitMovement();
            InitNpcAI(npcAccessor);
            InitTimeDependent();
            InitCullingSystem();
            
            TriggerOnVisualLoadedCallbacks();
            
            FullyInitBarks();
            FullyInitEnemyBaseClass();
            FullyInitIdleBehaviours();
            FullyInitAnimations();

            TriggerOnCompletelyInitializedCallbacks();
        }

        void InitAnimations() {
            var fightingStyle = Npc.FightingStyle;
            if (fightingStyle == null) {
                Log.Critical?.Error($"{Npc.Template} has null fighting style! This is gameBreaking! Please fix!", Npc.ParentTransform.gameObject);
                return;
            }
            Npc.AddElement(new NpcGeneralFSM(Controller.Animator, Controller.ARNpcAnimancer, (int) ARNpcAnimancer.NpcLayers.General, fightingStyle.generalMask));
            Npc.AddElement(new NpcAdditiveFSM(Controller.Animator, Controller.ARNpcAnimancer, (int) ARNpcAnimancer.NpcLayers.Additive, fightingStyle.additiveMask));
            Npc.AddElement(new NpcCustomActionsFSM(Controller.Animator, Controller.ARNpcAnimancer, (int) ARNpcAnimancer.NpcLayers.CustomActions, fightingStyle.customActionsMask));
            Npc.AddElement(new NpcTopBodyFSM(Controller.Animator, Controller.ARNpcAnimancer, (int) ARNpcAnimancer.NpcLayers.TopBody, fightingStyle.topBodyMask));
            Npc.AddElement(new NpcOverridesFSM(Controller.Animator, Controller.ARNpcAnimancer, (int) ARNpcAnimancer.NpcLayers.Overrides, fightingStyle.overridesMask, fightingStyle.areOverridesAdditive));
            Npc.Controller.ARNpcAnimancer.InitializeNpcWithFightingStyle(Npc, fightingStyle).Forget();
        }

        void InitEnemyBaseClass() {
            Location.TryGetElement<EnemyBaseClass>()?.OnVisualLoaded(Npc.ParentTransform);
        }

        void InitWeaponsHandler(in NpcElement.InitializationAccessor npcAccessor) {
            if (!Npc.CanDetachWeaponsToBelts) {
                return;
            }
            var handler = new NpcWeaponsHandler();
            npcAccessor.WeaponsHandler = handler;
            Npc.AddElement(handler);
            handler.Init(Npc.ItemsAddedToInventory);
        }
        
        void InitClothesAndBodyFeatures(in NpcElement.InitializationAccessor npcAccessor) {
            var clothes = Location.AddElement(new NpcClothes());
            var bodyFeatures = Npc.Element<BodyFeatures>();
            Gender gender = npcAccessor.SpawnedVisualPrefab?.Instance.GetComponent<NpcGenderMarker>()?.Gender ?? Gender.None;
            bodyFeatures.Gender = gender;
            bodyFeatures.InitCovers(clothes);
        }
        
        void InitWyrdConversion(in NpcElement.InitializationAccessor npcAccessor) {
            npcAccessor.WyrdConversionMarker = Npc.ParentTransform.GetComponentInChildren<NpcWyrdConversionMarker>();
            if (Npc.WyrdConverted) {
                npcAccessor.ChangeWyrdTattoos(true);
                npcAccessor.WyrdConversionMarker?.EnableWyrdObjects();
                npcAccessor.HandleWyrdEmpowerment();
            }
        }

        void InitMovement() {
            Npc.AddElement(new NpcMovement());
        }
        
        void InitNpcAI(NpcElement.InitializationAccessor npcAccessor) {
            if (NpcElement.DEBUG_DoNotSpawnAI) {
                return;
            }
            var npcAI = new NpcAI(Npc.ParentTransform.gameObject);
            npcAccessor.NpcAI = npcAI;
            Npc.AddElement(npcAI);
            npcAI.ListenTo(Model.Events.BeforeDiscarded, _ => npcAccessor.NpcAI = null, Npc);
        }

        void InitTimeDependent() {
            Location.GetOrCreateTimeDependent().WithTimeComponentsOf(Npc.ParentTransform.gameObject);
        }

        void InitCullingSystem() {
            // This code is duplicated in NpcDummy so ensure your changes are reflected there as well
            Location.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, Npc.RefreshDistanceBand, Npc);
            Npc.RefreshDistanceBand(Location.GetCurrentBandSafe(LocationCullingGroup.LastBand));
        }

        void FullyInitBarks() {
            Npc.TryGetElement<BarkElement>()?.OnLoaded();
        }

        void FullyInitEnemyBaseClass() {
            Location.TryGetElement<EnemyBaseClass>()?.OnInventoryInitialized();
        }

        void FullyInitIdleBehaviours() {
            Npc.Element<IdleBehaviours>().OnNpcVisualInitialize();
        }

        void FullyInitAnimations() {
            foreach (var machine in Npc.Elements<NpcAnimatorSubstateMachine>()) {
                machine.OnNpcVisualInitialize();
            }
        }

        // === Callbacks
        
        public void OnVisualLoaded(NpcVisualLoadedDelegate action) {
            if (_npcVisualLoaded) {
                action(Npc, Npc.Controller.transform);
            } else {
                _onNpcVisualLoaded += action;
            }
        }
        
        public void OnCompletelyInitialized(NpcCompletelyInitialized action) {
            if (_npcCompletelyInitialized) {
                action(Npc);
            } else {
                _onNpcCompletelyInitialized += action;
            }
        }

        void TriggerOnVisualLoadedCallbacks() {
            _npcVisualLoaded = true;
            _onNpcVisualLoaded?.Invoke(Npc, Npc.ParentTransform);
            _onNpcVisualLoaded = null;
        }
        
        void TriggerOnCompletelyInitializedCallbacks() {
            _npcCompletelyInitialized = true;
            _onNpcCompletelyInitialized?.Invoke(Npc);
            _onNpcCompletelyInitialized = null;
        }
        
        public delegate void NpcVisualLoadedDelegate(NpcElement npc, Transform parent);
        public delegate void NpcCompletelyInitialized(NpcElement npc);
    }
}