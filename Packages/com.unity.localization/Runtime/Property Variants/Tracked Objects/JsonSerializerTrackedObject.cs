#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Settings;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.PropertyVariants.TrackedObjects
{
    /// <summary>
    /// Uses JSON to apply changes to a tracked object.
    /// JSON can only be used with MonoBehaviour and ScriptableObject types.
    /// </summary>
    [Serializable]
    public abstract class JsonSerializerTrackedObject : TrackedObject
    {
        /// <summary>
        /// Determines the type of property update that will be performed.
        /// </summary>
        public enum ApplyChangesMethod
        {
            /// <summary>
            /// Partial update will generate a partial patch and apply the changes only for the tracked properties.
            /// Partial update provides improved performance however is not supported when modifying collections or properties that contain a serialized version such as Rect.
            /// </summary>
            Partial,

            /// <summary>
            /// Full update will read the entire object into JSON and then patch the properties before reapplying the new JSON.
            /// </summary>
            Full
        }

        [Tooltip("Determines the type of property update that will be performed." +
            "- Full update reads the entire object into JSON, patches the properties, then reapplies the new JSON.\n" +
            "- Partial update generates a partial patch and applies the changes for the tracked properties only.\n" +
            "Partial update provides better performance however is not supported when modifying collections or properties that contain a serialized version such as Rect.\n" +
            "This value is automatically set based on the properties tracked.")]
        [SerializeField]
        ApplyChangesMethod m_UpdateType = ApplyChangesMethod.Partial;

        /// <summary>
        /// Determines the type of property update that will be performed.
        /// </summary>
        public ApplyChangesMethod UpdateType
        {
            get => m_UpdateType;
            set => m_UpdateType = value;
        }

        public override void AddTrackedProperty(ITrackedProperty trackedProperty)
        {
            base.AddTrackedProperty(trackedProperty);

            // We can not partially patch array items as we need to know the array size or it will be resized.
            if (trackedProperty.PropertyPath.Contains(".Array.data[") || trackedProperty.PropertyPath.EndsWith(".Array.size"))
                UpdateType = ApplyChangesMethod.Full;
        }

        // Reusable class for when we need to wait for an async string operation to complete before we can apply it to a json value.
        class DeferredJsonStringOperation
        {
            public JValue jsonValue;
            public readonly Action<AsyncOperationHandle<string>> callback;

            public static readonly ObjectPool<DeferredJsonStringOperation> Pool = new ObjectPool<DeferredJsonStringOperation>(
                () => new DeferredJsonStringOperation(), collectionCheck: false);

            public DeferredJsonStringOperation()
            {
                callback = OnStringLoaded;
            }

            void OnStringLoaded(AsyncOperationHandle<string> asyncOperationHandle)
            {
                jsonValue.Value = asyncOperationHandle.Result;

                // Clear
                jsonValue = null;
                Pool.Release(this);
            }
        }

        // Reusable class for when we need to wait for an async object operation to complete before we can apply it to a json value.
        class DeferredJsonObjectOperation
        {
            public JValue jsonValue;
            public readonly Action<AsyncOperationHandle<Object>> callback;

            public static readonly ObjectPool<DeferredJsonObjectOperation> Pool = new ObjectPool<DeferredJsonObjectOperation>(
                () => new DeferredJsonObjectOperation(), collectionCheck: false);

            public DeferredJsonObjectOperation()
            {
                callback = OnAssetLoaded;
            }

            void OnAssetLoaded(AsyncOperationHandle<Object> asyncOperationHandle)
            {
                jsonValue.Value = asyncOperationHandle.Result != null ? asyncOperationHandle.Result.GetInstanceID() : 0;

                // Clear
                jsonValue = null;
                Pool.Release(this);
            }
        }

        public override AsyncOperationHandle ApplyLocale(Locale variantLocale, Locale defaultLocale)
        {
            if (Target == null)
                return default;

            // We need to capture a snapshot before, then patch in the changes and reapply.
            // We could use partial json that would not require the initial snapshot however this
            // has issues when dealing with partial lists and when we are patching to a field/type
            // that uses `serializedVersion` such as Rect.
            JObject jsonObject;
            if (UpdateType == ApplyChangesMethod.Full)
            {
                var jsonBefore = JsonUtility.ToJson(Target);
                jsonObject = JObject.Parse(jsonBefore);
            }
            else
            {
                jsonObject = new JObject();
            }

            var asyncOperations = ListPool<AsyncOperationHandle>.Get();
            var arraySizes = ListPool<ArraySizeTrackedProperty>.Get();
            var propertyChanged = false;
            var defaultLocaleIdentifier = defaultLocale != null ? defaultLocale.Identifier : default;

            // In the Editor the instanceID field is used however in the player a different
            // serialization path is taken and instanceID ends up getting mapped from m_FileID.
            // https://unity.slack.com/archives/C9SQHJGN6/p1623329879079600
            // This is now fixed in 2022.2.0a1 - 1342327
            #if UNITY_EDITOR || UNITY_2022_2_OR_NEWER
            const string instanceIdField = ".instanceID";
            #else
            const string instanceIdField = ".m_FileID";
            #endif

            foreach (var property in TrackedProperties)
            {
                if (property == null)
                    continue;

                #if UNITY_EDITOR
                if (!Application.isPlaying)
                    VariantsPropertyDriver.RegisterProperty(Target, property.PropertyPath);
                #endif

                switch (property)
                {
                    // Array size property?
                    case ArraySizeTrackedProperty arraySizeProp:
                    {
                        // We defer array size changes until the end so that array items that cause a resize can be handled(e.g remove them).
                        arraySizes.Add(arraySizeProp);
                        propertyChanged = true;
                        break;
                    }
                    case IStringProperty stringProperty:
                    {
                        var value = stringProperty.GetValueAsString(variantLocale.Identifier, defaultLocaleIdentifier);
                        if (value != null)
                        {
                            var valueContainer = (JValue)GetPropertyFromPath(property.PropertyPath, jsonObject);
                            valueContainer.Value = variantLocale is PseudoLocale pseudoLocale ? pseudoLocale.GetPseudoString(value) : value;
                            propertyChanged = true;
                        }

                        break;
                    }
                    case ITrackedPropertyValue<Object> objectProperty:
                    {
                        objectProperty.GetValue(variantLocale.Identifier, defaultLocaleIdentifier, out var value);
                        var jsonProperty = (JValue)GetPropertyFromPath(property.PropertyPath + instanceIdField, jsonObject);
                        jsonProperty.Value = value != null ? value.GetInstanceID() : 0;
                        propertyChanged = true;
                        break;
                    }
                    case LocalizedStringProperty localizedStringProperty:
                    {
                        // Ignore emptys
                        if (localizedStringProperty.LocalizedString.IsEmpty)
                            break;

                        localizedStringProperty.LocalizedString.LocaleOverride = variantLocale;
                        var stringOp = localizedStringProperty.LocalizedString.GetLocalizedStringAsync();
                        var jsonProperty = (JValue)GetPropertyFromPath(property.PropertyPath, jsonObject);
                        if (stringOp.IsDone)
                        {
                            jsonProperty.Value = stringOp.Result;
                            AddressablesInterface.Release(stringOp);
                        }
                        #if !UNITY_WEBGL // WebGL does not support WaitForCompletion
                        else if (localizedStringProperty.LocalizedString.ForceSynchronous)
                        {
                            jsonProperty.Value = stringOp.WaitForCompletion();
                            AddressablesInterface.Release(stringOp);
                        }
                        #endif
                        else
                        {
                            var asyncHandler = DeferredJsonStringOperation.Pool.Get();
                            asyncHandler.jsonValue = jsonProperty;
                            stringOp.Completed += asyncHandler.callback;
                            asyncOperations.Add(stringOp);
                        }
                        propertyChanged = true;
                        break;
                    }
                    case LocalizedAssetProperty localizedAssetProperty:
                    {
                        // Ignore emptys
                        if (localizedAssetProperty.LocalizedObject.IsEmpty)
                            break;

                        localizedAssetProperty.LocalizedObject.LocaleOverride = variantLocale;
                        var assetOp = localizedAssetProperty.LocalizedObject.LoadAssetAsObjectAsync();

                        var jsonProperty = (JValue)GetPropertyFromPath(property.PropertyPath + instanceIdField, jsonObject);
                        if (assetOp.IsDone)
                        {
                            var result = assetOp.Result;
                            jsonProperty.Value = result != null ? result.GetInstanceID() : 0;
                            AddressablesInterface.Release(assetOp);
                            
                        }
                        #if !UNITY_WEBGL // WebGL does not support WaitForCompletion
                        else if (localizedAssetProperty.LocalizedObject.ForceSynchronous)
                        {
                            var result = assetOp.WaitForCompletion();
                            jsonProperty.Value = result != null ? result.GetInstanceID() : 0;
                            AddressablesInterface.Release(assetOp);
                        }
                        #endif
                        else
                        {
                            var asyncHandler = DeferredJsonObjectOperation.Pool.Get();
                            asyncHandler.jsonValue = jsonProperty;
                            assetOp.Completed += asyncHandler.callback;
                            asyncOperations.Add(assetOp);
                        }

                        propertyChanged = true;
                        break;
                    }
                }
            }

            if (asyncOperations.Count > 0)
            {
                var operation = AddressablesInterface.CreateGroupOperation(asyncOperations);
                operation.Completed += res =>
                {
                    ApplyArraySizes(arraySizes, jsonObject, variantLocale.Identifier, defaultLocaleIdentifier);
                    ApplyJson(jsonObject);

                    foreach (var op in res.Result)
                        AddressablesInterface.Release(op);

                    ListPool<AsyncOperationHandle>.Release(asyncOperations);
                    ListPool<ArraySizeTrackedProperty>.Release(arraySizes);
                    AddressablesInterface.Release(res);
                };
                return operation;
            }

            if (propertyChanged)
            {
                ApplyArraySizes(arraySizes, jsonObject, variantLocale.Identifier, defaultLocaleIdentifier);
                ApplyJson(jsonObject);
            }

            ListPool<AsyncOperationHandle>.Release(asyncOperations);
            ListPool<ArraySizeTrackedProperty>.Release(arraySizes);

            return default;
        }

        void ApplyArraySizes(IEnumerable<ArraySizeTrackedProperty> arraySizes, JObject jsonObject, LocaleIdentifier variantLocale, LocaleIdentifier defaultLocale)
        {
            // If we are modifying items in the array then we always store a default value, we assume it will always exist. If the item does not exist,
            // such as when the array was resized then we need to first apply the default value and then change the array size which may result in the item being removed.
            foreach (var property in arraySizes)
            {
                var jsonContainer = (JArray)GetPropertyFromPath(property.PropertyPath, jsonObject);
                if (!property.GetValue(variantLocale, defaultLocale, out var newSize)) continue;

                if (jsonContainer.Count > newSize)
                {
                    while (jsonContainer.Count > newSize)
                    {
                        jsonContainer.RemoveAt(jsonContainer.Count - 1);
                    }
                }
                else if (jsonContainer.Count < newSize)
                {
                    while (jsonContainer.Count < newSize)
                    {
                        jsonContainer.Add(new JObject());
                    }
                }
            }
        }

        void ApplyJson(JObject jsonObject)
        {
            #if LOCALIZATION_DEBUG_JSON
            var json = jsonObject.ToString(Formatting.Indented);
            Debug.Log(json);
            #else
            var json = jsonObject.ToString();
            #endif

            JsonUtility.FromJsonOverwrite(json, Target);

            PostApplyTrackedProperties();
        }

        internal struct ArrayResult
        {
            public string path;
            public int arrayStartIndex;
            public int arrayDataIndexStart;
            public int arrayDataIndexEnd;

            // Is this an array size path?
            public bool IsArraySize => arrayStartIndex != -1 && arrayDataIndexStart == -1;

            // Is this an array element path? That is an item that ends in data[x], if not then it could be an item inside of an array element.
            public bool IsArrayElement => path?.Length == arrayDataIndexEnd + 1;

            public int GetDataIndex()
            {
                if (arrayDataIndexStart == -1)
                    return -1;

                var indexString = path.Substring(arrayDataIndexStart, arrayDataIndexEnd - arrayDataIndexStart);
                if (uint.TryParse(indexString, out var index))
                    return (int)index;

                Debug.LogError($"Failed to parse Array index `{indexString}` from property path `{path}`");
                return -1;
            }

            public ArrayResult(string p, int start, int bracketStart, int bracketEnd)
            {
                path = p;
                arrayStartIndex = start;
                arrayDataIndexStart = bracketStart;
                arrayDataIndexEnd = bracketEnd;
            }
        }

        internal static ArrayResult GetNextArrayItem(string path, int startIndex)
        {
            const string arrayPath = ".Array.";
            const string arrayDataPath = "data[";
            const string arraySizePath = "size";

            if (path.Length < startIndex + arrayPath.Length)
                return new ArrayResult(null, -1, -1, -1);

            var arrayStartIndex = path.IndexOf(arrayPath, startIndex, StringComparison.Ordinal);
            if (arrayStartIndex != -1)
            {
                if (path.Length > arrayStartIndex + arrayPath.Length + arrayDataPath.Length)
                {
                    // Extract data index
                    var dataStartIndex = path.IndexOf(arrayDataPath, arrayStartIndex + arrayPath.Length, StringComparison.Ordinal);
                    if (dataStartIndex != -1)
                    {
                        dataStartIndex += arrayDataPath.Length; // Go to the end

                        // Extract the number between [ and ].
                        var arrayBracketEndIdx = path.IndexOf(']', dataStartIndex);
                        if (arrayBracketEndIdx != -1)
                        {
                            return new ArrayResult(path, arrayStartIndex + 1, dataStartIndex, arrayBracketEndIdx); // +1 so we start at Array and not the '.'
                        }
                    }
                }

                // Is it an array size?
                if (path.Length == arrayStartIndex + arraySizePath.Length + arrayPath.Length && path.EndsWith(arraySizePath))
                    return new ArrayResult(path, arrayStartIndex + 1, -1, -1); // +1 so we start at Array and not the '.'
            }

            return new ArrayResult(null, -1, -1, -1);
        }

        internal static JToken GetPropertyFromPath(string path, JContainer obj)
        {
            var nextTokenStart = 0;
            var nextArrayElement = GetNextArrayItem(path, 0);

            var parent = obj;
            while (nextTokenStart != -1 && nextTokenStart < path.Length)
            {
                // Is this an array element?
                if (nextTokenStart == nextArrayElement.arrayStartIndex)
                {
                    if (!(parent is JArray arrayElement))
                    {
                        arrayElement = new JArray();
                        parent.Add(arrayElement);
                    }

                    if (nextArrayElement.IsArraySize)
                        return arrayElement;

                    var dataIndex = nextArrayElement.GetDataIndex();
                    if (dataIndex == -1)
                        return null;

                    // If the array is too small then add items until it contains at least enough for the new item.
                    while (arrayElement.Count <= dataIndex)
                    {
                        arrayElement.Add(new JObject());
                    }

                    if (nextArrayElement.IsArrayElement)
                    {
                        var arrayItem = arrayElement[dataIndex] as JValue;
                        if (arrayItem == null)
                        {
                            arrayItem = new JValue(string.Empty);
                            arrayElement[dataIndex] = arrayItem;
                        }
                        return arrayItem;
                    }

                    parent = arrayElement[dataIndex] as JObject;
                    if (parent == null)
                    {
                        parent = new JObject();
                        arrayElement[dataIndex] = parent;
                    }

                    nextTokenStart = nextArrayElement.arrayDataIndexEnd + 2; // "]."
                    nextArrayElement = GetNextArrayItem(path, nextTokenStart);
                }
                else
                {
                    var tokenEndIdx = path.IndexOf('.', nextTokenStart);
                    var token = tokenEndIdx == -1 ? path.Substring(nextTokenStart) : path.Substring(nextTokenStart, tokenEndIdx - nextTokenStart);

                    // Is this the last token?
                    if (tokenEndIdx == -1)
                    {
                        var property = (JProperty)parent[token]?.Parent;
                        JValue value;
                        if (property == null)
                        {
                            value = new JValue(string.Empty);
                            property = new JProperty(token, value);
                            parent.Add(property);
                        }
                        else
                        {
                            value = property.Value as JValue;
                            if (value == null)
                            {
                                value = new JValue(string.Empty);
                                property.Value = value;
                            }
                        }
                        return value;
                    }

                    var tokenChild = (JContainer)parent[token];
                    if (tokenChild == null)
                    {
                        tokenChild = new JObject();
                        parent[token] = tokenChild;
                    }
                    parent = tokenChild;

                    nextTokenStart = tokenEndIdx + 1;
                }
            }
            return null;
        }
    }
}

#endif
