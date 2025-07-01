using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

#region asynchronous

public class InitializationOperationExampleAsync : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        Debug.Log("Initialization Completed");
    }
}
#endregion

#region asynchronous-event

public class InitializationOperationExampleAsyncEvent : MonoBehaviour
{
    void Start()
    {
        var init = LocalizationSettings.InitializationOperation;
        init.Completed += a => Debug.Log("Initialization Completed");
    }
}
#endregion

#region synchronous

public class InitializationOperationExampleSync : MonoBehaviour
{
    void Start()
    {
        // Force initialization to complete synchronously.
        LocalizationSettings.InitializationOperation.WaitForCompletion();
    }
}
#endregion

#region selected-locale-changed

public class SelectedLocaleChangedExample : MonoBehaviour
{
    Locale currentLocale;

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    IEnumerable Start()
    {
        // Get the initial selected locale value
        var selectedLocale = LocalizationSettings.SelectedLocaleAsync;
        yield return selectedLocale;
        currentLocale = selectedLocale.Result;
    }

    void OnSelectedLocaleChanged(Locale locale)
    {
        currentLocale = locale;
    }

    void OnGUI()
    {
        if (currentLocale != null)
            GUILayout.Label("The current locale is " + currentLocale.LocaleName);
    }
}
#endregion
