using System;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Tags {
    [Serializable]
    public struct FlagLogic {
        const string HorizontalGroup = "Horizontal";
        const string RightGroup = HorizontalGroup + "/Right";
        const string BottomGroup = "Bottom";
        
        [SerializeField, HorizontalGroup(HorizontalGroup), Tags(TagsCategory.Flag), HideLabel] string flag;
        [SerializeField, HorizontalGroup(HorizontalGroup, 100), HideIfGroup(RightGroup, Condition = nameof(NoFlag)), HideLabel] LogicType resultProcessing;

        [HideIfGroup(BottomGroup, Condition = nameof(NoFlag))]
        // TODO: Fix this after release by proper drawer or smth
        [ShowInInspector, HideIfGroup(BottomGroup), ShowIf(nameof(Simple))]
        bool FlagIs {
            get => !resultShouldBeInversed;
            set => resultShouldBeInversed = !value;
        }
        [SerializeField, HideInInspector] bool resultShouldBeInversed;
        [SerializeField, HideIfGroup(BottomGroup), ShowIf(nameof(Advanced))] bool resultIfNoFlag;
        [SerializeField, HideIfGroup(BottomGroup), ShowIf(nameof(Advanced))] bool resultIfFlagTrue;
        [SerializeField, HideIfGroup(BottomGroup), ShowIf(nameof(Advanced))] bool resultIfFlagFalse;

        public readonly bool Get(bool emptyFlagResult = false) {
            if (NoFlag) return emptyFlagResult;
            var facts = World.Services.Get<GameplayMemory>().Context();
            return resultProcessing switch {
                LogicType.Simple => GetSimple(facts, flag),
                LogicType.Advanced => GetAdvanced(facts, flag),
                _ => false
            };
        }
    
        readonly bool GetSimple(ContextualFacts facts, string flag) {
            bool value = facts.Get<bool>(flag);
            return resultShouldBeInversed ? !value : value;
        }

        readonly bool GetAdvanced(ContextualFacts facts, string flag) {
            if (facts.HasValue(flag)) {
                return facts.Get<bool>(flag) ? resultIfFlagTrue : resultIfFlagFalse;
            } else {
                return resultIfNoFlag;
            }
        }

        readonly bool NoFlag => flag.IsNullOrWhitespace();
        readonly bool Simple => resultProcessing == LogicType.Simple;
        readonly bool Advanced => resultProcessing == LogicType.Advanced;
        
        public readonly string Flag => flag;
        public readonly bool HasFlag => !NoFlag;
        
        enum LogicType : byte {
            Simple,
            Advanced,
        }
    }
}