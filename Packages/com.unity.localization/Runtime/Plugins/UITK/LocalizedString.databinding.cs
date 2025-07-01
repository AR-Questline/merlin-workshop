#if MODULE_UITK && UNITY_2023_3_OR_NEWER

using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;

namespace UnityEngine.Localization
{
    [UxmlObject]
    internal partial class LocalVariable
    {
        [UxmlAttribute]
        public string Name { get; set; }

        [UxmlObjectReference]
        public IVariable Variable { get; set; }
    }

    [UxmlObject]
    public partial class LocalizedString
    {
        List<LocalVariable> m_UxmlLocalVariables;

        [UxmlObjectReference("variables")]
        internal List<LocalVariable> LocalVariablesUXML
        {
            get => m_UxmlLocalVariables;
            set
            {
                // Remove the old variables
                m_LocalVariables.Clear();
                m_UxmlLocalVariables = value;

                if (m_UxmlLocalVariables != null)
                {
                    foreach (var v in m_UxmlLocalVariables)
                    {
                        if (v != null && !string.IsNullOrEmpty(v.Name) && v.Variable != null)
                            Add(v.Name, v.Variable);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void Initialize() => StringChanged += UpdateBindingValue;

        /// <inheritdoc/>
        protected override void Cleanup() => StringChanged -= UpdateBindingValue;

        /// <summary>
        /// Extracts the localized string from the <see cref="LocalizedString"/> and applies the value to the bound target and field.
        /// </summary>
        /// <param name="context">Context object.</param>
        /// <returns></returns>
        protected override BindingResult Update(in BindingContext context)
        {
            if (IsEmpty)
                return new BindingResult(BindingStatus.Success);

            #if UNITY_EDITOR
            // When not in playmode and not previewing a language we want to show something, so we revert to the project locale.
            if (!LocalizationSettings.Instance.IsPlaying && LocaleOverride == null && LocalizationSettings.SelectedLocale == null)
            {
                LocaleOverride = LocalizationSettings.ProjectLocale;
            }
            #endif

            if (!CurrentLoadingOperationHandle.IsDone)
                return new BindingResult(BindingStatus.Pending);

            var element = context.targetElement;
            if (ConverterGroups.TrySetValueGlobal(ref element, context.bindingId, m_CurrentStringChangedValue, out var errorCode))
                return new BindingResult(BindingStatus.Success);
            return CreateErrorResult(context, errorCode, typeof(string));
        }

        void UpdateBindingValue(string _) => MarkDirty();

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
