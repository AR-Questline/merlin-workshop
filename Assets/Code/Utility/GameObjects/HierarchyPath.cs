using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awaken.Utility.GameObjects {
    public readonly struct HierarchyPath {
        public readonly string[] names;
        public readonly int[] idices;

        public HierarchyPath(Transform transform) {
            var nameList = new List<string>();
            var indexList = new List<int>();
            
            while (transform) {
                nameList.Add(transform.name);
                indexList.Add(transform.GetSiblingIndex());
                transform = transform.parent;
            }
            
            nameList.Reverse();
            indexList.Reverse();
            
            names = nameList.ToArray();
            idices = indexList.ToArray();
        }

        public Transform Retrieve(Transform root) {
            if (root.name != names[0]) {
                return null;
            }
            var current = root;
            for (int i = 1; i < names.Length; i++) {
                current = current.GetChild(idices[i]);
                if (!current || current.name != names[i]) {
                    return null;
                }
            }
            return current;
        }
    }
}