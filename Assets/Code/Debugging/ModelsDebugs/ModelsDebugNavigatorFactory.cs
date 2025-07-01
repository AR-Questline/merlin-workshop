using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.ModelsDebugs.Runtime;
using Awaken.TG.Utility.Reflections;

namespace Awaken.TG.Debugging.ModelsDebugs {
    public static class ModelsDebugNavigatorFactory {
        public static IModelsDebugNavigator Get(bool inGame) {
#if UNITY_EDITOR
            var editorNavigatorType = ImplementsInterface<IModelsDebugNavigator>()
                .First(t => t.FullName.Contains("Editor") != inGame);
            return (IModelsDebugNavigator)Activator.CreateInstance(editorNavigatorType);
            
            IEnumerable<Type> ImplementsInterface<TInterface>() {
                var type = typeof(TInterface);
                return AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
            }
#else
            return new RuntimeModelsDebugNavigator();
#endif
        }
    }
}