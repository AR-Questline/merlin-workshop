using System;
using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.MVC {
    public class Services {
        // === The actual service registry
        const int ServicesCapacity = 100;
        readonly Dictionary<RuntimeTypeHandle, IService> _serviceByType = new(ServicesCapacity);
        StructList<IDomainBoundService> _domainBoundServices = new(ServicesCapacity); 
        StructList<SerializedService> _serializedServices = new(ServicesCapacity); 

        // === Registering and retrieving services
        public T Register<T>(T service) where T : class, IService {
            if (service == null) {
                throw new NullReferenceException($"Service of type {typeof(T)} is null. Please fix it or call programmer to fix it.");
            }
            Type t = typeof(T);
            // don't allow second instance of same type
            if (!_serviceByType.TryAdd(t.TypeHandle, service)) {
                throw new NullReferenceException($"Service of type {typeof(T)} already exists.");
            }
            
            if (service is IDomainBoundService domainBoundService) {
                _domainBoundServices.Add(domainBoundService);
            }
            if (service is SerializedService serializedService) {
                _serializedServices.Add(serializedService);
            }
            // return a reference for chaining
            return service;
        }
        
        public void UnregisterDomainBoundServices(Domain domain) {
            var domainBoundServices = _domainBoundServices.BackingArray;
            int count = _domainBoundServices.Count;
            for (int i = count - 1; i >= 0; i--) {
                var service = domainBoundServices[i];
                if (service.Domain.IsChildOf(domain, true) && service.RemoveOnDomainChange()) {
                    World.EventSystem.RemoveAllListenersOwnedBy(service);
                    if (service is SerializedService serializedService) {
                        _serializedServices.RemoveSwapBack(serializedService);
                    }
                    _domainBoundServices.RemoveAtSwapBack(i);
                    _serviceByType.Remove(service.GetType().TypeHandle);
                }
            }
        }
        
        public T Get<T>() where T : class, IService {
            return Get(typeof(T).TypeHandle) as T;
        }

        public object Get(Type type) => Get(type.TypeHandle);
        
        public object Get(RuntimeTypeHandle typeHandle) {
            if (_serviceByType.TryGetValue(typeHandle, out var service)) {
                return service;
            }
            throw new ArgumentException($"No service registered with type {typeHandle.GetType().Name}.");
        }

        public T TryGet<T>(bool logIfNone = false) where T : class, IService {
            return TryGet(typeof(T).TypeHandle, logIfNone) as T;
        }
        
        public object TryGet(RuntimeTypeHandle typeHandle, bool logIfNone = false) {
            if (_serviceByType.TryGetValue(typeHandle, out var service)) {
                return service;
            }

            if (logIfNone) {
                Log.Important?.Error($"No service registered with type {typeHandle.GetType().Name}.");
            }
            
            return null;
        }

        public bool TryGet<T>(out T service) where T : class, IService {
            service = TryGet<T>();
            return service != null;
        }

        [UnityEngine.Scripting.Preserve]
        public async UniTask<T> WaitFor<T>() where T : class, IService {
            await UniTask.WaitUntil(() => TryGet<T>() != null);
            return TryGet<T>();
        }

        public ReadOnlySpan<IDomainBoundService> AllDomainBoundServices() {
            return _domainBoundServices.GetBackingArrayReadOnlySpan();
        }
        
        public ReadOnlySpan<SerializedService> AllSerializedServices() {
            return _serializedServices.GetBackingArrayReadOnlySpan();
        }

        public IEnumerable<IService> All() => _serviceByType.Values;

        public bool Has<T>() where T : class, IService {
            return _serviceByType.ContainsKey(typeof(T).TypeHandle);
        }
    }
}
