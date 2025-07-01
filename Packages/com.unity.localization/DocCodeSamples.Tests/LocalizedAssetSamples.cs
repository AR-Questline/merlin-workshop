using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

#if PACKAGE_UGUI

using UnityEngine.UI;

#region localized-text-font

public class LocalizedTextWithFont : MonoBehaviour
{
    [Serializable]
    public class LocalizedFont : LocalizedAsset<Font> {}

    public LocalizedString localizedString;
    public LocalizedFont localizedFont;

    public Text uiText;

    void OnEnable()
    {
        localizedString.StringChanged += UpdateText;
        localizedFont.AssetChanged += FontChanged;
    }

    void OnDisable()
    {
        localizedString.StringChanged -= UpdateText;
        localizedFont.AssetChanged -= FontChanged;
    }

    void FontChanged(Font f)
    {
        uiText.font = f;
    }

    void UpdateText(string s)
    {
        uiText.text = s;
    }
}
#endregion

#region localized-sprite

public class LocalizedSpriteExample : MonoBehaviour
{
    public LocalizedSprite localizedSprite;

    public Image image;

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += SelectedLocaleChanged;
        StartCoroutine(LoadAssetCoroutine());
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= SelectedLocaleChanged;
    }

    void SelectedLocaleChanged(Locale obj)
    {
        StartCoroutine(LoadAssetCoroutine());
    }

    IEnumerator LoadAssetCoroutine()
    {
        var operation = localizedSprite.LoadAssetAsync();
        yield return operation;
        image.sprite = operation.Result;
    }
}
#endregion

#region localized-prefab

public class LocalizedPrefabExample : MonoBehaviour
{
    [Serializable]
    public class LocalizedPrefab : LocalizedAsset<GameObject> {}

    public LocalizedPrefab localizedPrefab;

    GameObject currentInstance;

    void OnEnable()
    {
        localizedPrefab.AssetChanged += UpdatePrefab;
    }

    void OnDisable()
    {
        localizedPrefab.AssetChanged -= UpdatePrefab;
    }

    void UpdatePrefab(GameObject value)
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        currentInstance = Instantiate(value);
    }
}
#endregion

#endif

#region localized-scriptable-object

[CreateAssetMenu]
public class MyScriptableObject : ScriptableObject
{
    public string someStringValue;
}

[Serializable]
public class LocalizedMyScriptableObject : LocalizedAsset<MyScriptableObject> { }

public class Example : MonoBehaviour
{
    public LocalizedMyScriptableObject localizedScriptableObject;

    void OnEnable()
    {
        localizedScriptableObject.AssetChanged += OnAssetChanged;
    }

    void OnDisable()
    {
        localizedScriptableObject.AssetChanged -= OnAssetChanged;
    }

    void OnAssetChanged(MyScriptableObject value)
    {
        Debug.Log(value.someStringValue);
    }
}
#endregion

#region override-asset-entry-1

public class UpdateAssetTableExample : MonoBehaviour
{
    public LocalizedAssetTable myAssetTable = new LocalizedAssetTable("My Asset Table");

    public Texture englishTexture;
    public Texture frenchTexture;

    void OnEnable()
    {
        myAssetTable.TableChanged += UpdateTable;
    }

    void OnDisable()
    {
        myAssetTable.TableChanged -= UpdateTable;
    }

    Texture GetTextureForLocale(LocaleIdentifier localeIdentifier)
    {
        if (localeIdentifier.Code == "en")
            return englishTexture;
        else if (localeIdentifier == "fr")
            return frenchTexture;
        return null;
    }

    void UpdateTable(AssetTable value)
    {
        var entry = value.GetEntry("My Table Entry") ?? value.AddEntry("My Table Entry", string.Empty);
        entry.SetAssetOverride(GetTextureForLocale(value.LocaleIdentifier));
    }
}
#endregion

#region override-asset-entry-2

public class OverrideAllAssetTables : MonoBehaviour
{
    public LocalizedAssetTable myAssetTable = new LocalizedAssetTable("My Asset Table");

    public Texture englishTexture;
    public Texture frenchTexture;

    IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            var table = LocalizationSettings.AssetDatabase.GetTableAsync(myAssetTable.TableReference, locale);

            // Acquire a reference to the table. This will prevent the table from unloading until we have released it with Addressables.Release.
            Addressables.ResourceManager.Acquire(table);

            yield return table;
            UpdateTable(table.Result);
        }
    }

    Texture GetTextureForLocale(LocaleIdentifier localeIdentifier)
    {
        if (localeIdentifier.Code == "en")
            return englishTexture;
        else if (localeIdentifier == "fr")
            return frenchTexture;
        return null;
    }

    void UpdateTable(AssetTable value)
    {
        var entry = value.GetEntry("My Table Entry") ?? value.AddEntry("My Table Entry", string.Empty);
        entry.SetAssetOverride(GetTextureForLocale(value.LocaleIdentifier));
    }
}
#endregion
