using System;
using System.Collections;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Utility.Assets;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    public class AddressableEntryDraft {
        public Object Obj { get; private set; }
        public string Guid { get; private set; }
        public AddressableAssetGroup Group { get; private set; }
        public List<string> Labels { get; private set; } = new List<string>();
        public Func<Object, AddressableAssetEntry, string> AddressProvider { get; private set; }

        private AddressableEntryDraft() {
            
        }
        public class Builder {

            string _group;
            AddressableEntryDraft _draft;

            public Builder(Object obj) {
                _draft = new AddressableEntryDraft();
                _draft.Obj = obj;
            }

            public Builder WithGuid(string guid) {
                _draft.Guid = guid;
                return this;
            }

            public Builder WithLabel(string label) {
                _draft.Labels.Add(label);
                return this;
            }

            public Builder WithLabels(IEnumerable<string> labels) {
                _draft.Labels.AddRange(labels);
                return this;
            }
            
            public Builder WithLabels(params string[] labels) {
                return WithLabels((IEnumerable<string>)labels);
            }

            public Builder WithAddressProvider(Func<Object, AddressableAssetEntry, string> addressProvider) {
                _draft.AddressProvider = addressProvider;
                return this;
            }

            public Builder InGroup(string group) {
                _group = group;
                return this;
            }

            public Builder InGroup(AddressableGroup group) {
                return InGroup(group.NameOf());
            }

            public AddressableEntryDraft Build() {
                _draft.Group = AddressableHelper.FindGroup(_group);
                _draft.Guid ??= AssetsUtils.ObjectToGuid(_draft.Obj);
                
                return _draft;
            }
        }
    }
}