using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UniversalProfiling;

namespace Awaken.TG.Main.AI.Combat {
    public class CombatDirector : IService {
        public const float CombatPositionUpdateInterval = 0.2f;

        static readonly UniversalProfilerMarker UpdateMarker = new(Color.blue, $"{nameof(CombatDirector)}.{nameof(OnUpdate)}");
        // === Fields
        readonly List<WeakModelRef<EnemyBaseClass>> _enemies = new();
        readonly List<WeakModelRef<EnemyBaseClass>> _enemiesToAdd = new();
        readonly List<WeakModelRef<EnemyBaseClass>> _enemiesToRemove = new();
        readonly List<WeakModelRef<EnemyBaseClass>> _enemiesWithAttacksBooked = new();
        readonly List<UnBookCancellation> _unBookCancellationTokens = new();
        float _combatPositionUpdateDelay;

        // === Public API
        public static void AddEnemyToFight(EnemyBaseClass enemyBaseClass) {
            CombatDirector combatDirector = World.Services.Get<CombatDirector>();
            if (combatDirector._enemies.Contains(enemyBaseClass)) {
                return;
            }

            combatDirector._enemiesToRemove.Remove(enemyBaseClass);
            combatDirector._enemiesToAdd.Add(enemyBaseClass);
        }

        public static void RemoveEnemyFromFight(EnemyBaseClass enemyBaseClass) {
            CombatDirector combatDirector = World.Services.Get<CombatDirector>();
            combatDirector._enemiesToAdd.Remove(enemyBaseClass);
            combatDirector._enemiesToRemove.Add(enemyBaseClass);
            combatDirector._enemiesWithAttacksBooked.Remove(enemyBaseClass);
        }

        public static void BookAttackAction(EnemyBaseClass enemyBaseClass) {
            CombatDirector combatDirector = World.Services.Get<CombatDirector>();
            CancelUnBook();
            enemyBaseClass.TryPlayAttackOutsideFOVWarning();

            if (combatDirector._enemiesWithAttacksBooked.Contains(enemyBaseClass)) {
                return;
            }

            combatDirector._enemiesWithAttacksBooked.Add(enemyBaseClass);

            void CancelUnBook() {
                for (int index = combatDirector._unBookCancellationTokens.Count - 1; index >= 0; index--) {
                    UnBookCancellation unBookCancellation = combatDirector._unBookCancellationTokens[index];
                    if (unBookCancellation.enemyBaseClass.Get() == enemyBaseClass) {
                        unBookCancellation.cts.Cancel();
                        combatDirector._unBookCancellationTokens.RemoveAt(index);
                        break;
                    }
                }
            }
        }

        public static void UnBookAttackAction(EnemyBaseClass enemyBaseClass) {
            float delay = World.Only<DifficultySetting>().Difficulty.AttackActionUnBookProlong;
            World.Services.Get<CombatDirector>().UnBookAttackActionAfterDelay(enemyBaseClass, delay).Forget();
        }

        async UniTaskVoid UnBookAttackActionAfterDelay(EnemyBaseClass enemyBaseClass, float delay) {
            CancellationTokenSource cts = new();
            var unBookCancellation = new UnBookCancellation(enemyBaseClass, cts);
            _unBookCancellationTokens.Add(unBookCancellation);
            if (await AsyncUtil.DelayTime(enemyBaseClass, delay, source: cts)) {
                _enemiesWithAttacksBooked.Remove(enemyBaseClass);
            }

            _unBookCancellationTokens.Remove(unBookCancellation);
        }

        public static bool AnyAttackActionBooked() {
            return AttackActionsBooked() > 0;
        }

        public static int AttackActionsBooked() {
            CombatDirector combatDirector = World.Services.Get<CombatDirector>();
            combatDirector._enemiesWithAttacksBooked.RemoveAll(static enemyRef => !enemyRef.Exists());

            // --- We are alone in this fight, so we don't need to prevent others from attacking
            if (combatDirector._enemies.Count <= 1) {
                return 0;
            }

            return combatDirector._enemiesWithAttacksBooked.Count;
        }

