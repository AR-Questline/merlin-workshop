using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

#region preload-example

public class PreloadingSample : MonoBehaviour
{
    IEnumerator Start()
    {
        // Tables that are not marked as Preload can be manually preloaded.
        var preloadOperation = LocalizationSettings.StringDatabase.PreloadTables(new TableReference[] { "UI Text", "Game Text" });
        yield return preloadOperation;

        // Get some text from the table, this will be immediately available now the table has been preloaded
        var uiText = LocalizationSettings.StringDatabase.GetTableEntryAsync("UI Text", "Start_Game").Result;
        Debug.Log(uiText);
    }
}

#endregion

#region release-example

public class ReleaseSample : MonoBehaviour
{
    AsyncOperationHandle<StringTable> m_Table;

    IEnumerator Start()
    {
        TableReference tableReference = "My Game Text";
        m_Table = LocalizationSettings.StringDatabase.GetTableAsync(tableReference);

        yield return m_Table;

        // To prevent a table from being released we can acquire a reference to it.
        // Now we will always keep this table, even if the Selected Locale is changed.
        Addressables.ResourceManager.Acquire(m_Table);

        // We can tell the Localization system to release references to the table.
        LocalizationSettings.StringDatabase.ReleaseTable(tableReference);
    }

    private void OnDisable()
    {
        // To release the table we call Release.
        Addressables.Release(m_Table);
    }
}

#endregion

public class GetLocalizedStringExamples
{
    public void GetLocalizedStringAsyncExample1()
    {
        #region get-string-async

        var stringOperation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("My String");
        stringOperation.Completed += s =>
        {
            Debug.Log("String loaded: " + s);
        };
        #endregion
    }

    public void GetLocalizedStringAsyncExample2()
    {
        #region get-string-async-2

        var stringOperation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("My Table", "My String");
        stringOperation.Completed += s =>
        {
            Debug.Log("String loaded: " + s);
        };
        #endregion
    }

    public void GetLocalizedStringAsyncWithArgumentExample()
    {
        #region get-string-argument-async

        // Example string: "The value is {0}"
        var stringOperation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("My String", new object[] { 123 });
        stringOperation.Completed += s =>
        {
            // Example output: "The value is 123"
            Debug.Log(s.Result);
        };
        #endregion
    }

    public void GetLocalizedStringAsyncWithSmartArgumentExample()
    {
        #region get-string-smart-argument-async

        // Prepare Smart String Arguments.
        var dictionary = new Dictionary<string, string>();
        dictionary.Add("title", "General");
        dictionary.Add("name", "Radahn");

        // Example string: "I am {title} {name}!"
        var stringOperation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("Main Characters", "General_Radahn_Intro", new object[] { dictionary });
        stringOperation.Completed += s =>
        {
            // Example output: "I am General Radahn!"
            Debug.Log(s.Result);
        };
        #endregion
    }

    public void GetLocalizedStringNoFallbackExample()
    {
        #region get-string-smart-argument

        // Prepare Smart String Arguments.
        var dictionary = new Dictionary<string, string>();
        dictionary.Add("title", "General");
        dictionary.Add("name", "Radahn");

        // Example string: "I am {title} {name}!"
        var localizedString = LocalizationSettings.StringDatabase.GetLocalizedString("Main Characters", "General_Radahn_Intro", new object[] { dictionary });

        // Example output: "I am General Radahn!"
        Debug.Log(localizedString);
        #endregion
    }

    #region get-string-coroutine-async

    IEnumerator LoadStringWithCoroutine()
    {
        var stringOperation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("My String");

        // We can skip waiting if the operation is already done.
        if (!stringOperation.IsDone)
            yield return stringOperation;

        Debug.Log(stringOperation.Result);
    }
    #endregion

    public void GetLocalizedStringAsyncWaitForCompletion()
    {
        #region get-string-custom-locale-async

        var customLocale = LocalizationSettings.AvailableLocales.GetLocale("fr");

        var stringOperation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("My String", customLocale);

        // We can force the operation to complete.
        if (!stringOperation.IsDone)
            stringOperation.WaitForCompletion();

        Debug.Log(stringOperation.Result);
        #endregion
    }

    public void GetLocalizedStringExample()
    {
        #region get-string

        var localizedString = LocalizationSettings.StringDatabase.GetLocalizedString("My Table", "My String");
        Debug.Log(localizedString);
        #endregion
    }

    #region translation-not-found

    public void SubscribeToTranslationNotFound()
    {
        LocalizationSettings.StringDatabase.TranslationNotFound += StringDatabase_TranslationNotFound;
    }

    void StringDatabase_TranslationNotFound(string key, long keyId, TableReference tableReference, StringTable table, UnityEngine.Localization.Locale locale, string noTranslationFoundMessage)
    {
        Debug.Log($"Translation Not Found for {key} in {table?.TableCollectionName} for {locale}");
    }
    #endregion

    public IEnumerator GetAllTablesExampleAsync()
    {
        #region get-all-tables-async

        var tables = LocalizationSettings.StringDatabase.GetAllTables();
        yield return tables;

        foreach (var table in tables.Result)
        {
            Debug.Log("Loaded " + table.TableCollectionName);
        }
        #endregion
    }

    public void GetAllTablesExample()
    {
        #region get-all-tables

        var tables = LocalizationSettings.StringDatabase.GetAllTables();
        tables.WaitForCompletion();

        foreach (var table in tables.Result)
        {
            Debug.Log("Loaded " + table.TableCollectionName);
        }
        #endregion
    }
}
