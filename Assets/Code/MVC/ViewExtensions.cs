using System;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.MVC {
    public static class ViewExtensions {
        public static void Trigger<TSource, TPayload>(this TSource source, Event<TSource, TPayload> evt, TPayload payload)
            where TSource : IView, IEventSource => World.EventSystem.Trigger(source, evt, payload);

        [UnityEngine.Scripting.Preserve]
        public static void ListenTo<TSource, TPayload>(this TSource source, Event<TSource, TPayload> evt, Action callback, IListenerOwner owner)
            where TSource : IView, IEventSource =>
            World.EventSystem.ListenTo(source.ID, evt, owner, callback);

        public static void ListenTo<TSource, TPayload>(this TSource source, Event<TSource, TPayload> evt, Action<TPayload> callback, IListenerOwner owner)
            where TSource : IView, IEventSource =>
            World.EventSystem.ListenTo(source.ID, evt, owner, callback);
    }
}