using System;
using Awaken.TG.MVC.Events;
using JetBrains.Annotations;

namespace Awaken.TG.MVC {
    public static class ModelExtensions {
        public static void Trigger<TSource, TPayload>(this TSource source, Event<TSource, TPayload> evt, TPayload payload) where TSource : IModel {
            // trigger only if we're already added to the world
            if (source.ID != null) {
                World.EventSystem.Trigger(source, evt, payload);
            }
        }

        public static IEventListener ListenTo<TSource, TPayload>(this TSource source, IEvent<TSource, TPayload> evt, Action<TPayload> callback,
            IListenerOwner owner) where TSource : IModel {
            return World.EventSystem.ListenTo(EventSystem.PatternForModel(source), evt, owner, callback);
        }

        public static IEventListener ListenTo<TSource, TPayload>(this TSource source, Event<TSource, TPayload> evt, Action<TPayload> callback)
            where TSource : IModel {
            return ListenTo(source, evt, callback, source);
        }

        public static IEventListener ListenTo<TSource, TPayload>([NotNull] this TSource source, Event<TSource, TPayload> evt, Action callback) where TSource : IModel {
            return ListenTo(source, evt, callback, source);
        }

        public static IEventListener ListenTo<TSource, TPayload>(this TSource source, Event<TSource, TPayload> evt, Action callback, IListenerOwner owner)
            where TSource : IModel {
            return World.EventSystem.ListenTo(EventSystem.PatternForModel(source), evt, owner, _ => callback());
        }

        public static IEventListener ListenToLimited<TSource, TPayload>(this TSource source, Event<TSource, TPayload> evt, Action<TPayload> callback,
            IListenerOwner owner, int limit = 1) where TSource : IModel {
            return World.EventSystem.LimitedListenTo(EventSystem.PatternForModel(source), evt, owner, callback, limit);
        }

        public static IEventListener ListenToLimited<TSource, TPayload>(this TSource source, Event<TSource, TPayload> evt, Action callback,
            IListenerOwner owner, int limit = 1) where TSource : IModel {
            return World.EventSystem.LimitedListenTo(EventSystem.PatternForModel(source), evt, owner, _ => callback(), limit);
        }
    }
}