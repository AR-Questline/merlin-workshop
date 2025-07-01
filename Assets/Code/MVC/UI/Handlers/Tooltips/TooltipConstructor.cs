using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility;
using Awaken.Utility.Enums;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Handlers.Tooltips {
    public class TooltipConstructor {
        static Dictionary<string, TooltipConstructor> s_convertedTexts = new Dictionary<string, TooltipConstructor>();
        
        // === Queries 
        public List<TooltipValue> ElementsToSpawn { get; } = new List<TooltipValue>();
        public bool WithDelay { get; set; }
        public bool PlayHoverSound { get; set; } = true;
        public bool AttachToParent { get; set; }
        public StaticPositioning StaticPositioning { get; set; }
        public TooltipConstructor SubTooltip { get; set; }
        public bool CompareSubTooltip { get; set; } = true;

        public IEnumerable<object> Objects => ElementsToSpawn.Select(e => e.Value);
        
        // === Copy Constructor
        public TooltipConstructor() {}

        public TooltipConstructor(TooltipConstructor other) {
            ElementsToSpawn = new List<TooltipValue>(other.ElementsToSpawn.Count);
            foreach (var element in other.ElementsToSpawn) {
                ElementsToSpawn.Add(new TooltipValue(element.Value, element.Element));
            }

            WithDelay = other.WithDelay;
            PlayHoverSound = other.PlayHoverSound;
            AttachToParent = other.AttachToParent;
            CompareSubTooltip = other.CompareSubTooltip;
            if (other.StaticPositioning != null) {
                StaticPositioning = new StaticPositioning(other.StaticPositioning);
            }
            if (other.SubTooltip != null) {
                SubTooltip = new TooltipConstructor(other.SubTooltip);
            }
        }

        // === Methods
        [UnityEngine.Scripting.Preserve]
        public void Clear() {
            ElementsToSpawn.Clear();
        }

        TooltipConstructor WithElementValue(TooltipElement element, object value) {
            if (value == null || (value is string nullCheckString && string.IsNullOrWhiteSpace(nullCheckString))) {
                return this;
            }
            if (value is string stringValue) {
                value = stringValue.FormatSprite();
            }

            var tooltipValue = new TooltipValue(value, element);
            ElementsToSpawn.Add(tooltipValue);
            return this;
        }

        public TooltipConstructor WithTitle(string title) {
            return WithElementValue(TooltipElement.Title, title?.Trim());
        }

        public TooltipConstructor WithMainText(string text) {
            return WithElementValue(TooltipElement.MainText, text?.Trim());
        }

        public TooltipConstructor WithText(string text) {
            return WithElementValue(TooltipElement.Text, text?.Trim());
        }

        [UnityEngine.Scripting.Preserve]
        public TooltipConstructor WithCard(TemplateReference reference) {
            return WithElementValue(TooltipElement.Card, reference);
        }

        [UnityEngine.Scripting.Preserve]
        public TooltipConstructor WithHorizontalCard(TemplateReference reference) {
            return WithElementValue(TooltipElement.HorizontalCard, reference);
        }

        [UnityEngine.Scripting.Preserve]
        public TooltipConstructor WithoutSubTooltipComparison() {
            CompareSubTooltip = false;
            return this;
        }

        public TooltipConstructor WithStaticPositioningOf(RectTransform rectTransform, float positionChangeAllowedSqr = 0, float pivotChangeAllowedSqr = 0, bool allowOffset = true, Camera camera = null) {
            if (rectTransform != null) {
                StaticPositioning = new StaticPositioning {
                    position = camera == null ? rectTransform.position : camera.WorldToScreenPoint(rectTransform.position),
                    pivot = rectTransform.pivot,
                    allowOffset = allowOffset,
                    scale = rectTransform.localScale.x,
                    positionChangeAllowedSqr = positionChangeAllowedSqr,
                    pivotChangeAllowedSqr = pivotChangeAllowedSqr,
                };
            }
            return this;
        }

        // === Operators
        public static implicit operator TooltipConstructor(string text) {
            if (string.IsNullOrWhiteSpace(text)) {
                return null;
            }

            if (!s_convertedTexts.TryGetValue(text, out var constructor)) {
                constructor = new TooltipConstructor().WithMainText(text);
                s_convertedTexts[text] = constructor;
            } else {
                constructor.StaticPositioning = null;
            }

            return constructor;
        }
        
        public static implicit operator TooltipConstructor(LocString text) {
            return (string) text;
        }
        
        // === Equality members
        bool Equals(TooltipConstructor other) {
            return Objects.SequenceEqual(other.Objects) && Equals(StaticPositioning, other.StaticPositioning) && EqualsSubTooltips(other);
        }

        bool EqualsSubTooltips(TooltipConstructor other) {
            if (!CompareSubTooltip) {
                return true;
            }
            bool subTooltipsEqual = true;
            if (SubTooltip == null && other.SubTooltip == null) {
                subTooltipsEqual = true;
            } else if (SubTooltip != null && other.SubTooltip == null) {
                subTooltipsEqual = false;
            } else if (SubTooltip == null && other.SubTooltip != null) {
                subTooltipsEqual = false;
            } else if (SubTooltip != null && other.SubTooltip != null) {
                subTooltipsEqual = SubTooltip.Equals(other.SubTooltip);
            }
            return subTooltipsEqual;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TooltipConstructor) obj);
        }

        public override int GetHashCode() => Objects?.GetHashCode() ?? 0;

        public static bool operator ==(TooltipConstructor a, TooltipConstructor b) => Equals(a, b);
        public static bool operator !=(TooltipConstructor a, TooltipConstructor b) => !Equals(a, b);
    }

    public class StaticPositioning {
        public Vector2 position;
        public Vector2 pivot;
        public float scale = 1f;
        public bool allowOffset = true;

        public float positionChangeAllowedSqr = 0;
        public float pivotChangeAllowedSqr = 0;

        public StaticPositioning() {}
        
        public StaticPositioning(StaticPositioning other) {
            position = other.position;
            pivot = other.pivot;
            scale = other.scale;
            allowOffset = other.allowOffset;
            positionChangeAllowedSqr = other.positionChangeAllowedSqr;
            pivotChangeAllowedSqr = other.pivotChangeAllowedSqr;
        }

        protected bool Equals(StaticPositioning other) {
            bool positionsNotDiffers =  (position - other.position).sqrMagnitude <= positionChangeAllowedSqr;
            bool pivotsNotDiffers = (pivot - other.pivot).sqrMagnitude <= pivotChangeAllowedSqr;
            bool scaleNotDiffers = scale.Equals(other.scale);
            return positionsNotDiffers && pivotsNotDiffers && scaleNotDiffers && allowOffset == other.allowOffset;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StaticPositioning) obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = position.GetHashCode();
                hashCode = (hashCode * 397) ^ pivot.GetHashCode();
                hashCode = (hashCode * 397) ^ scale.GetHashCode();
                hashCode = (hashCode * 397) ^ allowOffset.GetHashCode();
                return hashCode;
            }
        }
    }
    
    public class TooltipValue {
        public object Value { get; }
        public TooltipElement Element { get; }

        public TooltipValue(object value, TooltipElement element) {
            Value = value;
            Element = element;
        }
    }
    
    public class TooltipElement : RichEnum {
        public string PrefabName => $"UI/Tooltip/{EnumName}Tooltip";
        public int Order { get; }
        public bool AutoBackground { get; }
        
        [UnityEngine.Scripting.Preserve]
        public static readonly TooltipElement Title = new TooltipElement(nameof(Title), TitleOrder, true),
            MainText = new TooltipElement(nameof(MainText), MainTextOrder, true),
            Text = new TooltipElement(nameof(Text), TextOrder, false),
            Icon = new TooltipElement(nameof(Icon), IconOrder, true),
            Card = new TooltipElement(nameof(Card), CardOrder, false),
            HorizontalCard = new TooltipElement(nameof(HorizontalCard), HorizontalCardOrder, false);

        TooltipElement(string enumName, int order, bool autoBackground) : base(enumName) {
            Order = order;
            AutoBackground = autoBackground;
        }

        const int IconOrder = 0;
        const int TitleOrder = 10;
        const int MainTextOrder = 20;
        const int TextOrder = 30;
        const int CardOrder = 40;
        const int HorizontalCardOrder = 999;
    }
}