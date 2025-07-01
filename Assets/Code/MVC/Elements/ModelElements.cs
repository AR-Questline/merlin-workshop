using System;
using System.Collections.Generic;
using System.Diagnostics;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.TG.MVC.Elements {
    /// <summary>
    /// Container used to manage elements tied to a model.
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    public partial struct ModelElements {
        public ushort TypeForSerialization => SavedTypes.ModelElements;

        const int StandardCollectionsSize = 4;
        const int MedianByTypeOuterCapacity = 1;
        const int MedianByTypeOwnCapacity = 0;

        // === State enumeration

        enum ElementOperationState : byte {
            Idle, InitializationInProgress, RemovalInProgress
        }

        // === Static properties
        public static readonly HierarchicalDictionary<Type, IModel> EmptyElementsByType = new(0, 0, 0);

        // === General properties

        Model _owner;
        List<Element> _elements;

        // === Transient properties

        HierarchicalDictionary<Type, IModel> _elementsByType;
        int _uninitializedElementsCount;
        ElementOperationState _elementOperationState;
        bool _elementsInitialized;

        // === Constructors
        public ModelElements(Model owner) {
            _owner = owner;
            _elements = null;
            _elementsByType = EmptyElementsByType;
            _uninitializedElementsCount = 0;
            _elementOperationState = ElementOperationState.Idle;
            _elementsInitialized = false;
        }

        public ModelElements(List<Element> elements) {
            _owner = null;
            _elements = elements;
            _elementsByType = EmptyElementsByType;
            _uninitializedElementsCount = 0;
            _elementOperationState = ElementOperationState.Idle;
            _elementsInitialized = false;
        }

        public bool IsCreated => _elementsByType != null;

        public void ReInit(Model owner) {
            _owner = owner;
            if (_elements != null) {
                foreach (var element in _elements) {
                    element?.Bind(_owner);
                }
            }
        }

        // === Queries

        public bool Exists<T>() where T : class, IModel {
            var elementsEnumerator = _elementsByType.Enumerate(typeof(T));
            return elementsEnumerator.MoveNext();
        }

        public T GetOne<T>() where T : class, IModel {
            T element = TryGetOne<T>();
            if (element == null) {
                throw new KeyNotFoundException($"Model {_owner.ID} does not have an element of type {typeof(T)}.");
            } else {
                return element;
            }
        }

        public T TryGetOne<T>() where T : class, IModel {
            return TryGetOne(typeof(T)) as T;
        }
        
        public IModel TryGetOne(Type t) {
#if UNITY_EDITOR || AR_DEBUG
            if (_owner == null) {
                Log.Critical?.Error($"Cannot get element! _owner is null");
                return null;
            }
            if (_elementsByType == null) {
                Log.Critical?.Error($"Cannot get element! _elementsByType is null. Owner: {_owner.ID}");
                return null;
            }
#endif
            var elementsEnumerator = _elementsByType.Enumerate(t);
            if (elementsEnumerator.MoveNext()) {
                return elementsEnumerator.Current;
            }
            return null;
        }

        public ModelsSet<T> AllOfType<T>() where T : class, IModel {
            return AllOfType(typeof(T)).As<T>();
        }
        
        public ModelsSet<IModel> AllOfType(Type t) {
            return new ModelsSet<IModel>(_elementsByType.GetOrDefault(t, _elementsByType.EmptyDefault));
        }

        public List<Element> All() {
            return _elements ?? ListExtensions<Element>.Empty;
        }

        // === Adding and removing elements

        public void Add(Element element) {
            if (_elementOperationState == ElementOperationState.RemovalInProgress) {
                throw new InvalidOperationException($"Cannot add new elements while a bulk removal operation is running. Model: {_owner.ID}, element type: {element.GetType()}");
            }
            DebugCheckTypeIntegrity(element);
            var implementedTypes = ModelUtils.ModelHierarchyTypes(element);
            _elements ??= new(StandardCollectionsSize);
            if (_elementsByType == EmptyElementsByType) {
                _elementsByType = new(implementedTypes.Length, MedianByTypeOuterCapacity, MedianByTypeOwnCapacity);
            }

            _elementsByType.Add(implementedTypes, element);
            element.Bind(_owner);
            
            // if our elements were already initialized, we initialize eagerly here
            // otherwise, we're pre-initialization and this will be done by InitializeElements()
            // in the process of adding this model to the world
            if (_elementsInitialized) {
                _elements.Add(element);
                InitializeElement(element);
            } else {
                // insert it before saved elements because saved elements are initialized after them (by load system)
                _elements.Insert(_uninitializedElementsCount, element);
                _uninitializedElementsCount++;
            }
            _owner.Trigger(Model.Events.AfterElementsCollectionModified, element);
            _owner.TriggerChange();
        }

        [Conditional("DEBUG")]
        void DebugCheckTypeIntegrity(Element element) {
            var beforeElementType = element.GetType();
            while (beforeElementType!.BaseType != typeof(Element)) {
                beforeElementType = beforeElementType.BaseType;
            }

            if (beforeElementType.IsGenericType && beforeElementType.GetGenericTypeDefinition() != typeof(Element<>)) {
                return;
            }

            var genericArguments = beforeElementType.GetGenericArguments();
            if (genericArguments.Length == 0) {
                return;
            }
            
            Type ownerType = _owner.GetType();
            if (!genericArguments[0].IsAssignableFrom(ownerType)) {
                throw new InvalidOperationException($"Cannot add element of type {element.GetType()} as child of object of type {ownerType} to model {_owner.ID}");
            }
        }

        public void RemoveAllOfType<T>() where T : class, IElement {
            RemoveAllOfType(typeof(T));
        }
        
        public void RemoveAllOfType(Type t) {
            _elementOperationState = ElementOperationState.RemovalInProgress;
            try {
                var elementsSet = AllOfType(t).As<IElement>();
                while (elementsSet.Any()) {
                    // modification-safe iteration
                    IElement next = elementsSet.First();
                    
                    // Can we remove the element right now?
                    if (!next.IsFullyInitializedWithParents()) {
                        LogNames(next);
                        throw new InvalidOperationException(
                            $"RemoveElement() invoked on uninitialized element. Cannot correctly remove element at this moment Model: {_owner.ID}, element: {next?.ID ?? "NULL"}");
                    }
                    
                    // Actual removal
                    Remove(next);
                    
                    // Was removal successful?
                    if (elementsSet.FirstOrDefault() == next) {
                        LogNames(next);
                        throw new InvalidOperationException(
                            $"RemoveElement() left the element on the typed list - listeners must have been not bound correctly. Model: {_owner.ID}, element: {next?.ID ?? "NULL"}");
                    }
                }
            } finally {
                _elementOperationState = ElementOperationState.Idle;
            }
        }

        public void RemoveAll() {
            _elementOperationState = ElementOperationState.RemovalInProgress;
            try {
                while (_elements != null && _elements.Count > 0) {
                    // modification-safe iteration
                    Element next = _elements[^1];
                    
                    // Can we remove the element right now?
                    if (!next.IsFullyInitializedWithParents() && ShouldThrowExceptions(next)) {
                        LogNames(next);
                        throw new InvalidOperationException(
                            $"RemoveElement() invoked on uninitialized element. Cannot correctly remove element at this moment Model: {_owner.ID}, element: {next?.ID ?? "NULL"}");
                    }
                    
                    //Actual removal
                    Remove(next);
                    
                    // sanity check - to prevent infinite loops when something goes wrong
                    if (_elements.Count > 0 && _elements[^1] == next) {
                        if (ShouldThrowExceptions(next)) {
                            LogNames(next);
                            throw new InvalidOperationException(
                                $"RemoveElement() left the element on the master list - listeners must have been not bound correctly. Model: {_owner.ID}, element: {next?.ID ?? "NULL"}");
                        } else {
                            _elements.RemoveAt(_elements.Count - 1);
                        }
                    }
                }
                _uninitializedElementsCount = 0;
            } finally {
                _elementOperationState = ElementOperationState.Idle;
            }

            static bool ShouldThrowExceptions(Element target) {
                return TitleScreen.wasLoadingFailed == LoadingFailed.False;
            }
        }

        public void Remove(IElement element) {
            element.Discard();
        }

        // === Initializing elements

        public void InitializeAll() {
            if (_elements == null) {
                _uninitializedElementsCount = 0;
                _elementsInitialized = true;
                return;
            }
            _elementOperationState = ElementOperationState.InitializationInProgress;
            try {
                // raw iteration to deal with elements being added to the end by another element's initialization
                // ReSharper disable once ForCanBeConvertedToForeach
                for (int i = 0; i < _elements.Count; i++) {
                    InitializeElement(_elements[i]);
                }
                _uninitializedElementsCount = 0;
                _elementsInitialized = true;
            } finally {
                _elementOperationState = ElementOperationState.Idle;
            }
        }

        public void InitializeNew() {
            if (_elements == null) {
                _uninitializedElementsCount = 0;
                _elementsInitialized = true;
                return;
            }
            _elementOperationState = ElementOperationState.InitializationInProgress;
            try {
                // do not initialize saved elements because they will be initialized later by load system
                for (int i = 0; i < _uninitializedElementsCount; i++) {
                    InitializeElement(_elements[i]);
                }
                _uninitializedElementsCount = 0;
                _elementsInitialized = true;
            } finally {
                _elementOperationState = ElementOperationState.Idle;
            }
        }

        void InitializeElement(Element element) {
            if (element.ID != null && World.ByID(element.ID) != null) {
                throw new InvalidOperationException(
                    $"Element of type {element.GetType()} with id: {element.ID} was added to the world before it was added to its parent model ({_owner.ID}) - don't do this, let the parent model handle World.Add().");
            }

            World.Add(element);
            
            if (element.HasBeenDiscarded) {
                UnregisterElement(element);
            }
        }

        // === Restoring elements
        public void RevalidateElements() {
            if (_elements == null) {
                return;
            }
            for (int i = _elements.Count - 1; i >= _uninitializedElementsCount; --i) {
                if (_elements[i] is null or { WasDiscarded: true }) {
                    _elements.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Called from Restore(), rebuilds the type->element map which is not persisted
        /// for space saving.
        /// </summary>
        public void RebuildElementIndex() {
            if (_elements == null || _elements.Count == 0) {
                return;
            }
            
            _elements.RemoveAll(static e => e == null);

            var maxTypes = 0;
            foreach (Element elem in _elements) {
                maxTypes = Math.Max(maxTypes, ModelUtils.ModelHierarchyTypes(elem).Length);
            }

            var implementsTypesEstimation = maxTypes + (int)Math.Ceiling(Math.Log10(maxTypes));
            if (_elementsByType == EmptyElementsByType) {
                _elementsByType = new(implementsTypesEstimation, MedianByTypeOuterCapacity, MedianByTypeOwnCapacity);
            }

            for (int i = _uninitializedElementsCount; i < _elements.Count; ++i) {
                Element elem = _elements[i];
                _elementsByType.Add(ModelUtils.ModelHierarchyTypes(elem), elem);
            }
        }

        public void RemoveReferences() {
            _owner = null;

            if (_elements != null) {
                _elements.Clear();
                _elements = null;
            }

            if (_elementsByType != EmptyElementsByType) {
                _elementsByType.Clear();
                _elementsByType = EmptyElementsByType;
            }
        }

        // === Other
        public void SetInitCapacity(int elementsCapacity) {
            if (_elements == null) {
                _elements = new List<Element>(elementsCapacity);
            }
        }

        public void SetInitCapacity(Type type, ushort byTypeOuterCapacity, ushort byTypeOwnCapacity) {
            _elementsByType.InitCapacity(ModelUtils.ModelHierarchyTypes(type), byTypeOuterCapacity, byTypeOwnCapacity);
        }

        // === Helpers
        void LogNames(IModel element) {
            string ownerName = "";
            string elementName = "";

            // don't want to crash while logging other error!
            try {
                ownerName = _owner is INamed ownerNamed
                    ? ownerNamed.DisplayName + ": " + ownerNamed.DebugName
                    : "";
                elementName = element is INamed elementNamed
                    ? elementNamed.DisplayName + ": " + elementNamed.DebugName
                    : "";
            } catch (Exception e) {
                Log.Important?.Error("Exception happened while logging element removal error. " + element.ID + "\n" + e);
            }
            ownerName += $"({_owner.ID})";
            elementName += $"({element?.ID})";


            Log.Critical?.Error($"Exception happened for model {ownerName} for element {elementName}");
        }

        public void UnregisterElement(Element element) {
            if (_elementOperationState == ElementOperationState.InitializationInProgress) {
                throw new InvalidOperationException($"Cannot remove elements while element initialization is still in progress. Model: {_owner.ID}, element: {element.ID}");
            }
            // remove from indices
            var index = _elements.IndexOf(element);
            if (index == -1) {
                return;
            }
            _elements.RemoveAt(index);
            if (index < _uninitializedElementsCount) {
                _uninitializedElementsCount--;
            }

            _elementsByType.Remove(element.GetType(), element);

            if (!_owner.IsBeingDiscarded) {
                _owner.Trigger(Model.Events.AfterElementsCollectionModified, element);
                _owner.TriggerChange();
            }
        }

        public struct Access {
            public static HierarchicalDictionary<Type, IModel> ElementsByType(ModelElements elements) => elements._elementsByType;
            public static List<Element> Elements(ModelElements elements) => elements._elements;
        }
    }
}
