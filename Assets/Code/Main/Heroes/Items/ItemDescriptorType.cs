using System;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Items {
    public class ItemDescriptorType : RichEnum {
        public Func<Item, IItemDescriptor> GetItemDescriptor { get; }
        
        ItemDescriptorType(string enumName, Func<Item, IItemDescriptor> getItemDescriptor, string inspectorCategory = "") : base(enumName, inspectorCategory) {
            GetItemDescriptor = getItemDescriptor;
        }

        public static readonly ItemDescriptorType
            ExistingItem = new(nameof(ExistingItem), item => new ExistingItemDescriptor(item)),
            VendorItem = new(nameof(VendorItem), item => new VendorItemDescriptor(item));
    }
}