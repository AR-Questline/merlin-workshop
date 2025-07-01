using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.TG.MVC {
    [Il2CppEagerStaticClassConstruction]
    public static class ModelUtils {
        // Arrays are faster for small collections than HashSets
        static readonly Type[] ModelHierarchyBlackListClassConcrete = new[] {
            typeof(Element),
            typeof(Model),
        };

        static readonly Type[] ModelHierarchyBlackListInterfaceConcrete = new[] {
            typeof(IModel),
            typeof(IElement),
            typeof(IListenerOwner),
            typeof(IEventSource),
        };

        static readonly Type[] ModelHierarchyBlackListClassGeneric = new[] {
            typeof(Element<>),
        };

        static readonly Type[] ModelHierarchyBlackListInterfaceGeneric = new[] {
            typeof(IElement<>),
        };

        static readonly OnDemandCache<Type, Type[]> ModelHierarchyTypesCache = new(t => ModelHierarchyTypesGenerate(t).ToArray());

        /// <summary>
        /// Returns all (except blacklisted) inherited class and implemented interfaces
        /// </summary>
        public static Type[] ModelHierarchyTypes(IModel model) => ModelHierarchyTypes(model.GetType());
        public static Type[] ModelHierarchyTypes(Type type) => ModelHierarchyTypesCache[type];

        static IEnumerable<Type> ModelHierarchyTypesGenerate(Type type) {
            // all superclasses up to Model
            Type currentType = type;
            while (currentType != typeof(Model)) {
                var isGeneric = currentType!.IsGenericType;
                if ((isGeneric && !ModelHierarchyBlackListClassGeneric.Contains(currentType.GetGenericTypeDefinition())) ||
                    (!isGeneric && !ModelHierarchyBlackListClassConcrete.Contains(currentType))) {
                    yield return currentType;
                }
                currentType = currentType.BaseType;
            }
            // all implemented interface types
            foreach (Type iface in type.GetInterfaces()) {
                var isGeneric = iface.IsGenericType;
                if ((isGeneric && !ModelHierarchyBlackListInterfaceGeneric.Contains(iface.GetGenericTypeDefinition())) ||
                    (!isGeneric && !ModelHierarchyBlackListInterfaceConcrete.Contains(iface))) {
                    yield return iface;
                }
            }
        }

        [Conditional("DEBUG")]
        public static void DebugCheckModelTypeForByTypeOperations(Type type) {
            if (type.IsInterface) {
                if ((type.IsGenericType && ModelHierarchyBlackListInterfaceGeneric.Contains(type.GetGenericTypeDefinition())) ||
                    (!type.IsGenericType && ModelHierarchyBlackListInterfaceConcrete.Contains(type))) {
                    throw new ArgumentException($"Model type {type} is blacklisted for by type operations");
                }
            } else {
                if ((type.IsGenericType && ModelHierarchyBlackListClassGeneric.Contains(type.GetGenericTypeDefinition())) ||
                    (!type.IsGenericType && ModelHierarchyBlackListClassConcrete.Contains(type))) {
                    throw new ArgumentException($"Model type {type} is blacklisted for by type operations");
                }
            }
        }

        /// <summary>
        /// Similar to Unity's GetComponentInParent, recursively searches up the hierarchy for matching model.
        /// </summary>
        public static T GetModelInParent<T>(this IModel model) where T : class {
            while (!(model is T) && model is Element ele) {
                model = ele.GenericParentModel;
            }

            return model as T;
        }

        /// <summary>
        /// Returns the parent of the whole hierarchy of elements. 
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public static IModel GetRoot(this IElement element) {
            IElement current = element;
            while (current.GenericParentModel is IElement parent) {
                current = parent;
            }

            return current.GenericParentModel;
        }

        public static IEnumerable<Element> GetChildren(this IModel model, bool recursive = true) {
            foreach (var element in model.AllElements()) {
                yield return element;
                if (recursive) {
                    foreach (var ele2 in element.GetChildren()) {
                        yield return ele2;
                    }
                }
            }
        }

        public static T FindInHierarchy<T>(IModel m) where T : class, IModel {
            if (m is T mt) {
                return mt;
            }

            // First try to find in this model's children
            T result = m.GetChildren().OfType<T>().FirstOrDefault();
            // If failed, try to find in parents
            result ??= m.GetModelInParent<T>();

            return result;
        }

        /// <summary>
        /// Executes given callback on first model of given type, either existing or next that will appear 
        /// </summary>
        public static IEventListener DoForFirstModelOfType<T>(Action<T> callback, IListenerOwner owner) where T : Model {
            T model = World.AllInOrder<T>().FirstOrDefault();
            if (model != null) {
                callback(model);
                return null;
            } else {
                return World.EventSystem.LimitedListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<T>(), owner, m => callback((T) m), 1);
            }
        }

        public static IEventListener DoForFirstModelOfType<T>(Action callback, IListenerOwner owner) where T : Model {
            return DoForFirstModelOfType<T>(_ => callback(), owner);
        }

        /// <summary>
        /// Attaches given listener to first existing model or next model of type 
        /// </summary>
        public static IEventListener ListenToFirstModelOfType<TSource, TPayload>(IEvent<TSource, TPayload> evt, Action<TPayload> callback, IListenerOwner owner)
            where TSource : class, IModel {
            TSource model = World.AllInOrder<TSource>().FirstOrDefault();
            if (model != null) {
                return model.ListenTo(evt, callback, owner);
            } else {
                return World.Services.Get<EventSystem>().ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<TSource>(), owner,
                    addedModel => { (addedModel as TSource).ListenTo(evt, callback, owner); });
            }
        }

        public static IEventListener ListenToFirstModelOfType<TSource, TPayload>(IEvent<TSource, TPayload> evt, Action callback, IListenerOwner owner)
            where TSource : class, IModel {
            return ListenToFirstModelOfType(evt, _ => callback(), owner);
        }

        public static void AfterFullyInitialized(this IModel target, Action callback, IListenerOwner owner = null) {
            owner ??= target;
            if (target.IsFullyInitialized) {
                callback();
            } else {
                target.ListenTo(Model.Events.AfterFullyInitialized, callback, owner);
            }
        }

        public static T GetSingletonModel<T>(Func<T> markerCreator) where T : Model {
            T model = World.Any<T>();
            model ??= World.Add(markerCreator());
            return model;
        }
        public static T GetSingletonModel<T>() where T : Model, new() {
            T model = World.Any<T>();
            model ??= World.Add(new T());
            return model;
        }

        public static void RemoveSingletonModel<T>() where T : Model {
            World.Any<T>()?.Discard();
        }

        public static T AddMarkerElement<T>(this IModel model, Func<T> markerCreator) where T : class, IElement {
            return model.TryGetElement<T>() ?? model.AddElement(markerCreator());
        }
        public static T AddMarkerElement<T>(this IModel model) where T : class, IElement, new() {
            return model.TryGetElement<T>() ?? model.AddElement(new T());
        }

        public static void RemoveMarkerElement<T>(this IModel model) where T : class, IElement {
            model.TryGetElement<T>()?.Discard();
        }

        /// <summary>
        /// Returns simplified model id also if searching model is a nested element
        /// </summary>
        public static string GetSimplifiedModelId(string fullModelId) {
            const string pattern = @":?([a-zA-Z0-9_]*:?\d*)$";
            var match = Regex.Match(fullModelId ?? string.Empty, pattern);
            return match.Success ? match.Groups[1].ToString() : string.Empty;
        }
    }
}
