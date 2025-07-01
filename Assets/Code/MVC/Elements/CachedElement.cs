using System;
using System.Runtime.CompilerServices;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.MVC.Elements {
    public interface ICachedElement<out TModel> {
        TModel Get();
        event Action<TModel> OnChanged;
    }

    public class CachedElement<TRoot, TElement> : ICachedElement<TElement>
        where TRoot : class, IModel
        where TElement : class, IModel
    {
        readonly IListenerOwner _owner;
        readonly ICachedElement<TRoot> _root;
        TElement _element;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TElement Get() => _element;
        public event Action<TElement> OnChanged;

        protected CachedElement(IListenerOwner owner, ICachedElement<TRoot> root) {
            _owner = owner;
            _root = root;
            _root.OnChanged += OnRootChanged;
            OnRootChanged(_root.Get());
        }
        public CachedElement(IListenerOwner owner, TRoot root) : this(owner, new ModelProvider<TRoot>(root)) { }

        void OnRootChanged(TRoot root) {
            _element = root?.TryGetElement<TElement>();
            root?.ListenTo(Model.Events.AfterElementsCollectionModified, OnRootElementsCollectionModifier, _owner);
            OnChanged?.Invoke(_element);
        }
        
        void OnRootElementsCollectionModifier(Element element) {
            if (element is TElement tElement) {
                _element = element.HasBeenDiscarded ? null : tElement;
                OnChanged?.Invoke(_element);
            }
        }
        
        class ModelProvider<TModel> : ICachedElement<TModel> {
            TModel _model;

            public ModelProvider(TModel model) {
                _model = model;
            }

            public TModel Get() => _model;

            public event Action<TModel> OnChanged { add{ } remove { } }
        }
    }

    public class CachedElement<TRoot, TElement1, TElement2> : CachedElement<TElement1, TElement2> 
        where TRoot : class, IModel
        where TElement1 : class, IModel
        where TElement2 : class, IModel
    {
        protected CachedElement(IListenerOwner owner, ICachedElement<TElement1> root) : base(owner, root) { }
        public CachedElement(IListenerOwner owner, TRoot root) : this(owner, new CachedElement<TRoot, TElement1>(owner, root)) { }
    }

    [UnityEngine.Scripting.Preserve]
    public class CachedElement<TRoot, TElement1, TElement2, TElement3> : CachedElement<TElement1, TElement2, TElement3> 
        where TRoot : class, IModel
        where TElement1 : class, IModel
        where TElement2 : class, IModel
        where TElement3 : class, IModel
    {
        protected CachedElement(IListenerOwner owner, ICachedElement<TElement2> root) : base(owner, root) { }
        public CachedElement(IListenerOwner owner, TRoot root) : this(owner, new CachedElement<TRoot, TElement1, TElement2>(owner, root)) { }
    }
}