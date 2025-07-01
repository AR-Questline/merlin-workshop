using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility.Collections;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.TG.MVC.Attributes {
    [Il2CppEagerStaticClassConstruction]
    public static class AttributesCache {
        static readonly Type UsesPrefabType = typeof(UsesPrefab);
        static readonly Type NoPrefabType = typeof(NoPrefab);
        
        static Dictionary<Type, SpawnsView[]> s_spawnsViewsCache = new();
        static Dictionary<Type, UsesPrefab> s_usesPrefabsCache = new();
        static Dictionary<Type, bool> s_noPrefabCache = new();
        static Dictionary<Type, Dictionary<MemberInfo, bool>> s_isSavedMembersCache = new();
        static Dictionary<Type, List<MemberInfo>> s_memberInfoByType = new();
        public static readonly OnDemandCache<Type, Attribute[]> TypeAttributes = new(static t => (Attribute[])t.GetCustomAttributes());
        public static readonly OnDemandCache<MemberInfo, Attribute[]> MemberAttributes = new(static t => (Attribute[])t.GetCustomAttributes());

        public static SpawnsView[] GetSpawnViews(IModel model) {
            Type modelType = model.GetType();
            if (!s_spawnsViewsCache.TryGetValue(modelType, out var spawnsViews)) {
                spawnsViews = model.GetAutomaticallySpawnedViews();
                s_spawnsViewsCache.Add(modelType, spawnsViews);
            }
            return spawnsViews;
        }
        
        public static UsesPrefab GetUsesPrefab(Type type) {
            if (!s_usesPrefabsCache.TryGetValue(type, out var usesPrefab)) {
                usesPrefab = Attribute.GetCustomAttribute(type, UsesPrefabType) as UsesPrefab;
                s_usesPrefabsCache.Add(type, usesPrefab);
            }
            return usesPrefab;
        }

        public static bool GetNoPrefab(Type type) {
            if (!s_noPrefabCache.TryGetValue(type, out var hasNoPrefab)) {
                hasNoPrefab = Attribute.IsDefined(type, NoPrefabType);
                s_noPrefabCache.Add(type, hasNoPrefab);
            }
            return hasNoPrefab;
        }

        public static bool GetIsSavedMember(Type type, MemberInfo memberInfo) {
            var typeMap = GetFromCache(type, s_isSavedMembersCache, static () => new Dictionary<MemberInfo, bool>());
            return GetFromCache(memberInfo, typeMap, () => memberInfo.IsWriteable() && HasCustomAttribute(memberInfo, typeof(SavedAttribute)));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetIsAssignableFrom(Type type, Type otherType) {
            return type.IsAssignableFrom(otherType);
        }

        public static List<MemberInfo> GetSavedMembersInfos(Type type) {
            return GetFromCache(
                type,
                s_memberInfoByType,
                () => type.GetMembers(LoadSave.DefaultBindingFlags)
                    .Where(field => GetIsSavedMember(type, field))
                    .ToList()
                );
        }
        
        public static TV GetFromCache<TK, TV>(TK type, Dictionary<TK, TV> cache, Func<TV> getFunc) {
            if (!cache.TryGetValue(type, out TV result)) {
                result = getFunc();
                cache.Add(type, result);
            }

            return result;
        }
        
        public static T GetCustomAttribute<T>(MemberInfo member) where T : Attribute => member == null ? null : MemberAttributes[member].OfType<T>().FirstOrDefault();
        public static T GetCustomAttribute<T>(Type type) where T : Attribute => type == null ? null : TypeAttributes[type].OfType<T>().FirstOrDefault();

        static bool HasCustomAttribute(MemberInfo member, Type attributeType) {
            return Attribute.GetCustomAttribute(member, attributeType) != null;
        }
    }
}