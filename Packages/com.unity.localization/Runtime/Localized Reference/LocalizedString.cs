using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using UnityEngine.Localization.SmartFormat.Core.Formatting;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization
{
    /// <summary>
    /// Provides a way to reference a <see cref="StringTableEntry"/> inside of a specific <see cref="StringTable"/> and request the localized string.
    /// </summary>
    /// <example>
    /// This example shows how to localize a [MonoBehaviour](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html) so that it updates automatically when the active locale changes.
    /// This example uses asynchronous loading to load the localized assets in the background.
    /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="simple-monobehaviour1"/>
    /// </example>
    /// <example>
    /// This example shows how to localize a [MonoBehaviour](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html) immediately.
    /// This example uses synchronous loading, which may cause a pause when first loading the localized assets.
    /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="simple-monobehaviour2"/>
    /// </example>
    /// <example>
    /// This example shows how to localize a [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html) to represent a quest in a game.
    /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="simple-scriptableobject"/>
    /// </example>
    /// <example>
    /// This example shows how to use local variables to represent a health counter.
    /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="health-counter"/>
    /// </example>
    /// <example>
    /// This example shows how to bind to a UI Toolkit property. Note that this requires Unity 2023.2 and above.
    /// <code source="../../DocCodeSamples.Tests/LocalizedStringUIDocumentExample.cs" region="example"/>
    /// </example>
    /// <example>
    /// This example shows how to use local variables when you bind to a UI Toolkit property. Note that this requires Unity 2023.2 and above.
    /// <code source="../../DocCodeSamples.Tests/LocalizedStringVariablesUIDocumentExample.cs" region="example"/>
    /// </example>
    [Serializable]
    public partial class LocalizedString : LocalizedReference, IVariableGroup, IDictionary<string, IVariable>, IVariableValueChanged, IDisposable
    {
        [SerializeField]
        List<VariableNameValuePair> m_LocalVariables = new List<VariableNameValuePair>();

        CallbackArray<ChangeHandler> m_ChangeHandler;
        string m_CurrentStringChangedValue;

        // Kept in sync with m_Variables so that users can make changes via the inspector without issues(duplicate/empty names etc).
        readonly Dictionary<string, VariableNameValuePair> m_VariableLookup = new Dictionary<string, VariableNameValuePair>();

        readonly List<IVariableValueChanged> m_UsedVariables = new List<IVariableValueChanged>();
        Action<IVariable> m_OnVariableChanged;
        Action<Locale> m_SelectedLocaleChanged;
        Action<AsyncOperationHandle<LocalizedStringDatabase.TableEntryResult>> m_AutomaticLoadingCompleted;
        Action<AsyncOperationHandle<LocalizedDatabase<StringTable, StringTableEntry>.TableEntryResult>> m_CompletedSourceValue;
        bool m_WaitingForVariablesEndUpdate;

        /// <inheritdoc/>
        public event Action<IVariable> ValueChanged;

        internal override bool ForceSynchronous => WaitForCompletion || LocalizationSettings.StringDatabase.AsynchronousBehaviour == AsynchronousBehaviour.ForceSynchronous;

        /// <summary>
        /// Arguments that will be passed through to Smart Format. These arguments are not serialized and will need to be set at runtime.
        /// See <seealso cref="Add(string, IVariable)"/> to add persistent serialized arguments.
        /// </summary>
        public IList<object> Arguments { get; set; }

        /// <summary>
        /// The current loading operation for the string when using <see cref="StringChanged"/> or <see langword="default"/> if one is not available.
        /// A string may not be immediately available, such as when loading the <see cref="StringTable"/> asset, so all string operations are wrapped
        /// with an <see cref="AsyncOperationHandle"/>.
        /// See also <seealso cref="RefreshString"/>
        /// </summary>
        public AsyncOperationHandle<LocalizedStringDatabase.TableEntryResult> CurrentLoadingOperationHandle
        {
            get;
            internal set;
        }

        /// <summary>
        /// Delegate used by <see cref="StringChanged"/>.
        /// </summary>
        /// <param name="value">The localized string.</param>
        public delegate void ChangeHandler(string value);

        /// <summary>
        /// Provides a callback that will be invoked when the translated string has changed.
        /// The following events will trigger an update:
        /// <list type="bullet">
        /// <item><description>The first time the action is added to the event.</description></item>
        /// <item><description>The <seealso cref="LocalizationSettings.SelectedLocale"/> changing.</description></item>
        /// <item><description>If the string is currently using a <see cref="IVariable"/> which supports <see cref="IVariableValueChanged"/> and it's value has changed.</description></item>
        /// <item><description>When <see cref="RefreshString"/> is called.</description></item>
        /// <item><description>The <see cref="TableReference"/> or <see cref="TableEntryReference"/> changing.</description></item>
        /// </list>
        /// When the first <see cref="ChangeHandler"/> is added, a loading operation (see <see cref="CurrentLoadingOperationHandle"/>) automatically starts.
        /// When the loading operation is completed, the localized string value is sent to the subscriber.
        /// If you add additional subscribers after loading has completed, they are also sent the latest localized string value.
        /// This ensures that a subscriber will always have the correct localized value regardless of when it was added.
        /// </summary>
        /// <example>
        /// This example shows how the <see cref="StringChanged"/> event can be used to trigger updates to a string.
        /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="localized-string-events"/>
        /// </example>
        public event ChangeHandler StringChanged
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException();

                m_ChangeHandler.Add(value);

                if (m_ChangeHandler.Length == 1)
                {
                    LocalizationSettings.ValidateSettingsExist();
                    ForceUpdate();

                    // We subscribe after the first update as its possible that a SelectedLocaleChanged may be fired
                    // during ForceUpdate when using WaitForCompletion and we want to avoid this.
                    LocalizationSettings.SelectedLocaleChanged += m_SelectedLocaleChanged;
                }
                else if (CurrentLoadingOperationHandle.IsValid() && CurrentLoadingOperationHandle.IsDone)
                {
                    // Call the event with the latest value.
                    value(m_CurrentStringChangedValue);
                }
            }
            remove
            {
                m_ChangeHandler.RemoveByMovingTail(value);
                if (m_ChangeHandler.Length == 0)
                {
                    LocalizationSettings.SelectedLocaleChanged -= m_SelectedLocaleChanged;
                    ClearLoadingOperation();
                }
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if <seealso cref="StringChanged"/> has any subscribers.
        /// </summary>
        public bool HasChangeHandler => m_ChangeHandler.Length != 0;

        /// <summary>
        /// Initializes and returns an empty instance of a <see cref="LocalizedString"/>.
        /// </summary>
        public LocalizedString()
        {
            m_SelectedLocaleChanged = HandleLocaleChange;
            m_OnVariableChanged = OnVariableChanged;
            m_AutomaticLoadingCompleted = AutomaticLoadingCompleted;
            m_CompletedSourceValue = CompletedSourceValue;
        }

        /// <summary>
        /// Initializes and returns an instance of a <see cref="LocalizedString"/>.
        /// </summary>
        /// <param name="tableReference">Reference to the String Table Collection.
        /// This can either be the name of the collection as a <see langword="string"/> or the Collection Guid as a [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/system.guid).</param>
        /// <param name="entryReference">Reference to the String Table Collection entry.
        /// This can either be the name of the Key as a <see langword="string"/> or the <see langword="long"/> Key Id.</param>
        /// <example>
        /// This example shows the different ways to construct a LocalizedString.
        /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="localized-string-constructor"/>
        /// </example>
        /// <example>
        /// This example shows how a LocalizedString could be set up in the Editor.
        /// By using the Guid and Id the table and entry references will not be lost if the table collection name or entry name was to be changed.
        /// Note: The performance benefits to using a Guid and Id are negligible.
        /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="localized-string-constructor-editor"/>
        /// </example>
        public LocalizedString(TableReference tableReference, TableEntryReference entryReference) : this()
        {
            TableReference = tableReference;
            TableEntryReference = entryReference;
        }

        /// <summary>
        /// Provides a way to force a refresh of the string when using <see cref="StringChanged"/>.
        /// </summary>
        /// <remarks>
        /// <para>This will only force the refresh if there is currently no active <see cref="CurrentLoadingOperationHandle"/>, if one is still being executed then it will be ignored and <see langword="false"/> will be returned.
        /// If a string is not static and will change during game play, such as when using format arguments, then this can be used to force the string to update itself.</para>
        /// You may wish to call this if the values <b>inside</b> of the <see cref="Arguments"/> list have changed or you wish to force all <see cref="StringChanged"/> subscribers to update.
        /// </remarks>
        /// <returns>Returns <see langword="true"/> if a new refresh could be requested or <see langword="false"/> if it could not, such as when <see cref="CurrentLoadingOperationHandle"/> is still loading.</returns>
        /// <example>
        /// This example shows how the string can be refreshed, such as when showing dynamic values like the current time.
        /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="localized-string-smart"/>
        /// </example>
        public bool RefreshString()
        {
            if (m_ChangeHandler.Length == 0 || !CurrentLoadingOperationHandle.IsValid())
                return false;

            if (!CurrentLoadingOperationHandle.IsDone)
            {
                #if !UNITY_WEBGL
                if (ForceSynchronous)
                {
                    CurrentLoadingOperationHandle.WaitForCompletion();
                    return true;
                }
                else
                #endif
                return false;
            }

            var entry = CurrentLoadingOperationHandle.Result.Entry;
            var formatCache = entry?.GetOrCreateFormatCache();
            if (formatCache != null)
            {
                formatCache.LocalVariables = this;
                formatCache.VariableTriggers.Clear();
            }

            var translatedText = LocalizationSettings.StringDatabase.GenerateLocalizedString(CurrentLoadingOperationHandle.Result.Table, entry, TableReference, TableEntryReference, LocalizationSettings.SelectedLocale, Arguments);

            if (formatCache != null)
            {
                formatCache.LocalVariables = null;
                UpdateVariableListeners(entry?.FormatCache?.VariableTriggers);
            }

            m_CurrentStringChangedValue = translatedText;
            InvokeChangeHandler(m_CurrentStringChangedValue);
            return true;
        }

        /// <summary>
        /// Provides a translated string from a <see cref="StringTable"/> with the <see cref="TableReference"/> and
        /// the translated string that matches <see cref="TableEntryReference"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="AsyncOperationHandle.Completed"/> event provides a notification once the operation has finished and the string has been found or an error has occurred.
        /// A string table may have already been loaded during a previous operation or when using Preload mode.
        /// Check the <see cref="AsyncOperationHandle.IsDone"/> property to see if the string table has already been loaded and the translated string is immediately available.
        /// See [Async operation handling](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/AddressableAssetsAsyncOperationHandle.html) for further details.
        /// To force the operation to complete immediately, call <see cref="AsyncOperationHandle.WaitForCompletion"/>.
        /// </remarks>
        /// <returns>Returns the loading operation for the request.</returns>
        /// <example>
        /// This example shows how <see cref="GetLocalizedStringAsync"/> can be used to request an updated string when the <see cref="LocalizationSettings.SelectedLocale"/> changes.
        /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="get-localized-string"/>
        /// </example>
        /// <example>
        /// This example shows how <see cref="GetLocalizedStringAsync"/> can be forced to complete immediately using <see cref="AsyncOperationHandle.WaitForCompletion"/>.
        /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="get-localized-string-synchronous"/>
        /// </example>
        public AsyncOperationHandle<string> GetLocalizedStringAsync() => GetLocalizedStringAsync(Arguments);

        /// <summary>
        /// Provides a translated string from a <see cref="StringTable"/> with the <see cref="TableReference"/> and
        /// the translated string that matches <see cref="TableEntryReference"/>.
        /// Uses [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) to force the loading to complete synchronously.
        /// Please note that [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) is not supported on
        /// [WebGL](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/SynchronousAddressables.html#webgl).
        /// </summary>
        /// <returns>The localized string for the <see cref="LocalizationSettings.SelectedLocale"/> or <see cref="LocalizedReference.LocaleOverride"/> if it is not <see langword="null"/>.</returns>
        public string GetLocalizedString() => GetLocalizedStringAsync().WaitForCompletion();

        /// <summary>
        /// Provides a translated string from a <see cref="StringTable"/> with the <see cref="TableReference"/> and
        /// the translated string that matches <see cref="TableEntryReference"/>.
        /// </summary>
        /// <param name="arguments">The arguments to pass into the Smart String formatter or [String.Format](https://docs.microsoft.com/en-us/dotnet/api/system.string.format).</param>
        /// <returns>Returns the loading operation for the request.</returns>
        public AsyncOperationHandle<string> GetLocalizedStringAsync(params object[] arguments) => GetLocalizedStringAsync((IList<object>)arguments);

        /// <summary>
        /// Provides a translated string from a <see cref="StringTable"/> with the <see cref="TableReference"/> and
        /// the translated string that matches <see cref="TableEntryReference"/>.
        /// Uses [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) to force the loading to complete synchronously.
        /// Please note that [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) is not supported on
        /// [WebGL](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/SynchronousAddressables.html#webgl).
        /// </summary>
        /// <param name="arguments">The arguments to pass into the Smart String formatter or [String.Format](https://docs.microsoft.com/en-us/dotnet/api/system.string.format).</param>
        /// <returns>The localized string for the <see cref="LocalizationSettings.SelectedLocale"/> or <see cref="LocalizedReference.LocaleOverride"/> if it is not <see langword="null"/>.</returns>
        public string GetLocalizedString(params object[] arguments) => GetLocalizedStringAsync((IList<object>)arguments).WaitForCompletion();

        /// <summary>
        /// Provides a translated string from a <see cref="StringTable"/> with the <see cref="TableReference"/> and
        /// the translated string that matches <see cref="TableEntryReference"/>.
        /// </summary>
        /// <param name="arguments">The arguments to pass into the Smart String formatter or [String.Format](https://docs.microsoft.com/en-us/dotnet/api/system.string.format).</param>
        /// <returns>The localized string for the <see cref="LocalizationSettings.SelectedLocale"/> or <see cref="LocalizedReference.LocaleOverride"/> if it is not <see langword="null"/>.</returns>
        public string GetLocalizedString(IList<object> arguments) => GetLocalizedStringAsync(arguments).WaitForCompletion();

        /// <summary>
        /// Provides a translated string from a <see cref="StringTable"/> with the <see cref="TableReference"/> and
        /// the translated string that matches <see cref="TableEntryReference"/>.
        /// </summary>
        /// <param name="arguments">The arguments to pass into the Smart String formatter or [String.Format](https://docs.microsoft.com/en-us/dotnet/api/system.string.format).</param>
        /// <returns>Returns the loading operation for the request.</returns>
        public AsyncOperationHandle<string> GetLocalizedStringAsync(IList<object> arguments)
        {
            LocalizationSettings.ValidateSettingsExist();
            return LocalizationSettings.StringDatabase.GetLocalizedStringAsync(TableReference, TableEntryReference, arguments, LocaleOverride, FallbackState, m_LocalVariables.Count > 0 ? this : null);
        }

        /// <summary>
        /// Returns the number of local variables inside this localized string.
        /// </summary>
        public int Count => m_VariableLookup.Count;

        /// <summary>
        /// Returns a collection containing all the unique local variable names.
        /// </summary>
        public ICollection<string> Keys => m_VariableLookup.Keys;

        /// <summary>
        /// Returns all the local variables for this localized string.
        /// </summary>
        public ICollection<IVariable> Values => m_VariableLookup.Values.Select(s => s.variable).ToList();

        /// <summary>
        /// Implemented as part of the IDictionary interface but not actually used. Will always return <see langword="false"/>.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the <see cref="IVariable"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The found variable.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if a variable with the specified name does not exist.</exception>
        /// <example>
        /// This example shows how to get and add a local variable.
        /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="add-get-variable"/>
        /// </example>
        public IVariable this[string name]
        {
            get => m_VariableLookup[name].variable;
            set => Add(name, value);
        }

        /// <summary>
        /// Gets the <see cref="IVariable"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The variable that was found or <see langword="default"/>.</param>
        /// <returns><see langword="true"/> if a variable was found and <see langword="false"/> if one could not.</returns>
        /// <example>
        /// This example shows how to get and add a local variable using TryGetValue.
        /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="try-get-variable"/>
        /// </example>
        public bool TryGetValue(string name, out IVariable value)
        {
            if (m_VariableLookup.TryGetValue(name, out var v))
            {
                value = v.variable;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Adds a new Local Variable to use during formatting.
        /// </summary>
        /// <param name="name">The name of the variable, must be unique. Note the name should not contain any whitespace, if any is found then it will be replaced with with '-'.</param>
        /// <param name="variable">The variable to use when formatting. See also <seealso cref="BoolVariable"/>, <seealso cref="FloatVariable"/>, <seealso cref="IntVariable"/>, <seealso cref="StringVariable"/>, <seealso cref="ObjectVariable"/>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when variable is null.</exception>
        /// <example>
        /// This example shows how to get and add a local variable.
        /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="add-get-variable"/>
        /// </example>
        public void Add(string name, IVariable variable)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name), "Name must not be null or empty.");
            if (variable == null)
                throw new ArgumentNullException(nameof(variable));

            name = name.ReplaceWhiteSpaces("-");
            if (m_VariableLookup.TryGetValue(name, out var value))
            {
                if (ReferenceEquals(value.variable, variable))
                    return;
                m_LocalVariables.Remove(value);
            }

            var v = new VariableNameValuePair { name = name, variable = variable };
            m_VariableLookup[name] = v;
            m_LocalVariables.Add(v);
        }

        /// <summary>
        /// <inheritdoc cref="Add(string, IVariable)"/>
        /// </summary>
        /// <param name="item">The local variable name and value to add.</param>
        public void Add(KeyValuePair<string, IVariable> item) => Add(item.Key, item.Value);

        /// <summary>
        /// Removes a local variable with the specified name.
        /// </summary>
        /// <param name="name">The name of the variable to be removed.</param>
        /// <returns><see langword="true"/> if a variable with the specified name was removed, <see langword="false"/> if one was not.</returns>
        /// <example>
        /// This example shows how to remove a local variable.
        /// <code source="../../DocCodeSamples.Tests/LocalizedStringSamples.cs" region="remove-variable"/>
        /// </example>
        public bool Remove(string name)
        {
            if (m_VariableLookup.TryGetValue(name, out var v))
            {
                m_LocalVariables.Remove(v);
                m_VariableLookup.Remove(name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a local variable with the specified key.
        /// </summary>
        /// <param name="item">The item to be removed, only the Key field will be considered.</param>
        /// <returns><see langword="true"/> if a variable with the specified name was removed, <see langword="false"/> if one was not.</returns>
        public bool Remove(KeyValuePair<string, IVariable> item) => Remove(item.Key);

        /// <summary>
        /// Returns <see langword="true"/> if a local variable with the specified name exists.
        /// </summary>
        /// <param name="name">The variable name to check for.</param>
        /// <returns><see langword="true"/> if a matching variable could be found or <see langword="false"/> if one could not.</returns>
        public bool ContainsKey(string name) => m_VariableLookup.ContainsKey(name);

        /// <summary>
        /// <inheritdoc cref="ContainsKey(string)"/>
        /// </summary>
        /// <param name="item">The item to check for. Both the Key and Value must match.</param>
        /// <returns><see langword="true"/> if a matching variable could be found or <see langword="false"/> if one could not.</returns>
        public bool Contains(KeyValuePair<string, IVariable> item) => TryGetValue(item.Key, out var v) && v == item.Value;

        /// <summary>
        /// Copies the local variables into an array starting at <paramref name="arrayIndex"/>.
        /// </summary>
        /// <param name="array">The array to copy the local variables into.</param>
        /// <param name="arrayIndex">The index to start copying the items into.</param>
        /// <exception cref="ArgumentNullException">Thrown when the array is null.</exception>
        public void CopyTo(KeyValuePair<string, IVariable>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            foreach (var entry in m_VariableLookup)
            {
                array[arrayIndex++] = new KeyValuePair<string, IVariable>(entry.Key, entry.Value.variable);
            }
        }

        /// <summary>
        /// <inheritdoc cref="GetEnumerator"/>
        /// </summary>
        /// <returns>The enumerator that can be used to iterate through all the local variables.</returns>
        IEnumerator<KeyValuePair<string, IVariable>> IEnumerable<KeyValuePair<string, IVariable>>.GetEnumerator()
        {
            foreach (var v in m_VariableLookup)
            {
                yield return new KeyValuePair<string, IVariable>(v.Key, v.Value.variable);
            }
        }

        /// <summary>
        /// Returns an enumerator for all local variables in this localized string.
        /// </summary>
        /// <returns>The enumerator that can be used to iterate through all the local variables.</returns>
        public IEnumerator GetEnumerator()
        {
            foreach (var v in m_VariableLookup)
            {
                yield return new KeyValuePair<string, IVariable>(v.Key, v.Value.variable);
            }
        }

        /// <summary>
        /// Removes all local variables.
        /// </summary>
        public void Clear()
        {
            m_VariableLookup.Clear();
            m_LocalVariables.Clear();
        }

        /// <summary>
        /// Allows for accessing metadata in a smart string.
        /// </summary>
        struct StringTableEntryVariable : IVariableGroup
        {
            readonly string m_Localized;
            readonly StringTableEntry m_StringTableEntry;

            public StringTableEntryVariable(string localized, StringTableEntry entry)
            {
                m_Localized = localized;
                m_StringTableEntry = entry;
            }

            public bool TryGetValue(string key, out IVariable value)
            {
                foreach (var md in m_StringTableEntry.MetadataEntries)
                {
                    if (md is IMetadataVariable v && v.VariableName == key)
                    {
                        value = v;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            /// <summary>
            /// Returns the localized string by default.
            /// </summary>
            /// <returns>The localized string.</returns>
            public override string ToString() => m_Localized;
        }

        /// <summary>
        /// Provides access to both the current local variables and those from the parent.
        /// </summary>
        struct ChainedLocalVariablesGroup : IVariableGroup
        {
            IVariableGroup ParentGroup { get; set; }
            IVariableGroup Group { get; set; }

            public ChainedLocalVariablesGroup(IVariableGroup group, IVariableGroup parent)
            {
                Group = group;
                ParentGroup = parent;
            }

            public bool TryGetValue(string key, out IVariable value)
            {
                if (Group.TryGetValue(key, out value))
                    return true;

                if (ParentGroup.TryGetValue(key, out value))
                    return true;

                value = null;
                return false;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="DataNotReadyException">Thrown when <see cref="IsEmpty"/>, no locale is available, the table is still loading, or the entry is missing.</exception>
        public object GetSourceValue(ISelectorInfo selector)
        {
            if (IsEmpty)
                throw new DataNotReadyException("{Empty}");

            // Determine what Locale we should use.
            var locale = LocaleOverride;
            if (locale == null && selector.FormatDetails.FormatCache != null)
                locale = LocalizationSettings.AvailableLocales.GetLocale(selector.FormatDetails.FormatCache.Table.LocaleIdentifier);
            if (locale == null && LocalizationSettings.SelectedLocaleAsync.IsDone)
                locale = LocalizationSettings.SelectedLocaleAsync.Result;
            if (locale == null)
                throw new DataNotReadyException("{No Available Locale}");

            var operation = LocalizationSettings.StringDatabase.GetTableEntryAsync(TableReference, TableEntryReference, locale, FallbackState);
            if (!operation.IsDone)
            {
                operation.Completed += m_CompletedSourceValue;
                throw new DataNotReadyException();
            }

            var entry = operation.Result.Entry;
            if (entry == null)
                throw new DataNotReadyException("{Missing Entry}");

            // If the entry is not smart then we do not need to forward as much information to the child.
            if (!entry.IsSmart)
            {
                var result = LocalizationSettings.StringDatabase.GenerateLocalizedString(operation.Result.Table, entry, TableReference, TableEntryReference, locale, Arguments);
                return new StringTableEntryVariable(result, entry);
            }

            var formatCache = entry?.GetOrCreateFormatCache();
            if (formatCache != null)
            {
                formatCache.VariableTriggers.Clear();

                if (m_VariableLookup.Count > 0)
                {
                    // Use the child and parent local variables.
                    formatCache.LocalVariables = new ChainedLocalVariablesGroup(this, selector.FormatDetails.FormatCache.LocalVariables);
                }
                else
                {
                    // Just use the parents local variables.
                    formatCache.LocalVariables = selector.FormatDetails.FormatCache.LocalVariables;
                }
            }

            using (ListPool<object>.Get(out var args))
            {
                if (selector.CurrentValue != null)
                    args.Add(selector.CurrentValue);
                if (Arguments != null)
                    args.AddRange(Arguments);

                var result = LocalizationSettings.StringDatabase.GenerateLocalizedString(operation.Result.Table, entry, TableReference, TableEntryReference, locale, args);

                if (formatCache != null)
                {
                    formatCache.LocalVariables = null;

                    // Subscribe to changes to local variables.
                    // Note: This could cause issues if the nested string is being used in multiple places.
                    // We may need to consider keeping multiple lists of callbacks in the future.
                    UpdateVariableListeners(formatCache.VariableTriggers);
                }
                return new StringTableEntryVariable(result, entry);
            }
        }

        void CompletedSourceValue(AsyncOperationHandle<LocalizedDatabase<StringTable, StringTableEntry>.TableEntryResult> _) => ValueChanged?.Invoke(this);

        /// <inheritdoc/>
        protected internal override void ForceUpdate()
        {
            if (m_ChangeHandler.Length != 0)
            {
                HandleLocaleChange(null);
            }

            ValueChanged?.Invoke(this);
        }

        void UpdateVariableListeners(List<IVariableValueChanged> variables)
        {
            // Unsubscribe from any old ones
            foreach (var gv in m_UsedVariables)
            {
                gv.ValueChanged -= m_OnVariableChanged;
            }

            m_UsedVariables.Clear();
            if (variables == null)
                return;

            foreach (var gv in variables)
            {
                m_UsedVariables.Add(gv);
                gv.ValueChanged += m_OnVariableChanged;
            }
        }

        void OnVariableChanged(IVariable globalVariable)
        {
            if (m_WaitingForVariablesEndUpdate)
                return;

            if (PersistentVariablesSource.IsUpdating)
            {
                // Its possible that multiple global variables will be changed, we don't want to force the
                // string to be updated for each change so we defer and do a single update during EndUpdate.
                m_WaitingForVariablesEndUpdate = true;
                PersistentVariablesSource.EndUpdate += OnVariablesSourceUpdateCompleted;
            }
            else
            {
                RefreshString();
                ValueChanged?.Invoke(this);
            }
        }

        void OnVariablesSourceUpdateCompleted()
        {
            PersistentVariablesSource.EndUpdate -= OnVariablesSourceUpdateCompleted;
            m_WaitingForVariablesEndUpdate = false;
            RefreshString();
            ValueChanged?.Invoke(this);
        }

        void InvokeChangeHandler(string value)
        {
            try
            {
                m_ChangeHandler.LockForChanges();
                var len = m_ChangeHandler.Length;
                if (len == 1)
                {
                    m_ChangeHandler.SingleDelegate(value);
                }
                else if (len > 1)
                {
                    var array = m_ChangeHandler.MultiDelegates;
                    for (int i = 0; i < len; ++i)
                        array[i](value);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            m_ChangeHandler.UnlockForChanges();
        }

        void HandleLocaleChange(Locale locale)
        {
            // Cancel any previous loading operations.
            ClearLoadingOperation();
            m_CurrentStringChangedValue = null;

            #if MODULE_UITK && UNITY_2023_3_OR_NEWER && UNITY_EDITOR
            HandleLocaleChangeDataBinding(locale);
            #endif

            #if UNITY_EDITOR
            m_CurrentTable = TableReference;
            m_CurrentTableEntry = TableEntryReference;

            // Don't update if we have no selected Locale
            if (!LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode && LocaleOverride == null && LocalizationSettings.SelectedLocale == null)
                return;
            #endif

            if (IsEmpty)
            {
                #if UNITY_EDITOR
                // If we are empty and playing or previewing then we should force an update.
                if (!LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
                    InvokeChangeHandler(null);
                #endif
                return;
            }

            CurrentLoadingOperationHandle = LocalizationSettings.StringDatabase.GetTableEntryAsync(TableReference, TableEntryReference, LocaleOverride, FallbackState);
            AddressablesInterface.Acquire(CurrentLoadingOperationHandle);

            if (!CurrentLoadingOperationHandle.IsDone)
            {
                #if !UNITY_WEBGL
                if (ForceSynchronous)
                {
                    CurrentLoadingOperationHandle.WaitForCompletion();
                }
                else
                #endif
                {
                    CurrentLoadingOperationHandle.Completed += m_AutomaticLoadingCompleted;
                    return;
                }
            }

            AutomaticLoadingCompleted(CurrentLoadingOperationHandle);
        }

        void AutomaticLoadingCompleted(AsyncOperationHandle<LocalizedStringDatabase.TableEntryResult> loadOperation)
        {
            if (loadOperation.Status != AsyncOperationStatus.Succeeded)
            {
                CurrentLoadingOperationHandle = default;
                return;
            }

            RefreshString();
        }

        void ClearLoadingOperation()
        {
            if (CurrentLoadingOperationHandle.IsValid())
            {
                // We should only call this if we are not done as its possible that the internal list is null if its not been used.
                if (!CurrentLoadingOperationHandle.IsDone)
                    CurrentLoadingOperationHandle.Completed -= m_AutomaticLoadingCompleted;
                AddressablesInterface.Release(CurrentLoadingOperationHandle);
                CurrentLoadingOperationHandle = default;
            }
        }

        /// <summary>
        /// Clears the current loading operation.
        /// </summary>
        protected override void Reset() => ClearLoadingOperation();

        public override void OnAfterDeserialize()
        {
            m_VariableLookup.Clear();
            foreach (var v in m_LocalVariables)
            {
                if (!string.IsNullOrEmpty(v.name))
                {
                    m_VariableLookup[v.name] = v;
                }
            }
        }

        /// <summary>
        /// Removes and releases internal references to Addressable assets.
        /// </summary>
        ~LocalizedString()
        {
            ClearLoadingOperation();
        }

        /// <summary>
        /// Removes and releases internal references to Addressable assets.
        /// </summary>
        void IDisposable.Dispose()
        {
            m_ChangeHandler.Clear();
            ClearLoadingOperation();
            LocalizationSettings.SelectedLocaleChanged -= m_SelectedLocaleChanged;
            GC.SuppressFinalize(this);
        }
    }
}
