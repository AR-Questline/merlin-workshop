# Upgrade guides

## Upgrade Guide to 1.4.2

- This version improves sub-asset support. A new rule has been added to the **Asset Table Addressables Analyzer** that checks if sub-assets are referenced by name and offers to upgrade them. This will help avoid the wrong asset being returned when referencing an asset that contains multiple sub-assets.

## Upgrade Guide to 1.3.0

- `Locale.GetFallback` has been replaced with `Locale.GetFallbacks` in order to support multiple fallbacks per locale. This will now return a list of fallbacks which can then be attempted in order.

## Upgrade Guide to 1.1.0

- `LocalizationEditorSettings.FindSimilarKey` has been deprecated. In Unity 2021.2 and above, you can use Unity Search as an alternative. See [TableEntrySearchData](xref:UnityEditor.Localization.Search.TableEntrySearchData) for further details.
- To configure your Android and iOS builds correctly, add `App Info` to the **Localization Settings** for the platform.
If `App Info` isn't included, the Unity Editor might not detect the available languages when attempting to select the system locale. The Editor displays a warning if `App Info` isn't included.

## Upgrade Guide to 1.0.0

- `LocalizedStringEvent` arguments have been deprecated. If your project uses this argument, the Inspector displays a warning and offers to upgrade by transferring them into the String Reference Local Variables field. Note the Smart String will need to be updated to use the local variable name. For example `{appleCount}` would need to be changed to `{local-variable-name.appleCount}`.
- `GlobalVariables` namespace has been renamed to `PersistentVariables` and associated scripts have also been renamed. The [API Updater](https://docs.unity3d.com/Manual/APIUpdater.html) automatically upgrades relevant scripts.
- `IGlobalVariable/IVariable` interface has changed. `object SourceValue { get; }` property has been changed into a method `object GetSourceValue(ISelectorInfo selector);`. This will need to be fixed manually. In most cases the `selector` value can be ignored however it can also be used to provide context to the current format operation to allow for more control over the returned value.
- Types marked with `ObsoleteAttribute` are no longer shown in dropdown menus.
