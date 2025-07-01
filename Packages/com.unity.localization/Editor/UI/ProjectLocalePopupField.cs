using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    /// <summary>
    /// Field to use to select a <see cref="Locale"/> from the project.
    /// </summary>
    #if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    #endif
    public partial class ProjectLocalePopupField : PopupField<Locale>
    {
        static List<Locale> s_Locales = new List<Locale>();

        #if UNITY_2023_2_OR_NEWER
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        #endif
        public new class UxmlFactory : UxmlFactory<ProjectLocalePopupField, UxmlTraits> {}

        #if UNITY_2023_2_OR_NEWER
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        #endif
        public new class UxmlTraits : PopupField<Locale>.UxmlTraits {}

        /// <summary>
        /// Creates a new instance of the field.
        /// </summary>
        public ProjectLocalePopupField() :
            base(GetChoices(), 0, LocaleLabel, LocaleLabel)
        {
            formatListItemCallback = LocaleLabel;
            formatSelectedValueCallback = LocaleLabel;

            if (!LocalizationSettings.HasSettings)
                return;

            this.RegisterValueChangedCallback(evt => LocalizationSettings.SelectedLocale = evt.newValue);

            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;

                if (LocalizationSettings.SelectedLocaleAsync.IsDone)
                {
                    if (LocalizationSettings.SelectedLocaleAsync.Result != null)
                        SetValueWithoutNotify(LocalizationSettings.SelectedLocaleAsync.Result);
                }
                else
                {
                    LocalizationSettings.SelectedLocaleAsync.Completed += op =>
                    {
                        if (op.Result != null)
                            SetValueWithoutNotify(op.Result);
                    };
                }

                LocalizationEditorSettings.EditorEvents.LocaleAdded += LocaleAddedToProject;
                LocalizationEditorSettings.EditorEvents.LocaleRemoved += LocaleRemovedFromProject;
                EditorApplication.playModeStateChanged += PlayModeStateChanged;
            });

            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
                LocalizationEditorSettings.EditorEvents.LocaleAdded -= LocaleAddedToProject;
                LocalizationEditorSettings.EditorEvents.LocaleRemoved -= LocaleRemovedFromProject;
                EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            });
        }

        ~ProjectLocalePopupField()
        {
            // Removing this will require a major version change.
        }

        void PlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredEditMode)
            {
                SetValueWithoutNotify(LocalizationSettings.SelectedLocale);
            }
            else if (obj == PlayModeStateChange.EnteredPlayMode)
            {
                LocalizationSettings.InitializationOperation.Completed += op =>
                {
                    SetValueWithoutNotify(LocalizationSettings.SelectedLocale);
                };
            }
        }

        static string LocaleLabel(Locale locale)
        {
            if (locale == null)
                return "None";
            return locale.ToString();
        }

        static List<Locale> GetChoices()
        {
            s_Locales.Clear();
            s_Locales.Add(null);
            s_Locales.AddRange(LocalizationEditorSettings.GetLocales());
            s_Locales.AddRange(LocalizationEditorSettings.GetPseudoLocales());
            return s_Locales;
        }

        void LocaleRemovedFromProject(Locale locale)
        {
            if (value == locale)
                value = null;
            GetChoices();
        }

        void LocaleAddedToProject(Locale locale)
        {
            GetChoices();
        }

        void OnLanguageChanged(Locale locale)
        {
            if (!GetChoices().Contains(locale))
                return;

            SetValueWithoutNotify(locale);
        }

        #if UNITY_2023_2_OR_NEWER
        [EventInterest(typeof(KeyDownEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt is KeyDownEvent kde)
                HandleKeyDownEvent(kde);
        }
        #else
        /// <summary>
        /// Supports pressing up and down to change selected Locale.
        /// </summary>
        /// <param name="evt"></param>
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt is KeyDownEvent kde)
                HandleKeyDownEvent(kde);
        }
        #endif

        void HandleKeyDownEvent(KeyDownEvent kde)
        {
            // Allow users to press up and down arrows to quickly change locale.
            if (kde.keyCode == KeyCode.UpArrow)
            {
                var newIndex = index - 1;
                if (newIndex < 0)
                    newIndex = s_Locales.Count - 1;
                index = newIndex;

                // Indicate the event was used.
                // Prevents the OS from making the alert sound when an input was not handled.
                kde.StopPropagation();
            }
            else if (kde.keyCode == KeyCode.DownArrow)
            {
                var newIndex = index + 1;
                if (newIndex >= s_Locales.Count)
                    newIndex = 0;
                index = newIndex;

                // Indicate the event was used.
                // Prevents the OS from making the alert sound when an input was not handled.
                kde.StopPropagation();
            }
        }
    }
}
