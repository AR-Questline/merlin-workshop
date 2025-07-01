#if PACKAGE_UGUI

using System.Collections;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

#region localized-string-events

public class LocalizedStringWithEvents : MonoBehaviour
{
    public LocalizedString myString;

    string localizedText;

    /// <summary>
    /// Register a ChangeHandler. This is called whenever the string needs to be updated.
    /// </summary>
    void OnEnable()
    {
        myString.StringChanged += UpdateString;
    }

    void OnDisable()
    {
        myString.StringChanged -= UpdateString;
    }

    void UpdateString(string s)
    {
        localizedText = s;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField(localizedText);
    }
}
#endregion

#region localized-string-locale-override

public class LocaleOverrideExample : MonoBehaviour
{
    public LocalizedString myString = new LocalizedString("My Table", "My Entry");
    public LocaleIdentifier localeOverride = SystemLanguage.Spanish; // Override to always use Spanish

    IEnumerator Start()
    {
        // Wait for the Locales to load.
        yield return LocalizationSettings.InitializationOperation;

        // Set the LocalizedString to use the locale override instead of the selected locale.
        myString.LocaleOverride = LocalizationSettings.AvailableLocales.GetLocale(localeOverride);
    }

    void OnGUI()
    {
        GUILayout.Label(myString.GetLocalizedString());
    }
}
#endregion

#region localized-string-smart

/// <summary>
/// This example expects a Smart String with a named placeholder of `TimeNow`, such as "The time now is {TimeNow}".
/// </summary>
public class LocalizedStringSmart : MonoBehaviour
{
    public LocalizedString myString;

    string localizedText;

    public float TimeNow => Time.time;

    /// <summary>
    /// Register a ChangeHandler. This is called whenever we need to update our string.
    /// </summary>
    void OnEnable()
    {
        myString.Arguments = new[] { this };
        myString.StringChanged += UpdateString;
    }

    void OnDisable()
    {
        myString.StringChanged -= UpdateString;
    }

    void UpdateString(string s)
    {
        localizedText = s;
    }

    void OnGUI()
    {
        // This calls UpdateString immediately (if the table is loaded) or when the table is available.
        myString.RefreshString();
        GUILayout.Label(localizedText);
    }
}
#endregion

#region get-localized-string

public class LocalizedStringGetExample : MonoBehaviour
{
    public Text myText;

    public LocalizedString myString = new LocalizedString
    {
        TableReference = "My String Table",
        TableEntryReference = "My Game Text"
    };

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
        LoadString();
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
    }

    void LocaleChanged(Locale locale)
    {
        LoadString();
    }

    void LoadString()
    {
        var operation = myString.GetLocalizedStringAsync();
        UpdateString(operation);
    }

    void UpdateString(AsyncOperationHandle<string> value)
    {
        if (!value.IsDone)
        {
            // Defer the callback until the operation is finished
            value.Completed += UpdateString;
            return;
        }

        myText.text = value.Result;
    }
}
#endregion

#region get-localized-string-synchronous

public class LocalizedStringSynchronousGetExample : MonoBehaviour
{
    public Text myText;

    public LocalizedString myString = new LocalizedString
    {
        TableReference = "My String Table",
        TableEntryReference = "My Game Text"
    };

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
        LoadString();
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
    }

    void LocaleChanged(Locale locale)
    {
        LoadString();
    }

    void LoadString()
    {
        var operation = myString.GetLocalizedStringAsync();
        operation.WaitForCompletion(); // Force synchronous loading
        myText.text = operation.Result;
    }
}
#endregion

public class LocalizedStringConstructor
{
    #region localized-string-constructor

    public LocalizedString usingNames = new LocalizedString("My String Table", "My Game Text");

    public LocalizedString usingTableNameAndEntryId = new LocalizedString("My String Table", 4324324541);

    public LocalizedString usingTableGuidAndEntryId = new LocalizedString(new System.Guid("6e79ded14bc9e0a4d9bf2b8aac246bfe"), 323453434);

    public LocalizedString usingTableGuidAndEntryName = new LocalizedString(new System.Guid("6e79ded14bc9e0a4d9bf2b8aac246bfe"), "Start Game");

    public LocalizedString withLocalVariables = new LocalizedString("My String Table", "My Game Text")
    {
        // These variables will be visible in the inspector Local Variables field.
        { "some-text", new StringVariable { Value = "Hello World" } },
        { "score", new IntVariable { Value = 100 } },
        { "player-health", new FloatVariable { Value = 0.5f } },
        { "object-reference", new ObjectVariable()}, // Set via the inspector
    };

    public LocalizedString withNestedTranslation = new LocalizedString("My String Table", "My Game Text")
    {
        { "some-text", new StringVariable { Value = "Hello World" } },
        { "nested", new LocalizedString("My String Table", "My Nested Text")
          {
              { "score", new IntVariable { Value = 100 } },
          }}
    };

    #endregion

    #region localized-string-constructor-editor

    public LocalizedString GenerateLocalizedStringInEditor()
    {
        // The main advantage to using a table Guid and entry Id is that references will not be lost when changes are made to the Table name or Entry name.
        var collection = LocalizationEditorSettings.GetStringTableCollection("My String Table");
        var entry = collection.SharedData.GetEntry("Start Game");
        return new LocalizedString(collection.SharedData.TableCollectionNameGuid, entry.Id);
    }

    #endregion

    void SetTableReferenceExamples()
    {
        #region localized-string-table-reference

        var localizedString = new LocalizedString();

        // Table Reference can be set with the name of the table
        localizedString.TableReference = "My String Table";

        // Or the Table Collection Guid
        localizedString.TableReference = new System.Guid("6e79ded14bc9e0a4d9bf2b8aac246bfe");
        #endregion
    }

