using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Housing.Farming;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors {
    public interface IItemDescriptor {
        ItemQuality Quality { get; }
        int Quantity { get; }
        
        string Name { get; }

        string ItemType { get; }
        IItemTypeSpecificDescriptor TypeSpecificDescriptor { get; }
        [UnityEngine.Scripting.Preserve] SpriteReference Icon { get; }
        
        string ItemFlavor { get; }
        string ItemDescription { get; }
        string ItemRequirements { get; }
        ItemRead Read { get; }
        IEnumerable<string> Effects { get; }
        IEnumerable<string> GemsSlot { get; }
        IEnumerable<GemAttached> Gems { get; }
        IEnumerable<AppliedItemBuff> Buffs { get; }
        TooltipConstructor KeywordsTooltip { get; }
        
        int Price { get; }
        float Weight { get; }
        bool IsEquipped { get; }
        bool RequirementsMet { get; }
        bool HasSkills { get; }
        bool IsMagic { get; }
        
        EquipmentType EquipmentType { get; }
        
        /// <summary>
        /// It is HACK. Try avoid using it.
        /// It is here only because ItemSlotUI uses Item and its not enough time to refactor it to use IITemDescriptor. 
        /// </summary>
        [CanBeNull, Obsolete] Item ExistingItem { get; }
        
        bool IsStolen { get; }
        string StolenText { get; }
        ItemSeed ItemSeed { get; }
        PlantSlot PlantSlot { get; }
    }
}