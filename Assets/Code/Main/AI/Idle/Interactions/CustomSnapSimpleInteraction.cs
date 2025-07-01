using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class CustomSnapSimpleInteraction : SimpleInteraction {
        const string SnapSettingGroup = InteractingGroup + "/Snap Settings";
        
        [SerializeField, FoldoutGroup(SnapSettingGroup)] Transform snapTransformOverride;
        [SerializeField, FoldoutGroup(SnapSettingGroup)] AdvancedSnapToPositionAndRotate.SetupData snapSettings = AdvancedSnapToPositionAndRotate.SetupData.Default;
        [SerializeField, FoldoutGroup(SnapSettingGroup)] bool snapToPreviousPositionOnExit = false;
        [SerializeField, FoldoutGroup(SnapSettingGroup), ShowIf(nameof(snapToPreviousPositionOnExit))] AdvancedSnapToPositionAndRotate.SetupData exitSnapSettings = AdvancedSnapToPositionAndRotate.SetupData.Default;
        
        Vector3 _npcPositionPreInteraction;
        
        public bool SnapTransformOverriden => snapTransformOverride != null;
        public override Transform Transform => SnapTransformOverriden ? snapTransformOverride : transform;
        protected override float SnapDuration => snapSettings.snapDuration;
        protected override MovementState TargetMovementState => new AdvancedSnapToPositionAndRotate(SnapToPosition, SnapToForward, gameObject, snapSettings);
        
        protected override void OnStart(NpcElement npc, InteractionStartReason reason) {
            _npcPositionPreInteraction = npc.Coords;
            base.OnStart(npc, reason);
        }

        protected override void OnEnd(NpcElement npc, InteractionStopReason reason) {
            base.OnEnd(npc, reason);
            
            if (IsStopping(npc) && snapToPreviousPositionOnExit) {
                var exitTargetMovementState = new AdvancedSnapToPositionAndRotate(_npcPositionPreInteraction, npc.Forward(), null, exitSnapSettings);
                npc.Movement?.InterruptState(exitTargetMovementState);
            }
        }
    }
}