using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Templates.Attachments {
    public static class PossibleAttachmentsUtil {
        public static Dictionary<AttachmentCategory, PossibleAttachmentsGroup> Get(Type type) {
#if UNITY_EDITOR
            Dictionary<AttachmentCategory, PossibleAttachmentsGroup> groups = new();
            
            (Type type, AttachesToAttribute attibute)[] extensionAttributes = UnityEditor.TypeCache.GetTypesWithAttribute<AttachesToAttribute>()
                .Select(t => (type: t, attribute: GetExtendsAttribute(t, type)))
                .Where(pair => pair.attribute != null)
                .ToArray();

            foreach (var g in extensionAttributes.GroupBy(a => a.attibute.Category).OrderBy(g => g.Key)) {
                var group = new PossibleAttachmentsGroup();
                groups[g.Key] = group;
                group.category = g.Key;
                
                foreach (var a in g.OrderBy(e => e.type.Name)) {
                    group.attachments.Add(new PossibleAttachmentEditorData {
                        description = a.attibute.Description,
                        name = UnityEditor.ObjectNames.NicifyVariableName(a.type.Name.Replace("Attachment", "")),
                        attachmentType = a.type,
                    });
                }
            }

            static AttachesToAttribute GetExtendsAttribute(Type t, Type type) {
                return AttributesCache.TypeAttributes[t].OfType<AttachesToAttribute>().FirstOrDefault(a => a.AttachedToType == type);
            }

            return groups;
#else
            return new Dictionary<AttachmentCategory, PossibleAttachmentsGroup>();
#endif
        }
    }

    [Serializable]
    public class PossibleAttachmentsGroup {
        [HideInInspector] [UnityEngine.Scripting.Preserve] 
        public AttachmentCategory category;
        [TableList(IsReadOnly = true, AlwaysExpanded = true, ShowPaging = false, HideToolbar = true), HideLabel]
        public List<PossibleAttachmentEditorData> attachments = new(10);

        public PossibleAttachmentsGroup WithContext(Component c) {
            foreach (var a in attachments) {
                a.go = c.gameObject;
            }
            return this;
        }
    }

    [Serializable]
    public class PossibleAttachmentEditorData {
        [DisplayAsString, TableColumnWidth(150)] [UnityEngine.Scripting.Preserve] 
        public string name;
        [DisplayAsString, TableColumnWidth(300)] [UnityEngine.Scripting.Preserve] 
        public string description;

        [NonSerialized]
        public Type attachmentType;
        [NonSerialized]
        public GameObject go;
        
        bool CanAddComponent => go != null && !go.TryGetComponent(attachmentType, out _);
        bool IsComponentAdded => go != null && go.TryGetComponent(attachmentType, out _);
        
        [Button, VerticalGroup("-"), ShowIf(nameof(CanAddComponent)), TableColumnWidth(50, false)]
        void Add() {
            go.AddComponent(attachmentType);
        }

        [ShowInInspector, VerticalGroup("-"), HideLabel, GUIColor(0f, 0.8f, 0f), ShowIf(nameof(IsComponentAdded)), DisplayAsString, TableColumnWidth(50, false)]
        string Added => "Added";
    }
}