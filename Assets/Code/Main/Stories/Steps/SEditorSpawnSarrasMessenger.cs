using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using Pathfinding;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Technical: Spawn Sarras Messenger"), NodeSupportsOdin]
    public class SEditorSpawnSarrasMessenger : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SSpawnSarrasMessenger();
        }
    }

    public partial class SSpawnSarrasMessenger : StoryStep {
        public override StepResult Execute(Story story) {
            SpawnMessengerAsync().Forget();
            return StepResult.Immediate;
        }
        
        async UniTaskVoid SpawnMessengerAsync() {
            await UniTask.WaitWhile(() => Hero.Current == null || !Hero.Current.IsFullyInitialized);
            if (!await AsyncUtil.DelayFrame(Hero.Current, 2)) {
                return;
            }

            if (Hero.Current.IsInCombat() || World.HasAny<DuelController>() || World.HasAny<Story>()) {
                return;
            }
            
            if (!GetSpawnPosition(out var spawnPoint)) {
                return;
            }

            var rotation = Quaternion.LookRotation(Hero.Current.Coords - spawnPoint);
            var sarrasMessenger = CommonReferences.Get.SarrasMessenger.Get<LocationTemplate>().SpawnLocation(spawnPoint, rotation);
            sarrasMessenger.Element<NpcElement>()?.OnCompletelyInitialized(_ => {
                if (sarrasMessenger.HasBeenDiscarded || Hero.Current == null || Hero.Current.HasBeenDiscarded) {
                    return;
                }
                
                if (sarrasMessenger.TryGetElement<DialogueAction>(out var dialogueAction)) {
                    dialogueAction.StartDialogue(sarrasMessenger, dialogueAction.Bookmark, false);
                }
            });
        }

        bool GetSpawnPosition(out Vector3 spawnPoint) {
            const float MaxAngleTries = 6;
            const int MaxRangeTries = 3;
            const float AcceptableAngleThreshold = 10f;
            const float SpawnRangeMin = 8;
            const float SpawnRangeMax = 15;
            
            var heroForward = Hero.Current.Forward();
            var heroCoords = Hero.Current.Coords;
            var heroHeadPosition = Hero.Current.Head.position;

            var heroFoV = World.Any<HeroFoV>();
            var heroMainCamera = Hero.Current.VHeroController?.MainCamera;

            if (heroFoV == null || heroMainCamera == null) {
                spawnPoint = Vector3.zero;
                return false;
            }
            
            float cameraFoVAngle = Camera.VerticalToHorizontalFieldOfView(heroFoV.FoV, heroMainCamera.aspect) / 2f;
            FloatRange spawnRange = new(SpawnRangeMin, SpawnRangeMax);
            
            for (int i = 0; i < MaxAngleTries; i++) {
                bool useLeftSide = RandomUtil.WithProbability(0.5f);
                var randomAngle = Random.Range(cameraFoVAngle, cameraFoVAngle + AcceptableAngleThreshold);
                if (useLeftSide) {
                    randomAngle *= -1f;
                }
                
                var searchForward = Quaternion.AngleAxis(randomAngle, Vector3.up) * heroForward;
                for (int j = 0; j < MaxRangeTries; j++) {
                    spawnPoint = heroCoords + searchForward * spawnRange.RandomPick();
                    spawnPoint = Ground.SnapToGround(spawnPoint, findClosest: false);
                    
                    if (AIUtils.CanSee(spawnPoint + Vector3.up * 2, heroHeadPosition)) {
                        var spawnPointNode = AstarPath.active.GetNearest(spawnPoint).node;
                        var heroPositionNode = AstarPath.active.GetNearest(heroCoords).node;
                        if (heroPositionNode == null) {
                            spawnPoint = Vector3.zero;
                            return false;
                        }
                        if (spawnPointNode == null) {
                            continue;
                        }
                        if (PathUtilities.IsPathPossible(spawnPointNode, heroPositionNode)) {
                            return true;
                        }
                    }
                }
            }

            spawnPoint = Vector3.zero;
            return false;
        }
    }
}