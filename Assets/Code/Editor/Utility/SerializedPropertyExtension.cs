using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility.Collections;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    public static class SerializedPropertyExtension {
        public const BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

        public static T[] ExtractAttributes<T>(this SerializedProperty serializedProperty) where T : Attribute {
            FieldInfo targetFieldInfo = serializedProperty.FieldInfo();
            return (T[])targetFieldInfo.GetCustomAttributes(typeof(T), true);
        }

        public static T ExtractAttribute<T>(this SerializedProperty serializedProperty) where T : Attribute {
            FieldInfo targetFieldInfo = serializedProperty.FieldInfoArrayAware();
            return (T)targetFieldInfo?.GetCustomAttribute(typeof(T), true);
        }

        public static FieldInfo FieldInfo(this SerializedProperty serializedProperty) {
            Type targetType = GetPropertyType(serializedProperty, 1);
            return targetType.GetFields(BindingFlags).FirstOrDefault(fi => fi.Name.Equals(serializedProperty.name));
        }

        public static FieldInfo FieldInfoArrayAware(this SerializedProperty serializedProperty) {
            Type targetType = GetPropertyType(serializedProperty, serializedProperty.name.Equals("data") ? 3 : 1);
            string propertyName = serializedProperty.name.Equals("data") ? serializedProperty.propertyPath.Split('.').SkipLastN(2).Last() : serializedProperty.name;
            return targetType.GetField(propertyName, BindingFlags);
        }

        public static float Height(this SerializedProperty serializedProperty) {
            var children = serializedProperty.GetChildren();
            return children.Sum(child => EditorGUI.GetPropertyHeight(child, true));
        }

        public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty property) {
            property = property.Copy();
            var nextElement = property.Copy();
            bool hasNextElement = nextElement.NextVisible(false);
            if (!hasNextElement) {
                nextElement = null;
            }

            property.NextVisible(true);
            while (true) {
                if ((SerializedProperty.EqualContents(property, nextElement))) {
                    yield break;
                }

                yield return property;

                bool hasNext = property.NextVisible(false);
                if (!hasNext) {
                    break;
                }
            }
        }
        
        public static object GetParentValue(this SerializedProperty serializedProperty) {
            return serializedProperty.GetPropertyValue(serializedProperty.name.Equals("data") ? 3 : 1);
        }
        
        static readonly Regex DataIndexExtractRegex = new Regex(@"(?<=data\[)\d+(?=\])", RegexOptions.Compiled);
        /// <summary>
        /// Get value of property (or property parent)
        /// </summary>
        /// <param name="serializedProperty">Property which value from we want</param>
        /// <param name="ofUpper">Set to 1 if want parent class instance, set to 2 if parent parent ... If just property value leave as 0</param>
        /// <returns>Real value of property</returns>
        public static object GetPropertyValue(this SerializedProperty serializedProperty, int ofUpper = 0) {
            string[] slices = serializedProperty.propertyPath.Split('.');
            object currentValue = serializedProperty.serializedObject.targetObject;
            Type type = currentValue.GetType();

            for (int i = 0; i < slices.Length - ofUpper; i++) {
                if (slices[i] == "Array")
                {
                    //go to 'data[x]'
                    i++; 
                    // extract x
                    var index = int.Parse( DataIndexExtractRegex.Match(slices[i]).Value );

                    var currentArray = (IEnumerable)currentValue;
                    var enumerator = currentArray.GetEnumerator();
                    enumerator.MoveNext();
                    
                    for (int j = 0; j < index; j++) {
                        enumerator.MoveNext();
                    }

                    currentValue = enumerator.Current;
                    type = currentValue.GetType();
                } else {
                    var fieldInfo = type.GetFieldRecursive(slices[i]);
                    currentValue = fieldInfo.GetValue(currentValue);
                    
                    type = currentValue.GetType();
                }  
            }

            return currentValue;
        }
        
        public static Type GetPropertyType(this SerializedProperty serializedProperty, int parentDepth = 0) {
            object target = serializedProperty.serializedObject.targetObject;
            Type type = target.GetType();
            // --- Implementation from GetParentType, little hacky but it works for now. GetParentType can be found in commit: 805b46b3abfb9df8564a29fa0184883beff9eaff
            if (serializedProperty.depth <= 0 && parentDepth >= 1) {
                return type;
            }

            string[] slices = serializedProperty.propertyPath.Split('.');
            for (int i = 0; i < slices.Length - parentDepth; i++) {
                if (slices[i] == "Array") {
                    i++; //skips "data[x]"
                    if (type.IsArray) {
                        type = type.GetElementType(); //gets info on array elements
                    } else {
                        type = type.GetGenericArguments()[0];
                    }

                    if (target is IList collection) {
                        var match = DataIndexExtractRegex.Match(slices[i]);
                        if (match.Success) {
                            var indexStr = match.Value;
                            if (int.TryParse(indexStr, out var index) && index >= 0 && index < collection.Count) {
                                target = collection[index];
                                if (target != null) {
                                    type = target.GetType();
                                }
                            }
                        }
                    } else {
                        target = null;
                    }
                } else {
                    var fieldInfo = type.GetFieldRecursive(slices[i]); 
                    type = fieldInfo.FieldType;
                    if (target != null) {
                        target = fieldInfo.GetValue(target);
                        if (target != null) {
                            type = target.GetType();
                        }
                    }
                }
            }

            return type;
        }
        
        // === Draw array
        public static void DrawArray(this SerializedProperty list, Action<SerializedProperty> elementDraw) {
            DrawArrayHeader(list);
            DrawArrayElements(list, elementDraw);
        }
        
        public static void DrawArrayHeader(SerializedProperty list) {
            EditorGUILayout.BeginHorizontal();
            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, list.displayName, true, list.arraySize > 0 ? EditorStyles.foldoutHeader : EditorStyles.label);
            EditorGUILayout.LabelField("Size:", GUILayout.Width(55));
            if (GUILayout.Button("-", GUILayout.Width(20)) && list.arraySize > 0) {
                list.arraySize--;
            }
            SerializedProperty size = list.FindPropertyRelative("Array.size");
            EditorGUILayout.PropertyField(size, GUIContent.none, GUILayout.Width(50));
            if (GUILayout.Button("+", GUILayout.Width(20))) {
                list.arraySize++;
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawArrayElements(SerializedProperty list, Action<SerializedProperty> elementDraw) {
            if (list.isExpanded) {
                EditorGUI.indentLevel += 1;
                for (int i = 0; i < list.arraySize; i++) {
                    var element = list.GetArrayElementAtIndex(i);
                    DrawLine();
                    elementDraw(element);
                }
                EditorGUI.indentLevel -= 1;
            }
        }
        
        static void DrawLine() {
            EditorGUILayout.LabelField("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(GUILayoutUtility.GetLastRect(), Color.black);
            EditorGUILayout.Space( );
        }

        //=== Code from: https://gist.github.com/aholkner/214628a05b15f0bb169660945ac7923b
        #region GitHubExtensions

        /// (Extension) Get the value of the serialized property.
        public static object GetValue(this SerializedProperty property) {
            string propertyPath = property.propertyPath;
            object value = property.serializedObject.targetObject;
            int i = 0;
            while (NextPathComponent(propertyPath, ref i, out var token))
                value = GetPathComponentValue(value, token);
            return value;
        }

        /// (Extension) Set the value of the serialized property.
        public static void SetValue(this SerializedProperty property, object value) {
            Undo.RecordObject(property.serializedObject.targetObject, $"Set {property.name}");
            SetValueNoRecord(property, value);

            EditorUtility.SetDirty(property.serializedObject.targetObject);
            property.serializedObject.ApplyModifiedProperties();
        }

        /// (Extension) Set the value of the serialized property, but do not record the change.
        /// The change will not be persisted unless you call SetDirty and ApplyModifiedProperties.
        public static void SetValueNoRecord(this SerializedProperty property, object value) {
            string propertyPath = property.propertyPath;
            object container = property.serializedObject.targetObject;

            int i = 0;
            NextPathComponent(propertyPath, ref i, out var deferredToken);
            while (NextPathComponent(propertyPath, ref i, out var token)) {
                container = GetPathComponentValue(container, deferredToken);
                deferredToken = token;
            }

            Debug.Assert(!container.GetType().IsValueType,
                $"Cannot use SerializedObject.SetValue on a struct object, as the result will be set on a temporary.  Either change {container.GetType().Name} to a class, or use SetValue with a parent member.");
            SetPathComponentValue(container, deferredToken, value);
        }

        // Union type representing either a property name or array element index.  The element
        // index is valid only if propertyName is null.
        struct PropertyPathComponent {
            public string propertyName;
            public int elementIndex;
        }

        static Regex arrayElementRegex = new Regex(@"\GArray\.data\[(\d+)\]", RegexOptions.Compiled);

        // Parse the next path component from a SerializedProperty.propertyPath.  For simple field/property access,
        // this is just tokenizing on '.' and returning each field/property name.  Array/list access is via
        // the pseudo-property "Array.data[N]", so this method parses that and returns just the array/list index N.
        //
        // Call this method repeatedly to access all path components.  For example:
        //
        //      string propertyPath = "quests.Array.data[0].goal";
        //      int i = 0;
        //      NextPropertyPathToken(propertyPath, ref i, out var component);
        //          => component = { propertyName = "quests" };
        //      NextPropertyPathToken(propertyPath, ref i, out var component) 
        //          => component = { elementIndex = 0 };
        //      NextPropertyPathToken(propertyPath, ref i, out var component) 
        //          => component = { propertyName = "goal" };
        //      NextPropertyPathToken(propertyPath, ref i, out var component) 
        //          => returns false
        static bool NextPathComponent(string propertyPath, ref int index, out PropertyPathComponent component) {
            component = new PropertyPathComponent();

            if (index >= propertyPath.Length)
                return false;

            var arrayElementMatch = arrayElementRegex.Match(propertyPath, index);
            if (arrayElementMatch.Success) {
                index += arrayElementMatch.Length + 1; // Skip past next '.'
                component.elementIndex = int.Parse(arrayElementMatch.Groups[1].Value);
                return true;
            }

            int dot = propertyPath.IndexOf('.', index);
            if (dot == -1) {
                component.propertyName = propertyPath.Substring(index);
                index = propertyPath.Length;
            } else {
                component.propertyName = propertyPath.Substring(index, dot - index);
                index = dot + 1; // Skip past next '.'
            }

            return true;
        }

        static object GetPathComponentValue(object container, PropertyPathComponent component) {
            if (component.propertyName == null)
                return ((IList) container)[component.elementIndex];
            else
                return GetMemberValue(container, component.propertyName);
        }

        static void SetPathComponentValue(object container, PropertyPathComponent component, object value) {
            if (component.propertyName == null)
                ((IList) container)[component.elementIndex] = value;
            else
                SetMemberValue(container, component.propertyName, value);
        }

        static object GetMemberValue(object container, string name) {
            if (container == null)
                return null;
            var type = container.GetType();
            var members = type.GetMember(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < members.Length; ++i) {
                if (members[i] is FieldInfo field)
                    return field.GetValue(container);
                else if (members[i] is PropertyInfo property)
                    return property.GetValue(container);
            }

            return null;
        }

        static void SetMemberValue(object container, string name, object value) {
            var type = container.GetType();
            var members = type.GetMember(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < members.Length; ++i) {
                if (members[i] is FieldInfo field) {
                    field.SetValue(container, value);
                    return;
                } else if (members[i] is PropertyInfo property) {
                    property.SetValue(container, value);
                    return;
                }
            }

            Debug.Assert(false, $"Failed to set member {container}.{name} via reflection");
        }

        #endregion
    }
}
