using System;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Templates;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    [Serializable]
    public class Context {
        public ContextType type;
        public TemplateReference template;

        public string ToContextID([CanBeNull] Story story) {
            switch (type) {
                case ContextType.Location:
                    return story?.OwnerLocation?.ContextID;
                case ContextType.Story:
                    return story?.ContextID ?? string.Empty;
                case ContextType.Quest:
                    return template.GUID;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum ContextType : byte {
        Location = 0,
        Story = 2,
        Quest = 3,
    }
}