using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.MVC {
    /// <summary>
    /// Interface implemented by every View.
    /// </summary>
    public interface IView : IListenerOwner, IEventSource, IModelProvider {
        Transform transform { get; }
        GameObject gameObject { get; }
        IModel GenericTarget { get; }
        bool IsInitialized { get; }
        bool HasBeenDiscarded { get; }
        
        /// <summary>
        /// Discards the view completely, destroying the game object
        /// and unregistering it from any relevant services.
        /// </summary>
        void Discard();
        
        // --- IModelProvider
        IModel IModelProvider.Model => GenericTarget;
    }
    
    public interface IView<out T> : IView where T : IModel {
        T Target { get; }
    }
}