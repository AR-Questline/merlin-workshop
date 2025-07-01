using Awaken.TG.Main.AI.Combat.CustomDeath;
using Awaken.TG.Main.AI.Combat.CustomDeath.Forwarder;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Fights.NPCs {
    public partial class NpcKillOnSpawnElement : Element<Location>, IRefreshedByAttachment<NpcKillOnSpawnAttachment> {
        public override ushort TypeForSerialization => SavedModels.NpcKillOnSpawnElement;

        NpcPresence _presence;
        Location _location;
        NpcKillOnSpawnAttachment _spec;
        
        public CustomDeathAnimation CustomDeathAnimation => _spec.customDeathAnimation;

        public void InitFromAttachment(NpcKillOnSpawnAttachment spec, bool isRestored) {
            _spec = spec;
            if (_spec.useCustomAnimation) {
                CustomDeathAnimation.Preload();
            }
        }
        
        protected override void OnFullyInitialized() {
            _presence = ParentModel.TryGetElement<NpcPresence>();
            if (_presence == null) {
                Discard();
                return;
            }
            
            if (_presence.Attached) {
                TryKill();
            } else if (_presence.AliveNpc is { } npc) {
                _presence.ListenTo(NpcPresence.Events.AttachedNpc, TryKill, this);
            }
        }
        
        void TryKill() {
            var npc = _presence.AliveNpc;
            if (npc == null) {
                return;
            }
            
            _location ??= npc.ParentModel;
            if (_spec.useCustomAnimation) {
                _location.AddElement(new NpcKillOnSpawnDeathAnimationForwarder(this));
            }
            
            if (npc.IsVisible) {
                Kill().Forget();
            } else {
                //That's a hack. NPCs can't properly die and create ragdolls if they are not visible when they die.
                //TODO unhack it and maybe whole dying when invisible
                _location.ListenTo(NpcElement.Events.AfterNpcInVisualBand, OnNpcVisible, this);
            }
        }

        void OnNpcVisible(NpcElement _) {
            Kill().Forget();
        }

        async UniTaskVoid Kill() {
            if (_presence.AliveNpc is not { } npc) {
                return;
            }

            await Services.Get<IdleBehavioursRefresher>().WaitForNextUpdate();
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }

            if (npc is not { HasBeenDiscarded: false }) {
                return;
            }
            
            var location = npc.ParentModel;
            
            if (!_presence.Attached) {
                Log.Important?.Error($"{location} has no attached npc!");
                return;
            }

            npc.KeepCorpseAfterDeath = true;
            location.RemoveElementsOfType<KillPreventionElement>();
            location.MoveAndRotateTo(ParentModel.Coords, ParentModel.Rotation, true);

            if (location.HasElement<NoLocationCrimeOverride>()) {
                location.Kill(markDeathAsNonCriminal: _spec.disableCorpseAlert);
            } else {
                var noCrime = location.AddElement<NoLocationCrimeOverride>();
                location.Kill(markDeathAsNonCriminal: _spec.disableCorpseAlert);
                noCrime.Discard();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_spec.useCustomAnimation) {
                CustomDeathAnimation.UnloadPreload();
            }
        }
    }
}