using Awaken.TG.Main.Heroes.Items;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Functionals.Predicates {
    [UnitTitle("Filter Item Not")] [UnityEngine.Scripting.Preserve]
    public class FilterItemNotUnit : FilterNotUnit<Item> { }
    [UnitTitle("Filter Item And")] [UnityEngine.Scripting.Preserve]
    public class FilterItemAndUnit : FilterAndUnit<Item> { }
    [UnitTitle("Filter Item Or")] [UnityEngine.Scripting.Preserve]
    public class FilterItemOrUnit : FilterOrUnit<Item> { }
    [UnitTitle("Filter Item If")] [UnityEngine.Scripting.Preserve]
    public class FilterItemIfUnit : FilterIfUnit<Item> { }
}