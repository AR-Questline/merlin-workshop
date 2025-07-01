using System;
using System.ComponentModel;
using System.Globalization;

namespace Awaken.TG.MVC.Relations
{
    /// <summary>
    /// Converts relation instances to strings and back.
    /// </summary>
    public class RelationConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string str) {
                return Relation.Deserialize(str);
            }
            throw new InvalidOperationException("Can't perform this conversion");
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (value is Relation rel && destinationType == typeof(string)) {
                return rel.Serialize();
            } else {
                throw new InvalidOperationException("Can't perform this conversion");
            }
        }
    }
}
