using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.Utility.Debugging;
using UnityEngine.UIElements;

namespace Awaken.TG.MVC {
    /// <summary>
    /// A Presenter is a controller of UIToolkit elements, bound to a <see cref="IModel"/> of our MVC.
    /// Only lifecycle methods included in the base Presenter.
    /// <br/> To initialise, use the constructor and then the <see cref="World.BindPresenter"/> method.
    /// </summary>
    public abstract class Presenter<TModel> : IPresenter, IReleasableOwner
        where TModel : IModel {
        static int s_nextID = 1;
        
        // === Assets references
        /// <summary>
        /// Track used assets to auto release on discard
        /// </summary>
        HashSet<IReleasableReference> _releasableReferences;
        
        public IModel GenericModel => TargetModel;
        public TModel TargetModel { get; private set; }
        public string ID { get; private set; }

        /// <summary>
        /// Points to the UI layout root where content will reside. Sometimes the same as <see cref="Content"/>.
        /// </summary>
        public VisualElement Parent { get; }
        /// <summary>
        /// The first element in the content hierarchy. 
        /// </summary>
        public VisualElement Content { get; protected set; }
        
        protected Services Services => World.Services;

        Action _onInitializedCallback;
        
        protected Presenter(VisualElement parent) {
            Parent = parent;
        }

        /// <summary>
        /// Binds Presenter with a <see cref="IModel"/> and set the <see cref="Content"/>.
        /// </summary>
        /// <param name="model"> The <see cref="IModel"/> to bind to. </param>
        /// <param name="onInitialized"> Optional callback when the Presenter is fully initialized. </param>
        public void Initialize(IModel model, Action onInitialized = null) {
            _onInitializedCallback = onInitialized;
            ID = GenerateID(model);
            TargetModel = (TModel)model;
            
            PrepareContent();
        }
        
        /// <summary>
        /// Unbinds Presenter from the <see cref="IModel"/> and invoke discard logic.
        /// </summary>
        public void Discard() {
            OnBeforeDiscard();
            World.RemovePresenter(this);
            DiscardInternal();
            ReleaseConnectionsWithView();
            ReleaseReleasable();
            OnAfterDiscard();
        }
        
        protected virtual string GenerateID(IModel target) {
            return $"{target.ID}[{GetType().Name}][{s_nextID++}]";
        }
        
        protected virtual void PrepareContent() {
            Content = Parent;
            NotifyFullyInitialized();
        }
        
        protected void NotifyFullyInitialized() {
            Content.name = GetType().Name; // For debugging purposes - easier to find in the UI Toolkit Debugger hierarchy.
            CacheVisualElements(Content);
            OnFullyInitialized();
            
            _onInitializedCallback?.Invoke();
            _onInitializedCallback = null;
        }
        
        protected abstract void CacheVisualElements(VisualElement contentRoot);
        
        protected virtual void OnFullyInitialized() { }
        protected virtual void OnBeforeDiscard() { }
        protected virtual void DiscardInternal() { }
        protected virtual void OnAfterDiscard() { }
        
        public void RegisterReleasableHandle(IReleasableReference releasableReference) {
            _releasableReferences ??= new(4);
            _releasableReferences.Add(releasableReference);
        }
        
        public void ReleaseReleasable() {
            if (_releasableReferences == null) {
                return;
            }
            foreach (var reference in _releasableReferences) {
                reference.Release();
            }
            _releasableReferences.Clear();
        }

        void ReleaseConnectionsWithView() {
            if (this is IVisualElementPromptPresenter promptPresenter) {
                promptPresenter.UnregisterPromptHost();
            }
        } 
    }
    
    /// <summary>
    /// Specific sync Presenter type for automatically querying a <see cref="Presenter{TModel}.Parent"/> using the <see cref="ContentName"/> to set the <see cref="Presenter{TModel}.Content"/>.
    /// <br/> Use if you know the content already exists in the hierarchy (findable in provided <see cref="Presenter{TModel}.Parent"/> in the constructor).
    /// <br/> To initialise, use the constructor and then the <see cref="World.BindPresenter"/> method.
    /// </summary>
    public abstract class QueryPresenter<TModel> : Presenter<TModel>
        where TModel : IModel {
        /// <summary>
        /// Name of the content root element used in VisualElement queries.
        /// </summary>
        public virtual string ContentName => GetType().Name;
        
        protected QueryPresenter(VisualElement parent) : base(parent) { }
        
        protected sealed override void PrepareContent() {
            LookForContent();
            NotifyFullyInitialized();
        }
        
        protected sealed override void DiscardInternal() {
            ClearContent();
        }
        
        void LookForContent() {
            if (Parent.Q<VisualElement>(ContentName) is { } existing) {
                Content = existing;
            } else {
                Log.Important?.Error($"Refers to presenter {ID}. \nCould not find {ContentName} in {Parent.name}! Check your UXML file and RootName override.");
            }
        }
        
        protected abstract void ClearContent();
    }

    /// <summary>
    /// Specific async Presenter type for spawning a layout from a <see cref="IPresenterData.BaseData"/> UXML asset.
    /// When discarding the <see cref="Presenter{Model}.Content"/> is removed from the hierarchy.
    /// <br/> Use when you want to spawn a new dynamic UI layout into a provided <see cref="Presenter{Model}.Parent"/>.
    /// <br/> To initialise, use the constructor and then the <see cref="World.BindPresenter"/> method.
    /// </summary>
    public abstract class AdditivePresenter<TData, TModel> : Presenter<TModel>
        where TData : IPresenterData
        where TModel : IModel {
        public TData Data { get; }

        ARAssetReference _prototypeReference;
        
        protected AdditivePresenter(TData data, VisualElement parent) : base(parent) {
            Data = data;
        }

        protected sealed override void PrepareContent() {
            _prototypeReference = Data.BaseData.uxml.GetAndLoad<VisualTreeAsset>(handle => OnPrototypeLoaded(handle.Result));
        }

        protected sealed override void DiscardInternal() {
            Content?.RemoveFromHierarchy();
            _prototypeReference?.ReleaseAsset();
        }
        
        void OnPrototypeLoaded(VisualTreeAsset prototype) {
            Content = prototype.Instantiate();
            Parent.Add(Content);
            NotifyFullyInitialized();
        }
    }
}