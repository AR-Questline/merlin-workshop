using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.AI {
    public partial class TargetBlindedElement : TargetOverrideElement {
        ImaginaryTarget _imaginaryTarget;
        
        protected override ICharacter Target => _imaginaryTarget;
        
        public override bool IsValid => true;
        
        [UnityEngine.Scripting.Preserve]
        public static TargetBlindedElement BlindTarget(ICharacter character) {
            return character.AddElement(new TargetBlindedElement());
        }
        
        public TargetBlindedElement() : base(null, int.MaxValue) { }

        protected override void OnInitialize() {
            CreateImaginaryTarget();
            _active = true;
        }

        void CreateImaginaryTarget() {
            _imaginaryTarget = World.Add(new ImaginaryTarget());

            MoveImaginaryTarget().Forget();
            
            if (GetTarget(ParentModel) == Target) {
                if (ParentModel is NpcElement npc) {
                    npc.NpcAI.EnterCombatWith(Target, true);
                }
            }
        }
        
        async UniTaskVoid MoveImaginaryTarget() {
            while (_imaginaryTarget != null) {
                _imaginaryTarget.TeleportToRandomPoint(ParentModel.Coords);
                if (!await AsyncUtil.DelayTime(_imaginaryTarget, _imaginaryTarget.WaitTime)) {
                    return;
                }
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            if (_imaginaryTarget != null) {
                _imaginaryTarget.Discard();
                _imaginaryTarget = null;
            }
            base.OnDiscard(fromDomainDrop);
        }
    }
}
