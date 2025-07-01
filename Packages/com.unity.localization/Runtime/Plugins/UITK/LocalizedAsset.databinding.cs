#if MODULE_UITK && UNITY_2023_3_OR_NEWER

using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace UnityEngine.Localization
{
    [UxmlObject]
    public partial class LocalizedAssetBase { }

    #if MODULE_AUDIO || PACKAGE_DOCS_GENERATION
    [UxmlObject]
    public partial class LocalizedAudioClip { }
    #endif

    [UxmlObject]
    public partial class LocalizedGameObject { }

    [UxmlObject]
    public partial class LocalizedMesh { }

    [UxmlObject]
    public partial class LocalizedMaterial { }

    [UxmlObject]
    public partial class LocalizedObject { }

    [UxmlObject]
    public partial class LocalizedSprite { }

    [UxmlObject]
    public partial class LocalizedTexture
    {
        protected override BindingResult ApplyDataBindingValue(in BindingContext context, Texture value)
        {
            // Some attributes expect Texture2D type so we need to convert it otherwise it requires a custom converter.
            if (value is Texture2D texture2D)
                return SetDataBindingValue(context, texture2D);
            return base.ApplyDataBindingValue(context, value);
        }
    }

    #if PACKAGE_TMP || (UNITY_2023_2_OR_NEWER && PACKAGE_UGUI) || PACKAGE_DOCS_GENERATION
    [UxmlObject]
    public partial class LocalizedTmpFont { }
    #endif

    [UxmlObject]
    public partial class LocalizedFont { }

    [UxmlObject]
    public partial class LocalizedAsset<TObject>
    {
        TObject m_CurrentValue;

        /// <inheritdoc/>
        protected override void Initialize() => AssetChanged += UpdateBindingValue;

        /// <inheritdoc/>
        protected override void Cleanup() => AssetChanged -= UpdateBindingValue;

        protected override BindingResult Update(in BindingContext context)
        {
            if (IsEmpty)
                return new BindingResult(BindingStatus.Success);

            #if UNITY_EDITOR
            // When not in playmode and not previewing a language we want to show something, so we revert to the project locale.
            if (!Application.isPlaying && LocaleOverride == null && LocalizationSettings.SelectedLocale == null)
            {
                LocaleOverride = LocalizationSettings.ProjectLocale;
            }
            #endif

            if (!CurrentLoadingOperationHandle.IsDone)
                return new BindingResult(BindingStatus.Pending);
            return ApplyDataBindingValue(context, m_CurrentValue);
        }

        /// <summary>
        /// Applies the <paramref name="value"/> to the bound target and field.
        /// </summary>
        /// <param name="context">Context object.</param>
        /// <param name="value">The value to apply to the binding.</param>
        protected virtual BindingResult ApplyDataBindingValue(in BindingContext context, TObject value)
        {
            return SetDataBindingValue(context, value);
        }

        internal BindingResult SetDataBindingValue<T>(in BindingContext context, T value)
        {
            var element = context.targetElement;
            if (ConverterGroups.TrySetValueGlobal(ref element, context.bindingId, value, out var errorCode))
                return new BindingResult(BindingStatus.Success);
            return CreateErrorResult(context, errorCode, typeof(TObject));
        }

        void UpdateBindingValue(TObject value)
        {
            m_CurrentValue = value;
            MarkDirty();
        }

        #if UNITY_EDITOR
        void HandleLocaleChangeDataBinding(Locale locale)
        {
            if (locale != null)
            {
                // We now have a locale so revert the editor override.
                m_LocaleOverride = null;
            }
            MarkDirty();
        }
        #endif
    }

}

#endif
