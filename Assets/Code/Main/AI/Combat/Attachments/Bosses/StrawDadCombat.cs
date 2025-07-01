using Awaken.Utility;
using System;
using Awaken.Kandra;
using Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [Serializable]
    public partial class StrawDadCombat : BaseBossCombat {
        public override ushort TypeForSerialization => SavedModels.StrawDadCombat;

        static readonly int TransitionProperty = Shader.PropertyToID("_Transition");
        
        [SerializeField, TemplateType(typeof(StatusTemplate))]
        TemplateReference thatchArmorStatusTemplate;
        [SerializeField, TemplateType(typeof(StatusTemplate))]
        TemplateReference burnedRageStatusTemplate;

        IEventListener _statusAddedListener;
        KandraRenderer _thatchRenderer;
        Material[] _thatchMaterials;
        
        [Saved] bool _thatchBurned;
        [Saved] WeakModelRef<Status> _thatchArmorStatusRef;
        [Saved] WeakModelRef<Status> _burnedRageStatusRef;

        public new static class Events {
            public static readonly Event<StrawDadCombat, StrawDadCombat> BurningEnded = new(nameof(BurningEnded));
        }

        public override void InitFromAttachment(BossCombatAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            StrawDadCombat copyFrom = (StrawDadCombat)spec.BossBaseClass;
            thatchArmorStatusTemplate = new TemplateReference(copyFrom.thatchArmorStatusTemplate.GUID);
            burnedRageStatusTemplate = new TemplateReference(copyFrom.burnedRageStatusTemplate.GUID);
        }
        
        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            
            _statusAddedListener = NpcElement.Statuses.ListenTo(CharacterStatuses.Events.AddedStatus, OnStatusAdded, this);
        }
        
        protected override void AfterVisualLoaded(Transform transform) {
            base.AfterVisualLoaded(transform);
            _thatchRenderer = transform.GetComponentInChildren<StrawDadThatchMarker>().GetComponent<KandraRenderer>();
            _thatchMaterials = _thatchRenderer.UseInstancedMaterials();
            SetInitialState();
        }
        
        void SetInitialState() {
            if (_thatchBurned) {
                SetBurned(true);
            } else {
                SetCoveredInThatch();
            }
        }

        void OnStatusAdded(Status status) {
            if (status is BuildupStatus buildup && buildup.BuildupStatusStatusType == BuildupStatusType.Burn) {
                World.EventSystem.DisposeListener(ref _statusAddedListener);
                buildup.CompleteBuildup();
                StartBurningThatch();
            }
        }

        void StartBurningThatch() {
            StartBehaviour(Element<StrawDadThatchBurnBehaviour>());
            SetBurned(false);
        }
        
        public void SetCoveredInThatch() {
            if (_burnedRageStatusRef.Exists()) {
                _burnedRageStatusRef.Get().Discard();
                _burnedRageStatusRef = null;
            }

            if (!_thatchArmorStatusRef.Exists() && thatchArmorStatusTemplate.IsSet) {
                _thatchArmorStatusRef = ApplyStatus(thatchArmorStatusTemplate.Get<StatusTemplate>());
            }
            
            UpdateThatchBurningProgress(0f);
            SetPhase(0);
            _thatchBurned = false;
        }

        public void SetBurned(bool instant) {
            if (!_burnedRageStatusRef.Exists() && burnedRageStatusTemplate.IsSet) {
                _burnedRageStatusRef = ApplyStatus(burnedRageStatusTemplate.Get<StatusTemplate>());
            }
            
            _thatchBurned = true;

            if (instant) {
                FinalizeBurning();
            }
        }

        public void FinalizeBurning() {
            if (_thatchArmorStatusRef.Exists()) {
                _thatchArmorStatusRef.Get().Discard();
                _thatchArmorStatusRef = null;
            }
            
            UpdateThatchBurningProgress(1f);
            SetPhase(1);
            this.Trigger(Events.BurningEnded, this);
        }
        
        public void UpdateThatchBurningProgress(float progress) {
            UpdateThatchMaterialsDissolve(1f - progress);
        }
        
        void UpdateThatchMaterialsDissolve(float status) {
            foreach (var material in _thatchMaterials) {
                material.SetFloat(TransitionProperty, status);
            }
        }

        Status ApplyStatus(StatusTemplate statusTemplate) {
            var statusInfo = StatusSourceInfo.FromStatus(statusTemplate).WithCharacter(NpcElement);
            return NpcElement.Statuses.AddStatus(statusTemplate, statusInfo).newStatus;
        }
    }
}