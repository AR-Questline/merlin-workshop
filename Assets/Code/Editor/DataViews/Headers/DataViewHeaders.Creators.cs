using System;
using Awaken.TG.Editor.DataViews.Types;
using Awaken.Utility.Enums;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Headers {
    public static partial class DataViewHeaders {
        static DataViewHeader Property<TComponent>(string name, float width) 
            where TComponent : Object 
        {
            var type = GetDataViewTypeOf(typeof(TComponent), name);
            if (type == null) {
                return null;
            }

            return new DataViewPropertyHeader(typeof(TComponent), name, width, type);
        }

        static DataViewHeader Property<TComponent>(string name, string niceName, float width)
            where TComponent : Object 
        {
            var type = GetDataViewTypeOf(typeof(TComponent), name);
            if (type == null) {
                return null;
            }

            return new DataViewPropertyHeader(typeof(TComponent), name, niceName, width, type);
        }

        static DataViewHeader Computable<TComponent>(string name, Func<TComponent, bool> getter, float width)
            where TComponent : Object 
        {
            return DataViewComputableHeader.Create(getter, name, width, DataViewTypeBool.Instance);
        }

        static DataViewHeader Computable<TComponent>(string name, Func<TComponent, bool> getter, Action<TComponent, bool> setter, float width) 
            where TComponent : Object 
        {
            return DataViewComputableHeader.Create(getter, setter, name, width, DataViewTypeBool.Instance);
        }

        static DataViewHeader Computable<TComponent>(string name, Func<TComponent, int> getter, float width)
            where TComponent : Object 
        {
            return DataViewComputableHeader.Create(getter, name, width, DataViewTypeInt.Instance);
        }

        static DataViewHeader Computable<TComponent>(string name, Func<TComponent, int> getter, Action<TComponent, int> setter, float width) 
            where TComponent : Object 
        {
            return DataViewComputableHeader.Create(getter, setter, name, width, DataViewTypeInt.Instance);
        }

        static DataViewHeader Computable<TComponent>(string name, Func<TComponent, float> getter, float width) 
            where TComponent : Object 
        {
            return DataViewComputableHeader.Create(getter, name, width, DataViewTypeFloat.Instance);
        }

        static DataViewHeader Computable<TComponent>(string name, Func<TComponent, float> getter, Action<TComponent, float> setter, float width) 
            where TComponent : Object 
        {
            return DataViewComputableHeader.Create(getter, setter, name, width, DataViewTypeFloat.Instance);
        }

        static DataViewHeader Computable<TComponent>(string name, Func<TComponent, string> getter, float width)
            where TComponent : Object 
        {
            return DataViewComputableHeader.Create(getter, name, width, DataViewTypeString.Instance);
        }

        static DataViewHeader Computable<TComponent>(string name, Func<TComponent, string> getter, Action<TComponent, string> setter, float width) 
            where TComponent : Object 
        {
            return DataViewComputableHeader.Create(getter, setter, name, width, DataViewTypeString.Instance);
        }

        static DataViewHeader ComputableEnum<TComponent, TEnum>(string name, Func<TComponent, TEnum> getter, float width) 
            where TComponent : Object where TEnum : Enum 
        {
            return DataViewComputableHeader.Create(getter, name, width, DataViewTypeEnum<TEnum>.Instance);
        }

        static DataViewHeader ComputableEnum<TComponent, TEnum>(string name, Func<TComponent, TEnum> getter, Action<TComponent, TEnum> setter, float width) 
            where TComponent : Object where TEnum : Enum 
        {
            return DataViewComputableHeader.Create(getter, setter, name, width, DataViewTypeEnum<TEnum>.Instance);
        }

        static DataViewHeader ComputableRichEnum<TComponent, TRichEnum>(string name, Func<TComponent, TRichEnum> getter, float width) 
            where TComponent : Object where TRichEnum : RichEnum 
        {
            return DataViewComputableHeader.Create(getter, name, width, DataViewTypeRichEnum<TRichEnum>.Instance);
        }

        static DataViewHeader ComputableRichEnum<TComponent, TRichEnum>(string name, Func<TComponent, TRichEnum> getter, Action<TComponent, TRichEnum> setter, float width) 
            where TComponent : Object where TRichEnum : RichEnum 
        {
            return DataViewComputableHeader.Create(getter, setter, name, width, DataViewTypeRichEnum<TRichEnum>.Instance);
        }

        static DataViewHeader ComputableObject<TComponent, TObject>(string name, Func<TComponent, TObject> getter, float width) 
            where TComponent : Object where TObject : Object 
        {
            return DataViewComputableHeader.Create(getter, name, width, DataViewTypeObject<TObject>.Instance);
        }

        static DataViewHeader ComputableObject<TComponent, TObject>(string name, Func<TComponent, TObject> getter, Action<TComponent, TObject> setter, float width) 
            where TComponent : Object where TObject : Object 
        {
            return DataViewComputableHeader.Create(getter, setter, name, width, DataViewTypeObject<TObject>.Instance);
        }
        
        static DataViewHeader LootData<TType, TValue>(string propertyName, float width) {
            Type type = typeof(TType);
            DataViewType dataType = GetDataViewTypeOf(typeof(TType), propertyName);
            if (dataType == null) {
                return null;
            }
            return new DataViewLootDataHeader<TValue>(type, propertyName, width, dataType);
        }
    }
}