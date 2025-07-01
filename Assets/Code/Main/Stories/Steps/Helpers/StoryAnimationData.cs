using System.Threading;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Animations.FSM.Npc.States.Custom;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    public class StoryAnimationData {
        const float TransitionDuration = 0.6f;
        
        public NpcElement npcElement;
        public ARNpcAnimancer arNpcAnimancer;
        public AnimationClip animationClip;
        CancellationToken _token;

        public async UniTaskVoid StartAnimation(CancellationToken stopToken) {
            bool inSimpleInteraction = false;
            bool topBodyOnly = false;
            if (npcElement.Behaviours.CurrentUnwrappedInteraction is SimpleInteractionBase simpleInteraction) {
                inSimpleInteraction = simpleInteraction.CanTalkInInteraction;
                topBodyOnly = inSimpleInteraction && simpleInteraction.TalkRotateOnlyUpperBody;
            }

            NpcCustomActionsFSM customActionsFSM = npcElement.Element<NpcCustomActionsFSM>();
            NpcTopBodyFSM topBodyFSM = npcElement.Element<NpcTopBodyFSM>();
            bool isGesticulating = topBodyOnly
                ? topBodyFSM.CurrentAnimatorState.Type == NpcStateType.CustomGesticulate
                : customActionsFSM.CurrentAnimatorState.Type == NpcStateType.CustomGesticulate;
            bool isInTransition = customActionsFSM.AnimancerLayer.IsInTransition();

            NpcFSMType fsmType = topBodyOnly ? NpcFSMType.TopBodyFSM : NpcFSMType.CustomActionsFSM;
            _token = stopToken;
            
            if (isGesticulating) {
                npcElement.SetAnimatorState(fsmType, topBodyOnly || !inSimpleInteraction ? NpcStateType.None : NpcStateType.CustomLoop, TransitionDuration);
            }
            if (isGesticulating || isInTransition) {
                if (!await AsyncUtil.DelayTime(npcElement, TransitionDuration, _token)) return;
            }
            
            if (arNpcAnimancer == null || animationClip == null) {
                await EnterCustomStoryTalking(_token);
            } else {
                npcElement.SetAnimatorState(fsmType, NpcStateType.CustomGesticulate);
                npcElement.GetAnimatorSubstateMachine(fsmType).Element<CustomGesticulate>().PlayClip(animationClip);
                await AsyncUtil.UntilCancelled(_token);
            }

            if (npcElement is { HasBeenDiscarded: false }) {
                npcElement.SetAnimatorState(fsmType, topBodyOnly || !inSimpleInteraction ? NpcStateType.None : NpcStateType.CustomLoop, TransitionDuration);
            }
        }

        async UniTask EnterCustomStoryTalking(CancellationToken stopToken) {
            NpcCustomActionsFSM customActionsFSM = npcElement.Element<NpcCustomActionsFSM>();
            customActionsFSM.StoryLoopTalking = true;
            await AsyncUtil.UntilCancelled(stopToken);
            customActionsFSM.StoryLoopTalking = false;
        }
    }
}