using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    /// <summary>
    /// Provides a popup menu in the GameView for changing the <see cref="LocalizationSettings.SelectedLocale"/>.
    /// </summary>
    [InitializeOnLoad]
    class GameViewLanguageMenu : PopupField<Locale>
    {
        static List<GameViewLanguageMenu> s_GameViews = new List<GameViewLanguageMenu>();
        static List<Locale> s_Locales = new List<Locale>();

        static GameViewLanguageMenu()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            if (Application.isPlaying && LocalizationSettings.HasSettings && LocalizationEditorSettings.ShowLocaleMenuInGameView)
                Show();
        }

        public GameViewLanguageMenu() :
            base(GetChoices(), 0)
        {
            focusable = false;
            labelElement.style.minWidth = 60;
            labelElement.style.maxWidth = 60;

            formatListItemCallback = loc => loc == null ? "None" : loc.ToString();
            formatSelectedValueCallback = loc => loc == null ? "None" : loc.ToString();
        }

        /// <summary>
        /// Enabled the menu in all GameViews.
        /// </summary>
        public static void Show()
        {
            if (!LocalizationSettings.HasSettings)
                return;

            // Don't show if we have 0 locales and are using Addressables
            if (LocalizationEditorSettings.GetLocales().Count == 0 && LocalizationSettings.AvailableLocales is LocalesProvider)
                return;

            LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;

            var initOp = LocalizationSettings.InitializationOperation;
            if (initOp.IsDone)
            {
                AddToolbarsToGameViews();
            }
            else
            {
                initOp.Completed += (hnd) => AddToolbarsToGameViews();
            }
        }

        /// <summary>
        /// Removes the menu from all GameViews.
        /// </summary>
        public static void Hide()
        {
            if (LocalizationSettings.HasSettings)
                LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
            ClearViews();
        }

        public override void SetValueWithoutNotify(Locale newValue)
        {
            var choices = GetChoices();

            // If the value is not contained in the known Locales(such as null) then we add a temp entry.
            if (!choices.Contains(newValue))
            {
                choices.Insert(0, newValue);
            }

            base.SetValueWithoutNotify(newValue);
        }

        static void PlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredPlayMode && LocalizationSettings.HasSettings && LocalizationEditorSettings.ShowLocaleMenuInGameView)
            {
                Show();
            }
            else if (s_GameViews.Count > 0)
            {
                Hide();
            }
        }

        static void ClearViews()
        {
            foreach (var gv in s_GameViews)
            {
                gv.RemoveFromHierarchy();
            }
            s_GameViews.Clear();
        }

        static void AddToolbarsToGameViews()
        {
            Assembly assembly = typeof(EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.GameView");
            var gameViews = UnityEngine.Resources.FindObjectsOfTypeAll(type);

            ClearViews();

            foreach (EditorWindow gameView in gameViews)
            {
                var menu = new GameViewLanguageMenu();
                menu.style.backgroundImage = EditorStyles.popup.normal.background;
                menu.RegisterValueChangedCallback((evt) => LocalizationSettings.SelectedLocale = evt.newValue);
                menu.style.alignSelf = Align.FlexEnd;
                menu.style.top = 22;

                gameView.rootVisualElement.Add(menu);
                menu.BringToFront();
                s_GameViews.Add(menu);
            }

            var localeOp = LocalizationSettings.SelectedLocaleAsync;
            if (!localeOp.IsDone)
            {
                OnLanguageChanged(localeOp.Result);
            }
            else
            {
                localeOp.Completed += op => OnLanguageChanged(op.Result);
            }
        }

        static List<Locale> GetChoices()
        {
            s_Locales.Clear();
            s_Locales.AddRange(LocalizationSettings.AvailableLocales.Locales);
            return s_Locales;
        }

        static void OnLanguageChanged(Locale locale)
        {
            foreach (var gv in s_GameViews)
            {
                gv.SetValueWithoutNotify(locale);
            }
        }
    }
}
