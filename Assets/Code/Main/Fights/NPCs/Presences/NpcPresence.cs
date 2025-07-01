using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Idle.Data.Attachment;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs.Presences {
    public partial class NpcPresence : Element<Location>, IRefreshedByAttachment<NpcPresenceAttachment> {
        public override ushort TypeForSerialization => SavedModels.NpcPresence;

        public static readonly Vector3 AbyssPosition = new(-5000, -5000, -5000);
        static NpcRegistry NpcRegistry => World.Services.Get<NpcRegistry>();

        NpcPresenceAttachment _spec;

        [CanBeNull] public NpcElement AliveNpc => Npc is { HasBeenDiscarded: false, IsAlive: true } npc ? npc : null;
        [CanBeNull] NpcElement Npc { get; set; }

        bool _attached;
        bool _inBand;
        bool _movingToAbyss;

        bool _flagBasedAvailability;

        [Saved(false)] bool _manualAvailabilitySaved;
        bool? _manualAvailabilityOneSession;

        bool _listenersAdded;
        
        public LocationTemplate Template { get; private set; }
        IdleDataElement IdleData => ParentModel.Element<IdleDataElement>();
        
        bool ManualAvailability => _manualAvailabilityOneSession ?? _manualAvailabilitySaved;
        bool Availability => _spec.Manual ? ManualAvailability : _flagBasedAvailability;
        bool InCurrentDomain => CurrentDomain == Domain.CurrentScene();
        public bool Available => Availability && InCurrentDomain;
        public bool Attached => _attached;
        public bool IsManual => _spec.Manual;

        public Vector3 DesiredPosition => ParentModel.Coords;
        public IInteractionSource InteractionSource => IdleData.GetCurrentSource();
        public RichLabelSet RichLabelSet => _spec.RichLabelSet;

        public new static class Events {
            public static readonly Event<NpcPresence, NpcElement> AttachedNpc = new(nameof(AttachedNpc));
            public static readonly Event<NpcPresence, NpcElement> DetachedNpc = new(nameof(DetachedNpc));
        }

        public void InitFromAttachment(NpcPresenceAttachment spec, bool isRestored) {
            _spec = spec;
            Template = spec.Template;
        }

        protected override void OnInitialize() {
            _manualAvailabilitySaved = _spec.InitialManualAvailability;
            Init();
        }

        protected override void OnRestore() {
            Init();
        }

        public void Init() {
            if (Validate() && IsNpcAlive()) {
                InitNpc();
                EnsureListeners();
            }
        }

        public void OnWorldStateChanged() {
            Refresh(false);
        }

        void InitNpc() {
            Npc = GetOrCreateNpc();
            ParentModel.StickToReferenceOverride = Npc?.ParentModel.LocationView;
            UniqueNpcUtils.Check(Npc);
            _flagBasedAvailability = _spec.FlagAvailability.Get(true);
            OnBandChanged(ParentModel.GetCurrentBandSafe(LocationCullingGroup.LastBand));
        }

        void EnsureListeners() {
            if (_listenersAdded) {
                return;
            }
            
            if (!_spec.Manual && _spec.FlagAvailability.HasFlag) {
                World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.UniqueFlagChanged(_spec.FlagAvailability.Flag), this, RefreshFlagAvailability);
            }

            ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, OnBandChanged, this);
            ParentModel.ListenTo(IIdleDataSource.Events.InteractionIntervalChanged, OnIntervalChanged, this);
            ParentModel.ListenTo(IIdleDataSource.Events.InteractionOneShotTriggered, OnOneShotTriggered, this);
            Npc.ListenTo(IAlive.Events.BeforeDeath, OnNpcDeath, this);
            Npc.ListenTo(Model.Events.AfterDiscarded, OnNpcDiscard, this);
            
            _listenersAdded = true;
        }

        bool Validate() {
            if (Template == null) {
                Log.Important?.Error($"NpcPresence on location {ParentModel} has no template", _spec);
                return false;
            }

            if (Template.GetComponent<NpcAttachment>() == null) {
                Log.Important?.Error($"NpcPresence on location {ParentModel} has template {Template} with no NpcAttachment", Template);
                return false;
            }

            if (!ParentModel.Spec.TryGetComponentInChildren(out IdleDataAttachment _)) {
                Log.Important?.Error($"NpcPresence on location {ParentModel} has no IdleDataAttachment", _spec);
                return false;
            }

            return true;
        }

        NpcElement GetOrCreateNpc() {
            if (!NpcRegistry.TryGetNpc(Template, out var npc)) {
                var transform = _spec.transform;
                var location = Template.SpawnLocation(AbyssPosition, transform.rotation, Template.transform.localScale);

                location.MoveToDomain(Domain.Gameplay);

                npc = location.Element<NpcElement>();
                if (npc.ParentModel.TryGetElement(out IdleDataElement data)) {
                    Log.Minor?.Error($"Npc ({LogUtils.GetDebugName(npc)}) with unique presence cannot have IdleDataAttachment!", npc.ParentModel.Spec);
                    data.Discard();
                }
            }

            return npc;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_attached && IsNpcAlive() && !Npc.HasBeenDiscarded) {
                Npc.ResetNpcPresence();
            }
        }

        void OnBandChanged(int band) {
            _inBand = LocationCullingGroup.InNpcVisibilityBand(band);
            Refresh(false);
        }

        void OnIntervalChanged(IIdleDataSource data) {
            if (_attached) {
                if (NpcHasBeenDiscarded()) {
                    MarkAsDead();
                    return;
                }

                if (!IsNpcAlive()) {
                    return;
                }

                Npc.ParentModel.Trigger(IIdleDataSource.Events.InteractionIntervalChanged, data);
            }
        }

        void OnOneShotTriggered(InteractionOneShotData oneShotData) {
            if (_attached) {
                if (NpcHasBeenDiscarded()) {
                    MarkAsDead();
                    return;
                }

                if (!IsNpcAlive()) {
                    return;
                }

                Npc.ParentModel.Trigger(IIdleDataSource.Events.InteractionOneShotTriggered, oneShotData);
            }
        }

        void OnNpcDiscard(Model model) {
            if (model.WasDiscardedFromDomainDrop) {
                return;
            }

            MarkAsDead();
        }

        void OnNpcDeath(DamageOutcome obj) {
            RemoveListeners();
        }

        void RemoveListeners() {
            World.EventSystem.RemoveAllListenersOwnedBy(this);
            _listenersAdded = false;     
        }

        public void Attach(NpcElement npc) {
            _attached = true;
            Npc = npc;

            Npc.ParentModel.SetInteractability(LocationInteractability.Active);
            this.Trigger(Events.AttachedNpc, Npc);
        }

        public void Detach(NpcElement npc, NpcPresenceDetachReason reason) {
            _attached = false;

            if (reason == NpcPresenceDetachReason.Death) {
                return;
            }

            Npc.ParentModel.Trigger(IIdleDataSource.Events.InteractionIntervalChanged, null);

            if (reason == NpcPresenceDetachReason.MySceneUnloading) {
                npc.Movement?.Controller?.MoveToAbyss();
            }
            
            this.Trigger(Events.DetachedNpc, Npc);
        }

        public void SetManualAvailability(bool value, bool forceTeleport) {
            if (!_spec.Manual) {
                Log.Minor?.Error($"ForceAvailable can only be called on manual npc presence {LogUtils.GetDebugName(ParentModel)}", ParentModel.Spec);
                return;
            }

            _manualAvailabilityOneSession = null;
            _manualAvailabilitySaved = value;

            Refresh(forceTeleport);
        }

        void RefreshFlagAvailability() {
            _flagBasedAvailability = _spec.FlagAvailability.Get(true);

            bool flagForceTeleport = _flagBasedAvailability ? _spec.ForceTeleportIn : _spec.ForceTeleportOut;
            Refresh(flagForceTeleport);
        }

        void Refresh(bool forceTeleport) {
            if (NpcHasBeenDiscarded()) {
                MarkAsDead();
                return;
            }

            if (!IsNpcAlive()) {
                return;
            }

            if (!Npc.HasCompletelyInitialized) {
                Npc.OnCompletelyInitialized(_ => Refresh(forceTeleport));
                return;
            }

            if (forceTeleport && Available) {
                _inBand = Available;
            }

            if (Available && !_attached && (_inBand || Npc.NpcPresence == null)) {
                if (forceTeleport) {
                    NpcTeleporter.Teleport(Npc, _spec.transform.position, TeleportContext.PresenceRefresh);
                    Npc.Movement?.Controller?.SetRotationInstant(_spec.transform.rotation);
                }
                Npc.ChangeNpcPresence(this);
            } else if (_attached && !Available) {
                if (forceTeleport) {
                    Npc.Movement.Controller.MoveToAbyss();
                }

                Npc.ResetNpcPresence();
            }
        }

        void MarkAsDead() {
            if (IsNpcAlive()) {
                NpcRegistry.MarkAsDead(Template);
            }

            RemoveListeners();
            Detach(Npc, NpcPresenceDetachReason.Death);
        }

        public bool IsMine(NpcElement npc) {
            return npc.ParentModel.Template == Template;
        }

        bool NpcHasBeenDiscarded() => Npc == null || Npc.HasBeenDiscarded;

        bool IsNpcAlive() => NpcRegistry.IsAlive(Template);

        public bool AllowIntervalChange(IIdleDataSource data) => data == IdleData;

        public static bool InAbyss(Vector3 position) {
            return (position - NpcPresence.AbyssPosition).sqrMagnitude < 10000f; //AbyssPosition +/- 100 units
        }
    }
}