using System;
using System.IO;
using Awaken.TG.Assets;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.TimeLines.Markers;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Awaken.TG.Main.Utility.Video;
using Awaken.Utility.LowLevel;
using FMODUnity;

namespace Awaken.TG.Main.Stories.Runtime {
    public unsafe class StoryWriter {
        readonly StoryGraphRuntime _graph;
        readonly FileWriter _writer;
        
        StoryWriter(StoryGraphRuntime graph, string path) {
            _graph = graph;
            _writer = new FileWriter(path, FileMode.CreateNew);
        }

        void Dispose() {
            _writer.Dispose();
        }
        
        public static void Write(StoryGraphRuntime graph, string path) {
            var writer = new StoryWriter(graph, path);
            writer.Write();
            writer.Dispose();
        }

        void Write() {
            Write(_graph.usedSoundBanksNames);
            Write(_graph.tags);
            Write(_graph.variables);
            Write(_graph.variableReferences);
            Write(_graph.sharedBetweenMultipleNPCs);
            
            Write(_graph.chapters.Length);
            Write(_graph.conditions.Length);
            foreach (var conditions in _graph.conditions) {
                Write(conditions.Type);
            }

            if (_graph.startNode != null) {
                Write(true);
                
                Write(_graph.startNode.enableChoices);
                Write(_graph.startNode.involveHero);
                Write(_graph.startNode.involveAI);
                Write(_graph.startNode.choices.Length);
                foreach (var step in _graph.startNode.choices) {
                    step.Write(this);
                }
                Write(_graph.startNode.continuation);
            } else {
                Write(false);
            }

            foreach (var chapter in _graph.chapters) {
                Write(chapter.steps.Length);
                foreach (var step in chapter.steps) {
                    Write(step.Type);
                    step.Write(this);
                }
                Write(chapter.continuation);
            }

            
            foreach (var conditions in _graph.conditions) {
                Write(conditions.inputs.Length);
                foreach (var input in conditions.inputs) {
                    Write(input);
                }
                Write(conditions.conditions.Length);
                foreach (var condition in conditions.conditions) {
                    Write(condition.Type);
                    condition.Write(this);
                }
            }
        }
        
        public void Write<T>(T value) where T : unmanaged {
            _writer.Write(value);
        }
        
        public void Write(string value) {
            value ??= string.Empty;
            int length = value.Length;
            Write(length);
            fixed (char* ptr = value) {
                _writer.Write(ptr, length);
            }
        }
        
        public void Write(StoryBookmark value) {
            value ??= new StoryBookmark();
            Write(value.story);
            Write(value.chapterName);
        }
        
        public void Write(TemplateReference value) {
            value ??= new TemplateReference();
            Write(value.GUID);
        }

        public void Write(RichEnumReference value) {
            value ??= new RichEnumReference();
            Write(value.EnumRef);
        }

        public void Write(LocationReference value) {
            value ??= new LocationReference();
            Write(value.targetTypes);
            Write(value.tags);
            Write(value.locationRefs);
            Write(value.actors);
        }

        public void Write(LocString value) {
            value ??= new LocString();
            Write(value.IdOverride);
            Write(value.ID);
        }

        public void Write(in RuntimeChoice value) {
            Write(value.targetChapter);
            Write(value.isMainChoice);
            Write(value.text);
        }

        public void Write(in EventReference value) {
            Write(value.Guid);
        }

        public void Write(in IntRange value) {
            Write(value.low);
            Write(value.high);
        }

        public void Write(BaseAudioSource value) {
            value ??= new BaseAudioSource();
            var accessor = BaseAudioSource.Serialization(value);
            Write(accessor.EventRef);
            Write(accessor.PriorityOverride);
            Write(accessor.IsCopyrighted);
        }

        public void Write(in StoryChapter value) {
            Write(Array.IndexOf(_graph.chapters, value));
        }

        public void Write(in StoryConditions value) {
            Write(Array.IndexOf(_graph.conditions, value));
        }

        public void Write(in StoryConditionInput value) {
            Write(value.conditions);
            Write(value.negate);
        }

        public void Write(LoadingHandle value) {
            value ??= new LoadingHandle();
            Write(value.video);
            Write(value.subtitlesReference);
            Write(value.videoAudio);
        }

        public void Write(ItemSpawningData value) {
            value ??= new ItemSpawningData();
            Write(value.itemTemplateReference);
            Write(value.quantity);
            Write(value.itemLvl);
        }

