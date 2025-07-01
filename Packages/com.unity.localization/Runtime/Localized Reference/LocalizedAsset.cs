using System;
using UnityEngine.Localization.Operations;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization
{
    #if MODULE_AUDIO || PACKAGE_DOCS_GENERATION
    /// <summary>
    /// Provides a specialized <see cref="LocalizedAsset{TObject}"/> which can be used to localize [AudioClip](https://docs.unity3d.com/ScriptReference/AudioClip.html) assets.
    /// </summary>
    /// <example>
    /// This example shows how to use the LocalizedAudioClip in a [MonoBehaviour](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html).
    /// <code source="../../DocCodeSamples.Tests/LocalizedAudioClipSamples.cs" region="example"/>
    /// </example>
    [Serializable]
    public partial class LocalizedAudioClip : LocalizedAsset<AudioClip> {}
    #endif

    /// <summary>
    /// Provides a specialized <see cref="LocalizedAsset{TObject}"/> which can be used to localize [Prefabs](https://docs.unity3d.com/Manual/Prefabs.html).
    /// </summary>
    [Serializable]
    public partial class LocalizedGameObject : LocalizedAsset<GameObject> {}

    /// <summary>
    /// Provides a specialized <see cref="LocalizedAsset{TObject}"/> which can be used to localize a [Mesh](https://docs.unity3d.com/ScriptReference/Mesh.html).
    /// </summary>
    [Serializable]
    public partial class LocalizedMesh : LocalizedAsset<Mesh> {}

    /// <summary>
    /// Provides a specialized <see cref="LocalizedAsset{TObject}"/> which can be used to localize [Materials](https://docs.unity3d.com/ScriptReference/Material.html).
    /// </summary>
    [Serializable]
    public partial class LocalizedMaterial : LocalizedAsset<Material> {}

    /// <summary>
    /// Provides a <see cref="LocalizedAsset{TObject}"/> which you can use to localize any [UnityEngine.Object](https://docs.unity3d.com/ScriptReference/Object.html).
    /// </summary>
    [Serializable]
    public partial class LocalizedObject : LocalizedAsset<Object> {}

    /// <summary>
    /// Provides a <see cref="LocalizedAsset{TObject}"/> which you can use to localize [Sprites](https://docs.unity3d.com/ScriptReference/Sprite.html).
    /// </summary>
    [Serializable]
    public partial class LocalizedSprite : LocalizedAsset<Sprite> {}

    /// <summary>
    /// Provides a <see cref="LocalizedAsset{TObject}"/> which you can use to localize [Textures](https://docs.unity3d.com/ScriptReference/Texture.html) assets.
    /// </summary>
    [Serializable]
    public partial class LocalizedTexture : LocalizedAsset<Texture> {}

    #if PACKAGE_TMP || (UNITY_2023_2_OR_NEWER && PACKAGE_UGUI) || PACKAGE_DOCS_GENERATION
    /// <summary>
    /// Provides a <see cref="LocalizedAsset{TObject}"/> which you can use to localize a TextMeshPro [TMPro.TMP_FontAsset](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest?subfolder=/api/TMPro.TMP_FontAsset)/>.
    /// </summary>
    [Serializable]
    public partial class LocalizedTmpFont : LocalizedAsset<UnityEngine.TextCore.Text.SpriteAsset> {}
    #endif

    /// <summary>
    /// Provides a <see cref="LocalizedAsset{TObject}"/> which you can use to localize a [Font](https://docs.unity3d.com/ScriptReference/Font.html)/>.
    /// </summary>
    [Serializable]
    public partial class LocalizedFont : LocalizedAsset<Font> {}

    /// <summary>
    /// Base class for all localized assets.
    /// </summary>
    public abstract partial class LocalizedAssetBase : LocalizedReference
    {
        /// <summary>
        /// Returns the localized asset as a [UnityEngine.Object](https://docs.unity3d.com/ScriptReference/Object.html).
        /// </summary>
        /// <returns></returns>
        public abstract AsyncOperationHandle<Object> LoadAssetAsObjectAsync();

        /// <summary>
        /// Overrides the asset's default type. This loads a type from <see cref="AssetTable"/> with the <see cref="TableReference"/> and the
        /// the asset that matches<see cref= "TableEntryReference" />.
        /// This helps to filter sub-assets when trying to load them and they share a common type among other sub-assets with the same name.
        /// <br/>For example, an asset could have the following structure:
        /// <br/>Main Asset [GameObject]
        /// <br/>- Sub Asset[GameObject]
        /// <br/>- Sub Asset[Mesh]
        /// If you were using a <see cref="LocalizedObject"/>, calling<see cref="LoadAsset"/> or<see cref="LoadAssetAsObjectAsync"/> to load
        /// "Main Asset/Sub Asset" would return the first matching asset. In this case, this would be the GameObject version.
        /// With this method, you could provide the type Mesh to filter out the GameObject version and return the Mesh.
        /// </summary>
        /// <typeparam name="TObject">The type to use instead of the default.</typeparam>
        /// <returns>Returns the loading operation for the request.</returns>
        public abstract AsyncOperationHandle<TObject> LoadAssetAsync<TObject>() where TObject : Object;
    }

    /// <summary>
    /// Provides a way to reference an <see cref="AssetTableEntry"/> inside of a specific <see cref="AssetTable"/> and request the localized asset.
    /// </summary>
    /// <typeparam name="TObject">The type that should be supported. This can be any type that inherits from [UnityEngine.Object](https://docs.unity3d.com/ScriptReference/Object.html).</typeparam>
    /// <example>
    /// This example shows how a <see cref="LocalizedAsset{TObject}"/> can be used to localize a [Prefabs](https://docs.unity3d.com/Manual/Prefabs.html).
    /// See also <seealso cref="LocalizedGameObject"/> and <seealso cref="Components.LocalizedGameObjectEvent"/>.
    /// <code source="../../DocCodeSamples.Tests/LocalizedAssetSamples.cs" region="localized-prefab"/>
    /// </example>
    /// <example>
    /// This example shows how a <see cref="LocalizedAsset{TObject}"/> can be used to localize a [ScriptableObject](https://docs.unity3d.com/ScriptReference/ScriptableObject.html).
    /// <code source="../../DocCodeSamples.Tests/LocalizedAssetSamples.cs" region="localized-scriptable-object"/>
    /// </example>
    /// <example>
    /// This example shows how to bind to a UI Toolkit property. Note that this requires Unity 2023.2 and above.
    /// <code source="../../DocCodeSamples.Tests/LocalizedTextureUIDocumentExample.cs" region="example"/>
    /// </example>
    [Serializable]
    public partial class LocalizedAsset<TObject> : LocalizedAssetBase, IDisposable where TObject : Object
    {
        CallbackArray<ChangeHandler> m_ChangeHandler;
        Action<Locale> m_SelectedLocaleChanged;
        Action<AsyncOperationHandle<TObject>> m_AutomaticLoadingCompleted;
        AsyncOperationHandle<TObject> m_PreviousLoadingOperation;

        /// <summary>
        /// Delegate used by <see cref="AssetChanged"/>.
        /// </summary>
        /// <param name="value">The localized asset.</param>
        public delegate void ChangeHandler(TObject value);

        /// <inheritdoc/>
        public override bool WaitForCompletion
        {
            set
            {
                if (value == WaitForCompletion)
                    return;

                base.WaitForCompletion = value;
                #if !UNITY_WEBGL // WebGL does not support WaitForCompletion
                if (value && CurrentLoadingOperationHandle.IsValid() && !CurrentLoadingOperationHandle.IsDone)
                    CurrentLoadingOperationHandle.WaitForCompletion();
                #endif
            }
        }

        internal override bool ForceSynchronous => WaitForCompletion || LocalizationSettings.AssetDatabase.AsynchronousBehaviour == AsynchronousBehaviour.ForceSynchronous;

        /// <summary>
        /// The current loading operation for the asset when using <see cref="AssetChanged"/>. This is <see langword="default"/> if a loading operation is not available.
        /// </summary>
        public AsyncOperationHandle<TObject> CurrentLoadingOperationHandle
        {
            get;
            internal set;
        }

        /// <summary>
        /// Provides a callback that will be invoked when the asset is available or has changed.
        /// </summary>
        /// <remarks>
        /// The following events will trigger an update:
        /// - The first time the action is added to the event.
        /// - The <seealso cref="LocalizationSettings.SelectedLocale"/> changing.
        /// - The <see cref="TableReference"/> or <see cref="TableEntryReference"/> changing.
        ///
        /// When the first <see cref="ChangeHandler"/> is added, a loading operation (see <see cref="CurrentLoadingOperationHandle"/>) will automatically
        /// start and the localized asset will be sent to the subscriber when completed. Any adding additional subscribers added after
        /// loading has completed will also be sent the latest localized asset when they are added.
        /// This ensures that a subscriber will always have the correct localized value regardless of when it was added.
        /// </remarks>
        /// <example>
        /// This example shows how the <see cref="AssetChanged"/> event could be used to change the Font on some localized Text.
        /// <code source="../../DocCodeSamples.Tests/LocalizedAssetSamples.cs" region="localized-text-font"/>
        /// </example>
        public event ChangeHandler AssetChanged
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
                    value(CurrentLoadingOperationHandle.Result);
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
        /// Returns <see langword="true"/> if <seealso cref="AssetChanged"/> has any subscribers.
        /// </summary>
        public bool HasChangeHandler => m_ChangeHandler.Length != 0;

        /// <summary>
        /// Initializes and returns an empty instance of a <see cref="LocalizedAsset{TObject}"/>.
        /// </summary>
        public LocalizedAsset()
        {
            m_SelectedLocaleChanged = HandleLocaleChange;
            m_AutomaticLoadingCompleted = AutomaticLoadingCompleted;
        }

        /// <summary>
        /// Provides a localized asset from a <see cref="AssetTable"/> with the <see cref="TableReference"/> and the
        /// the asset that matches <see cref="TableEntryReference"/>.
        /// </summary>
        /// <remarks>
        /// The asset may have already been loaded, either during a previous operation or if Preload mode is used. Check the <see cref="AsyncOperationHandle.IsDone"/> property to see if the asset is already loaded and therefore is immediately available.
        /// See [Async operation handling](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/AddressableAssetsAsyncOperationHandle.html) for further details.
        /// </remarks>
        /// <returns>Returns the loading operation for the request.</returns>
        /// <example>
        /// This example shows how <see cref="LoadAssetAsync"/> can be used to request a sprite asset when the <see cref="LocalizationSettings.SelectedLocale"/> changes.
        /// <code source="../../DocCodeSamples.Tests/LocalizedAssetSamples.cs" region="localized-sprite"/>
        /// </example>
        public AsyncOperationHandle<TObject> LoadAssetAsync() => LoadAssetAsync<TObject>();

        /// <inheritdoc/>
        public override AsyncOperationHandle<T> LoadAssetAsync<T>()
        {
            LocalizationSettings.ValidateSettingsExist("Can not Load Asset.");
            return LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<T>(TableReference, TableEntryReference, locale: LocaleOverride);
        }

        class ConvertToObjectOperation : WaitForCurrentOperationAsyncOperationBase<Object>
        {
            public static readonly ObjectPool<ConvertToObjectOperation> Pool = new ObjectPool<ConvertToObjectOperation>(
                () => new ConvertToObjectOperation(), collectionCheck:false);

            AsyncOperationHandle<TObject> m_Operation;

            public void Init(AsyncOperationHandle<TObject> operation)
            {
                AddressablesInterface.ResourceManager.Acquire(operation);
                m_Operation = operation;
                CurrentOperation = operation;
            }

            protected override void Execute()
            {
                if (m_Operation.IsDone)
                    OnCompleted(m_Operation);
                else
                    m_Operation.Completed += OnCompleted;
            }

            void OnCompleted(AsyncOperationHandle<TObject> op)
            {
                Complete(op.Result, op.Status == AsyncOperationStatus.Succeeded, null);
            }

            protected override void Destroy()
            {
                AddressablesInterface.Release(m_Operation);
                Pool.Release(this);
            }
        }

        /// <inheritdoc/>
        public override AsyncOperationHandle<Object> LoadAssetAsObjectAsync()
        {
            var wrappedOperation = LoadAssetAsync();
            var operation = ConvertToObjectOperation.Pool.Get();
            operation.Init(wrappedOperation);
            return AddressablesInterface.ResourceManager.StartOperation(operation, default);
        }

        /// <summary>
        /// Provides a localized asset from a <see cref="AssetTable"/> with the <see cref="TableReference"/> and the
        /// the asset that matches <see cref="TableEntryReference"/>.
        /// Uses [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) to force the loading to complete synchronously.
        /// Please note that [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) is not supported on
        /// [WebGL](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/SynchronousAddressables.html#webgl).
        /// </summary>
        /// <returns>Returns the localized asset.</returns>
        public TObject LoadAsset()
        {
            return LoadAssetAsync().WaitForCompletion();
        }

        /// <inheritdoc/>
        protected internal override void ForceUpdate()
        {
            if (m_ChangeHandler.Length != 0)
            {
                HandleLocaleChange(null);
            }
        }

        void HandleLocaleChange(Locale locale)
        {
            #if UNITY_EDITOR
            m_CurrentTable = TableReference;
            m_CurrentTableEntry = TableEntryReference;

            #if MODULE_UITK && UNITY_2023_3_OR_NEWER
            HandleLocaleChangeDataBinding(locale);
            #endif

            // Dont update if we have no selected Locale
            if (!LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode && LocaleOverride == null && LocalizationSettings.SelectedLocale == null)
            {
                ClearLoadingOperation();
                return;
            }
            #endif

            if (IsEmpty)
            {
                ClearLoadingOperation();
                #if UNITY_EDITOR
                // If we are empty and playing or previewing then we should force an update.
                if (!LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
                    InvokeChangeHandler(null);
                #endif
                return;
            }


            // Track the previous loading operation so we can release it when the new one completes (LOC-976).
            m_PreviousLoadingOperation = CurrentLoadingOperationHandle;
            CurrentLoadingOperationHandle = LoadAssetAsync();
            AddressablesInterface.Acquire(CurrentLoadingOperationHandle);

            if (!CurrentLoadingOperationHandle.IsDone)
            {
                #if !UNITY_WEBGL
                if (WaitForCompletion || LocalizationSettings.AssetDatabase.AsynchronousBehaviour == AsynchronousBehaviour.ForceSynchronous)
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

        void AutomaticLoadingCompleted(AsyncOperationHandle<TObject> loadOperation)
        {
            if (loadOperation.Status != AsyncOperationStatus.Succeeded)
            {
                CurrentLoadingOperationHandle = default;
                return;
            }

            InvokeChangeHandler(loadOperation.Result);
            ClearPreviousLoadingOperation();
        }

        void InvokeChangeHandler(TObject value)
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

        internal void ClearLoadingOperation()
        {
            ClearLoadingOperation(CurrentLoadingOperationHandle);
            CurrentLoadingOperationHandle = default;
        }

        void ClearPreviousLoadingOperation()
        {
            ClearLoadingOperation(m_PreviousLoadingOperation);
            m_PreviousLoadingOperation = default;
        }

        void ClearLoadingOperation(AsyncOperationHandle<TObject> operationHandle)
        {
            if (operationHandle.IsValid())
            {
                // We should only call this if we are not done as its possible that the internal list is null if its not been used.
                if (!operationHandle.IsDone)
                    operationHandle.Completed -= m_AutomaticLoadingCompleted;
                AddressablesInterface.Release(operationHandle);
            }
        }

        /// <inheritdoc/>
        protected override void Reset()
        {
            ClearLoadingOperation();
        }

        /// <summary>
        /// Removes and releases internal references to Addressable assets.
        /// </summary>
        ~LocalizedAsset()
        {
            ClearLoadingOperation();
        }

        /// <summary>
        /// Removes and releases internal references to Addressable assets.
        /// </summary>
        void IDisposable.Dispose()
        {
            m_ChangeHandler.Clear();
            LocalizationSettings.SelectedLocaleChanged -= m_SelectedLocaleChanged;
            ClearLoadingOperation();
            GC.SuppressFinalize(this);
        }
    }
}
