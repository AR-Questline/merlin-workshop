using System;
using System.Collections.Generic;
using System.Text;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Threads;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.TG.MVC {
    [Il2CppEagerStaticClassConstruction]
    public abstract partial class Model : IModel {
        static StringBuilder s_idBuilder = new StringBuilder();

        // === General properties
        [ProvidesContext]
        protected static Services Services => World.Services;
        public string ID { get; private set; }
        public virtual string ContextID => ID;

        public bool IsBeingInitialized { get; private set; } = false;
        public bool IsInitialized { get; private set; } = false;
        public bool DiscardAfterInit { get; private set; } = false;
        public bool WasDiscarded { get; private set; } = false;
        public bool IsBeingDiscarded { get; private set; } = false;
        public bool HasBeenDiscarded => WasDiscarded | IsBeingDiscarded;
        public bool WasDiscardedFromDomainDrop { get; private set; } = false;
        public bool IsBeingSaved { get; private set; } = false;
        public bool IsFullyInitialized { get; private set; }
        
        // === Domains
        public abstract Domain DefaultDomain { get; }
        public Domain CurrentDomain { get; private set; }

        // === Elements
        [Saved] ModelElements _modelElements;
        protected ref ModelElements ModelElements => ref _modelElements;
        
        // === Available events and relations
        [Il2CppEagerStaticClassConstruction]
        public static class Events {
            /// <summary>
            /// Triggers after this model, all its elements, and all the views for model and elements are initialized.
            /// Used for initialization that really has to happen after all of this is available.
            /// </summary>
            public static readonly Event<IModel, Model> BeforeFullyInitialized = new(nameof(BeforeFullyInitialized));
            public static readonly Event<IModel, Model> AfterFullyInitialized = new(nameof(AfterFullyInitialized));
            public static readonly Event<IModel, Model> AfterChanged = new(nameof(AfterChanged), true);
            public static readonly Event<IModel, Model> BeforeDiscarded = new(nameof(BeforeDiscarded));
            public static readonly Event<IModel, Model> BeingDiscarded = new(nameof(BeingDiscarded));
            public static readonly Event<IModel, Model> AfterDiscarded = new(nameof(AfterDiscarded));
            public static readonly Event<IModel, Element> AfterElementsCollectionModified = new(nameof(AfterElementsCollectionModified));
        }

        // === Constructors

        protected Model() {
            ModelElements = new ModelElements(this);
        }

        // === Cloning
        protected Model Clone() {
            if (this is not ICloneAbleModel cloneAbleModel) {
                throw new Exception($"Trying to clone not cloneable model! This is not valid. Model: {this}");
            }
            Model clone = (Model) this.MemberwiseClone();
            clone.ModelElements = new ModelElements(clone);
            cloneAbleModel.CopyPropertiesTo(clone);
            return clone;
        }

        // === Initialization and disposal

        public void Initialize() {
            IsBeingInitialized = true;
            OnInitialize();
            IsInitialized = true;
        }

        /// <summary>
        /// Called after loading this model's content <br/>
        /// Used for initializing fields with default values when it was not present in JSON
        /// </summary>
        public void AfterDeserialize() {
            if (_modelElements.IsCreated) {
                _modelElements.ReInit(this);
            } else {
                _modelElements = new ModelElements(this);
            }
            _relations?.RestoreOwner(this);
            OnAfterDeserialize();
        }

        /// <summary>
        /// Called just after loading all models' content <br/>
        /// Used for removing its elements that are not valid anymore (mostly with AttachmentTracker.PreRestore)
        /// </summary>
        public void PreRestore() {
            OnPreRestore();
        }
        
        public void Restore() {
            IsBeingInitialized = true;
            ModelElements.RebuildElementIndex();
            OnRestore();
            IsInitialized = true;
        }

        public bool IsValidAfterLoad() {
            return !IsNotSaved && !WasDiscarded && CanBeRestored();
        }

        public void RevalidateElements() {
            ModelElements.RevalidateElements();
        }
        
        public void RevalidateRelations() {
            _relations?.Revalidate();
        }

        public void StoppedInitialization() {
            IsBeingInitialized = false;
        }

        public void AfterWorldRestored() {
            if (DiscardAfterInit) {
                Discard();
            }
        }

        public void MarkAsFullyInitialized() {
            IsFullyInitialized = true;
            this.Trigger(Events.BeforeFullyInitialized, this);
            OnFullyInitialized();
            this.Trigger(Events.AfterFullyInitialized, this);
            
            var implementedTypes = ModelUtils.ModelHierarchyTypes(this);
            // Trigger generic fully initialized events
            foreach (Type implementedType in implementedTypes) {
                if (World.Events.IsFullyInitializedEventRelevant(implementedType)) {
                    this.Trigger(World.Events.ModelFullyInitialized(implementedType), this);
                }
            }
            this.Trigger(World.Events.ModelFullyInitializedAnyType, this);
        }
        
        public void DiscardFromDomainDrop() => DiscardInternal(true);

        public void Discard() {
            if (IsBeingInitialized) {
                DiscardAfterInit = true;
            } else {
                DiscardInternal(false);
            }
        }

        void DiscardInternal(bool fromDomainDrop) {
            ThreadSafeUtils.AssertMainThread();
            
            // warn on double discards
            if (WasDiscarded) {
                Log.Debug?.Warning($"Discarding already discarded model: {ID}");
                return;
            }
            if (IsBeingDiscarded) {
                Log.Debug?.Error($"Discarding model which is discarding right now: {ID}");
                return;
            }

            WasDiscardedFromDomainDrop = fromDomainDrop;
            if (this is Element element) {
                WasDiscardedFromDomainDrop |= element.GenericParentModel.WasDiscardedFromDomainDrop;
            }
            IsBeingDiscarded = true;
            // trigger reactions
            OnBeforeDiscard();
            this.Trigger(Events.BeforeDiscarded, this);
            OnDiscard(WasDiscardedFromDomainDrop);
            // break all relations
            BreakAllRelations();
            // discard all views
            this.Trigger(Events.BeingDiscarded, this);
            // discard all elements
            ModelElements.RemoveAll();
            // unbind events
            World.EventSystem.RemoveAllListenersOwnedBy(this, true);
            // unbind all tweaks
            Services.Get<TweakSystem>().RemoveAllTweaksAttachedTo(this);
            Services.Get<TweakSystem>().RemoveAllTweakedBy(this);
            // dispose bound references
            if (_disposableReferences != null) {
                _disposableReferences.ForEach(d => d.Dispose());
                _disposableReferences.Clear();
            }
            // remove from the registry
            World.Remove(this);
            // done!
            WasDiscarded = true;
            IsBeingDiscarded = false;
            // trigger one last event and remove all listeners
            this.Trigger(Events.AfterDiscarded, this);
            foreach (Type implementedType in ModelUtils.ModelHierarchyTypes(this)) {
                if (World.Events.IsDiscardEventRelevant(implementedType)) {
                    this.Trigger(World.Events.ModelDiscarded(implementedType), this);
                }
            }
            this.Trigger(World.Events.ModelDiscardedAnyType, this);

            World.EventSystem.RemoveAllListenersTiedTo(this, true);
            CleanupReferences();
            OnFullyDiscarded();
        }

        void CleanupReferences() {
            // Cleanup references
            ModelElements.RemoveReferences();

            _relations?.Clear();
            _relations = null;

            _singleRelated?.Clear();
            _singleRelated = null;

            _listRelated?.Clear();
            _listRelated = null;
        }

        public void DeserializationFailed() {
            WasDiscarded = true;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnFullyInitialized() { }
        /// <inheritdoc cref="AfterDeserialize"/>
        protected virtual void OnAfterDeserialize() { }
        /// <inheritdoc cref="PreRestore"/>
        protected virtual void OnPreRestore() { }
        protected virtual void OnRestore() => OnInitialize();
        protected virtual void OnBeforeDiscard() { }
        protected virtual void OnDiscard(bool fromDomainDrop) { }
        protected virtual void OnFullyDiscarded() { }
        protected virtual bool OnSave() => true;
        protected virtual bool CanBeRestored() => true;

        // === Persistence

        public bool MarkedNotSaved { get; set; }
        public virtual bool IsNotSaved => MarkedNotSaved;
        public virtual ushort TypeForSerialization => 0;

        public void PrepareForSaving() {
            IsBeingSaved = false;
            // marked [NotSaved]?
            if (IsNotSaved) {
                return;
            }

            // check if element's parent is saved
            if (this is Element ele && !ele.GenericParentModel.AllowElementSave(ele)) {
                return;
            }
            
            // can be saved, check internal logic (if any)
            IsBeingSaved = OnSave();
        }

        public virtual void Serialize(SaveWriter writer) {
            var elements = ModelElements.Access.Elements(_modelElements);
            if (elements is { Count: > 0 }) {
                writer.WriteName(SavedFields._modelElements);
                writer.Write(_modelElements);
                writer.WriteSeparator();
            }
            if (_relations != null && !_relations.IsEmpty()) {
                writer.WriteName(SavedFields._relations);
                writer.Write(_relations);
                writer.WriteSeparator();
            }
        }

        public void SerializationEnded() {
            // Remember there is a bug, so we can be in invalid state here, so if you need to add some convoluted logic here
            // please check if you need check HasBeenDiscarded
            IsBeingSaved = false;
        }

        public virtual bool AllowElementSave(Element ele) => IsBeingSaved;

        internal virtual void EnsureIdIsValid() {}

        // === Elements

        public bool HasElement<T>() where T : class, IModel => ModelElements.Exists<T>();
        public T Element<T>() where T : class, IModel => ModelElements.GetOne<T>();
        [CanBeNull]
        public T TryGetElement<T>() where T : class, IModel => ModelElements.TryGetOne<T>();
        public IModel TryGetElement(Type type) => ModelElements.TryGetOne(type);
        public bool TryGetElement<T>(out T element) where T : class, IModel {
            element = ModelElements.TryGetOne<T>();
            return element != null;
        }
        public ModelsSet<T> Elements<T>() where T : class, IModel => ModelElements.AllOfType<T>();
        
        public ModelsSet<T> ElementsFlat<T>() where T : class, IModel {
            var modelsSet = ModelElements.AllOfType<T>();
            Asserts.IsTrue(modelsSet.IsFlat, $"Model {this} has non-flat elements of type {typeof(T)}");
            return modelsSet;
        }

        public ModelsSet<IModel> Elements(Type type) => ModelElements.AllOfType(type);
        public List<Element> AllElements() => ModelElements.All();

        internal void InitializeAllElements() => ModelElements.InitializeAll();
        internal void InitializeNewElements() => ModelElements.InitializeNew();

        public void AddElement(IElement element) => ModelElements.Add((Element)element);
        public T AddElement<T>(T element) where T : IElement {
            AddElement((IElement)element);
            return element;
        }
        
        [UnityEngine.Scripting.Preserve]
        public T AddElement<T>() where T : IElement, new() {
            return AddElement(new T());
        }
        
        [UnityEngine.Scripting.Preserve] public void RemoveAllElements() => ModelElements.RemoveAll();
        public void RemoveElementsOfType<T>() where T : class, IElement => ModelElements.RemoveAllOfType<T>();
        [UnityEngine.Scripting.Preserve]
        public void RemoveElementsOfType(Type type) => ModelElements.RemoveAllOfType(type);
        public void RemoveElement(IElement element) => ModelElements.Remove(element);
        public void NotifyElementDiscarded(Element element) => ModelElements.UnregisterElement(element);

        public T CachedElement<T>(ref T cache) where T : class, IModel {
            if (!ReferenceEquals(cache, null)) {
                return cache;
            }
            return cache = Element<T>();
        }
        
        public T CachedElementWithChecks<T>(ref T cache) where T : class, IModel {
            if (!ReferenceEquals(cache, null)) {
                if (cache.HasBeenDiscarded) {
                    cache = null;
                } else {
                    return cache;
                }
            }
            return cache = Element<T>();
        }
        
        public T TryGetCachedElement<T>(ref T cache) where T : class, IModel {
            if (!ReferenceEquals(cache, null)) {
                return cache;
            }
            TryGetElement(out cache);
            return cache;
        }
        
        public T TryGetCachedElementWithChecks<T>(ref T cache) where T : class, IModel {
            if (!ReferenceEquals(cache, null)) {
                if (cache.HasBeenDiscarded) {
                    cache = null;
                } else {
                    return cache;
                }
            }
            TryGetElement(out cache);
            return cache;
        }

        // === Change handling

        public void TriggerChange() => this.Trigger(Events.AfterChanged, this);

        // === Generating and assigning IDs

        public void AssignID(string id) {
            ID = id;
        }

        public void AssignID(Services services) {
            s_idBuilder.Clear();
            ID = GenerateID(services, s_idBuilder);
            s_idBuilder.Clear();
        }

        protected virtual string GenerateID(Services services, StringBuilder idBuilder) {
            AppendJustThisModelID(services, idBuilder);
            return idBuilder.ToString();
        }
        
        protected void AppendJustThisModelID(Services services, StringBuilder idBuilder) {
            bool global = DefaultDomain.IsChildOf(Domain.SaveSlot) == false;
            Type modelType = this.GetType();
            idBuilder.Append(TypeNameCache.Name(modelType));
            idBuilder.Append(':');
            if (global) {
                idBuilder.Append('G');
            }
            idBuilder.Append(services.Get<IdStorage>().NextIdFor(this, modelType, global));
        }

        // === Relation support

        [Saved] RelationStore _relations;
        Dictionary<Relation, object> _singleRelated;
        Dictionary<Relation, object> _listRelated;

        RelationStore IModel.GetRelationStore(bool createIfMissing) {
            if (createIfMissing && _relations == null) {
                _relations = new RelationStore(this);
            }
            return _relations;
        }

        public RelatedValue<TRelated> RelatedValue<TRelated>(Relation<TRelated> viaRelation) where TRelated : class, IModel {
            if (_singleRelated == null) _singleRelated = new Dictionary<Relation, object>();
            if (!_singleRelated.TryGetValue(viaRelation, out var relatedValue)) {
                relatedValue = new RelatedValue<TRelated>(this, viaRelation);
                _singleRelated.TryAdd(viaRelation, relatedValue);
            }
            return (RelatedValue<TRelated>)relatedValue;
        }

        public RelatedList<TRelated> RelatedList<TRelated>(Relation<TRelated> viaRelation) where TRelated : class, IModel {
            if (_listRelated == null) _listRelated = new Dictionary<Relation, object>();
            if (!_listRelated.TryGetValue(viaRelation, out var relatedList)) {
                relatedList = new RelatedList<TRelated>(this, viaRelation);
                _listRelated.TryAdd(viaRelation, relatedList);
            }
            return (RelatedList<TRelated>)relatedList;
        }

        void BreakAllRelations() {
            RelationStore store = ((IModel)this).GetRelationStore();
            store?.BreakAllRelations();
        }
        
        // === Disposable Binding

        HashSet<IDisposable> _disposableReferences;
        public void BindDisposable(IDisposable disposable) {
            _disposableReferences ??= new(2);
            _disposableReferences.Add(disposable);
        }

        // === Views

        public View MainView => World.MainViewFor(this);

        public T View<T>() where T : class, IView {
            return World.View<T>(this);
        }

        public T CachedView<T>(ref T cache) where T : class, IView {
            if (!ReferenceEquals(cache, null)) {
                return cache;
            }
            return cache = World.View<T>(this);
        }

        public IEnumerable<IView> Views => World.ViewsFor(this);

        public virtual SpawnsView[] GetAutomaticallySpawnedViews() {
            Attribute[] attributes = Attribute.GetCustomAttributes(GetType(), typeof(SpawnsView));
            SpawnsView[] spawnViews = Array.ConvertAll(attributes, a => (SpawnsView) a);
            Array.Sort(spawnViews);
            return spawnViews;
        }
        
        // === Presenters
        
        public T Presenter<T>() where T : class, IPresenter {
            return World.Presenter<T>(this);
        }

        public IEnumerable<IPresenter> Presenters => World.PresentersFor(this);

        // === Domains
        
        public void SetDomain(Domain domain) {
            if (domain.Modal) {
                throw new InvalidOperationException($"Can't use domain {domain.FullName} for models or services.");
            }
            CurrentDomain = domain;
        }

        public void ClearDomain() {
            CurrentDomain = default;
        }

        public void MoveToDomain(Domain domain) {
            if (HasBeenDiscarded) {
                Log.Important?.Error("Trying to move discarded model. This is not allowed.");
                return;
            }
            if (CurrentDomain == domain) {
                return;
            }
#if UNITY_EDITOR
            if (CurrentDomain.IsChildOf(Domain.SaveSlot) != domain.IsChildOf(Domain.SaveSlot)) {
                throw new InvalidOperationException($"Cannot move model from one modal domain to another. {CurrentDomain} -> {domain}");
            }
#endif
            
            MoveSet(domain);
            foreach (var element in this.GetChildren()) {
                element.MoveSet(domain);
            }
        }
        
        void MoveSet(Domain domain) {
            SetDomain(domain);
            if (this is IWithDomainMovedCallback movedModel) {
                movedModel.DomainMoved(domain);
            }
        }
        
        // === Debugging help

        public override string ToString() {
            return $"{GetType().Name}[{ID}]";
        }

        public struct Access {
            public static ModelElements ModelElements(Model model) => model.ModelElements;
        }
        
        public static implicit operator bool(Model model) => model != null && !model.WasDiscarded;
    }
}
