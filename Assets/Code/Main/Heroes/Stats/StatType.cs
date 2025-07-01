using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Stats {
    public class StatType<T> : StatType where T : class, IWithStats {
        readonly Func<T, Stat> _getter;

        protected StatType(string id, string displayName, Func<T, Stat> getter, 
            string inspectorCategory = "", Param param = null)
            : base(id, displayName, inspectorCategory, param) {

            _getter = getter;
        }

        public Stat RetrieveFrom(T statOwner) => _getter(statOwner);
    }

    public class StatType : RichEnum {
        // === Properties

        public LocString DisplayName { get; }
        public LocString Description { get; }
        public string Abbreviation { get; }
        public string IconTag => _iconTag.AddTooltip(Tooltip);
        public string IconTagNoTooltip { get; }
        public Color Color { get; }
        public bool IsTweakable { get; }
        public string Tooltip { get; }
        readonly string _iconTag;
        
        public static class Events {
            static readonly OnDemandCache<Type, Event<IWithStats, Stat>> StatOfTypeChangedCache = new(type => new($"StatOfTypeChanged/{type.Name}"));
            [UnityEngine.Scripting.Preserve] public static Event<IWithStats, Stat> StatOfTypeChanged(Type type) => StatOfTypeChangedCache[type];
            public static Event<IWithStats, Stat> StatOfTypeChanged<T>() where T : StatType => StatOfTypeChangedCache[typeof(T)];

            public static void TriggerStatOfTypeChanged(IWithStats owner, Stat stat) {
                var type = stat.Type.GetType();
                while (type != null) {
                    if (StatOfTypeChangedCache.TryGetValue(type, out var statOfTypeChanged)) {
                        owner.Trigger(statOfTypeChanged, stat);
                    }

                    type = type.BaseType;
                }
            }
        }

        // === Constructors

        protected StatType(string id, string displayName, string inspectorCategory = "", Param param = null)
            : base(id, inspectorCategory) {

            (Color? color, string iconColor, string tooltip, string description, bool tweakable, string abbreviation) = param ?? new Param();

            DisplayName = new LocString {ID = displayName};
            Description = new LocString {ID = description};

            Tooltip = string.IsNullOrWhiteSpace(tooltip) ? displayName : tooltip;

            Abbreviation = string.IsNullOrWhiteSpace(abbreviation) ? id : abbreviation;

            IconTagNoTooltip = $"{{tooltip:;technical:{description};sprite:{Abbreviation.Replace("max ", "")};color:{iconColor}}}";

            _iconTag = IconTagNoTooltip;

            Color = color ?? Color.white;
            IsTweakable = tweakable;
        }
        
        [UnityEngine.Scripting.Preserve]
        public string ColoredString(string text) {
            Color color = Color.Lerp(Color, Color.white, 0.4f);
            string colorString = ColorUtility.ToHtmlStringRGBA(color);
            return $"<color=#{colorString}>{text}</color>";
        }
    }
    
    public class Param {
        public Color? Color { get; [UnityEngine.Scripting.Preserve] set; }
        public string IconColor { get; [UnityEngine.Scripting.Preserve] set; }
        public string Tooltip { get; set; }
        public string Description { get; set; }
        public bool Tweakable { get; set; } = true;
        public string Abbreviation { get; set; }
        
        public void Deconstruct(out Color? color, out string iconColor, out string tooltip, out string description, out bool tweakable, out string abbreviation) {
            color = Color;
            iconColor = IconColor;
            tooltip = Tooltip;
            description = Description;
            tweakable = Tweakable;
            abbreviation = Abbreviation;
        }
    }
}