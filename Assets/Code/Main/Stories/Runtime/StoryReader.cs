using System;
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
using Awaken.Utility.Files;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using FMODUnity;
using Unity.Collections;

namespace Awaken.TG.Main.Stories.Runtime {
    public unsafe class StoryReader {
        BufferStreamReader _reader;
        StoryGraphRuntime _graph;

        StoryReader(in UnsafeArray<byte> buffer) {
            _reader = new BufferStreamReader(buffer);
        }
        
        public static StoryGraphRuntime Read(string guid, string path) {
            var data = FileRead.ToNewBuffer<byte>(path, Allocator.Temp);
            var reader = new StoryReader(data);
            var graph = reader.Read();
            graph.guid = guid;
            data.Dispose();
            return graph;
        }

        StoryGraphRuntime Read() {
            Read(ref _graph.usedSoundBanksNames);
            Read(ref _graph.tags);
            Read(ref _graph.variables);
            Read(ref _graph.variableReferences);
            Read(ref _graph.sharedBetweenMultipleNPCs);

            int chaptersLength = 0;
            Read(ref chaptersLength);
            _graph.chapters = new StoryChapter[chaptersLength];
            for (int i = 0; i < chaptersLength; i++) {
                _graph.chapters[i] = new StoryChapter();
            }
            
            int conditionsLength = 0;
            Read(ref conditionsLength);
            _graph.conditions = new StoryConditions[conditionsLength];
            for (int i = 0; i < conditionsLength; i++) {
                byte type = 0;
                Read(ref type);
                _graph.conditions[i] = StoryConditions.Create(type);
            }

            bool hasStartNode = false;
            Read(ref hasStartNode);
            if (hasStartNode) {
                var startNode = _graph.startNode = new StartNode();
                Read(ref startNode.enableChoices);
                Read(ref startNode.involveHero);
                Read(ref startNode.involveAI);
            
                int choicesLength = 0;
                Read(ref choicesLength);
                startNode.choices = new SStoryStartChoice[choicesLength];
                for (int i = 0; i < choicesLength; i++) {
                    var step = new SStoryStartChoice();
                    step.Read(this);
                    startNode.choices[i] = step;
                }
                Read(ref startNode.continuation);   
            }

            for (int i = 0; i < chaptersLength; i++) {
                var chapter = _graph.chapters[i];
                int stepsLength = 0;
                Read(ref stepsLength);
                chapter.steps = new StoryStep[stepsLength];
                for (int j = 0; j < stepsLength; j++) {
                    byte stepType = 0;
                    Read(ref stepType);
                    var step = StorySerializerType.CreateStep(stepType);
                    step.Read(this);
                    step.parentChapter = chapter;
                    chapter.steps[j] = step;
                }
                Read(ref chapter.continuation);
            }
            
            for (int i = 0; i < conditionsLength; i++) {
                var conditions = _graph.conditions[i];

                int inputsLength = 0;
                Read(ref inputsLength);
                conditions.inputs = new StoryConditionInput[inputsLength];
                for (int j = 0; j < inputsLength; j++) {
                    Read(ref conditions.inputs[j]);
                }
                
                int nestedConditionsLength = 0;
                Read(ref nestedConditionsLength);
                conditions.conditions = new StoryCondition[nestedConditionsLength];
                for (int j = 0; j < nestedConditionsLength; j++) {
                    byte conditionType = 0;
                    Read(ref conditionType);
                    var condition = StorySerializerType.CreateCondition(conditionType);
                    condition.Read(this);
                    conditions.conditions[j] = condition;
                }
            }
            
            return _graph;
        }
        
        public void Read<T>(ref T value) where T : unmanaged {
            value = _reader.Read<T>();
        }
        
        public void Read(ref string value) {
            int length = 0;
            Read(ref length);
            var span = _reader.ReadSpan<char>((uint)length);
            value = new string(span.Ptr, 0, length);
        }
        
        public void Read(ref StoryBookmark value) {
            value = new StoryBookmark();
            Read(ref value.story);
            Read(ref value.chapterName);
        }
        
        public void Read(ref TemplateReference value) {
            value = new TemplateReference();
            Read(ref TemplateReference.Serialization(value).Guid);
        }
        
        public void Read(ref RichEnumReference value) {
            value = new RichEnumReference();
            Read(ref RichEnumReference.Serialization(value).EnumRef);
            value.OnAfterDeserialize();
        }
        
        public void Read(ref LocationReference value) {
            value = new LocationReference();
            Read(ref value.targetTypes);
            Read(ref value.tags);
            Read(ref value.locationRefs);
            Read(ref value.actors);
        }
        
        public void Read(ref LocString value) {
            value = new LocString();
            Read(ref value.IdOverride);
            Read(ref value.ID);
        }
        
        public void Read(ref RuntimeChoice value) {
            Read(ref value.targetChapter);
            Read(ref value.isMainChoice);
            Read(ref value.text);
        }
        
        public void Read(ref EventReference value) {
            Read(ref value.Guid);
        }
        
        public void Read(ref IntRange value) {
            Read(ref value.low);
            Read(ref value.high);
        }
        
        public void Read(ref BaseAudioSource value) {
            value = new BaseAudioSource();
            var accessor = BaseAudioSource.Serialization(value);
            Read(ref accessor.EventRef);
            Read(ref accessor.PriorityOverride);
            Read(ref accessor.IsCopyrighted);
        }
        
        public void Read(ref StoryChapter value) {
            int index = 0;
            Read(ref index);
            value = index == -1 ? null : _graph.chapters[index];
        }
        
