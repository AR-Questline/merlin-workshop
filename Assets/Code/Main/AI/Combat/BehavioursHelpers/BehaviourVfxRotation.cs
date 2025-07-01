using System;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Grounds;
using Awaken.Utility.Enums;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.BehavioursHelpers {
    public class BehaviourVfxRotation : RichEnum {
        Func<EnemyBaseClass, Vector3, Quaternion> _getVfxRotationFunc;

        BehaviourVfxRotation(string enumName, Func<EnemyBaseClass, Vector3, Quaternion> getVfxRotationFunc) : base(enumName) {
            _getVfxRotationFunc = getVfxRotationFunc;
        }

        [UnityEngine.Scripting.Preserve]
        public static readonly BehaviourVfxRotation
            None = new(nameof(None), (baseClass, spawnPosition) => Quaternion.identity),
            NpcForward = new(nameof(NpcForward), (baseClass, spawnPosition) => Quaternion.LookRotation(baseClass.NpcElement.Forward().X0Z().normalized)),
            FromSpawnToForward = new(nameof(FromSpawnToForward), (baseClass, spawnPosition) => GetFromSpawnToForwardRotation(baseClass, spawnPosition, 1)),
            FromSpawnToForwardDoubled = new(nameof(FromSpawnToForwardDoubled), (baseClass, spawnPosition) => GetFromSpawnToForwardRotation(baseClass, spawnPosition, 2)),
            FromSpawnToForwardTripled = new(nameof(FromSpawnToForwardTripled), (baseClass, spawnPosition) => GetFromSpawnToForwardRotation(baseClass, spawnPosition, 3)),
            MainHandForward = new(nameof(MainHandForward), (baseClass, spawnPosition) => Quaternion.LookRotation(baseClass.NpcElement.MainHand.forward.X0Z().normalized)),
            OffHandForward = new(nameof(OffHandForward), (baseClass, spawnPosition) => Quaternion.LookRotation(baseClass.NpcElement.MainHand.forward.X0Z().normalized)),
            FromNpcToTarget = new(nameof(FromNpcToTarget), GetRotationToTarget);

        static Quaternion GetFromSpawnToForwardRotation(EnemyBaseClass baseClass, Vector3 spawnPosition, float forwardMultiplier) {
            return Quaternion.LookRotation((baseClass.NpcElement.Coords + baseClass.NpcElement.Forward() * forwardMultiplier - spawnPosition).X0Z().normalized);
        }
        
        static Quaternion GetRotationToTarget(EnemyBaseClass enemy, Vector3 spawnPosition) {
            var target = enemy.NpcElement?.GetCurrentTarget();
            if (target == null) {
                return NpcForward._getVfxRotationFunc(enemy, spawnPosition);
            }
            
            Vector3 direction = (target.Coords - enemy.Coords).normalized;
            return Quaternion.LookRotation(direction);
        }

        public Quaternion GetVfxRotation(EnemyBaseClass baseClass, Vector3 spawnPosition) => _getVfxRotationFunc(baseClass, spawnPosition);
    }
}
