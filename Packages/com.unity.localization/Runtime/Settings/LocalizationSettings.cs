using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Operations;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// The localization settings is the core component to the localization system.
    /// It provides the entry point to all player based localization features.
    /// </summary>
    public class LocalizationSettings : ScriptableObject, IReset, IDisposable
    {
        /// <summary>
        /// The name to use when retrieving the LocalizationSettings from CustomObject API.
        /// </summary>
        internal const string ConfigName = "com.unity.localization.settings";
        internal const string ConfigEditorLocale = "com.unity.localization-edit-locale";

        // Used when faking an empty project
        internal const string IgnoreSettings = "IgnoreSettings";

        internal const string LocaleLabel = "Locale";
        internal const string PreloadLabel = "Preload";

        [SerializeReference]
        List<IStartupLocaleSelector> m_StartupSelectors = new List<IStartupLocaleSelector>
        {
            new CommandLineLocaleSelector(),
            new SystemLocaleSelector(),
            new SpecificLocaleSelector()
        };

        [SerializeReference]
        ILocalesProvider m_AvailableLocales = new LocalesProvider();

        [SerializeReference]
        LocalizedAssetDatabase m_AssetDatabase = new LocalizedAssetDatabase();

        [SerializeReference]
        LocalizedStringDatabase m_StringDatabase = new LocalizedStringDatabase();

        [MetadataType(MetadataType.LocalizationSettings)]
        [SerializeField]
        MetadataCollection m_Metadata = new MetadataCollection();

        [SerializeField]
        internal LocaleIdentifier m_ProjectLocaleIdentifier = "en";

        [SerializeField]
        PreloadBehavior m_PreloadBehavior = PreloadBehavior.PreloadSelectedLocale;

        [SerializeField]
        bool m_InitializeSynchronously;

        internal AsyncOperationHandle<LocalizationSettings> m_InitializingOperationHandle;
        AsyncOperationHandle<Locale> m_SelectedLocaleAsync;
        Locale m_ProjectLocale;
        CallbackArray<Action<Locale>> m_SelectedLocaleChanged;

        internal bool IsChangingSelectedLocale { get; private set; }

        internal static LocalizationSettings s_Instance;

        /// <summary>
        /// Called when the <see cref="SelectedLocale"/> is changed.
        /// This will be called after <see cref="InitializationOperation"/> is completed so any preloading operations will be finished.
        /// </summary>
        /// <summary>
        /// Returns <see langword="true"/> if <seealso cref="OnSelectedLocaleChanged"/> has any subscribers.
        /// </summary>
        internal bool HasSelectedLocaleChangedSubscribers => m_SelectedLocaleChanged.Length != 0;

        /// <summary>
        /// Called when the <see cref="SelectedLocale"/> is changed.
        /// This will be called after <see cref="InitializationOperation"/> is completed so any preloading operations will be finished.
        /// </summary>
        public event Action<Locale> OnSelectedLocaleChanged
        {
            add => m_SelectedLocaleChanged.Add(value);
            remove => m_SelectedLocaleChanged.RemoveByMovingTail(value);
        }

        /// <summary>
        /// Indicates if there is a LocalizationSettings present. If one is not found then it will attempt to find one however
        /// unlike <see cref="Instance"/> it will not create a default, if one can not be found.
        /// </summary>
        /// <value><see langword="true"/> if has settings; otherwise, <see langword="false"/>.</value>
        public static bool HasSettings
        {
            get
            {
                // Use ReferenceEquals so we dont get false positives when using MoQ
                if (ReferenceEquals(s_Instance, null))
                    s_Instance = GetInstanceDontCreateDefault();
                return !ReferenceEquals(s_Instance, null);
            }
        }

        /// <summary>
        /// The localization system may not be immediately ready. Loading Locales, preloading assets etc.
        /// This operation can be used to check when the system is ready. You can yield on this in a coroutine to wait.
        /// If <see cref="InitializeSynchronously"/> is <see langword="true"/> then this operation will complete synchronously the first time it is called.
        /// Uses [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) to force the loading to complete synchronously.
        /// Please note that [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) is not supported on
        /// [WebGL](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/SynchronousAddressables.html#webgl) and <see cref="InitializeSynchronously"/>
        /// will be ignored when running on WebGL.
        /// </summary>
        /// <example>
        /// This shows how to use a coroutine to wait for the Initialization Operation to complete.
        /// <code source="../../DocCodeSamples.Tests/LocalizationSettingsSamples.cs" region="asynchronous"/>
        /// </example>
        /// <example>
        /// This shows how to use the <see cref="AsyncOperationHandle{TObject}.Completed"/> event to get a callback when the Initialization Operation is complete.
        /// <code source="../../DocCodeSamples.Tests/LocalizationSettingsSamples.cs" region="asynchronous-event"/>
        /// </example>
        /// <example>
        /// This shows how to force the Initialization Operation to complete synchronously using [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion).
        /// Note [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) is not supported on WebGL.
        /// <code source="../../DocCodeSamples.Tests/LocalizationSettingsSamples.cs" region="synchronous"/>
        /// </example>
        public static AsyncOperationHandle<LocalizationSettings> InitializationOperation => Instance.GetInitializationOperation();

        /// <summary>
        /// Singleton instance for the Localization Settings.
        /// </summary>
        public static LocalizationSettings Instance
        {
            get
            {
                // Use ReferenceEquals so we dont get false positives when using MoQ
                if (ReferenceEquals(s_Instance, null))
                    s_Instance = GetOrCreateSettings();
                return s_Instance;
            }
            set => s_Instance = value;
        }

        /// <summary>
        /// <inheritdoc cref="IStartupLocaleSelector"/>
        /// </summary>
        public static List<IStartupLocaleSelector> StartupLocaleSelectors => Instance.GetStartupLocaleSelectors();

        /// <summary>
        /// <inheritdoc cref="AvailableLocales"/>
        /// </summary>
        public static ILocalesProvider AvailableLocales
        {
            get => Instance.GetAvailableLocales();
            set => Instance.SetAvailableLocales(value);
        }

        /// <summary>
        /// The asset database is responsible for providing localized assets.
        /// </summary>
        public static LocalizedAssetDatabase AssetDatabase
        {
            get => Instance.GetAssetDatabase();
            set => Instance.SetAssetDatabase(value);
        }

        /// <summary>
        /// The string database is responsible for providing localized string assets.
        /// </summary>
        public static LocalizedStringDatabase StringDatabase
        {
            get => Instance.GetStringDatabase();
            set => Instance.SetStringDatabase(value);
        }

        /// <summary>
        /// Returns the Localization Settings Metadata.
        /// Metadata can be used to contain additional information such as App Name localization settings.
        /// </summary>
        public static MetadataCollection Metadata => Instance.GetMetadata();

        /// <summary>
        /// The current selected <see cref="Locale"/>. This is the Locale that will be used by default when localizing assets and strings.
        /// Calling this when the Localization system has not initialized will force the Localization system to load all Locales before returning,
        /// see <see cref="SelectedLocaleAsync"/> for a version that will load the Locales asynchronously.
        /// </summary>
        public static Locale SelectedLocale
        {
            get => Instance.GetSelectedLocale();
            set => Instance.SetSelectedLocale(value);
        }

        /// <summary>
        /// The current selected Locale. This is the Locale that will be used by default when localizing assets and strings.
        /// If <see cref="InitializationOperation"/> has not been completed yet then this will wait for the <see cref="AvailableLocales"/> part to complete first.
        /// It will not wait for the entire <see cref="InitializationOperation"/> but just the part that initializes the Locales.
        /// See <seealso cref="SelectedLocale"/> for a synchronous version that will block until the Locales have been loaded.
        /// </summary>
        public static AsyncOperationHandle<Locale> SelectedLocaleAsync => Instance.GetSelectedLocaleAsync();

        /// <summary>
        /// Event that is sent when the <see cref="SelectedLocale"/> is changed.
        /// </summary>
        /// <example>
        /// This shows how to keep track of the current selected <see cref="Locale"/>.
        /// <code source="../../DocCodeSamples.Tests/LocalizationSettingsSamples.cs" region="selected-locale-changed"/>
        /// </example>
        public static event Action<Locale> SelectedLocaleChanged
        {
            add => Instance.OnSelectedLocaleChanged += value;
            remove => Instance.OnSelectedLocaleChanged -= value;
        }

        /// <summary>
        /// When tracking property variants in a scene, any changes you make whilst in this Locale are saved into the source object instead of as a variant.
        /// </summary>
        public static Locale ProjectLocale
        {
            get
            {
                if (Instance.m_ProjectLocale == null || Instance.m_ProjectLocale.Identifier != Instance.m_ProjectLocaleIdentifier)
                {
                    // Initialize the locales (LOC-1139)
                    if (Instance.GetAvailableLocales() is IPreloadRequired localesProvider && !localesProvider.PreloadOperation.IsDone)
                    {
                        localesProvider.PreloadOperation.WaitForCompletion();
                    }

                    Instance.m_ProjectLocale = AvailableLocales?.GetLocale(Instance.m_ProjectLocaleIdentifier);
                }
                return Instance.m_ProjectLocale;
            }
            set
            {
                Instance.m_ProjectLocale = value;
                Instance.m_ProjectLocaleIdentifier = value != null ? value.Identifier : default;
            }
        }

        /// <summary>
        /// Forces the <see cref="InitializationOperation"/> to complete immediately when it is started.
        /// Uses [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) to force the loading to complete synchronously.
        /// Please note that [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) is not supported on
        /// [WebGL](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/SynchronousAddressables.html#webgl) and <see cref="InitializeSynchronously"/>
        /// will be ignored when running on WebGL.
        /// </summary>
        public static bool InitializeSynchronously
        {
            get => Instance.m_InitializeSynchronously;
            set => Instance.m_InitializeSynchronously = value;
        }

        /// <summary>
        /// Determines which tables, that have been marked as preload, will be loaded during the preloading step.
        /// </summary>
        public static PreloadBehavior PreloadBehavior
        {
            get => Instance.m_PreloadBehavior;
            set => Instance.m_PreloadBehavior = value;
        }

        internal virtual void OnEnable()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;

            if (UnityEditor.SessionState.GetBool(IgnoreSettings, false))
            {
                return;
            }
            #endif

            if (s_Instance == null)
            {
                s_Instance = this;
            }
        }

        #if UNITY_EDITOR
        void EditorApplication_playModeStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            if (obj == UnityEditor.PlayModeStateChange.ExitingEditMode || obj == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                ResetState();
            }
        }

        void OnDisable()
        {
            ResetState();
            UnityEditor.EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
        }

        #endif

        /// <summary>
        /// Used to ensure a <see cref="LocalizationSettings"/> exists in the project.
        /// Throws an exception if <see cref="HasSettings"/> is false.
        /// </summary>
        /// <param name="error"></param>
        internal static void ValidateSettingsExist(string error = "")
        {
            if (!HasSettings)
            {
                throw new Exception($"There is no active LocalizationSettings.\n {error}");
            }
        }

        /// <summary>
        /// <inheritdoc cref="InitializationOperation"/>
        /// </summary>
        /// <returns></returns>
        public virtual AsyncOperationHandle<LocalizationSettings> GetInitializationOperation()
        {
            if (!m_InitializingOperationHandle.IsValid())
            {
                var operation = Operations.InitializationOperation.Pool.Get();
                operation.Init(this);

                // We need to depend on InitializeAsync to workaround an issue (LOC-823)
                operation.Dependency = AddressablesInterface.Instance.InitializeAddressablesAsync();
                m_InitializingOperationHandle = AddressablesInterface.ResourceManager.StartOperation(operation, operation.Dependency);

                #if !UNITY_WEBGL // WebGL does not support WaitForCompletion
                if (!m_InitializingOperationHandle.IsDone && m_InitializeSynchronously && IsPlaying)
                    m_InitializingOperationHandle.WaitForCompletion();
                #endif
            }

            return m_InitializingOperationHandle;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// We use this for testing so we don't have to enter play mode.
        /// </summary>
        internal bool? IsPlayingOverride { get; set; }
        #endif

        internal bool IsPlayingOrWillChangePlaymode
        {
            get
            {
                #if UNITY_EDITOR
                if (IsPlayingOverride.HasValue)
                    return IsPlayingOverride.Value;
                return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || IsPlaying;
                #else
                return true;
                #endif
            }
        }

        internal bool IsPlaying
        {
            get
            {
                #if UNITY_EDITOR
                if (IsPlayingOverride.HasValue)
                    return IsPlayingOverride.Value;
                #endif
                return Application.isPlaying;
            }
        }

        internal virtual RuntimePlatform Platform => Application.platform;

        /// <summary>
        /// <inheritdoc cref="IStartupLocaleSelector"/>
        /// </summary>
        /// <returns>\</returns>
        public List<IStartupLocaleSelector> GetStartupLocaleSelectors() => m_StartupSelectors;

        /// <summary>
        /// <inheritdoc cref="AvailableLocales"/>
        /// </summary>
        /// <param name="available"></param>
        public void SetAvailableLocales(ILocalesProvider available) => m_AvailableLocales = available;

        /// <summary>
        /// <inheritdoc cref="AvailableLocales"/>
        /// </summary>
        /// <returns>\</returns>
        public virtual ILocalesProvider GetAvailableLocales() => m_AvailableLocales;

        /// <summary>
        /// <inheritdoc cref="AssetDatabase"/>
        /// </summary>
        /// <param name="database"></param>
        public void SetAssetDatabase(LocalizedAssetDatabase database) => m_AssetDatabase = database;

        /// <summary>
        /// <inheritdoc cref="AssetDatabase"/>
        /// </summary>
        /// <returns></returns>
        public virtual LocalizedAssetDatabase GetAssetDatabase() => m_AssetDatabase;

        /// <summary>
        /// Sets the string database to be used for localizing all strings.
        /// </summary>
        /// <param name="database"></param>
        public void SetStringDatabase(LocalizedStringDatabase database) => m_StringDatabase = database;

        /// <summary>
        /// Returns the string database being used to localize all strings.
        /// </summary>
        /// <returns>The string database.</returns>
        public virtual LocalizedStringDatabase GetStringDatabase() => m_StringDatabase;

        /// <summary>
        /// Returns the Localization Settings Metadata.
        /// Metadata can be used to contain additional information such as App Name localization settings.
        /// </summary>
        /// <returns></returns>
        public MetadataCollection GetMetadata() => m_Metadata;

        /// <summary>
        /// Sends out notifications when the locale has changed. Ensures the the events are sent in the correct order.
        /// </summary>
        /// <param name="locale">The new locale.</param>
        internal void SendLocaleChangedEvents(Locale locale)
        {
            #if UNITY_EDITOR
            if (locale == null)
            {
                LocalizationPropertyDriver.UnregisterProperties();
                VariantsPropertyDriver.UnregisterProperties();
            }
            #endif

            m_StringDatabase?.OnLocaleChanged(locale);
            m_AssetDatabase?.OnLocaleChanged(locale);

            if (m_InitializingOperationHandle.IsValid())
            {
                AddressablesInterface.SafeRelease(m_InitializingOperationHandle);
                m_InitializingOperationHandle = default;
            }

            var initOp = GetInitializationOperation();
            if (initOp.Status == AsyncOperationStatus.Succeeded)
            {
                InvokeSelectedLocaleChanged(locale);
            }
            else
            {
                // We use a coroutine to call the OnSelectedLocaleChanged event, we do not want to use the initOp Completed event as this
                // will create issues if a user was to call WaitForCompletion inside of the callback, it would fail with:
                // "Exception: Reentering the Update method is not allowed.  This can happen when calling WaitForCompletion on
                // an operation while inside of a callback".
                LocalizationBehaviour.Instance.StartCoroutine(InitializeAndCallSelectedLocaleChangedCoroutine(locale));
            }
        }

        IEnumerator InitializeAndCallSelectedLocaleChangedCoroutine(Locale locale)
        {
            yield return m_InitializingOperationHandle;
            InvokeSelectedLocaleChanged(locale);
        }

        void InvokeSelectedLocaleChanged(Locale locale)
        {
            IsChangingSelectedLocale = true;
            try
            {
                m_SelectedLocaleChanged.LockForChanges();
                var len = m_SelectedLocaleChanged.Length;
                if (len == 1)
                {
                    m_SelectedLocaleChanged.SingleDelegate(locale);
                }
                else if (len > 1)
                {
                    var array = m_SelectedLocaleChanged.MultiDelegates;
                    for (int i = 0; i < len; ++i)
                        array[i](locale);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            IsChangingSelectedLocale = false;
            m_SelectedLocaleChanged.UnlockForChanges();
        }

        #if UNITY_EDITOR
        internal static string EditorLocaleCode
        {
            get => UnityEditor.EditorPrefs.GetString(ConfigEditorLocale, string.Empty);
            set => UnityEditor.EditorPrefs.SetString(ConfigEditorLocale, value);
        }
        #endif

        Locale SelectActiveLocale()
        {
            if (m_AvailableLocales == null)
            {
                Debug.LogError("AvailableLocales is null, can not pick a Locale.");
                return null;
            }

            if (m_AvailableLocales.Locales == null)
            {
                Debug.LogError("AvailableLocales.Locales is null, can not pick a Locale.");
                return null;
            }

            #if UNITY_EDITOR
            if (!IsPlayingOrWillChangePlaymode)
            {
                return m_AvailableLocales.GetLocale(EditorLocaleCode);
            }
            #endif

            return SelectLocaleUsingStartupSelectors();
        }

        /// <summary>
        /// Uses <see cref="StartupLocaleSelectors"/> to select the most appropriate <see cref="Locale"/>.
        /// </summary>
        protected internal virtual Locale SelectLocaleUsingStartupSelectors()
        {
            foreach (var sel in m_StartupSelectors)
            {
                var locale = sel.GetStartupLocale(GetAvailableLocales());
                if (locale != null)
                {
                    return locale;
                }
            }

            using (StringBuilderPool.Get(out var sb))
            {
                sb.AppendLine("No Locale could be selected:");

                if (m_AvailableLocales.Locales.Count == 0)
                {
                    sb.AppendLine("No Locales were available. Did you build the Addressables?");
                }
                else
                {
                    sb.AppendLine($"The following ({m_AvailableLocales.Locales.Count}) Locales were considered:");
                    foreach (var locale in m_AvailableLocales.Locales)
                    {
                        sb.AppendLine($"\t{locale}");
                    }
                }

                sb.AppendLine($"The following ({m_StartupSelectors.Count}) IStartupLocaleSelectors were used:");
                foreach (var selector in m_StartupSelectors)
                {
                    sb.AppendLine($"\t{selector}");
                }

                Debug.LogError(sb.ToString(), this);
            }

            return null;
        }

        /// <summary>
        /// <inheritdoc cref="SelectedLocale"/>
        /// </summary>
        /// <param name="locale"></param>
        public void SetSelectedLocale(Locale locale)
        {
            // Do nothing if we are assigning the same locale
            if (m_SelectedLocaleAsync.IsValid())
            {
                var previousLocale = m_SelectedLocaleAsync.Result;
                if (ReferenceEquals(previousLocale, locale))
                    return;

                // Sometimes users will hold a list of Locales in a script and assign them in the player,
                // these assets are now copies and wont have the same reference so we should also check the asset code and name.(LOC-1096)
                if (previousLocale != null && locale != null && previousLocale.name == locale.name && previousLocale.Identifier.Code == locale.Identifier.Code)
                    return;
            }

            // We need to ensure initialization has been started
            GetInitializationOperation();

            #if UNITY_EDITOR
            // Running the player loop outside of play mode will force an update for many types, especially UGUI.
            if (!IsPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            }
            #endif

            // Ignore null locales in play mode
            if (locale == null && IsPlayingOrWillChangePlaymode)
                return;

            if (!m_SelectedLocaleAsync.IsValid() || !ReferenceEquals(m_SelectedLocaleAsync.Result, locale))
            {
                #if UNITY_EDITOR
                if (!IsPlayingOrWillChangePlaymode)
                {
                    var code = locale == null ? string.Empty : locale.Identifier.Code;
                    EditorLocaleCode = code;
                }
                #endif

                if (m_SelectedLocaleAsync.IsValid())
                    AddressablesInterface.Release(m_SelectedLocaleAsync);

                m_SelectedLocaleAsync = AddressablesInterface.ResourceManager.CreateCompletedOperation(locale, null);
                SendLocaleChangedEvents(locale);
            }
        }

        /// <summary>
        /// <inheritdoc cref="SelectedLocaleAsync"/>
        /// </summary>
        /// <returns></returns>
        public virtual AsyncOperationHandle<Locale> GetSelectedLocaleAsync()
        {
            if (!m_SelectedLocaleAsync.IsValid())
            {
                if (GetAvailableLocales() is IPreloadRequired localesProvider && !localesProvider.PreloadOperation.IsDone)
                {
                    m_SelectedLocaleAsync = AddressablesInterface.ResourceManager.CreateChainOperation(localesProvider.PreloadOperation, (op) => AddressablesInterface.ResourceManager.CreateCompletedOperation(SelectActiveLocale(), null));
                }
                else
                {
                    m_SelectedLocaleAsync = AddressablesInterface.ResourceManager.CreateCompletedOperation(SelectActiveLocale(), null);
                }
            }
            return m_SelectedLocaleAsync;
        }

        /// <summary>
        /// <inheritdoc cref="SelectedLocale"/>
        /// </summary>
        /// <returns>\</returns>
        public virtual Locale GetSelectedLocale()
        {
            var localeOp = GetSelectedLocaleAsync();
            if (localeOp.IsDone)
                return localeOp.Result;
            return localeOp.WaitForCompletion();
        }

        /// <summary>
        /// Indicates that the Locale is no longer available.
        /// If the locale is the current <see cref="SelectedLocale"/> then a new one will be found using <see cref="StartupLocaleSelectors"/>.
        /// </summary>
        /// <param name="locale"></param>
        public virtual void OnLocaleRemoved(Locale locale)
        {
            if (m_SelectedLocaleAsync.IsValid() && ReferenceEquals(m_SelectedLocaleAsync.Result, locale))
            {
                AddressablesInterface.Release(m_SelectedLocaleAsync);
                m_SelectedLocaleAsync = default;
            }
        }

        /// <inheritdoc/>
        public void ResetState()
        {
            m_SelectedLocaleAsync = default;
            m_InitializingOperationHandle = default;
            (m_AvailableLocales as IReset)?.ResetState();
            (m_AssetDatabase as IReset)?.ResetState();
            (m_StringDatabase as IReset)?.ResetState();
        }

        /// <summary>
        /// Releases all Addressables assetss.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (m_InitializingOperationHandle.IsValid())
            {
                if (!m_InitializingOperationHandle.IsDone)
                    m_InitializingOperationHandle.WaitForCompletion();
                AddressablesInterface.Release(m_InitializingOperationHandle);
            }

            if (m_SelectedLocaleAsync.IsValid())
            {
                Debug.Assert(m_SelectedLocaleAsync.IsDone, "Disposing an incomplete locale operation");
                AddressablesInterface.Release(m_SelectedLocaleAsync);
            }

            m_InitializingOperationHandle = default;
            m_SelectedLocaleAsync = default;
            (m_AvailableLocales as IDisposable)?.Dispose();
            (m_AssetDatabase as IDisposable)?.Dispose();
            (m_StringDatabase as IDisposable)?.Dispose();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns the singleton of the LocalizationSettings but does not create a default one if no active settings are found.
        /// </summary>
        /// <returns></returns>
        public static LocalizationSettings GetInstanceDontCreateDefault()
        {
            // Use ReferenceEquals so we dont get false positives when using MoQ
            if (!ReferenceEquals(s_Instance, null))
                return s_Instance;

            LocalizationSettings settings;
            #if UNITY_EDITOR
            UnityEditor.EditorBuildSettings.TryGetConfigObject(ConfigName, out settings);
            #else
            settings = FindObjectOfType<LocalizationSettings>();
            #endif
            return settings;
        }

        static LocalizationSettings GetOrCreateSettings()
        {
            var settings = GetInstanceDontCreateDefault();

            // Use ReferenceEquals so we dont get false positives when using MoQ
            if (ReferenceEquals(settings, null))
            {
                Debug.LogWarning("Could not find localization settings. Default will be used.");

                settings = CreateInstance<LocalizationSettings>();
                settings.name = "Default Localization Settings";
            }

            return settings;
        }
    }
}