        public void Read(ref StoryConditions value) {
            int index = 0;
            Read(ref index);
            value = index == -1 ? null : _graph.conditions[index];
        }
        
        public void Read(ref StoryConditionInput value) {
            Read(ref value.conditions);
            Read(ref value.negate);
        }
        
        public void Read(ref LoadingHandle value) {
            value = new LoadingHandle();
            Read(ref value.video);
            Read(ref value.subtitlesReference);
            Read(ref value.videoAudio);
        }
        
        public void Read(ref ItemSpawningData value) {
            value = new ItemSpawningData();
            Read(ref value.itemTemplateReference);
            Read(ref value.quantity);
            Read(ref value.itemLvl);
        }
        
        public void Read(ref RichLabelUsage value) {
            value = new RichLabelUsage(RichLabelConfigType.Presence);
            var accessor = RichLabelUsage.Serialization(value);
            Read(ref accessor.RichLabelUsageEntries);
            Read(ref accessor.RichLabelConfigType);
        }
        
        public void Read(ref RichLabelUsageEntry value) {
            value = new RichLabelUsageEntry();
            var accessor = RichLabelUsageEntry.Serialization(value);
            Read(ref accessor.RichLabelGuid);
            Read(ref accessor.Include);
        }
        
        public void Read(ref ActorRef value) {
            Read(ref value.guid);
        }
        
        public void Read(ref ActorStateRef value) {
            Read(ref value.stateName);
        }
        
        public void Read(ref SceneReference value) {
            value = new SceneReference();
            Read(ref SceneReference.Serialization(value).Reference);
        }
        
        public void Read(ref SpriteReference value) {
            value = new SpriteReference();
            Read(ref value.arSpriteReference);
        }
        
        public void Read(ref ShareableSpriteReference value) {
            value = new ShareableSpriteReference();
            Read(ref value.arSpriteReference);
        }
        
        public void Read(ref ARAssetReference value) {
            value = new ARAssetReference();
            var accessor = ARAssetReference.Serialization(value);
            Read(ref accessor.Address);
            Read(ref accessor.SubObjectName);
        }
        
        public void Read(ref ShareableARAssetReference value) {
            value = new ShareableARAssetReference();
            var accessor = ShareableARAssetReference.Serialization(value);
            Read(ref accessor.ARReference);
        }
        
        public void Read(ref Variable value) {
            value = new Variable();
            Read(ref value.name);
            Read(ref value.value);
            Read(ref value.type);
        }
        
        public void Read(ref VariableDefine value) {
            value = new VariableDefine();
            Read(ref value.name);
            Read(ref value.defaultValue);
            Read(ref value.context);
            Read(ref value.contexts);
        }
        
        public void Read(ref VariableReferenceDefine value) {
            value = new VariableReferenceDefine();
            Read(ref value.name);
            Read(ref value.template);
        }
        
        public void Read(ref Context value) {
            value = new Context();
            Read(ref value.type);
            Read(ref value.template);
        }
        
        public void Read(ref DuelistSettings value) {
            Read(ref value.fightToDeath);
            Read(ref value.canBeTalkedToDefeated);
            Read(ref value.keepsDuelAlive);
            Read(ref value.restoreHealthOnStart);
            Read(ref value.restoreHealthOnEnd);
            Read(ref value.defeatedAnimationsOverrides);
        }
        
        public void Read(ref DuelArenaReferenceData value) {
            Read(ref value.arenaDataSource);
            Read(ref value.arenaFaction);
            Read(ref value.sceneRef);
            Read(ref value.locationRef);
        }
        
        public void Read(ref JournalGuid value) {
            value = new JournalGuid();
            var accessor = ARGuid.Serialization(value);
            Read(ref accessor.GUID);
        }
        
        public void Read(ref EmotionData value) {
            Read(ref value.startTime);
            Read(ref value.emotionKey);
            Read(ref value.state);
            Read(ref value.expressionHandler);
            Read(ref value.roundDuration);
        }
        
        public void Read(ref string[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<string>();
                return;
            }
            
            value = new string[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref TemplateReference[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<TemplateReference>();
                return;
            }
            
            value = new TemplateReference[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref RichEnumReference[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<RichEnumReference>();
                return;
            }
            
            value = new RichEnumReference[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref VariableDefine[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<VariableDefine>();
                return;
            }
            
            value = new VariableDefine[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref VariableReferenceDefine[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<VariableReferenceDefine>();
                return;
            }
            
            value = new VariableReferenceDefine[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref Context[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<Context>();
                return;
            }
            
            value = new Context[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref ItemSpawningData[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<ItemSpawningData>();
                return;
            }
            
            value = new ItemSpawningData[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref ActionData[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<ActionData>();
                return;
            }
            
            value = new ActionData[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref SequenceKey[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<SequenceKey>();
                return;
            }
            
            value = new SequenceKey[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref StoryConditionInput[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<StoryConditionInput>();
                return;
            }
            
            value = new StoryConditionInput[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref ActorRef[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<ActorRef>();
                return;
            }
            
            value = new ActorRef[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref RichLabelUsageEntry[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<RichLabelUsageEntry>();
                return;
            }
            
            value = new RichLabelUsageEntry[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
        
        public void Read(ref EmotionData[] value) {
            int length = 0;
            Read(ref length);
            
            if (length == 0) {
                value = Array.Empty<EmotionData>();
                return;
            }
            
            value = new EmotionData[length];
            for (int i = 0; i < length; i++) {
                Read(ref value[i]);
            }
        }
    }
}