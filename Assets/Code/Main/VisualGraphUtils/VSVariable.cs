namespace Awaken.TG.Main.VisualGraphUtils {
    public struct VSVariable {
        public string name;
        public object value;
        
        [UnityEngine.Scripting.Preserve]
        public VSVariable(string name, object value) {
            this.name = name;
            this.value = value;
        }
    }
}