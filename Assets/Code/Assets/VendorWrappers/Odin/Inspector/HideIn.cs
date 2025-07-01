using System;

namespace Sirenix.OdinInspector
{
    public class HideInAttribute : Attribute
    {
        public PrefabKind PrefabKind;

        public HideInAttribute(PrefabKind prefabKind) => this.PrefabKind = prefabKind;
    }
}