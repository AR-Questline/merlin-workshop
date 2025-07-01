using System.Collections.Generic;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Idle.Interactions.Patrols;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Dialogue/Hero: Move To"), NodeSupportsOdin]
    public class SEditorHeroMove : SEditorCharacterMoveBase {
        public bool rotateTowardsMovement = true;
        public bool usePatrolPath;
        [ShowIf(nameof(usePatrolPath))] 
        public bool moveFromCurrentHeroPosition = true;
        [ShowIf(nameof(usePatrolPath)), Tags(TagsCategory.InteractionID)] 
        public string uniqueID;
        
        protected override bool ShowTargetSelection => !usePatrolPath;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SHeroMove {
                rotateTowardsMovement = rotateTowardsMovement,
                usePatrolPath = usePatrolPath,
                moveFromCurrentHeroPosition = moveFromCurrentHeroPosition,
                uniqueID = uniqueID,
                
                stoppingDistance = stoppingDistance,
                waitForEnd = waitForEnd,
                moveToType = moveToType,
                target = target,
                targetPos = targetPos,
                moveToOffsetType = moveToOffsetType,
                offset = offset,
            };
        }
    }

    public partial class SHeroMove : SCharacterMoveBase {
        public bool rotateTowardsMovement = true;
        public bool usePatrolPath;
        public bool moveFromCurrentHeroPosition = true;
        public string uniqueID;
        
        protected override IEnumerable<ICharacter> CharactersToMove(Story api) {
            return new ICharacter[] { api.Hero };
        }

        protected override void TryMoveCharacter(ICharacter character, Story api, StepResult result) {
            if (character is Hero hero) {
                MoveHero(hero, api, result);
            }
        }
        
        void MoveHero(Hero hero, Story api, StepResult result) {
            if (hero.TrySetMovementType<DialogueNavmeshBasedMovement>(out var movement)) {
                if (usePatrolPath) {
                    var patrol = (PatrolInteraction) World.Services.Get<InteractionProvider>().GetUniqueSearchable(uniqueID);
                    movement.SetPatrolPath(patrol.PatrolPath, moveFromCurrentHeroPosition, rotateTowardsMovement);
                } else {
                    movement.SetABPath(ExtractTargetPos(api, hero), rotateTowardsMovement);
                }
                movement.ListenTo(Model.Events.BeforeDiscarded, result.Complete);
            } else {
                Log.Minor?.Error($"Hero movement system hasn't been set to DialogueNavmeshBasedMovement. Current movement is {hero.MovementSystem.Type}");
                result.Complete();
            }
        }
    }
}