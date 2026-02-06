// Inspired by: spiney199 https://discussions.unity.com/t/serializable-system-type-get-it-while-its-hot/508053/14
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

#if UNITY_EDITOR && ODIN_INSPECTOR
namespace Awaken.TG.Editor.Utility.SerializableTypeReference {
    [UsedImplicitly]
    public class OdinSerializableTypeDrawerInjection : OdinPropertyProcessor<Awaken.Utility.SerializableTypeReference.SerializableTypeReference> {
        #region Processor Overrides

        public override void ProcessMemberProperties(List<InspectorPropertyInfo> propertyInfos) {
            var property = this.Property;
            var typeDrawerSettings = property.GetAttribute<TypeDrawerSettingsAttribute>();

            var typeviewIndex = propertyInfos.FindIndex(p => p.PropertyName == "_type");
            var propertyInfo = propertyInfos[typeviewIndex];
            var extraAttributes = propertyInfo.Attributes;
            
            if (typeDrawerSettings == null) {
                propertyInfos.RemoveAt(typeviewIndex);
                propertyInfos.AddValue<Type>("_type",
                    getter: GetSerialisedType,
                    setter: SetSerialisedType,
                    extraAttributes.ToArray());
                return;
            }
            
            Attribute[] extraAttributesArray = new Attribute[extraAttributes.Count + 1];
            extraAttributes.CopyTo(extraAttributesArray, 0);
            extraAttributesArray[extraAttributes.Count] = typeDrawerSettings;

            propertyInfos.RemoveAt(typeviewIndex);
            propertyInfos.AddValue<Type>("_type",
                getter: GetSerialisedType,
                setter: SetSerialisedType,
                extraAttributesArray);
        }

        #endregion

        #region Internal Methods

        private Type GetSerialisedType() {
            var sType = this.ValueEntry.SmartValue;
            return sType.Type;
        }

        private void SetSerialisedType(Type type) {
            var sType = this.ValueEntry.SmartValue;
            sType.Type = type;
            this.Property.MarkSerializationRootDirty();
        }

        #endregion
    }
}
#endif