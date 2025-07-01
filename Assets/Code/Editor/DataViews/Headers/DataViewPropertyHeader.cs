using System;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Types;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Headers {
    public sealed class DataViewPropertyHeader : DataViewHeader {
        static readonly OnDemandCache<Object, SerializedObject> SerializedObjectCache = new(static o => new(o));
        
        readonly Func<Object, Object> _objectGetter;
        readonly string _propertyPath;
        
        public DataViewPropertyHeader(Type componentType, string name, float width, DataViewType type) : base(
            $"{componentType.Name}/{name}", 
            $"{StringUtil.NicifyName(componentType.Name)}/{StringUtil.NicifyName(name)}", 
            width, type
        ) {
            _objectGetter = ExtractComponent(componentType);
            _propertyPath = name;
        }

        public DataViewPropertyHeader(Type componentType, string name, string niceName, float width, DataViewType type) : base(
            $"{componentType.Name}/{name}", 
            $"{StringUtil.NicifyName(componentType.Name)}/{niceName}", 
            width, type
        ) {
            _objectGetter = ExtractComponent(componentType);
            _propertyPath = name;
        }

        public override UniversalPtr CreateMetadata(IDataViewSource o) {
            var component = _objectGetter(o.UnityObject);
            if (component == null) {
                return default;
            }
            var serializedObject = SerializedObjectCache[_objectGetter(o.UnityObject)];
            return UniversalPtr.CreateManaged(serializedObject.FindProperty(_propertyPath));
        }
        
        public override void FreeMetadata(ref UniversalPtr ptr) {
            ptr.FreeManaged();
        }

        public override DataViewValue GetValue(in DataViewCell cell) {
            var serializedProperty = cell.headerMetadata.GetManaged<SerializedProperty>();
            if (serializedProperty == null) {
                return default;
            }
            return Type.GetValue(serializedProperty);
        }

        public override void SetValue(in DataViewCell cell, in DataViewValue value) {
            var serializedProperty = cell.headerMetadata.GetManaged<SerializedProperty>();
            if (serializedProperty == null) {
                return;
            }
            Type.SetValue(serializedProperty, value);
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        public override Object GetModifiedObject(in DataViewCell cell) {
            return _objectGetter(cell.source.UnityObject);
        }

        public static void ClearCache() {
            SerializedObjectCache.Clear();
        }
    }
}