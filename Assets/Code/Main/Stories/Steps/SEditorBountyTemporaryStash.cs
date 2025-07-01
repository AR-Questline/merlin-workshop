using System;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Bounty: Temporary Stash"), NodeSupportsOdin]
    public class SEditorBountyTemporaryStash : EditorStep {
        [DisableIf(nameof(IsCrimeOwnerSet))]
        public LocationReference guard;
        [TemplateType(typeof(CrimeOwnerTemplate))]
        public TemplateReference crimeOwner;
        public StashAction stashAction;

        bool IsCrimeOwnerSet => crimeOwner?.IsSet ?? false;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SBountyTemporaryStash {
                guard = guard,
                crimeOwnerTemplate = crimeOwner,
                stashAction = stashAction,
            };
        }
    }

    public partial class SBountyTemporaryStash : StoryStep {
        public LocationReference guard;
        public TemplateReference crimeOwnerTemplate;
        public StashAction stashAction;
        
        public override StepResult Execute(Story story) {
            var crimeOwner = crimeOwnerTemplate.TryGet<CrimeOwnerTemplate>();
            if (crimeOwner == null) {
                StoryUtils.TryGetCrimeOwnerTemplate(story, guard, out crimeOwner);
            }
            var currentStash = TemporaryBountyStashElement.TryGet(crimeOwner);
            switch (stashAction) {
                case StashAction.StashAway:
                    if (currentStash == null) {
                        var currentBounty = CrimeUtils.Bounty(crimeOwner);
                        Hero.Current.AddElement(new TemporaryBountyStashElement(crimeOwner, currentBounty));
                        CrimeUtils.ClearBounty(crimeOwner);
                    } else {
                        Log.Important?.Error($"Temporary Bounty Stash Element is already registered {LogUtils.GetDebugName(story)}");
                    }
                    break;
                case StashAction.ApplyAgain:
                    if (currentStash != null) {
                        var crimeSource = new FakeCrimeSource(crimeOwner, currentStash.value);
                        Crime.Custom(crimeSource, CrimeSituation.IgnoresVisibility | CrimeSituation.InstantReport).TryCommitCrime();
                        currentStash.Discard();
                    } else {
                        Log.Important?.Error($"Temporary Bounty Stash Element is missing {LogUtils.GetDebugName(story)}");
                    }
                    break;
                case StashAction.Forget:
                    if (currentStash != null) {
                        currentStash.Discard();
                    } else {
                        Log.Important?.Error($"Temporary Bounty Stash Element is missing {LogUtils.GetDebugName(story)}");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return StepResult.Immediate;
        }
    }

    public partial class TemporaryBountyStashElement : Element<Hero> {
        public sealed override bool IsNotSaved => true;
        
        public readonly float value;
        public readonly CrimeOwnerTemplate owner;

        public static TemporaryBountyStashElement TryGet(CrimeOwnerTemplate owner) {
            foreach (var element in Hero.Current.Elements<TemporaryBountyStashElement>()) {
                if (element.owner == owner) {
                    return element;
                }
            }
            return null;
        }
        
        public TemporaryBountyStashElement(CrimeOwnerTemplate owner, float value) {
            this.value = value;
            this.owner = owner;
        }
    }

    public enum StashAction {
        StashAway,
        ApplyAgain,
        Forget,
    }
}