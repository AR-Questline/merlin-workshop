using Awaken.TG.Main.UI.EmptyContent;
using UnityEngine;

namespace Awaken.TG.Main.Crafting {
    public interface IVCrafting : IEmptyInfo {
        Transform InventoryParent { get; }
        Transform WorkbenchParent { get; }
        Transform StaticTooltip { get; set; }
    }
}