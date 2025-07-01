using System;
using System.Linq;
using System.Reflection;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Types;
using Awaken.TG.Editor.SceneCaches.Items;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility;
using Awaken.Utility.LowLevel;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Headers {
    public class DataViewLootDataHeader<T> : DataViewHeader {
        const BindingFlags DefaultBindingFlags = BindingFlags.Instance |
                                                 BindingFlags.NonPublic |
                                                 BindingFlags.Public |
                                                 BindingFlags.GetField |
                                                 BindingFlags.GetProperty;
        
        readonly Type _type;
        readonly string _propertyName;
        
        public DataViewLootDataHeader(Type objectType, string name, float width, DataViewType type) : base(name, StringUtil.NicifyName(name), width, type) {
            _type = objectType;
            _propertyName = name;
            SupportEditing = false;
        }

        public override UniversalPtr CreateMetadata(IDataViewSource o) {
            var source = (DataViewLootDataSource)o;
            SceneItemSources sceneSources = LootCache.Get.sceneSources[source.SceneIndex];
            ItemSource itemSource = sceneSources.sources[source.SourceIndex];
            ItemLootData lootData = itemSource.lootData[source.LootIndex];

            MemberInfo member = _type.GetMember(_propertyName, DefaultBindingFlags).FirstOrDefault();
            object takeValueFrom = _type == typeof(ItemSource) ? itemSource : lootData;
            T value = (T)member.MemberValue(takeValueFrom);

            return UniversalPtr.CreateManaged(new LootDataMetadata {
                value = DataViewValue.Create(value)
            });
        }

        public override void FreeMetadata(ref UniversalPtr ptr) {
            ptr.FreeManaged();
        }

        public override DataViewValue GetValue(in DataViewCell cell) {
            return cell.headerMetadata.GetManaged<LootDataMetadata>().value;
        }

        public override void SetValue(in DataViewCell cell, in DataViewValue value) { }

        public override Object GetModifiedObject(in DataViewCell cell) {
            return LootCache.Get;
        }

        class LootDataMetadata {
            public DataViewValue value;
        }
    }
}