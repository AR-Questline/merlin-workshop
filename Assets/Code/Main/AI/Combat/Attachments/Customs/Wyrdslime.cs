using System;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class Wyrdslime : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.Wyrdslime;

        [SerializeField] float maxHealthForWightConversion = 1000f;
        [SerializeField] float wightConversionChancePerAttack = 1f;

        [SerializeField, TemplateType(typeof(LocationTemplate))]
        TemplateReference wightTemplate;

        Location _wightLocation;
        bool _wightConversionStarted;

        public override void InitFromAttachment(CustomCombatAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            Wyrdslime copyFrom = (Wyrdslime)spec.CustomCombatBaseClass;
            wightTemplate = new TemplateReference(copyFrom.wightTemplate.GUID);
            maxHealthForWightConversion = copyFrom.maxHealthForWightConversion;
            wightConversionChancePerAttack = copyFrom.wightConversionChancePerAttack;
        }
        
        protected override void OnInitialize() {
            base.OnInitialize();
            NpcElement.ListenTo(NpcAI.Events.NpcStateChanged, OnWyrdslimeStateChanged, this);
        }

        void OnWyrdslimeStateChanged(Change<IState> change) {
            if (change.from is StateIdle) {
                EnsureLastIdlePositionIsOnGround();
            }
        }

        void EnsureLastIdlePositionIsOnGround() {
            NpcElement npc = NpcElement;
            var groundCoords = Ground.SnapNpcToGround(npc.LastIdlePosition);
            npc.LastIdlePosition = npc.LastOutOfCombatPosition = groundCoords;
        }

        protected override void OnBehaviourStarted(IBehaviourBase behaviour) {
            if (CanStartWightConversionOnBehaviour(behaviour)) {
                StartWightConversion();
            }
        }
        
        bool CanStartWightConversionOnBehaviour(IBehaviourBase behaviour) =>
            !_wightConversionStarted
            && behaviour.CanBeInterrupted
            && NpcElement.HealthElement.Health.ModifiedValue < maxHealthForWightConversion
            && RandomUtil.UniformFloat(0.0f, 1.0f) <= wightConversionChancePerAttack;
        
        void StartWightConversion() {
            _wightConversionStarted = true;

            StopCurrentBehaviour(false);
            NpcElement.NpcAI.SetActivePerceptionUpdate(false);
            NpcElement.Controller.ToggleGlobalRichAIActivity(false);
            
            SpawnWight();
        }

        void SpawnWight() {
            var wightLocation = wightTemplate.Get<LocationTemplate>();

            if (!wightLocation) {
                Log.Minor?.Error($"Wight template is not set in {this}. Wight conversion will not happen.");
                return;
            }
            
            _wightLocation = wightLocation.SpawnLocation(ParentModel.Coords, ParentModel.Rotation, spawnScene: ParentModel.MainView.gameObject.scene);
            _wightLocation.ViewParent.localScale = Vector3.zero;

            OnWightSpawnInProgress();
        }

        void OnWightSpawnInProgress() {
            SetTemporaryWightInvisibility(true);
            
            var wight = _wightLocation.Element<NpcElement>();
            wight.StartInSpawn = true;
            wight.ListenTo(NpcElement.Events.NpcSpawning, FinalizeWightSpawn, this);
            wight.ListenTo(NpcAI.Events.NpcStateChanged, OnWightStateChanged, this);
        }

        void FinalizeWightSpawn() {
            SetTemporaryWightInvisibility(false);
            NpcElement.SetAnimatorState(NpcFSMType.OverridesFSM, NpcStateType.CustomAction, 0);
        }
        
        void SetTemporaryWightInvisibility(bool invisible) {
            if (_wightLocation) {
                var nearZeroScale = Vector3.one * 0.01f;
                _wightLocation.ViewParent.localScale = invisible ? nearZeroScale : Vector3.one;
            }
        }

        void OnWightStateChanged(Change<IState> change) {
            if (change.from is StateSpawn) {
                FinalizeWightConversion();
            }
        }
        
        void FinalizeWightConversion() {
            NpcElement.AddElement<PreventExpRewardMarker>();
            ParentModel.Kill();
        }
    }
}