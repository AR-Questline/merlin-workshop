using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.Utility;
using Awaken.Utility.TwoWayReferences;
using UnityEngine;

namespace Awaken.TG.Main.AI {
    public partial class ImaginaryTarget : DummyCharacter {
        const float MinWaitTime = 3f;
        const float MaxWaitTime = 6f;
        const float MinDistance = 3f;
        const float MaxDistance = 6f;

        public sealed override bool IsNotSaved => true;

        Vector3 _coords;
        Transform _head, _torso, _hips;

        FightingPair.LeftStorage _possibleTargets;
        FightingPair.RightStorage _possibleAttackers;

        public override Vector3 Coords => _coords;
        public override Transform Head => _head;
        public override Transform Torso => _torso;
        public override Transform Hips => _hips;
        public override Transform ParentTransform { get; }

        public float WaitTime => Random.Range(MinWaitTime, MaxWaitTime);

        public override ref FightingPair.LeftStorage PossibleTargets => ref _possibleTargets;
        public override ref FightingPair.RightStorage PossibleAttackers => ref _possibleAttackers;

        public ImaginaryTarget() {
            ParentTransform = new GameObject("Imaginary Target").transform;
            _head = new GameObject("Head").transform;
            _torso = new GameObject("Torso").transform;
            _hips = new GameObject("Hips").transform;
        }

        protected override void OnInitialize() {
            _head.SetParent(ParentTransform);
            _head.SetLocalPositionAndRotation(Vector3.up * 2f, Quaternion.identity);
            _torso.SetParent(ParentTransform);
            _torso.SetLocalPositionAndRotation(Vector3.up * 1.6f, Quaternion.identity);
            _hips.SetParent(ParentTransform);
            _hips.SetLocalPositionAndRotation(Vector3.up, Quaternion.identity);

            _possibleTargets = new TwoWayRef<ICharacter, ICharacter>.LeftStorage(this);
            _possibleAttackers = new TwoWayRef<ICharacter, ICharacter>.RightStorage(this);
        }

        public void TeleportToRandomPoint(Vector3 center) {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(MinDistance, MaxDistance);
            _coords = center + randomCircle.ToHorizontal3();
            ParentTransform.position = _coords;
            ParentTransform.LookAt(center);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Object.Destroy(ParentTransform.gameObject);
        }
    }
}
