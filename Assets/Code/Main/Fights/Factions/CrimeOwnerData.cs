using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions {
    [Serializable]
    public partial class CrimeOwnerData {
        public ushort TypeForSerialization => SavedTypes.CrimeOwnerData;

        const float GuardCallDuration = TemporaryBounty.CrimeApplyDelay * 2;
        [Saved] public CrimeOwnerTemplate CrimeOwner { get; private set; }
        [Saved] public List<NpcTemplate> MurderedNPCs { get; private set; } = new();
        public Vector3? LastCrimeLocationOfInterest { get; private set; }
        
        IEventListener _listener;
        CancellationTokenSource _guardCallEndCancellation;
        
        //TODO: paranoia events that decay with time
        // do we have a timed event listener of sort?

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public CrimeOwnerData() { }
        
        public CrimeOwnerData(CrimeOwnerTemplate crimeOwner) {
            CrimeOwner = crimeOwner;
        }
        
        public void AddMurderedNPC(NpcTemplate npc) {
            MurderedNPCs.Add(npc);
        }
        
        public void RemoveMurderedNPC(NpcTemplate npc) {
            MurderedNPCs.Remove(npc);
        }

        public void ClearParanoia() {
            foreach (ParanoiaEvent paranoiaEvent in World.All<ParanoiaEvent>().ToArraySlow()) {
                paranoiaEvent.Discard();
            }
        }
        
        public void CallForHelp(Vector3 location) {
            if (GuardIntervention.InterventionInProgress) {
                return;
            }
            
            ClearCallForHelp();
            
            LastCrimeLocationOfInterest = location;
            
            _listener = Hero.Current.ListenToLimited(ICharacter.Events.CombatExited, ClearCallForHelp, Hero.Current);
            DelayedCallCancelation().Forget();
        }

        async UniTaskVoid DelayedCallCancelation() {
            _guardCallEndCancellation?.Cancel();
            _guardCallEndCancellation = new CancellationTokenSource();
            if (!await AsyncUtil.DelayTime(Hero.Current, GuardCallDuration, token: _guardCallEndCancellation.Token)) {
                return;
            }
            ClearCallForHelp();
        }

        void ClearCallForHelp() {
            _guardCallEndCancellation?.Cancel();
            _guardCallEndCancellation?.Dispose();
            _guardCallEndCancellation = null;
            World.EventSystem.TryDisposeListener(ref _listener);
            LastCrimeLocationOfInterest = null;
        }
    }
}
