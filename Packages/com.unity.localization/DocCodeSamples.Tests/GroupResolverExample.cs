using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization.Addressables;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Places all English assets into a local group and all other languages into a remote group which could be downloaded after the game is released.
/// </summary>
[System.Serializable]
public class GroupResolverExample : GroupResolver
{
    public string localAssetsGroup = "Localization-Local";
    public string remoteAssetsGroup = "Localization-Remote";

    [MenuItem("Localization Samples/Create Group Resolver")]
    static void CreateAsset()
    {
        var path = EditorUtility.SaveFilePanelInProject("Create Addressable Rules", "Localization Addressable Group Rules.asset", "asset", "");
        if (string.IsNullOrEmpty(path))
            return;

        var instance = ScriptableObject.CreateInstance<AddressableGroupRules>();
        var resolver = new GroupResolverExample();

        // Apply our custom group resolver to everything
        instance.LocaleResolver = resolver;
        instance.AssetTablesResolver = resolver;
        instance.AssetResolver = resolver;
        instance.StringTablesResolver = resolver;

        // Make this our new AddressableGroupRules
        AssetDatabase.CreateAsset(instance, path);
        AddressableGroupRules.Instance = instance;
    }

    public override string GetExpectedGroupName(IList<LocaleIdentifier> locales, Object asset, AddressableAssetSettings aaSettings)
    {
        // Use default behaviour for shared assets
        if (locales == null || locales.Count == 0)
            return base.GetExpectedGroupName(locales, asset, aaSettings);

        var locale = locales[0];
        if (locale.Code == "en")
            return localAssetsGroup;
        return remoteAssetsGroup;
    }
}
