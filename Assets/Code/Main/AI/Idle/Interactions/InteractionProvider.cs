using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Awaken.Utility.PhysicUtils;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class InteractionProvider : IService {
        readonly Dictionary<string, INpcInteractionSearchable> _uniqueInteractionsByID = new();
        readonly List<INpcInteractionSearchable> _reusableInteractionList = new(5);

        static readonly List<INpcInteraction> FoundInteractions = new();
        static readonly Dictionary<string, WeightedInteractionList> NpcInteractionLists = new();
        static readonly WeightedInteractionList DefaultInteractionList = new();
        
        public static void EDITOR_RuntimeReset() {
            FoundInteractions.Clear();
            NpcInteractionLists.Clear();
            DefaultInteractionList.Clear();
        }

        public bool TryRegisterUniqueSearchable(string id, INpcInteractionSearchable searchable) {
            if (id.IsNullOrWhitespace()) return false;
            if (!_uniqueInteractionsByID.TryAdd(id, searchable)) {
                var newInteraction = GetInteraction(null, searchable);
                if (newInteraction is MonoBehaviour mb) {
                    Log.Critical?.Error($"There are interactions with the same UniqueID. First at {mb.gameObject.PathInSceneHierarchy()}", mb);
                } else {
                    Log.Critical?.Error($"There are interactions with the same UniqueID. First is {newInteraction}");
                }
                
                var existing = _uniqueInteractionsByID[id];
                var existingInteraction = GetInteraction(null, existing);
                if (existingInteraction is NpcInteractionBase existingNpcInteraction) {
                    Log.Critical?.Error($"Second interaction with the same id at {existingNpcInteraction.gameObject.PathInSceneHierarchy()}", existingNpcInteraction);
                } else {
                    Log.Critical?.Error($"Second interaction with the same id is {existingInteraction}");
                }

                return false;
            }
            return true;
        }

        public bool UnregisterUniqueSearchable(string id) {
            return !id.IsNullOrWhitespace() && _uniqueInteractionsByID.Remove(id);
        }

        public INpcInteractionSearchable GetUniqueSearchable(string id) {
            if (_uniqueInteractionsByID.TryGetValue(id, out var searchable)) {
                return searchable;
            } else {
                Log.Important?.Warning($"Cannot find unique interaction with id {id}");
                return null;
            }
        }

        public INpcInteraction FindAndGetInteraction(NpcElement npc, IInteractionFinder finder, Vector3 center, float range, bool allowAlreadyBooked, bool allowInteractionRepeat) {
            foreach (var collider in PhysicsQueries.OverlapSphere(center, range, RenderLayers.Mask.AIInteractions)) {
                collider.GetComponentsInParent(false, _reusableInteractionList);
                foreach (var interactionSearchable in _reusableInteractionList) {
                    NpcInteraction npcInteraction = null;
                    if (interactionSearchable is NpcInteraction searchableNpcInteraction) {
                        npcInteraction = searchableNpcInteraction;
                    }
                    bool allowInteraction = allowAlreadyBooked || npcInteraction == null || !npcInteraction.BookedBy(npc);
                    if (!allowInteraction || !interactionSearchable.AvailableFor(npc, finder)) {
                        continue;
                    }
                    var interaction = GetInteraction(npc, interactionSearchable);
                    FoundInteractions.Add(interaction);
                }
                _reusableInteractionList.Clear();
            }
            var randomInteraction = GetRandomInteraction(FoundInteractions, npc, finder, allowInteractionRepeat);
            FoundInteractions.Clear();
            return randomInteraction;
        }
        
        public INpcInteraction FindAndGetInteractionOfType(NpcElement npc, IInteractionFinder finder, Vector3 center, float range, bool allowAlreadyBooked, bool allowInteractionRepeat, Type requiredType) {
            foreach (var collider in PhysicsQueries.OverlapSphere(center, range, RenderLayers.Mask.AIInteractions)) {
                collider.GetComponentsInParent(false, _reusableInteractionList);
                foreach (var interactionSearchable in _reusableInteractionList) {
                    NpcInteraction npcInteraction = null;
                    if (interactionSearchable is NpcInteraction searchableNpcInteraction) {
                        npcInteraction = searchableNpcInteraction;
                    }
                    bool allowInteraction = allowAlreadyBooked || npcInteraction == null || !npcInteraction.BookedBy(npc);
                    if (!allowInteraction || !interactionSearchable.AvailableFor(npc, finder)) {
                        continue;
                    }
                    var interaction = GetInteraction(npc, interactionSearchable);
                    if (interaction.GetType() == requiredType) {
                        FoundInteractions.Add(interaction);
                    }
                }
                _reusableInteractionList.Clear();
            }
            var randomInteraction = GetRandomInteraction(FoundInteractions, npc, finder, allowInteractionRepeat);
            FoundInteractions.Clear();
            return randomInteraction;
        }

        public static INpcInteraction GetRandomInteraction(IEnumerable<INpcInteraction> interactions, NpcElement npc, IInteractionFinder finder, bool allowInteractionRepeat) {
            foreach (var interaction in interactions) {
                float weight = 1;
                NpcInteraction npcInteraction = interaction switch {
                    NpcInteraction forwarderNpcInteraction => forwarderNpcInteraction,
                    INpcInteractionWrapper { Interaction: NpcInteraction wrappedInteraction } => wrappedInteraction,
                    _ => null
                };

                if (npcInteraction != null) {
                    string tags = string.Concat(npcInteraction.tags.OrderBy(s => s)); //TODO Mateusz Sabat rework this after Tags v2 
                    if (!NpcInteractionLists.TryGetValue(tags, out var currentList)) {
                        currentList = new WeightedInteractionList();
                        NpcInteractionLists.Add(tags, currentList);
                    }

                    bool isForced = false;
                    if (!allowInteractionRepeat && npcInteraction.WasBookedBy(npc)) {
                        weight = 0;
                    } else {
                        weight = npcInteraction.Weight(out isForced);
                    }

                    if (isForced) {
                        currentList.Clear();
                        currentList.Add(interaction, weight);
                        currentList.Close();
                        continue;
                    }

                    currentList.Add(interaction, weight);
                } else {
                    DefaultInteractionList.Add(interaction, weight);
                }
            }

            foreach (var weightedList in NpcInteractionLists) {
                DefaultInteractionList.sumOfWeights += weightedList.Value.sumOfWeights;
                DefaultInteractionList.interactions.AddRange(weightedList.Value.interactions);
                DefaultInteractionList.weights.AddRange(weightedList.Value.weights);
            }
            var randomInteraction = DefaultInteractionList.GetRandom();
            NpcInteractionLists.Clear();
            DefaultInteractionList.Clear();
            return randomInteraction;
        }
        
        public static INpcInteraction GetInteraction(NpcElement npc, INpcInteractionSearchable searchable) {
            while (searchable is INpcInteractionForwarder forwarder) {
                searchable = forwarder.GetInteraction(npc);
            }
            
            if (searchable is INpcInteraction interaction) {
                return interaction;
            }

            return null;
        }

        class WeightedInteractionList {
            bool _closed;
            public float sumOfWeights;
            public List<float> weights = new();
            public List<INpcInteraction> interactions = new();

            public void Add(INpcInteraction interaction, float weight) {
                if (_closed) {
                    return;
                }
                interactions.Add(interaction);
                weights.Add(weight);
                sumOfWeights += weight;
            }

            public void Close() {
                _closed = true;
            }
            
            public void Clear() {
                interactions.Clear();
                weights.Clear();
                sumOfWeights = 0;
                _closed = false;
            }

            public INpcInteraction GetRandom() {
                if (interactions.Count == 0) return null;
                if (interactions.Count == 1) return interactions[0];
                float random = RandomUtil.UniformFloat(0, sumOfWeights);
                for (int i = 0; i < interactions.Count; i++) {
                    random -= weights[i];
                    if (random <= 0) {
                        return interactions[i];
                    }
                }
                return interactions[^1];
            }
        }
    }
}