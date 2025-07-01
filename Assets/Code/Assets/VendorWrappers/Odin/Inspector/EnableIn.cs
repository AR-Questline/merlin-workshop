using System;

namespace Sirenix.OdinInspector
{
    public class EnableInAttribute : Attribute
    {
        public PrefabKind PrefabKind;

        public EnableInAttribute(PrefabKind prefabKind) => this.PrefabKind = prefabKind;
    }
}