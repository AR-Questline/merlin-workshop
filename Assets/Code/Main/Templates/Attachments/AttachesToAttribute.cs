using System;
using System.Diagnostics;

namespace Awaken.TG.Main.Templates.Attachments {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true), Conditional("UNITY_EDITOR")]
    public class AttachesToAttribute : Attribute {
        public Type AttachedToType { get; }
        public AttachmentCategory Category { get;}
        public string Description { get; }

        public AttachesToAttribute(Type attachedToType, AttachmentCategory category, string description) {
            AttachedToType = attachedToType;
            Category = category;
            Description = description;
        }
    }

    public enum AttachmentCategory {
        Common = 0,
        Rare = 1,
        ExtraCustom = 2,
        Technical = 3,
        
        // Quests
        Trackers = 10,
        Effectors = 11,
    }
}