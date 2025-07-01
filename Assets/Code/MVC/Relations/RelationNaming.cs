using System;

namespace Awaken.TG.MVC.Relations {
    /// <summary>
    /// Internal helper for generating human-readable relation names.
    /// </summary>
    internal static class RelationNaming {
        internal static string HalfDescription(Type type, Arity arity) {
            return (arity == Arity.One) ? $"1 {type.Name}" : $"N {type.Name}s";
        }

        internal static string SideDescription<TMe, TOther>(Relation<TMe, TOther> side) {
            return
                $"{HalfDescription(typeof(TMe), side.MyArity)} --[{side.Name}]-> {HalfDescription(typeof(TOther), side.OtherArity)}";
        }
    }
}