        // === Initialization
        public void Init() {
            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopSystem[] updateLoopSystems = playerLoop.subSystemList;
            int updateIndex = Array.FindIndex(updateLoopSystems, static s => s.type == typeof(Update));
            ref PlayerLoopSystem updateLoopSystem = ref updateLoopSystems[updateIndex];
            CreateCombatDirectorUpdateSystem(ref updateLoopSystem);
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        void CreateCombatDirectorUpdateSystem(ref PlayerLoopSystem updateLoopSystem) {
            PlayerLoopSystem combatDirectorUpdateSystem = new() {
                type = typeof(CombatDirector),
                updateDelegate = OnUpdate
            };
            ref PlayerLoopSystem[] subsystems = ref updateLoopSystem.subSystemList;
            Array.Resize(ref subsystems, subsystems.Length + 1);
            subsystems[^1] = combatDirectorUpdateSystem;
        }

        void OnUpdate() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return;
            }
#endif
            using var marker = UpdateMarker.Auto();
            _combatPositionUpdateDelay -= Time.deltaTime;
            bool updateCombatPosition = _combatPositionUpdateDelay < 0;
            // --- Manage enemies List
            _enemies.AddRange(_enemiesToAdd);
            _enemiesToAdd.Clear();

            foreach (var enemyRefToRemove in _enemiesToRemove) {
                _enemies.Remove(enemyRefToRemove);
            }
            _enemiesToRemove.Clear();
            // --- Remove all enemies that have been discarded
            _enemiesWithAttacksBooked.RemoveAll(static enemyRef => !enemyRef.Exists());
            _enemies.RemoveAll(static enemyRef => !enemyRef.Exists());
            if (_enemies.Count <= 0) {
                return;
            }

            // --- Get enemies ordered by aggressiveness
            if (updateCombatPosition) {
                _combatPositionUpdateDelay = CombatPositionUpdateInterval;
                var hero = Hero.Current;
                // --- If hero died don't update enemies
                if (hero == null || hero.HasBeenDiscarded || !hero.IsAlive) {
                    return;
                }
                Vector3 heroForward = hero.Rotation * Vector3.forward;
                var heroCombatSlots = hero.CombatSlots; 
                foreach (var enemyRef in _enemies) {
                    var aggressionScore = enemyRef.Get().UpdateAggressionScore(heroForward);
                    heroCombatSlots.UpdateEnemyAggressionScore(enemyRef, aggressionScore);
                }
            }

            _enemies.Sort(static (enemyRef1, enemyRef2) => enemyRef2.Get().AggressionScore.CompareTo(enemyRef1.Get().AggressionScore));

            foreach (EnemyBaseClass enemy in _enemies) {
                if (updateCombatPosition) {
                    bool attackActionBooked = _enemiesWithAttacksBooked.Contains(enemy);
                    if (!enemy.RequiresCombatSlot) {
                        enemy.SetDesiredPosition(GetNotCombatSlotTargetPosition(enemy));
                    } else if (!attackActionBooked && enemy.TrySetBetterCombatSlot(1f, out var slotPosition)) {
                        enemy.SetDesiredPosition(slotPosition);
                    }
                }

                enemy.CombatUpdate(null);
            }
        }

        Vector3 GetNotCombatSlotTargetPosition(EnemyBaseClass enemy) {
            var npc = enemy.NpcElement;
            var target = npc?.GetCurrentTarget();
            var calculatedDesiredDistanceToTarget = DistancesToTargetHandler.DesiredDistanceToTarget(npc, target);
            return CombatBehaviourUtils.GetTargetPosition(enemy, target, calculatedDesiredDistanceToTarget, KeepPositionBehaviour.TargetPositionAcceptRange).Position;
        }

        readonly struct UnBookCancellation : IEquatable<UnBookCancellation> {
            public readonly WeakModelRef<EnemyBaseClass> enemyBaseClass;
            public readonly CancellationTokenSource cts;

            public UnBookCancellation(EnemyBaseClass enemyBaseClass, CancellationTokenSource cts) {
                this.enemyBaseClass = enemyBaseClass;
                this.cts = cts;
            }

            public bool Equals(UnBookCancellation other) {
                return enemyBaseClass.Equals(other.enemyBaseClass) && Equals(cts, other.cts);
            }

            public override bool Equals(object obj) {
                return obj is UnBookCancellation other && Equals(other);
            }

            public override int GetHashCode() {
                return HashCode.Combine(enemyBaseClass, cts);
            }
        }
    }
}