#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.PropertyVariants
{
    [InitializeOnLoad]
    static class ScenePropertyTracker
    {
        internal static bool ProcessingUndoData;

        static ScenePropertyTracker()
        {
            Undo.postprocessModifications += PostProcessModifications;
            ObjectChangeEvents.changesPublished += ChangesPublished; // 2020.2 feature
        }

        /// <summary>
        /// Checks if a tracked component was removed, if it was then remove the object tracker and
        /// create an Undo record so it can be undone with the object deletion.
        /// </summary>
        /// <param name="stream"></param>
        static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.ChangeGameObjectStructure)
                    continue;

                stream.GetChangeGameObjectStructureEvent(i, out var eventArgs);

                if (!(EditorUtility.InstanceIDToObject(eventArgs.instanceId) is GameObject goAsset
                      && goAsset.GetComponent<GameObjectLocalizer>() is {} objectLocalizer)) continue;

                var removedComponents = objectLocalizer.TrackedObjects.Where(t => t.Target == null).ToList();
                if (removedComponents.Count == 0)
                    continue;

                Undo.RecordObject(objectLocalizer, "Remove Tracked Object");
                foreach (var r in removedComponents)
                {
                    objectLocalizer.TrackedObjects.Remove(r);
                }

                if (objectLocalizer.TrackedObjects.Count == 0)
                {
                    Undo.DestroyObjectImmediate(objectLocalizer);
                }
            }
        }

        internal static UndoPropertyModification[] PostProcessModifications(UndoPropertyModification[] modifications)
        {
            if (ProcessingUndoData)
                return modifications;

            // If we detect a change to a LocalizedTable then we force a refresh to the editor preview.
            foreach (var mod in modifications)
            {
                if (mod.currentValue?.target is LocalizationTable)
                {
                    EditorApplication.delayCall += LocalizationEditorSettings.RefreshEditorPreview;
                    break;
                }
            }

            if (Application.isPlaying || !LocalizationSettings.HasSettings || LocalizationSettings.SelectedLocale == null)
                return modifications;

            // We want to create variant data in our GameObjectLocalizer however we also need to create Undo records for these changes.
            // We can not record Undo data inside of PostProcessModifications so we defer the task and use the group id so we can fold our
            // changes into this Undo operation later.
            EditorApplication.delayCall += () => DelayedPostProcessModifications(Undo.GetCurrentGroup(), Undo.GetCurrentGroupName(), modifications);

            return modifications;
        }

        static void DelayedPostProcessModifications(int undoGroup, string groupName, UndoPropertyModification[] modifications)
        {
            // We ignore Create undo events as they sometimes occur when UGUI swaps the Transform for a RectTransform.
            // When a new object is created the Undo groupName will sometimes bleed over into the next event and wont be updated until after PostProcessModifications,
            // to handle this we compare the current name against the name at the time of the event.
            if ((groupName.StartsWith("Create") && Undo.GetCurrentGroupName().StartsWith("Create")) || LocalizationSettings.SelectedLocale == null)
                return;

            var variant = LocalizationSettings.SelectedLocale.Identifier;
            var shouldAddVariant = LocalizationProjectSettings.TrackChanges && LocalizationSettings.ProjectLocale != LocalizationSettings.SelectedLocale;
            var hasChanges = false;

            try
            {
                // We don't want any Undo events processed that we may have generated, such as when we update a Localized table.
                ProcessingUndoData = true;

                foreach (var mod in modifications)
                {
                    // Ignore anything to do with GameObjectLocalizer.
                    if (mod.currentValue?.target == null || mod.currentValue.target is GameObjectLocalizer)
                        continue;

                    // Ignore anything with no previous value. (LOC-359)
                    if (mod.previousValue == null)
                        continue;

                    // Ignore values that have not actually changed. Some editors will change values even though the value is the same.
                    if (mod.currentValue.value == mod.previousValue.value && mod.currentValue.objectReference == mod.previousValue.objectReference)
                        continue;

                    // We may need to revert all the changes we make if we find the property can not be tracked
                    if (UpdateTrackedSceneProperty(mod, shouldAddVariant, variant))
                        hasChanges = true;
                }

                // Fold the changes into the undo operation that made the original change
                if (hasChanges)
                    Undo.CollapseUndoOperations(undoGroup);
            }
            finally
            {
                ProcessingUndoData = false;
            }
        }

        static bool UpdateTrackedSceneProperty(UndoPropertyModification propertyModification, bool addNewVariant, LocaleIdentifier localeIdentifier)
        {
            // We only support GameObject components. Maybe more in the future
            var targetComponent = propertyModification.currentValue.target as Component;
            if (targetComponent == null)
                return false;

            // Get/Add the GameObjectLocalizer
            var localizerAdded = false;
            var localizer = targetComponent.GetComponent<GameObjectLocalizer>();
            if (localizer == null)
            {
                if (!addNewVariant)
                    return false;
                localizerAdded = true;

                // We add here so it supports
                localizer = Undo.AddComponent<GameObjectLocalizer>(targetComponent.gameObject);
            }

            Undo.RecordObject(localizer, "Record property variant");

            // Get/Add the tracked object
            var trackedObject = localizer.GetTrackedObject(targetComponent);
            if (trackedObject == null)
            {
                if (!addNewVariant)
                    return false;

                trackedObject = TrackedObjectFactory.CreateTrackedObject(targetComponent);
                if (trackedObject == null)
                {
                    Debug.LogWarning($"Could not find a TrackedObject for {targetComponent.GetType()}.");

                    // We just destroy the object here because a reverting the Undo group would trigger the Undo callbacks and break things such that use drag operations to make changes (slider, transform etc).
                    if (localizerAdded)
                        Object.DestroyImmediate(localizer);
                    return false;
                }

                localizer.TrackedObjects.Add(trackedObject);
            }

            // Get/Add the tracked property
            var propertyPath = propertyModification.currentValue.propertyPath;
            var trackedProperty = trackedObject.GetTrackedProperty(propertyPath);
            if (trackedProperty == null)
            {
                if (!addNewVariant || !trackedObject.CanTrackProperty(propertyPath))
                {
                    // We just destroy the object here because reverting the Undo group would trigger the Undo callbacks and break things (dragging operations such as sliders, transform etc).
                    if (localizerAdded)
                        Object.DestroyImmediate(localizer);
                    return false;
                }

                trackedProperty = trackedObject.CreateCustomTrackedProperty(propertyPath) ?? TrackedObjectFactory.CreateTrackedProperty(targetComponent, propertyModification.currentValue.propertyPath);
                if (trackedProperty == null)
                {
                    // We just destroy the object here because reverting the Undo group would trigger the Undo callbacks and break things (dragging operations such as sliders, transform etc).
                    if (localizerAdded)
                        Object.DestroyImmediate(localizer);
                    return false;
                }

                // TODO: If the new property is an array size or if the array size has increased then we may want to capture the new array items.
                // The Undo event will only include the new size and not the new values in the array item.
                // We may want to just track all elements in an array when the size value is modified.
                PrepareNewTrackedProperty(propertyModification);
                trackedObject.AddTrackedProperty(trackedProperty);

                if (LocalizationSettings.ProjectLocale != null)
                    UpdateTrackedProperty(LocalizationSettings.ProjectLocale.Identifier, propertyModification.previousValue, trackedProperty);
            }

            // Dont add new variant values to a property when we are not adding new variants. (LOC-380).
            if (!trackedProperty.HasVariant(localeIdentifier) && !addNewVariant)
                return false;

            UpdateTrackedProperty(localeIdentifier, propertyModification.currentValue, trackedProperty);
            return true;
        }

        static void PrepareNewTrackedProperty(UndoPropertyModification propertyModification)
        {
            // The first time a property is modified it will not have been marked as driven.
            // This means that it will have been changed to the selected locale and the default/scene value will not have been preserved.
            // We need to revert the value back to its original, mark the property as driven and then reapply the change or the scene
            // value will be whatever locale happened to be selected when the first change was made and not the one set in LocalizationSettings.

            // Revert the value
            var so = new SerializedObject(propertyModification.currentValue.target);
            var prop = so.FindProperty(propertyModification.currentValue.propertyPath);
            prop.ApplyPropertyModification(propertyModification.previousValue);
            so.ApplyModifiedProperties();

            // If the object has just been added we can not revert.
            if (propertyModification.currentValue.target == null) return;

            // Mark the property driven and set it back to its new value
            VariantsPropertyDriver.RegisterProperty(propertyModification.currentValue.target, propertyModification.currentValue.propertyPath);
            prop.ApplyPropertyModification(propertyModification.currentValue);
            so.ApplyModifiedProperties();
        }

        static void UpdateTrackedProperty(LocaleIdentifier variant, PropertyModification propertyModification, ITrackedProperty trackedProperty)
        {
            switch (trackedProperty)
            {
                case IStringProperty stringProperty:
                    stringProperty.SetValueFromString(variant, propertyModification.value);
                    break;
                case UnityObjectProperty objectProperty:
                    objectProperty.SetValue(variant, propertyModification.objectReference);
                    break;
                case LocalizedStringProperty localizedStringProperty:
                    UpdateLocalizedStringProperty(variant, propertyModification, localizedStringProperty);
                    break;
                case LocalizedAssetProperty localizedAssetProperty:
                    UpdateLocalizedAssetProperty(variant, propertyModification, localizedAssetProperty);
                    break;
            }
        }

        static void UpdateLocalizedStringProperty(LocaleIdentifier variant, PropertyModification propertyModification, LocalizedStringProperty localizedStringProperty)
        {
            var locString = localizedStringProperty.LocalizedString;
            var collection = LocalizationEditorSettings.GetStringTableCollection(locString.TableReference);
            if (collection == null)
                return;

            var entry = collection.SharedData.GetEntryFromReference(locString.TableEntryReference);
            if (entry == null)
                return;

            var stringTable = collection.GetTable(variant) as StringTable;
            if (stringTable == null)
                return;

            Undo.RecordObject(stringTable, "Set Localized Value");
            stringTable.AddEntry(entry.Id, propertyModification.value);
            EditorUtility.SetDirty(stringTable);

            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        }

        static void UpdateLocalizedAssetProperty(LocaleIdentifier variant, PropertyModification propertyModification, LocalizedAssetProperty localizedAssetProperty)
        {
            var locObject = localizedAssetProperty.LocalizedObject;
            var collection = LocalizationEditorSettings.GetAssetTableCollection(locObject.TableReference);
            if (collection == null)
                return;

            var entry = collection.SharedData.GetEntryFromReference(locObject.TableEntryReference);
            if (entry == null)
                return;

            var assetTable = collection.GetTable(variant) as AssetTable;
            if (assetTable == null)
                return;

            if (propertyModification.objectReference != null)
                collection.AddAssetToTable(assetTable, locObject.TableEntryReference, propertyModification.objectReference, true);
            else
                collection.RemoveAssetFromTable(assetTable, locObject.TableEntryReference, true);
        }
    }
}

#endif
