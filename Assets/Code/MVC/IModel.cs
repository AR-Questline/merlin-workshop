using System.Collections.Generic;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;

namespace Awaken.TG.MVC {
    public interface IModel : IListenerOwner, IEventSource {
        string ContextID { get; }
        Domain DefaultDomain { get; }
        Domain CurrentDomain { get; }
        void MoveToDomain(Domain domain);
        void AssignID(string id);
        void AssignID(Services services);

        // === Lifecycle
        void Discard();
        void DiscardFromDomainDrop();
        bool IsBeingInitialized { get; }
        bool IsInitialized { get; }
        bool IsFullyInitialized { get; }
        bool DiscardAfterInit { get; }
        bool WasDiscarded { get; }
        bool IsBeingDiscarded { get; }
        /// <summary>
        /// WasDiscarded || IsBeingDiscarded;
        /// </summary>
        bool HasBeenDiscarded { get; }
        bool WasDiscardedFromDomainDrop { get; }
        bool IsBeingSaved { get; }
        bool AllowElementSave(Element ele);
        void DeserializationFailed();

        // === Persistence

        public bool IsNotSaved { get; }
        public bool MarkedNotSaved { get; set; }

        // === Elements

        T AddElement<T>(T element) where T : IElement;
        bool HasElement<T>() where T : class, IModel;
        T Element<T>() where T : class, IModel;
        T TryGetElement<T>() where T : class, IModel;
        bool TryGetElement<T>(out T element) where T : class, IModel;
        T CachedElement<T>(ref T cache) where T : class, IModel;
        ModelsSet<T> Elements<T>() where T : class, IModel;
        List<Element> AllElements();
        void RemoveElementsOfType<T>() where T : class, IElement;
        void RemoveElement(IElement element);
        void NotifyElementDiscarded(Element element);

        // === View-related
        
        View MainView { get; }
        T View<T>() where T : class, IView;
        IEnumerable<IView> Views { get; }
        SpawnsView[] GetAutomaticallySpawnedViews();
        
        // === Presenter-related

        T Presenter<T>() where T : class, IPresenter;
        IEnumerable<IPresenter> Presenters { get; }
        
        // === Event-related

        bool IListenerOwner.CanReceiveEvents => !WasDiscarded;
        void TriggerChange();
        
        // === Relations
        RelatedValue<TRelated> RelatedValue<TRelated>(Relation<TRelated> viaRelation) where TRelated : class, IModel;
        RelatedList<TRelated> RelatedList<TRelated>(Relation<TRelated> viaRelation) where TRelated : class, IModel;
        internal RelationStore GetRelationStore(bool createIfMissing = false);
    }
}