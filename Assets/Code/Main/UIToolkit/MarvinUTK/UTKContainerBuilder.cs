using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.MarvinUTK {
    public class UTKLayoutBuilder : UTKContainerBuilder<VisualElement> {
        public UTKLayoutBuilder(string name = null, params string[] customUssClasses) : base(name, customUssClasses) { }
        
        protected override VisualElement CreateRoot() {
            return new VisualElement();
        }
    }
    
    public class UTKScrollContainerBuilder : UTKContainerBuilder<ScrollView> {
        ScrollViewMode ScrollMode { get; }

        public UTKScrollContainerBuilder(ScrollViewMode mode, string name = null, params string[] customUssClasses) : base(name, customUssClasses) { 
            ScrollMode = mode;
        }

        protected override ScrollView CreateRoot() {
            return new ScrollView(ScrollMode);
        }
    }

    public abstract class UTKContainerBuilder<T> : UTKControlsFactory<T>, IUTKContainerBuilder where T : VisualElement, new() {
        public List<IUTKControlsFactory<VisualElement>> Factories { get; } = new();

        protected UTKContainerBuilder(string name = null, params string[] customUssClasses) : base(name, customUssClasses) { }
        
        public IUTKContainerBuilder Add(IUTKControlsFactory<VisualElement> factory) {
            Factories.Add(factory);
            return this;
        }
        
        protected abstract T CreateRoot();

        public sealed override T Create() {
            return Build() as T;
        }
        
        public virtual VisualElement Build() {
            T root = CreateRoot();
            SetupElement(root);
            
            foreach (var factory in Factories) {
                root.Add(factory.Create());
            }
            
            return root;
        }
    }
}