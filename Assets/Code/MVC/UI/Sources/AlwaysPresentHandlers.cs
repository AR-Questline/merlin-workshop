using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC.Elements;
using Newtonsoft.Json;

namespace Awaken.TG.MVC.UI.Sources {
    /// <summary>
    /// This source doesn't look for anything - it just always returns the same objects,
    /// ensuring they get a chance to handle every event.
    /// </summary>
    public partial class AlwaysPresentHandlers : Element<GameUI>, IUIHandlerSource {
        public sealed override bool IsNotSaved => true;
        // === Configuration

        public IModel Owner { get; }
        public UIContext Context { get; }
        IUIAware[] _handlers;
        // Higher priority executes first.
        public int Priority { get; }

        // === Constructors

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        protected AlwaysPresentHandlers() { }

        /// <param name="context">Limit input category</param>
        /// <param name="handler">Callback interface</param>
        /// <param name="owner">Bind handler to model lifetime</param>
        /// <param name="priority">Higher priority executes first</param>
        public AlwaysPresentHandlers(UIContext context, IUIAware handler, IModel owner = null, int priority = 0) : this(context, new[] {handler}, owner, priority) { }
        public AlwaysPresentHandlers(UIContext context, IEnumerable<IUIAware> handlers, IModel owner = null, int priority = 0) {
            Context = context;
            _handlers = handlers.ToArray();
            Owner = owner;
            Owner?.ListenTo(Events.BeforeDiscarded, Discard, this);
            Priority = priority;
        }

        // === Implementation

        public virtual void ProvideHandlers(UIPosition _, List<IUIAware> handlers) => handlers.AddRange(_handlers);
    }
}
