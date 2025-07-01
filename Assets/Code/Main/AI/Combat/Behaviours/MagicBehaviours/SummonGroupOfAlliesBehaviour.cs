using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Attachments.Bosses;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public partial class SummonGroupOfAlliesBehaviour : SummonAllyBehaviour {
        const float RowSize = 3, PositionOffset = 3;
        
        [SerializeField] GroupSpawnerAlly[] alliesToSpawn;
        [SerializeField] LocationReference spawnLocation;

        int _amountOfSpawnsThisCombat;

        Location SpawnLocation {
            get {
                _spawnLocationRef ??= new WeakReference<Location>(spawnLocation.MatchingLocations(null).FirstOrDefault());
                return _spawnLocationRef.TryGetTarget(out Location location) ? location : null;
            }
        }
        WeakReference<Location> _spawnLocationRef;
        List<Location> _spawnedLocations;
        bool _canBeInvoked;

        // === Events
        public new static class Events {
            public static readonly Event<EnemyBaseClass, bool> AllSummonsKilled = new(nameof(AllSummonsKilled));
            public static readonly Event<SummonGroupOfAlliesBehaviour, bool> ResetGroupSpawner = new(nameof(ResetGroupSpawner));
        }

        protected override void OnInitialize() {
            _spawnedLocations = new List<Location>();
            _canBeInvoked = true;
            this.ListenTo(Events.ResetGroupSpawner, () => _canBeInvoked = true, this);
            Npc.ListenTo(ICharacter.Events.CombatExited, () => _amountOfSpawnsThisCombat = 0, this);
            base.OnInitialize();
        }

        public override bool UseConditionsEnsured() => !IsMuted && _canBeInvoked && SpawnLocation != null;
        
        protected override void OnAnimationSpawnTriggered() {
            _canBeInvoked = false;
            Transform spawnPosition = SpawnLocation.ViewParent;
            
            Vector3 positionOffset = Vector3.zero;
            int j = 0;
            foreach (var groupOfAllies in alliesToSpawn) {
                int amountToSpawn = groupOfAllies.AmountToSpawn;
                if (groupOfAllies.IncreaseAmountAfterEachSpawn) {
                    amountToSpawn += _amountOfSpawnsThisCombat;
                    if (amountToSpawn > groupOfAllies.MaxAmountToSpawn) {
                        amountToSpawn = groupOfAllies.MaxAmountToSpawn;
                    }
                }
                
                for (int i = 0; i < amountToSpawn; i++) {
                    if (j > 0 && j % RowSize == 0) {
                        positionOffset = Vector3.zero + spawnPosition.forward * -PositionOffset * (i/RowSize);
                    }
                
                    Location spawnedAlly = SpawnSummon(groupOfAllies.AllyToSpawn, spawnPosition.position + positionOffset, spawnPosition.rotation);
                    _spawnedLocations.Add(spawnedAlly);
                    // --- Listeners
                    spawnedAlly.TryGetElement<IAlive>()?.ListenTo(Model.Events.BeforeDiscarded,
                        _ => AfterSummonKilled(spawnedAlly), this);
                
                    // --- Position Update
                    positionOffset += spawnPosition.right * PositionOffset;
                    j++;
                }
            }
            _amountOfSpawnsThisCombat++;
            
            if (ParentModel is MistbearerCombatBase mistbearerCombat) {
                mistbearerCombat.allSummonsKilled = false;
                mistbearerCombat.StartWaitBehaviour();
            }
        }

        void AfterSummonKilled(Location location) {
            if (IsBeingDiscarded || Npc is not { IsAlive: true }) {
                return;
            }
            _spawnedLocations.Remove(location);
            if (_spawnedLocations.Count <= 0) {
                ParentModel.Trigger(Events.AllSummonsKilled, true);
            }
        }

        [Serializable]
        public class GroupSpawnerAlly {
            [SerializeField] int amountToSpawn;
            [SerializeField] bool increaseAmountAfterEachSpawn;
            [SerializeField, ShowIf(nameof(increaseAmountAfterEachSpawn))] int maxAmountToSpawn;
            
            [InfoBox("Cannot be unique npc", InfoMessageType.Error, nameof(NotRepetitiveNpc))]
            [SerializeField, TemplateType(typeof(LocationTemplate))]
            TemplateReference allyToSpawn;

            public int AmountToSpawn => amountToSpawn;
            public bool IncreaseAmountAfterEachSpawn => increaseAmountAfterEachSpawn;
            public int MaxAmountToSpawn => maxAmountToSpawn;
            public LocationTemplate AllyToSpawn => allyToSpawn.Get<LocationTemplate>();
            
            bool NotRepetitiveNpc => RepetitiveNpcUtils.InvalidLocation(allyToSpawn);
        }
    }
}