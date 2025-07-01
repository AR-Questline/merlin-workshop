using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.CustomSerializers;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Locations.Attachments {
    /// <summary>
    /// Helps track groups of attachments added to a model.
    /// </summary>
    [Serializable]
    public sealed partial class AttachmentTracker {
        public ushort TypeForSerialization => SavedTypes.AttachmentTracker;

        // === State
        Model _owner;
        [Saved] MultiMap<string, Element> _attachmentsByGroup = new();
        [Saved] MultiMap<string, ushort> _removedTypes = new();
        [Saved] List<string> _disabledGroups = new();
        
        List<(string groupName, Element element)> _toListenDiscard = new();
        List<(IAttachmentGroup group, Element element, IAttachmentSpec spec)> _postponeToAdd = new();

        IAttachmentGroup _defaultTemplateGroup;

        // === Queries
        public IEnumerable<Element> GetElements(IAttachmentGroup group) => GetElements(group.AttachGroupId);
        public IEnumerable<Element> GetElements(string groupName) => _attachmentsByGroup.GetValues(groupName, true);
        public bool ContainsElement(string groupName, Element element) => _attachmentsByGroup.GetValues(groupName, true).Contains(element);

        // === Constructors
        public AttachmentTracker() { }

        public void SetOwner(Model owner) {
            _owner = owner;
        }
        
        // === Initialize
        public void Initialize(IEnumerable<IAttachmentGroup> groupsFromSource) {
            ClearDebugSet();

            IAttachmentGroup[] toInit = groupsFromSource.Where(group => group.StartEnabled).ToArray();
            _defaultTemplateGroup = toInit.FirstOrDefault(group => group.AttachGroupId == Template.DefaultAttachmentGroupName);

            foreach (var group in toInit) {
                VerifyGroupOnInit(group);
                var attachments = group.GetAttachments();
                Add(group, attachments);
            }
            
            TryTriggerPostponeAction();
            ClearDebugSet();
        }
        
        // === Restore
        public void PreRestore(IEnumerable<IAttachmentGroup> groupsFromSource) {
            IAttachmentGroup[] groups = groupsFromSource as IAttachmentGroup[] ?? groupsFromSource.ToArray();
            _defaultTemplateGroup = groups.FirstOrDefault(group => group.AttachGroupId == Template.DefaultAttachmentGroupName);
            RemoveNotExisting(groups);
            foreach (var group in groups) {
                PreRestore(group);
            }
        }
        
        void PreRestore(IAttachmentGroup group) {
            List<IAttachmentSpec> attachments = group.GetAttachments().ToList();
            string groupId = group.AttachGroupId;

            foreach (var ele in GetElements(group).ToList()) {
                IAttachmentSpec correspondingAttachment = attachments.FirstOrDefault(a => a.IsMine(ele));
                IRefreshedByAttachment refreshed = ele as IRefreshedByAttachment;

                if (ReInitElement(correspondingAttachment, refreshed)) {
                    ListenToDiscard(groupId, ele);
                } else {
                    _attachmentsByGroup.Remove(groupId, ele);
                }
                
                if (correspondingAttachment != null) {
                    // Remove attachment from further use (to avoid 2 elements taking from 1 attachment)
                    attachments.Remove(correspondingAttachment);
                }
            }

            if (group.StartEnabled && !_disabledGroups.Contains(groupId)) {
                Add(group, attachments);
                TryTriggerPostponeAction();
            }
        }
        
        /// <returns>Was element initialized</returns>
        bool ReInitElement(IAttachmentSpec correspondingAttachment, IRefreshedByAttachment refreshed) {
            if (correspondingAttachment != null && refreshed != null) {
                // Element paired with attachment
                refreshed.InitFromAttachment(correspondingAttachment, true);
            } else if (correspondingAttachment == null && refreshed != null) {
                // Not found any attachment that fits given element, let's remove it
                refreshed.DeserializationFailed();
                return false;
            }

            return true;
        }

        // === Changing attachments
        public void EnableGroup(IAttachmentGroup group, IEnumerable<IAttachmentSpec> newAttachments) {
            _disabledGroups.Remove(group.AttachGroupId);
            ResetAttachments(group.AttachGroupId);
            Add(group, newAttachments);
            TryTriggerPostponeAction();
        }

        public void DisableGroup(string groupName) {
            _disabledGroups.Add(groupName);
            ResetAttachments(groupName);
        }

        public void Add(IAttachmentGroup group, IEnumerable<IAttachmentSpec> newAttachments) {
            Add(group.AttachGroupId, PrepareElementsToSpawn(group, newAttachments).ToList());
            _owner.TriggerChange();
        }

        public void Add(string groupName, List<(IModel owner, Element element)> newElements) {
            foreach (var toAdd in newElements) {
                Add(groupName, toAdd.element, toAdd.owner);
            }
        }

        public void Add(string groupName, Element newElement, IModel owner) {
            _attachmentsByGroup.Add(groupName, newElement);
            owner.AddElement(newElement);
            _toListenDiscard.Add((groupName, newElement));

            if (!ReferenceEquals(owner, _owner)) {
                owner.TriggerChange();
            }
        }

        public void Remove(string groupName, Element element, bool manualRemoval = true) {
            if (_attachmentsByGroup.TryGetValue(groupName, out var hashSet)) {
                hashSet.Remove(element);
            }

            if (manualRemoval) {
                _removedTypes.Add(groupName, element.TypeForSerialization);
            }

            if (!element.HasBeenDiscarded) {
                element.Discard();
            }
        }

        void ListenToDiscard(string groupName, Element element) {
            if (element.HasBeenDiscarded) {
                ElementDiscarded(groupName, element);
                return;
            }
            element.ListenTo(Model.Events.BeforeDiscarded, e => {
                ElementDiscarded(groupName, (Element) e);
            });
        }

        void ElementDiscarded(string groupName, Element element) {
            if (_attachmentsByGroup.GetValues(groupName, true).Remove(element)) {
                // If element was removed by ReplaceAll or Restore, this will not be triggered
                // In all other cases of element removal, we will prevent elements of that type from spawning again
                _removedTypes.Add(groupName, element.TypeForSerialization);
            }
        }

        void ResetAttachments(string groupName) {
            // remove old state-based attachments
            HashSet<Element> toRemove = _attachmentsByGroup.GetValues(groupName, true);
            _attachmentsByGroup.RemoveAll(groupName);

            foreach (Element existing in toRemove.Where(existing => !existing.HasBeenDiscarded)) {
                existing.Discard();
            }
        }

        /// <summary>
        /// Postpone some actions to be triggered after the owner and its elements are fully initialized
        /// </summary>
        void TryTriggerPostponeAction() {
            if (_postponeToAdd.Count > 0) {
                _owner.AfterFullyInitialized(AddElementsWithOverrideOwner);
            }

            if (_toListenDiscard.Count > 0) {
                _owner.AfterFullyInitialized(AttachListenToDiscard);
            }
        }

        void AddElementsWithOverrideOwner() {
            bool anyChangedHappened;

            // At each step of the loop, there will be an attempt to find an owner for elements and initialize those elements
            // If any element was initialized, we will repeat the loop
            // Because sometimes the element owner is initialized in the previous loop step
            do {
                anyChangedHappened = false;

                for (int i = _postponeToAdd.Count - 1; i >= 0; i--) {
                    (IAttachmentGroup group, Element element, IAttachmentSpec spec) elementData = _postponeToAdd[i];

                    if (TryGetOwner(elementData.group, elementData.spec, out IModel owner)) {
                        Add(elementData.group.AttachGroupId, elementData.element, owner);
                        anyChangedHappened = true;

                        _postponeToAdd.RemoveAt(i);
                    }
                }
            } while (anyChangedHappened);
            
            // Throw an exception if there are any uninitialized elements with an overriding owner at the end of the process
            // This means that the owner of the element was not found
            if (_postponeToAdd.Count > 0) {
                foreach (var toAdd in _postponeToAdd) {
                    Log.Critical?.Error($"Attachment spec {toAdd.spec.GetType()} with override owner was not initialized at {nameof(AttachmentTracker)} of {_owner.ID}"); 
                }

                throw new InvalidOperationException("Not all elements with override owner were initialized. Check logs for more info.");
            }
        }
        
        void AttachListenToDiscard() {
            foreach (var toListen in _toListenDiscard) {
                ListenToDiscard(toListen.groupName, toListen.element);
            }
            _toListenDiscard.Clear();
        }
        
        IEnumerable<(IModel, Element)> PrepareElementsToSpawn(IAttachmentGroup group, IEnumerable<IAttachmentSpec> specs) {
            foreach (var spec in specs) {
                if (TrySpawnElement(group.AttachGroupId, spec, out Element element)) {
                    if (TryGetOwner(group, spec, out IModel owner)) {
                        yield return (owner, element);
                    } else {
                        _postponeToAdd.Add((group, element, spec));
                    }
                }
            }
        }

        bool TryGetOwner(IAttachmentGroup group, IAttachmentSpec spec, out IModel owner) {
            // If the owner is _owner of the tracker, we don't need to find it
            if (spec.IsValidOwner(_owner)) {
                owner = _owner;
                return true;
            }
            
            // If the owner is overridden, we need to find it in current group or default one
            owner = GetOwner(group.AttachGroupId);
            owner ??= _defaultTemplateGroup != null && !_defaultTemplateGroup.Equals(group) ? GetOwner(_defaultTemplateGroup.AttachGroupId) : null;
            return owner != null;
            
            IModel GetOwner(string groupName) {
                return GetElements(groupName).FirstOrDefault(element => !element.WasDiscarded && spec.IsValidOwner(element));
            }
        }

        bool TrySpawnElement(string groupId, IAttachmentSpec spec, out Element element) {
            element = spec.SpawnElement();

            // Don't add elements that were already removed
            if (_removedTypes.GetValues(groupId, true).Contains(element.TypeForSerialization)) {
                return false;
            }

            if (element is IRefreshedByAttachment refreshed) {
                refreshed.InitFromAttachment(spec, false);
            }

            return true;
        }

        void RemoveNotExisting(IAttachmentGroup[] groupsFromSource) {
            foreach (string group in _attachmentsByGroup.Keys.ToList()) {
                if (groupsFromSource.All(g => g.AttachGroupId != group)) {
                    // Group doesn't exist in source, remove it whole
                    foreach (Element existing in _attachmentsByGroup.GetValues(group, true)) {
                        existing.DeserializationFailed();
                    }

                    _attachmentsByGroup.RemoveAll(group);
                } else {
                    // Remove only nulls and discarded elements
                    _attachmentsByGroup.GetValues(group, true).RemoveWhere(e => e is not { HasBeenDiscarded: false });
                }
            }
        }

#if UNITY_EDITOR || AR_DEBUG
        void OnBeforeWorldSerialize() {
            foreach (var set in _attachmentsByGroup.Values) {
                foreach (var element in set) {
                    if (element.IsNotSaved) {
                        throw new InvalidOperationException($"Element {element.GetType()} is not saved. Elements refreshed by attachments have to be saved");
                    }
                }
            }
        }
#endif

        // === Debug
        static HashSet<string> s_debugSet = new();

        [Conditional("DEBUG")]
        void ClearDebugSet() {
            s_debugSet.Clear();
        }

        [Conditional("DEBUG")]
        void VerifyGroupOnInit(IAttachmentGroup group) {
            if (!s_debugSet.Add(group.AttachGroupId)) {
                string additionalData = "";
                GameObject obj = null;
                if (_owner is Location loc) {
                    additionalData = loc.Spec.gameObject.PathInSceneHierarchy();
                    obj = loc.Spec.gameObject;
                } else if (_owner is Item item) {
                    additionalData = $"Guid: {item.Template.GUID}";
                    obj = item.Template?.gameObject;
                } else if (_owner is Quest quest) {
                    additionalData = $"Guid: {quest.Template.GUID}";
                    obj = quest.Template.gameObject;
                } else if (_owner is Objective objective) {
                    additionalData = $"Quest Guid: {objective.ParentModel.Template.GUID}";
                    obj = objective.ParentModel.Template.gameObject;
                }

                Log.Important?.Error($"Exception below happened for {obj?.name}", obj);
                throw new ArgumentException($"There are two copies of Attachment Group with identical Id ({group.AttachGroupId}). More info: {additionalData}");
            }
        }
    }
}