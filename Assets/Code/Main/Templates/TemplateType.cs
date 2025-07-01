using System;

namespace Awaken.TG.Main.Templates {
    public enum TemplateType : byte {
        Regular,
        System,
        Debug,
        ForRemoval,
    }
    
    [Flags]
    public enum TemplateTypeFlag : byte {
        Regular = 1 << 0,
        System = 1 << 1,
        Debug = 1 << 2,
        ForRemoval = 1 << 3,
        All = byte.MaxValue
    }

    public static class TemplateTypeUtils {
        public static TemplateTypeFlag ToFlag(this TemplateType type) {
            return type switch {
                TemplateType.Regular => TemplateTypeFlag.Regular,
                TemplateType.System => TemplateTypeFlag.System,
                TemplateType.Debug => TemplateTypeFlag.Debug,
                TemplateType.ForRemoval => TemplateTypeFlag.ForRemoval,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
        
        public static bool Contains(this TemplateTypeFlag flag, TemplateType type) {
            return (flag & type.ToFlag()) != 0;
        }
    }
}