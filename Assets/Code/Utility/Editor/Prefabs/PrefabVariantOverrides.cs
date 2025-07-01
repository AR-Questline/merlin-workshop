using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using static Awaken.Utility.Editor.Prefabs.PrefabVariantCrawler;

namespace Awaken.Utility.Editor.Prefabs {
    public static class PrefabVariantOverrides {
        public static Node<ComponentsData<TOwned, TOverriden>>[] GetOverrides<TComponent, TOwned, TOverriden>(Node<PrefabNodeData>[] prefabs, OwnedDataDelegate<TComponent, TOwned> owning, OverridenDataDelegate<TComponent, TOverriden> overrides) where TComponent : Component {
            return Select(prefabs, (parentPrefabTransform, currentPrefab) => {
                var components = currentPrefab.Prefab.GetComponentsInChildren<TComponent>();
                var ownedList = new List<ComponentData<TOwned>>();
                var overridenList = new List<ComponentData<TOverriden>>();

                foreach (var current in components) {
                    var parent = GetFromSource(parentPrefabTransform, current, out var source);
                    if (source is Source.CurrentPrefab or Source.NestedPrefab) {
                        var data = owning(current);
                        ownedList.Add(new ComponentData<TOwned>(current.gameObject, data));
                    } else {
                        var data = overrides(parent, current);
                        overridenList.Add(new ComponentData<TOverriden>(current.gameObject, data));
                    }
                }
                
                return new ComponentsData<TOwned, TOverriden>(currentPrefab, ownedList.ToArray(), overridenList.ToArray());
            });
        }

        [Serializable]
        public struct ComponentsData<TOwned, TOverriden> : INodeData {
            public string Path { get; }
            [ShowInInspector, HideLabel] public GameObject Prefab { get; set; }
            
            public ComponentData<TOwned>[] owned;
            public ComponentData<TOverriden>[] overriden;

            public ComponentsData(in PrefabNodeData prefab, ComponentData<TOwned>[] owned, ComponentData<TOverriden>[] overriden) {
                Path = prefab.Path;
                Prefab = prefab.Prefab;
                this.owned = owned;
                this.overriden = overriden;
            }
        }

        [Serializable]
        public struct ComponentData<TData> {
            // we store gameObject instead of component in case of component replacement in variant's parent
            public GameObject gameObject;
            public TData data;

            public ComponentData(GameObject gameObject, TData data) {
                this.gameObject = gameObject;
                this.data = data;
            }
        }
        
        public delegate TOwned OwnedDataDelegate<in TComponent, out TOwned>(TComponent current);
        public delegate TOverriden OverridenDataDelegate<in TComponent, out TOverriden>(TComponent parent, TComponent current);
    }
}