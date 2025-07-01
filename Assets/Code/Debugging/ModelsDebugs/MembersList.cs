using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Debugging.ModelsDebugs {
    public class MembersList : IEquatable<MembersList> {
        public object RelatedObject { get; }
        public string Name { get; }
        public float ScrollY { get; set; }
        Type Type { get; }

        public MembersListItem[] Items { get; private set; }
        public MethodMemberListItem[] Methods { get; private set; }

        static MembersListItem ConvertToMemberItem(MemberInfo memberInfo) {
            return new MembersListItem(memberInfo);
        }
        
        static MethodMemberListItem ConvertToMethodMemberItem(MethodInfo methodInfo) {
            return new MethodMemberListItem(methodInfo);
        }

        // === Construction
        public MembersList(object relatedObject) {
            RelatedObject = relatedObject;

            Type = RelatedObject.GetType();
            if (RelatedObject is Model model) {
                Name = ExtractElementName(model.ID);
            } else if (RelatedObject is ITemplate template) {
                Name = $"{TemplatesUtil.TemplateToObject(template).name}--{template.GUID}";
            } else {
                Name = Type.Name;
            }

            if (ItemsCache.TryGetValue(Type, out var items)) {
                Items = items;
            } else {
                Items = FilterOfSameMembers(relatedObject.AllFields().Select(ConvertToMemberItem)).ToArray();
                ItemsCache.Add(Type, Items);
            }
            
            if (MethodsCache.TryGetValue(Type, out var methods)) {
                Methods = methods;
            } else {
                Methods = relatedObject.AllMethods().Select(ConvertToMethodMemberItem).ToArray();
                MethodsCache.Add(Type, Methods);
            }
        }

        // === Static cache
        static Dictionary<Type, MembersListItem[]> ItemsCache = new Dictionary<Type, MembersListItem[]>();
        static Dictionary<Type, MethodMemberListItem[]> MethodsCache = new Dictionary<Type, MethodMemberListItem[]>();

        static readonly Dictionary<string, MembersListItem> NamedMembers = new Dictionary<string, MembersListItem>();

        public static void BuildCache() {
            if (ItemsCache.Count == 0) {
                var modelSubclasses = ReflectionExtension.SubClassesOf<Model>();
                foreach (Type modelSubclass in modelSubclasses) {
                    ItemsCache.Add(modelSubclass, FilterOfSameMembers(modelSubclass.AllFields().Select(ConvertToMemberItem)).ToArray());
                    MethodsCache.Add(modelSubclass, modelSubclass.AllMethods().Select(ConvertToMethodMemberItem).ToArray());
                }
            }
        }

        static IEnumerable<MembersListItem> FilterOfSameMembers(IEnumerable<MembersListItem> items) {
            NamedMembers.Clear();

            foreach (MembersListItem membersListItem in items) {
                if (NamedMembers.TryGetValue(membersListItem.Name, out var saved)) {
                    if (!saved.Writeable && membersListItem.Writeable) {
                        NamedMembers[membersListItem.Name] = membersListItem;
                    }
                } else {
                    NamedMembers.Add(membersListItem.Name, membersListItem);
                }
            }
            
            return NamedMembers.Values;
        }
        
        // === Helpers
        static readonly OnDemandCache<string, string> NameCache = new(id => {
            if (id.CountCharacter(':') < 2) {
                return id;
            }
            string[] parts = id.Split(':');
            string elementId = parts[parts.Length - 2];
            string elementIndex = parts[parts.Length - 1];
            return $"{elementId}:{elementIndex}";
        });
        static string ExtractElementName(string id) {
            return NameCache[id];
        }

        public bool Equals(MembersList other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(RelatedObject, other.RelatedObject) && Name == other.Name && Type == other.Type;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MembersList) obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (RelatedObject != null ? RelatedObject.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(MembersList left, MembersList right) {
            return Equals(left, right);
        }

        public static bool operator !=(MembersList left, MembersList right) {
            return !Equals(left, right);
        }
    }
}