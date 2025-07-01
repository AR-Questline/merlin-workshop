using System;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Locations.Shops.Stocks;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    [UnityEngine.Scripting.Preserve]
    public class Patcher028_029 : Patcher_DeleteSaves {
        protected override Version MaxInputVersion => new Version(0, 28);
        protected override Version FinalVersion => new Version(0, 29);
    }
}