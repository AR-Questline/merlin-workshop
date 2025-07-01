using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Editor.DataViews.Headers;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.SceneCaches.Items;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Data {
    public class DataViewArchetype : RichEnum {
        static readonly List<IDataViewSource> ReusableSources = new();
        
        public readonly Func<IDataViewSource[]> getCorrespondingObjects;

        readonly HeaderProvider[] _headerProviders;

        DataViewHeader[] _headers;
        public DataViewHeader[] Headers => _headers ??= GetHeaders().ToArray();

        [UsedImplicitly] public static readonly DataViewArchetype
            Armors = new(nameof(Armors),
                GetTemplates<ItemTemplate>(template => template.IsArmor, Has<ItemEquipSpec>, Has<ItemStatsAttachment>),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemEquipSpecHeaders,
                DataViewHeaders.ItemStatsAttachmentHeaders,
                DataViewHeaders.ItemTagsHeaders,
                DataViewHeaders.ItemEffectsHeaders
            ),
            Equippable = new(nameof(Equippable),
                GetTemplates<ItemTemplate>(Has<ItemEquipSpec>, Has<ItemStatsAttachment>),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemEquipSpecHeaders,
                DataViewHeaders.ItemStatsAttachmentHeaders,
                DataViewHeaders.ItemTagsHeaders
            ),
            Garbage = new(nameof(Garbage),
                GetTemplates<ItemTemplate>(template => template.Quality == ItemQuality.Garbage),
                DataViewHeaders.ItemTemplateHeaders
            ),
            Gems = new(nameof(Gems),
                GetTemplates<ItemTemplate>(Has<GemAttachment>),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.GemAttachmentHeaders,
                DataViewHeaders.ItemTagsHeaders
            ),
            Items = new(nameof(Items),
                GetTemplates<ItemTemplate>(),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemTagsHeaders
            ),
            Jewelery = new(nameof(Jewelery),
                GetTemplates<ItemTemplate>(template => template.IsJewelry, Has<ItemEquipSpec>),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemEquipSpecHeaders,
                DataViewHeaders.ItemTagsHeaders
            ),
            Npcs = new(nameof(Npcs),
                GetTemplates<NpcTemplate>(),
                DataViewHeaders.NpcTemplateHeaders,
                DataViewHeaders.NpcTagsHeaders
            ),
            Potions = new(nameof(Potions),
                GetTemplates<ItemTemplate>(template => template.IsPotion),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemTagsHeaders,
                DataViewHeaders.ItemEffectsHeaders
            ),
            Consumables = new(nameof(Consumables),
                GetTemplates<ItemTemplate>(template => template.IsConsumable),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemTagsHeaders,
                DataViewHeaders.ItemEffectsHeaders
            ),
            Weapons = new(nameof(Weapons),
                GetTemplates<ItemTemplate>(template => template.IsWeapon, Has<ItemEquipSpec>, Has<ItemStatsAttachment>),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemEquipSpecHeaders,
                DataViewHeaders.ItemStatsAttachmentHeaders,
                DataViewHeaders.ItemTagsHeaders,
                DataViewHeaders.ItemEffectsHeaders
            ),
            WeaponsMagic = new(nameof(WeaponsMagic),
                GetTemplates<ItemTemplate>(template => template.IsWeapon && template.IsMagic, Has<ItemEquipSpec>, Has<ItemStatsAttachment>),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemEquipSpecHeaders,
                DataViewHeaders.ItemStatsAttachmentHeaders,
                DataViewHeaders.ItemTagsHeaders,
                DataViewHeaders.ItemEffectsHeaders
            ),
            WeaponsMagicProjectiles = new(nameof(WeaponsMagicProjectiles),
                GetTemplates<ItemTemplate>(template => template.IsWeapon && template.IsMagic, Has<ItemEquipSpec>, Has<ItemStatsAttachment>, Has<ItemProjectileAttachment>),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemEquipSpecHeaders,
                DataViewHeaders.ItemStatsAttachmentHeaders,
                DataViewHeaders.ItemTagsHeaders,
                DataViewHeaders.ItemEffectsHeaders,
                DataViewHeaders.ItemProjectileHeaders
            ),
            WeaponsMelee = new(nameof(WeaponsMelee),
                GetTemplates<ItemTemplate>(template => template.IsWeapon && template.IsMelee, Has<ItemEquipSpec>, Has<ItemStatsAttachment>),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemEquipSpecHeaders,
                DataViewHeaders.ItemStatsAttachmentHeaders,
                DataViewHeaders.ItemTagsHeaders,
                DataViewHeaders.ItemEffectsHeaders
            ),
            WeaponsRange = new(nameof(WeaponsRange),
                GetTemplates<ItemTemplate>(template => (template.IsWeapon && template.IsRanged), Has<ItemEquipSpec>, Has<ItemStatsAttachment>),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemEquipSpecHeaders,
                DataViewHeaders.ItemStatsAttachmentHeaders,
                DataViewHeaders.ItemTagsHeaders,
                DataViewHeaders.ItemEffectsHeaders
            ),
            WeaponsProjectile = new(nameof(WeaponsProjectile),
                GetTemplates<ItemTemplate>(template => template.IsArrow || template.IsThrowable, Has<ItemEquipSpec>, Has<ItemStatsAttachment>, Has<ItemProjectileAttachment>),
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemEquipSpecHeaders,
                DataViewHeaders.ItemStatsAttachmentHeaders,
                DataViewHeaders.ItemTagsHeaders,
                DataViewHeaders.ItemProjectileHeaders
            ),
            Talents = new(nameof(Talents),
                GetTemplates<TalentTemplate>(),
                DataViewHeaders.TalentHeaders
            ),
            LootCaches = new(nameof(LootCaches),
                GetLootCacheData(),
                DataViewHeaders.LootCacheHeaders,
                DataViewHeaders.ItemTemplateHeaders,
                DataViewHeaders.ItemEquipSpecHeaders,
                DataViewHeaders.ItemStatsAttachmentHeaders,
                DataViewHeaders.ItemTagsHeaders,
                DataViewHeaders.ItemEffectsHeaders
            ),
            Quests = new(nameof(Quests),
                GetQuestsData(objectives: false),
                DataViewHeaders.QuestHeaders
            ),
            QuestObjectives = new(nameof(QuestObjectives),
                GetQuestsData(objectives: true),
                DataViewHeaders.QuestObjectivesHeaders
            ),
            Statuses = new(nameof(Statuses),
                GetTemplates<StatusTemplate>(),
                DataViewHeaders.StatusTemplateHeaders
            );

        DataViewArchetype(string name, Func<IDataViewSource[]> getCorrespondingObjects, params HeaderProvider[] headers) : base(name) {
            this.getCorrespondingObjects = getCorrespondingObjects;
            _headerProviders = headers;
        }

        IEnumerable<DataViewHeader> GetHeaders() {
            foreach (var provider in _headerProviders) {
                if (provider.dynamic) {
                    foreach (var header in provider.dynamicHeaders()) {
                        if (header != null) {
                            yield return header;
                        }
                    }
                } else {
                    foreach (var header in provider.staticHeaders) {
                        if (header != null) {
                            yield return header;
                        }
                    }
                }
            }
        }

        static Func<IDataViewSource[]> GetTemplates<TTemplate>(params Func<TTemplate, bool>[] filters) where TTemplate : Object, ITemplate {
            return () => {
                var templates = TemplatesSearcher.FindAllOfType<TTemplate>();
                ReusableSources.Clear();
                ReusableSources.EnsureCapacity(templates.Count);
                foreach (var template in templates) {
                    if (filters.All(f => f(template))) {
                        ReusableSources.Add(new DataViewSource(template));
                    }
                }
                var array = ReusableSources.ToArray();
                ReusableSources.Clear();
                return array;
            };
        }

        static Func<IDataViewSource[]> GetLootCacheData() {
            return () => {
                ReusableSources.Clear();
                for (int i = 0; i < LootCache.Get.sceneSources.Count; i++) {
                    SceneItemSources scene = LootCache.Get.sceneSources[i];
                    for (int j = 0; j < scene.sources.Count; j++) {
                        ItemSource itemSource = scene.sources[j];
                        for (int k = 0; k < itemSource.lootData.Count; k++) {
                            ItemLootData lootData = itemSource.lootData[k];
                            if (lootData.Template != null) {
                                ReusableSources.Add(new DataViewLootDataSource(lootData.Template, i, j, k));
                            }
                        }
                    }
                }
                var array = ReusableSources.ToArray();
                ReusableSources.Clear();
                return array;
            };
        }

        static Func<IDataViewSource[]> GetQuestsData(bool objectives) {
            return () => {
                var templates = TemplatesSearcher.FindAllOfType<QuestTemplateBase>();
                ReusableSources.Clear();
                ReusableSources.EnsureCapacity(templates.Count);
                foreach (var template in templates) {
                    if (objectives) {
                        using var objectiveSpecs = template.ObjectiveSpecs;
                        foreach (var objective in objectiveSpecs.value) {
                            ReusableSources.Add(new DataViewQuestObjectiveSource(template, objective));
                        }
                    } else {
                        ReusableSources.Add(new DataViewQuestSource(template));
                    }
                }
                var array = ReusableSources.ToArray();
                ReusableSources.Clear();
                return array;
            };
        }

        static bool Has<TComponent>(Component component) where TComponent : Component {
            return component.GetComponent<TComponent>() != null;
        }

        public static void RefreshHeaderCache() {
            foreach (var archetype in RichEnum.AllValuesOfType<DataViewArchetype>()) {
                archetype._headers = null;
            }
        }

        public readonly struct HeaderProvider {
            public readonly bool dynamic;
            public readonly DataViewHeader[] staticHeaders;
            public readonly Func<IEnumerable<DataViewHeader>> dynamicHeaders;

            HeaderProvider(bool dynamic) : this() {
                this.dynamic = dynamic;
            }

            public HeaderProvider(Func<IEnumerable<DataViewHeader>> dynamicHeaders) : this(true) {
                this.dynamicHeaders = dynamicHeaders;
            }

            public HeaderProvider(DataViewHeader[] staticHeaders) : this(false) {
                this.staticHeaders = staticHeaders;
            }

            public static implicit operator HeaderProvider(DataViewHeader[] staticHeaders) {
                return new HeaderProvider(staticHeaders);
            }
        }
    }
}