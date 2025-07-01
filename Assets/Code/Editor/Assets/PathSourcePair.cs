
using UnityEngine;

namespace Awaken.TG.Editor.Assets {
    public class PathSourcePair<T> where T : Object {
        public string path;
        public T source;

        public PathSourcePair(string path, T source) {
            this.path = path;
            this.source = source;
        }
    }
}