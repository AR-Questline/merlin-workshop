using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    public readonly struct RenderingContextObject : IEquatable<RenderingContextObject> {
        public readonly Object context;
        public readonly HashSet<GameObject> sceneObjects;
        
        // We need list because Odin is very slow when displaying hashset, so at first we need to fill hashset
        // because we need fast add without duplicates, and then, after all elements added, we need to convert it to list
        public readonly List<GameObject> sceneObjectsList;

        public RenderingContextObject(Object context, HashSet<GameObject> sceneObjects) {
            this.context = context;
            this.sceneObjects = sceneObjects;
            sceneObjectsList = new();
        }

        public void Bake() {
            sceneObjectsList.Clear();
            sceneObjectsList.AddRange(sceneObjects);
        }

        public bool Equals(RenderingContextObject other) {
            return context == other.context;
        }
        public override bool Equals(object obj) {
            return obj is RenderingContextObject other && Equals(other);
        }
        public override int GetHashCode() {
            return context != null ? context.GetHashCode() : 0;
        }
        public static bool operator ==(in RenderingContextObject left, in RenderingContextObject right) {
            return left.Equals(right);
        }
        public static bool operator !=(in RenderingContextObject left, in RenderingContextObject right) {
            return !left.Equals(right);
        }
    }
}
