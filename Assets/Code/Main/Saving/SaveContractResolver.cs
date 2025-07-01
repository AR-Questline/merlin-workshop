using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Awaken.TG.Main.Saving
{
    public class SaveContractResolver : DefaultContractResolver {

        public SaveContractResolver() {
#pragma warning disable 618
            DefaultMembersSearchFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
#pragma warning restore 618
        }
        
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
            var jProperty = base.CreateProperty(member, memberSerialization);
            if (jProperty.Writable) {
                return jProperty;
            }

            jProperty.Writable = (member as PropertyInfo)?.SetMethod != null;
            return jProperty;
        }

        Dictionary<Type, List<MemberInfo>> _cachedTypesWithMembers = new Dictionary<Type, List<MemberInfo>>(400);
        List<MemberInfo> _memberCache = new List<MemberInfo>(100);

        protected override List<MemberInfo> GetSerializableMembers(Type objectType) {
            _memberCache.Clear();
            if (objectType == null) {
                return _memberCache;
            }
            List<MemberInfo> members = GetMembers(objectType, _memberCache);
            return members;
        }

        // Gets members straight from type that declares it, instead of inherited one
        // Members of our types must have SavedAttribute to be saved
        List<MemberInfo> GetMembers(Type type, List<MemberInfo> list) {
            List<MemberInfo> members;
            
            if (_cachedTypesWithMembers.ContainsKey(type)) {
                members = _cachedTypesWithMembers[type];
            } else {
                bool awaken = type.Namespace?.StartsWith("Awaken.") ?? false;
                members = base.GetSerializableMembers(type)
                    .Where(m => m.DeclaringType == type)
                    .Where(m => !awaken || m.GetCustomAttribute<SavedAttribute>() != null)
                    .ToList();
                _cachedTypesWithMembers.Add(type, members);
            }

            list.AddRange(members);
            if (type.BaseType == null) {
                return list;
            } else {
                return GetMembers(type.BaseType, list);
            }
        }
    }
}