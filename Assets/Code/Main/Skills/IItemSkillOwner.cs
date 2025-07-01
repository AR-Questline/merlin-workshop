using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Skills;

namespace Awaken.TG.Main.Skills {
    public interface IItemSkillOwner : ISkillOwner, IItemAction {
        Item Item { get; }
        IEnumerable<Skill> Skills { get; }
        
        /// <summary>
        /// The amount of times the skills of this item were performed. To distinguish between calls from stackable items.
        /// </summary>
        int PerformCount { get; set; }
        void IncrementPerformCount() => PerformCount++;
    }
}