using System.Runtime.InteropServices;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Structure {
    [StructLayout(LayoutKind.Explicit)]
    public struct DataViewValue {
        [FieldOffset(0)] public int intValue;
        [FieldOffset(0)] public float floatValue;
        [FieldOffset(0)] public bool boolValue;
        [FieldOffset(0)] public string stringValue;
        [FieldOffset(0)] public Object objectReferenceValue;
        
        public static implicit operator DataViewValue(int value) => new() { intValue = value };
        public static implicit operator DataViewValue(float value) => new() { floatValue = value };
        public static implicit operator DataViewValue(bool value) => new() { boolValue = value };
        public static implicit operator DataViewValue(string value) => new() { stringValue = value };
        public static implicit operator DataViewValue(Object value) => new() { objectReferenceValue = value };
        
        public static DataViewValue Create<T>(T value) => GenericMethods.Create(value);
        public readonly T Get<T>() => GenericMethods.Get<T>(this);
        public void Set<T>(T value) => GenericMethods.Set(ref this, value);
        
        public static class GenericMethods {
            static GenericMethods() {
                Cache<int>.creator = value => value;
                Cache<float>.creator = value => value;
                Cache<bool>.creator = value => value;
                Cache<string>.creator = value => value;
                Cache<Object>.creator = value => value;
                
                Cache<int>.getter = GetInt;
                Cache<float>.getter = GetFloat;
                Cache<bool>.getter = GetBool;
                Cache<string>.getter = GetString;
                Cache<Object>.getter = GetObject;
                
                Cache<int>.setter = SetInt;
                Cache<float>.setter = SetFloat;
                Cache<bool>.setter = SetBool;
                Cache<string>.setter = SetString;
                Cache<Object>.setter = SetObject;
            }
        
            public static DataViewValue Create<T>(T value) => Cache<T>.creator(value);
            public static T Get<T>(in DataViewValue value) => Cache<T>.getter(value);
            public static void Set<T>(ref DataViewValue dataViewValue, T value) => Cache<T>.setter(ref dataViewValue, value);

            static int GetInt(in DataViewValue value) => value.intValue;
            static float GetFloat(in DataViewValue value) => value.floatValue;
            static bool GetBool(in DataViewValue value) => value.boolValue;
            static string GetString(in DataViewValue value) => value.stringValue;
            static Object GetObject(in DataViewValue value) => value.objectReferenceValue;
            
            static void SetInt(ref DataViewValue dataViewValue, int value) => dataViewValue.intValue = value;
            static void SetFloat(ref DataViewValue dataViewValue, float value) => dataViewValue.floatValue = value;
            static void SetBool(ref DataViewValue dataViewValue, bool value) => dataViewValue.boolValue = value;
            static void SetString(ref DataViewValue dataViewValue, string value) => dataViewValue.stringValue = value;
            static void SetObject(ref DataViewValue dataViewValue, Object value) => dataViewValue.objectReferenceValue = value;
            
            public static class Cache<T> {
                public static Creator creator;
                public static Getter getter;
                public static Setter setter;

                public delegate DataViewValue Creator(T value);
                public delegate T Getter(in DataViewValue value);
                public delegate void Setter(ref DataViewValue dataViewValue, T value);
            }
        }
    }
}