        public void Write(RichLabelUsage value) {
            value ??= new RichLabelUsage(RichLabelConfigType.Presence);
            var accessor = RichLabelUsage.Serialization(value);
            Write(accessor.RichLabelUsageEntries);
            Write(accessor.RichLabelConfigType);
        }

        public void Write(RichLabelUsageEntry value) {
            value ??= new RichLabelUsageEntry();
            var accessor = RichLabelUsageEntry.Serialization(value);
            Write(accessor.RichLabelGuid);
            Write(accessor.Include);
        }

        public void Write(in ActorRef value) {
            Write(value.guid);
        }

        public void Write(in ActorStateRef value) {
            Write(value.stateName);
        }

        public void Write(SceneReference value) {
            value ??= new SceneReference();
            Write(SceneReference.Serialization(value).Reference);
        }

        public void Write(SpriteReference value) {
            value ??= new SpriteReference();
            Write(value.arSpriteReference);
        }

        public void Write(ShareableSpriteReference value) {
            value ??= new ShareableSpriteReference();
            Write(value.arSpriteReference);
        }

        public void Write(ARAssetReference value) {
            value ??= new ARAssetReference();
            var accessor = ARAssetReference.Serialization(value);
            Write(accessor.Address);
            Write(accessor.SubObjectName);
        }

        public void Write(ShareableARAssetReference value) {
            value ??= new ShareableARAssetReference();
            var accessor = ShareableARAssetReference.Serialization(value);
            Write(accessor.ARReference);
        }

        public void Write(Variable value) {
            value ??= new Variable();
            Write(value.name);
            Write(value.value);
            Write(value.type);
        }

        public void Write(VariableDefine value) {
            value ??= new VariableDefine();
            Write(value.name);
            Write(value.defaultValue);
            Write(value.context);
            Write(value.contexts);
        }

        public void Write(VariableReferenceDefine value) {
            value ??= new VariableReferenceDefine();
            Write(value.name);
            Write(value.template);
        }

        public void Write(Context value) {
            value ??= new Context();
            Write(value.type);
            Write(value.template);
        }

        public void Write(in DuelistSettings value) {
            Write(value.fightToDeath);
            Write(value.canBeTalkedToDefeated);
            Write(value.keepsDuelAlive);
            Write(value.restoreHealthOnStart);
            Write(value.restoreHealthOnEnd);
            Write(value.defeatedAnimationsOverrides);
        }

        public void Write(in DuelArenaReferenceData value) {
            Write(value.arenaDataSource);
            Write(value.arenaFaction);
            Write(value.sceneRef);
            Write(value.locationRef);
        }

        public void Write(JournalGuid value) {
            value ??= new JournalGuid();
            Write(ARGuid.Serialization(value).GUID);
        }

        public void Write(in EmotionData value) {
            Write(value.startTime);
            Write(value.emotionKey);
            Write(value.state);
            Write(value.expressionHandler);
            Write(value.roundDuration);
        }
        
        // === Collections

        public void Write(string[] value) {
            value ??= Array.Empty<string>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }

        public void Write(TemplateReference[] value) {
            value ??= Array.Empty<TemplateReference>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }

        public void Write(RichEnumReference[] value) {
            value ??= Array.Empty<RichEnumReference>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }

        public void Write(Context[] value) {
            value ??= Array.Empty<Context>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }

        public void Write(VariableDefine[] value) {
            value ??= Array.Empty<VariableDefine>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }

        public void Write(VariableReferenceDefine[] value) {
            value ??= Array.Empty<VariableReferenceDefine>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }

        public void Write(ItemSpawningData[] value) {
            value ??= Array.Empty<ItemSpawningData>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }
        
        public void Write(ActionData[] value) {
            value ??= Array.Empty<ActionData>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }
        
        public void Write(SequenceKey[] value) {
            value ??= Array.Empty<SequenceKey>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }
        
        public void Write(StoryConditionInput[] value) {
            value ??= Array.Empty<StoryConditionInput>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }
        
        public void Write(RichLabelUsageEntry[] value) {
            value ??= Array.Empty<RichLabelUsageEntry>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }
        
        public void Write(ActorRef[] value) {
            value ??= Array.Empty<ActorRef>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }

        public void Write(EmotionData[] value) {
            value ??= Array.Empty<EmotionData>();
            Write(value.Length);
            for (int i = 0; i < value.Length; i++) {
                Write(value[i]);
            }
        }
    }
}