using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Debugging.UI.InteractionTrees.Leaves;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Handlers;
using DG.Tweening.Plugins.Core.PathCore;

namespace Awaken.TG.Editor.Debugging.UI.InteractionTrees.Containers {
    public abstract class HandlerContainer : IHandlerItem {
        public string Name { get; }

        public UIResult? Result {
            get {
                UIResult? result = null;
                foreach (var r in Handlers.Select(h => h.Result)) {
                    if (r is UIResult.Accept or UIResult.Prevent) {
                        return r;
                    }
                    if (r != null) {
                        result = r;
                    }
                }
                return result;
            }
        }

        public (string, UIResult?) HandlerAndResult {
            get {
                foreach (var h in Handlers) {
                    if (h is UIAwareItem or SmartHandlerItem) {
                        if (h.Result is UIResult.Accept or UIResult.Prevent) {
                            return ($"{Name}/{h.Name}", h.Result);
                        }
                    } else if (h is HandlerContainer container) {
                        (string path, UIResult? r) = container.HandlerAndResult;
                        if (r is UIResult.Accept or UIResult.Prevent) {
                            return ($"{Name}/{path}", r);
                        }
                    }
                }
                return ("", UIResult.Ignore);
            }
        }

        public abstract IEnumerable<IHandlerItem> Handlers { get; }
        
        protected HandlerContainer(string name) {
            Name = name;
        }

        IEnumerable<IHandlerItem> AllHandlersFlat {
            get {
                foreach (var handler in Handlers) {
                    yield return handler;
                    if (handler is HandlerContainer c) {
                        foreach (var h in c.AllHandlersFlat) {
                            yield return h;
                        }
                    }
                }
            }
        }

        IEnumerable<UIAwareItem> AllUIAwares => AllHandlersFlat.Where(h => h is UIAwareItem).Cast<UIAwareItem>();
        IEnumerable<SmartHandlerItem> AllSmartHandlers => AllHandlersFlat.Where(h => h is SmartHandlerItem).Cast<SmartHandlerItem>();
        
        
        public UIAwareItem FindItemFor(IUIAware aware) {
            return AllUIAwares.First(a => a.Aware == aware);
        }
        public SmartHandlerItem FindItemFor(ISmartHandler handler) {
            return AllSmartHandlers.First(h => h.Handler == handler);
        }
    }
}