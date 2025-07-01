using System;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Types;
using Awaken.Utility;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Headers {
    public static class DataViewComputableHeader {
        public static DataViewComputableHeader<TValue> Create<TComponent, TValue>(Func<TComponent, TValue> getter, string name, float width, DataViewType type)
            where TComponent : Object 
        {
            var componentName = typeof(TComponent).Name;    
            var (niceName, nicePath) = DataViewHeader.GetNiceNameAndPath($"{StringUtil.NicifyName(componentName)}/{StringUtil.NicifyName(name)}");
            return new DataViewComputableHeader<TValue> {
                Name = $"{componentName}/{name}",
                NiceName = niceName,
                NicePath = nicePath,
                Width = width,
                Type = type,
                ModifiedObjectGetter = DataViewHeader.ExtractObject<TComponent>,
                Getter = o => getter(DataViewHeader.ExtractObject<TComponent>(o)),
                Setter = null,
                SupportEditing = false,
            }; 
        }
        
        public static DataViewComputableHeader<TValue> Create<TComponent, TValue>(Func<TComponent, TValue> getter, Action<TComponent, TValue> setter, string name, float width, DataViewType type)
            where TComponent : Object 
        {
            var componentName = typeof(TComponent).Name;
            var (niceName, nicePath) = DataViewHeader.GetNiceNameAndPath($"{StringUtil.NicifyName(componentName)}/{StringUtil.NicifyName(name)}");
            return new DataViewComputableHeader<TValue> {
                Name = $"{componentName}/{name}",
                NiceName = niceName,
                NicePath = nicePath,
                Width = width,
                Type = type,
                ModifiedObjectGetter = DataViewHeader.ExtractObject<TComponent>,
                Getter = o => getter(DataViewHeader.ExtractObject<TComponent>(o)),
                Setter = (o, value) => setter(DataViewHeader.ExtractObject<TComponent>(o), value),
                SupportEditing = true,
            }; 
        }
    }
    
    public sealed class DataViewComputableHeader<T> : DataViewHeader {
        public Func<IDataViewSource, Object> ModifiedObjectGetter { get; init; }
        public Func<IDataViewSource, T> Getter { get; init; }
        public Action<IDataViewSource, T> Setter { get; init; }

        public DataViewComputableHeader() {
            SupportEditing = false;
        }

        public override DataViewValue GetValue(in DataViewCell cell) => DataViewValue.Create(Getter(cell.source));
        public override void SetValue(in DataViewCell cell, in DataViewValue value) => Setter(cell.source, value.Get<T>());
        public override Object GetModifiedObject(in DataViewCell cell) => ModifiedObjectGetter(cell.source);
    }
}