    void SetTableEntryReferenceExamples()
    {
        #region localized-string-table-entry-reference

        var localizedString = new LocalizedString();

        // Table Entry Reference can be set with the name of the Key
        localizedString.TableEntryReference = "Exit Game";

        // Or the Key Id
        localizedString.TableEntryReference = 342423423;
        #endregion
    }

    void SetReferenceExamples()
    {
        #region localized-string-set-reference

        var localizedString = new LocalizedString();

        // Setting with names
        localizedString.SetReference("UI Text", "Options");

        // Setting with the table name and Key Id
        localizedString.SetReference("UI Text", 3432444324);

        // Setting with the Table Name Guid and Key name
        localizedString.SetReference(new System.Guid("6e79ded14bc9e0a4d9bf2b8aac246bfe"), "Options");

        // Setting with the Table Name Guid and the Key Id
        localizedString.SetReference(new System.Guid("6e79ded14bc9e0a4d9bf2b8aac246bfe"), 3432444324);
        #endregion
    }

    void AddAndGetVariable()
    {
        #region add-get-variable

        var localizedString = new LocalizedString("My Table", "My Entry");

        // An example of a Smart String using the variable would be: "You have {player-money:C}.".
        // :C will apply the current Locale currency and number formatting.
        localizedString.Add("player-money", new FloatVariable { Value = 100.45f });

        // Get a variable from the localized string
        var variable = localizedString["player-money"] as FloatVariable;
        Debug.Log("The value is " + variable);

        #endregion
    }

    void TryGetVariable()
    {
        var localizedString = new LocalizedString("My Table", "My Entry");

        #region try-get-variable

        FloatVariable playerScore = null;
        if (!localizedString.TryGetValue("player-score", out var variable))
        {
            playerScore = new FloatVariable();
            localizedString.Add("player-score", playerScore);
        }
        else
        {
            playerScore = variable as FloatVariable;
        }

        playerScore.Value += 10;

        #endregion
    }

    void RemoveVariable()
    {
        var localizedString = new LocalizedString("My Table", "My Entry");

        #region remove-variable

        const string variableName = "my-variable";

        localizedString.Add(variableName, new FloatVariable { Value = 100.45f });
        Debug.LogFormat("Contains variable before remove: {0}", localizedString.ContainsKey(variableName));

        localizedString.Remove(variableName);
        Debug.LogFormat("Contains variable after remove: {0}", localizedString.ContainsKey(variableName));

        #endregion
    }
}

#region simple-monobehaviour1

public class ExampleMonoBehaviour1 : MonoBehaviour
{
    public LocalizedString myLocalizedString = new LocalizedString("My Table", "My Entry");

    public string TranslatedValue { get; private set; }

    void Start()
    {
        // Register to get an update when the string is changed.
        myLocalizedString.StringChanged += ValueChanged;
    }

    void ValueChanged(string value)
    {
        TranslatedValue = value;
        Debug.Log("Value changed to: " + value);
    }
}

#endregion

#region simple-monobehaviour2

public class ExampleMonoBehaviour2 : MonoBehaviour
{
    public LocalizedString myLocalizedString = new LocalizedString("My Table", "My Entry");

    public string TranslatedValue => myLocalizedString.GetLocalizedString();

    void OnGUI()
    {
        GUILayout.Label(TranslatedValue);
    }
}

#endregion

#region simple-scriptableobject

[CreateAssetMenu()]
public class GameQuest : ScriptableObject
{
    [SerializeField] LocalizedString questTitle = new LocalizedString("My Quests", "QUEST_1_TITLE");
    [SerializeField] LocalizedString questDescription = new LocalizedString("My Quests", "QUEST_1_DESCRIPTION");
    [SerializeField] LocalizedString success = new LocalizedString("My Quests", "QUEST_1_SUCCESS");
    [SerializeField] LocalizedString failure = new LocalizedString("My Quests", "QUEST_1_FAILURE");

    [SerializeField]  float rewardMoney = 100;

    public string Title => questTitle.GetLocalizedString();
    public string Description => questDescription.GetLocalizedString();
    public string Failure => failure.GetLocalizedString();

    // Return the success message with the reward money included.
    // For example:
    // "Congratulations, you saved the village! Please take this reward of {0:C} as a sign of appreciation"
    // in English(GB) would become
    // "Congratulations, you saved the village! Please take this reward of £100 as a sign of appreciation"
    public string Success => success.GetLocalizedString(rewardMoney);
}

#endregion

#region health-counter

public class HealthCounterExample : MonoBehaviour
{
    public Text uiText;
    public float delay = 5;

    // Some example English strings could be:
    // "{player-name} has {player-health} health"
    // "{player-name} {player-health:cond:<100?has {} remaing health|is at full health}"
    public LocalizedString myLocalizedString = new LocalizedString("My Table", "My Entry")
    {
        { "player-name", new StringVariable { Value = "Player 1" } },
        { "player-health", new IntVariable { Value = 100 } }
    };

    IEnumerator Start()
    {
        // Register to get an update when the string is changed.
        // This will be called every time the playerHealth variable is modified.
        myLocalizedString.StringChanged += val => uiText.text = val;

        var playerHealth = myLocalizedString["player-health"] as IntVariable;
        var wait = new WaitForSeconds(delay);

        while (playerHealth.Value > 0)
        {
            yield return wait;

            // Changing the value triggers the LocalizedString to update itself.
            playerHealth.Value -= Random.Range(1, 10);
        }
    }
}
#endregion

#endif
