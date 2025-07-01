using System;
using System.Collections.Generic;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Locations.NoticeBoards {
    public partial class NoticeBoard : Element<Location>, IRefreshedByAttachment<NoticeBoardAttachment> {
        public override ushort TypeForSerialization => SavedModels.NoticeBoard;

        NoticeBoardAttachment _spec;

        [Saved] List<ItemTemplate> _pickedItems = new();
        
        NoticeQueue[] Notices => _spec.notices;
        
        public void InitFromAttachment(NoticeBoardAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            foreach (ref var notice in Notices.RefIterator()) {
                SpawnNextNotice(ref notice);
            }
            
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.QuestStateChanged, this, QuestStateChanged);
        }

        void QuestStateChanged(QuestUtils.QuestStateChange stateChange) {
            if (stateChange.newState is not (QuestState.Completed or QuestState.Failed)) {
                return;
            }
            foreach (ref var notice in Notices.RefIterator()) {
                if (notice.currentQuest == stateChange.quest.Template) {
                    notice.currentQuest = null;
                    notice.currentLocation?.Discard();
                    notice.currentLocation = null;
                    SpawnNextNotice(ref notice);
                }
            }
        }

        void SpawnNextNotice(ref NoticeQueue queue) {
            var memory = World.Services.Get<GameplayMemory>();
            foreach (ref var entry in queue.entries.RefIterator()) {
                var state = QuestUtils.StateOfQuestWithId(memory, entry.quest);
                if (state is QuestState.Completed or QuestState.Failed) {
                    continue;
                }
                queue.currentQuest = entry.quest.Get<QuestTemplate>();
                queue.currentLocation = SpawnReadable(queue.place, entry.item.Get<ItemTemplate>());
                return;
            }
        }

        Location SpawnReadable(Transform parent, ItemTemplate item) {
            if (_pickedItems.Contains(item)) {
                return null;
            }
            var template = World.Services.Get<GameConstants>().DefaultItemDropPrefab;
            parent.GetPositionAndRotation(out var position, out var rotation);
            var location = template.SpawnLocation(position, rotation, Vector3.one, item.DropPrefab.Get(), item.ItemName, parent.gameObject.scene);
            location.AddElement(new PickItemAction(new ItemSpawningDataRuntime(item), true));
            location.MarkedNotSaved = true;
            location.ListenTo(Events.BeforeDiscarded, _ => _pickedItems.Add(item), this);
            return location;
        }
        
        [Serializable]
        public struct NoticeQueue {
            public Transform place;
            public NoticeEntry[] entries;
            
            [NonSerialized] public QuestTemplate currentQuest;
            [NonSerialized] public Location currentLocation;
        }

        [Serializable]
        public struct NoticeEntry {
            [TemplateType(typeof(ItemTemplate))] public TemplateReference item;
            [TemplateType(typeof(QuestTemplate))] public TemplateReference quest;
        }
    }
}