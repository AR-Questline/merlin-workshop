using System;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.MVC {
    /// <summary>
    /// Specific type of <see cref="IVisualPresenter"/> that is bound to a <see cref="IModel"/>.
    /// </summary>
    public interface IPresenter : IVisualPresenter, IListenerOwner {
        IModel GenericModel { get; }

        void Initialize(IModel model, Action onInitialized = null);
        void Discard();
    }
}