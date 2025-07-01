using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    public abstract class MemberListItemInspector {
        public static readonly GUIStyle LabelStyle;
        [UnityEngine.Scripting.Preserve]
        static readonly Lazy<string> EmptyValue = new(string.Empty);

        static MemberListItemInspector() {
            var skin = GUI.skin;
            LabelStyle = new GUIStyle(skin.label);
            LabelStyle.richText = true;
            
            BuildInspectorTypes();
            DefaultInspector = new DefaultInspector();
            MethodInspector = new MethodItemInspector();
        }
        
        static readonly Dictionary<string, MemberListItemInspector> InspectorsCache = new Dictionary<string, MemberListItemInspector>();
        static readonly Dictionary<Type, Type> Type2Inspector = new Dictionary<Type, Type>();
        protected static readonly MemberListItemInspector DefaultInspector;
        static StringBuilder s_cacheKeyBuilder = new StringBuilder();
        public static readonly MethodItemInspector MethodInspector;

        public static MemberListItemInspector GetInspector(MembersListItem membersListItem, object targetValue, object target) {
            s_cacheKeyBuilder.Clear();
            var targetType = targetValue?.GetType() ?? membersListItem.Type;
            s_cacheKeyBuilder.Append(target.GetHashCode());
            s_cacheKeyBuilder.Append(membersListItem.Name);
            
            if (targetValue == null) {
                s_cacheKeyBuilder.Append("-1");
            } else if (targetValue is IEnumerable && !(targetValue is ICollection)) {
                s_cacheKeyBuilder.Append("Enumerable");
            } else {
                s_cacheKeyBuilder.Append(targetValue.GetHashCode());
            }

            string cacheKey = s_cacheKeyBuilder.ToString();
            if (targetValue != null && InspectorsCache.TryGetValue(cacheKey, out var inspector)) {
                return inspector;
            }

            CheckType2Inspector(targetType);
            if (!Type2Inspector.ContainsKey(targetType) || targetValue == null) {
                return DefaultInspector;
            }
            inspector = (MemberListItemInspector)Activator.CreateInstance(Type2Inspector[targetType]);
            InspectorsCache.Add(cacheKey, inspector);
            return inspector;
        }

        static void CheckType2Inspector(Type target) {
            if (!Type2Inspector.ContainsKey(target)) {
                foreach (var type in ImplementedTypes(target)) {
                    if (Type2Inspector.ContainsKey(type)) {
                        Type2Inspector.Add(target, Type2Inspector[type]);
                        return;
                    }
                }
            }
        }

        static void BuildInspectorTypes() {
            IEnumerable<Type> inspectors = ReflectionExtension.SubClassesOf<MemberListItemInspector>();

            foreach (Type inspectorType in inspectors) {
                if (!inspectorType.IsAbstract) {
                    Type2Inspector.Add(((MemberListItemInspector)Activator.CreateInstance(inspectorType)).DrawingType, inspectorType);
                }
            }
        }

        static IEnumerable<Type> ImplementedTypes(Type target) {
            // all superclasses up to Model
            Type currentType = target;
            while (currentType != typeof(object) && currentType != null) {
                yield return currentType;
                currentType = currentType.BaseType;
            }
            // all implemented interface types
            foreach (Type iface in target.GetInterfaces()) {
                yield return iface;
            }
        }

        public abstract Type DrawingType { get; }
        public abstract void Draw(MembersListItem member, object value, object target, ModelsDebug modelsDebug, string[] searchContext, int searchHash);

        protected int _lastSearchContextHash = -1;
        protected int _lastValueHash = -1;
        protected bool _lastInContext = true;
        
        protected bool IsInContext(MembersListItem member, object value, string[] searchContext, int hash) {
            if (searchContext.IsNullOrEmpty()) {
                return true;
            }

            var valueHash = value?.GetHashCode() ?? -1;
            if (hash == _lastSearchContextHash && _lastValueHash == valueHash) {
                return _lastInContext;
            }
            _lastValueHash = valueHash;
            _lastSearchContextHash = hash;

            var inContext = false;
            var i = 0;
            while (!inContext && i < searchContext.Length) {
                inContext = member.Name.IndexOf(searchContext[i], StringComparison.InvariantCultureIgnoreCase) >= 0;
                ++i;
            }
            if (inContext) {
                _lastInContext = true;
                return true;
            }
            if (value == null) {
                _lastInContext = false;
                return false;
            }

            i = 0;
            var stringValue = new Lazy<string>(value.ToString);
            while (!inContext && i < searchContext.Length) {
                inContext = ValueInContext(value, stringValue, searchContext[i]);
                ++i;
            }

            _lastInContext = inContext;
            return inContext;
        }

        protected virtual bool ValueInContext(object value, Lazy<string> stringValue, string searchPart) {
            return stringValue.Value.IndexOf(searchPart, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
    }
    
    public abstract class MemberListItemInspector<T> : MemberListItemInspector {
        public override Type DrawingType => typeof(T);
        public override void Draw(MembersListItem member, object value, object target, ModelsDebug modelsDebug, string[] searchContext, int searchHash) {
            if (!IsInContext(member, value, searchContext, searchHash)) {
                return;
            }
            var oldEnable = GUI.enabled;
            GUI.enabled = member.Writeable;
            if (value != null) {
                DrawValue(member, value, target, modelsDebug);
            } else {
                GUILayout.Label($"<color=lightblue>{member.Name}</color>:{YellowNull}", LabelStyle);
            }
            GUI.enabled = oldEnable;
        }

        public abstract void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug);
        
        protected T CastedValue(object value) => (T)value;
        protected static string YellowNull => "<color=yellow>NULL</color>";
    }
}