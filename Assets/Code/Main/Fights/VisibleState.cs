using System;
using System.Collections.Generic;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Fights {
    public class VisibleState : RichEnum {
        // Need to be updated when adding/removing new states
        const int StatesCount = 3;
        static readonly VisibleState[] StateByOrder = new VisibleState[StatesCount];

        public byte Order { get; }
        public float VisibilityMultiplier { get; }

        public static readonly VisibleState
            Covered = new(nameof(Covered), 0, 0),
            PartlyVisible = new(nameof(PartlyVisible), 1, 0.5f),
            Visible = new(nameof(Visible), 2, 1f);
        
        protected VisibleState(string enumName, byte order, float multiplier) : base(enumName) {
            Order = order;
            VisibilityMultiplier = multiplier;
            StateByOrder[order] = this;
        }

        public VisibleState Union(VisibleState toUnion) {
            return (VisibleState)Math.Min(Order, toUnion.Order);
        }

        public static explicit operator VisibleState(byte order) => order >= StatesCount ? Covered : StateByOrder[order];
    }
}
