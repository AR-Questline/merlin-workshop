using System;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class UntilIdle : NonEditableDuration<IWithDuration>, IEquatable<UntilIdle> {
        public override ushort TypeForSerialization => SavedModels.UntilIdle;

        [Saved] NpcElement _npc;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] UntilIdle() { }

        public UntilIdle(NpcElement npc) {
            _npc = npc;
        }

        bool _delayingCheck;

        public override bool Elapsed => false;
        public override string DisplayText => string.Empty;

        protected override void OnFullyInitialized() {
            if (_npc == null) {
                Log.Debug?.Error("UntilIdle spawned on null npc");
                DelayedDiscard().Forget();
                return;
            }
            _npc.ListenTo(Events.AfterElementsCollectionModified, DiscardElementCheck, this);
            _npc.ListenTo(NpcAI.Events.NpcStateChanged, StateChangedCheck, this);
            _npc.ListenTo(Events.AfterDiscarded, Discard, this);
            CheckIfInIdle().Forget();
        }

        async UniTaskVoid DelayedDiscard() {
            if (!await AsyncUtil.DelayFrame(this, 2)) {
                return;
            }
            Discard();
        }
        
        async UniTaskVoid CheckIfInIdle() {
            if (!await AsyncUtil.DelayFrame(this, 2)) {
                return;
            }

            if (_npc.HasBeenDiscarded || _npc.NpcAI.HasBeenDiscarded)
                return;

            if (_npc.NpcAI.InIdle || !_npc.NpcAI.Working) {
                Discard();
                Log.Debug?.Warning($"Until idle spawned on AI in idle state: {_npc}");
            }
        }

        void DiscardElementCheck(Element e) {
            if (_delayingCheck) {
                return;
            }
            if (e is not NpcDisappearedElement and not UnconsciousElement) {
                return;
            }
            if (!e.HasBeenDiscarded) {
                return;
            }
            if (_npc.IsUnconscious || _npc.IsDisappeared) {
                return;
            }
            if (_npc.NpcAI.InIdle) {
                Discard();
            }
            if (!_npc.NpcAI.Working) {
                // Safety check to see if nothing changed after few frames, "NpcDisappearedElement" needs time to reappear npc
                DelayedCheck().Forget();
            }
        }

        async UniTaskVoid DelayedCheck() {
            _delayingCheck = true;
            if (!await AsyncUtil.DelayFrame(this, 3)) {
                _delayingCheck = false;
                return;
            }
            _delayingCheck = false;
            if (_npc.IsUnconscious || _npc.IsDisappeared) {
                return;
            }
            if (_npc.NpcAI.InIdle || !_npc.NpcAI.Working) {
                Discard();
            }
        }

        void StateChangedCheck(Change<IState> ch) {
            if (_delayingCheck) {
                return;
            }
            if (_npc.IsUnconscious || _npc.IsDisappeared) {
                return;
            }
            if (ch.to is StateIdle or StateAINotWorking) {
                Discard();
            }
        }

        public bool Equals(UntilIdle other) {
            return _npc == other?._npc;
        }
    }
}