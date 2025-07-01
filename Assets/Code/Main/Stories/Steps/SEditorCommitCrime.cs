using System;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Crime: Commit"), NodeSupportsOdin]
    public class SEditorCommitCrime : EditorStep {
        [TemplateType(typeof(CrimeOwnerTemplate)), HideLabel]
        public TemplateReference owner;

        [LabelWidth(120)]
        public float bountyMultiplier = 1f;
        public SimpleCrimeType crime;
        [LabelWidth(120)]
        public bool ignoreVisibility = true;
        [LabelWidth(120), Tooltip("Instant report skips temporary bounty")]
        public bool instantReport = true;

        [ShowIf(nameof(ShowCustomBounty))] public float customBounty = 100f;
        [ShowIf(nameof(ShowItemData))] public ItemSpawningData itemData;
        [ShowIf(nameof(ShowActorRef))] public ActorRef actorRef;

        bool ShowCustomBounty => crime is SimpleCrimeType.Custom;
        bool ShowItemData => crime is SimpleCrimeType.Theft or SimpleCrimeType.Pickpocket;
        bool ShowActorRef => crime is SimpleCrimeType.Murder or SimpleCrimeType.Pickpocket;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SCommitCrime {
                owner = owner,
                bountyMultiplier = bountyMultiplier,
                crime = crime,
                ignoreVisibility = ignoreVisibility,
                customBounty = customBounty,
                itemData = itemData,
                actorRef = actorRef,
            };
        }
    }

    public partial class SCommitCrime : StoryStep {
        public TemplateReference owner;

        public float bountyMultiplier = 1f;
        public SimpleCrimeType crime;
        public bool ignoreVisibility = true;
        public bool instantReport = true;

        public float customBounty = 100f;
        public ItemSpawningData itemData;
        public ActorRef actorRef;
        
        bool ShowCustomBounty => crime is SimpleCrimeType.Custom;
        CrimeItemValue ItemCrimeValue => itemData?.ItemTemplate(null)?.CrimeValue ?? CrimeItemValue.Medium;
        int ItemQuantity => itemData?.quantity ?? 1;
        CrimeNpcValue NpcCrimeValue(Story api) => StoryUtils.FindCharacter(api, actorRef.Get()) is NpcElement npc ? npc.CrimeValue : CrimeNpcValue.Medium;
        
        public override StepResult Execute(Story story) {
            if (crime is not SimpleCrimeType.None) {
                var situation = CommitCrime.GetSituation(ignoreVisibility, instantReport: instantReport);
                GetCrime(story).TryCommitCrime(situation);
            }
            return StepResult.Immediate;
        }
        
        Crime GetCrime(Story api) {
            var crimeSource = new FakeCrimeSource(owner.Get<CrimeOwnerTemplate>(), ShowCustomBounty ? bountyMultiplier * customBounty : bountyMultiplier);
            switch (crime) {
                case SimpleCrimeType.Trespassing:
                    return Crime.Trespassing(crimeSource);
                case SimpleCrimeType.Theft:
                    return Crime.Theft(ItemCrimeValue, crimeSource, ItemQuantity);
                case SimpleCrimeType.Pickpocket:
                    return Crime.Pickpocket(ItemCrimeValue, NpcCrimeValue(api), crimeSource, ItemQuantity);
                case SimpleCrimeType.Combat:
                    return Crime.Combat(NpcCrimeValue(api), crimeSource);
                case SimpleCrimeType.Murder:
                    return Crime.Murder(NpcCrimeValue(api), crimeSource);
                case SimpleCrimeType.Lockpicking:
                    return Crime.Lockpicking(crimeSource);
                case SimpleCrimeType.Custom:
                    return Crime.Custom(crimeSource);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}