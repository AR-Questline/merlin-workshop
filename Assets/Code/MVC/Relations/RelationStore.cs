using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.CustomSerializers;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.MVC.Relations {
    /// <summary>
    /// Stores relations between a model and other models, and allows changing them.
    /// </summary>
    public sealed partial class RelationStore {
        public ushort TypeForSerialization => SavedTypes.RelationStore;

        // === Fields

        IModel _owner;
        [Saved] Dictionary<Relation, List<IModel>> _related = new Dictionary<Relation, List<IModel>>();

        // === Constructors
        [UnityEngine.Scripting.Preserve]
        RelationStore() { }

        internal RelationStore(Model owner) {
            _owner = owner;
        }

        public void RestoreOwner(Model owner) {
            _owner = owner;
        }
        
        internal void Clear() {
            _owner = null;
            if (_related != null) {
                _related.Clear();
                _related = null;
            }
        }

        public bool IsEmpty() {
            return _related == null || _related.Count == 0;
        }
        
        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            
            jsonWriter.WritePropertyName(nameof(_related));
            DictionaryConverter.CustomWriteJson(jsonWriter, serializer, _related);

            jsonWriter.WriteEndObject();
        }

        // === High-level operations

        internal static bool EstablishRelation(Relation genericLtr, Relation genericRtl, IModel leftSideModel, IModel rightSideModel, int indexLtr = int.MaxValue, int indexRtl = int.MaxValue) {
            RelationStore leftStore = leftSideModel.GetRelationStore(true);
            RelationStore rightStore = rightSideModel.GetRelationStore(true);
            if (leftSideModel.HasBeenDiscarded
                || rightSideModel.HasBeenDiscarded
                || leftStore.ContainsRelation(genericLtr, rightSideModel) 
                || rightStore.ContainsRelation(genericRtl, leftSideModel)) {
                return true;
            }
            
            EventTriggerArgs leftArgs = new(genericLtr, leftSideModel, rightSideModel, true);
            EventTriggerArgs rightArgs = new(genericRtl, rightSideModel, leftSideModel, true);
            
            if (!leftStore._related.TryGetValue(genericLtr, out var leftRelated)) {
                leftRelated = new List<IModel>(genericLtr.OtherArity == Arity.One ? 1 : 4);
                leftStore._related[genericLtr] = leftRelated;
            }

            if (!rightStore._related.TryGetValue(genericRtl, out var rightRelated)) {
                rightRelated = new List<IModel>(genericRtl.OtherArity == Arity.One ? 1 : 4);
                rightStore._related[genericRtl] = rightRelated;
            }

            bool isEstablishingRelationLtr = leftRelated.Count == 0;
            bool isEstablishingRelationRtl = rightRelated.Count == 0;
            
            if (isEstablishingRelationLtr && RunBeforeEstablishedHooks(leftArgs)) {
                return false;
            }

            if (isEstablishingRelationRtl && RunBeforeEstablishedHooks(rightArgs)) {
                return false;
            }

            if (RunBeforeAttachedHooks(leftArgs)) {
                return false;
            }

            if (RunBeforeAttachedHooks(rightArgs)) {
                return false;
            }

            if (genericLtr.OtherArity == Arity.One) {
                var lPreviouslyRelated = leftStore.SingleRelatedBy(genericLtr);
                if (lPreviouslyRelated != null) {
                    BreakRelation(genericLtr, genericRtl, leftSideModel, lPreviouslyRelated);
                }
            }
            
            if (genericRtl.OtherArity == Arity.One) {
                var rPreviouslyRelated = rightStore.SingleRelatedBy(genericRtl);
                if (rPreviouslyRelated != null) {
                    BreakRelation(genericLtr, genericRtl, rPreviouslyRelated, rightSideModel);
                }
            }

            // --- Sanity Check
            if (genericLtr.OtherArity == Arity.One) {
                var lPreviouslyRelated = leftStore.SingleRelatedBy(genericLtr);
                if (lPreviouslyRelated != null) {
                    Log.Important?.Error($"After breaking relation there is still related model attached! Relation:{genericLtr}, related: {lPreviouslyRelated}");
                }
            }
            
            if (genericRtl.OtherArity == Arity.One) {
                var rPreviouslyRelated = rightStore.SingleRelatedBy(genericRtl);
                if (rPreviouslyRelated != null) {
                    Log.Important?.Error($"After breaking relation there is still related model attached! Relation {genericRtl}, related: {rPreviouslyRelated}");
                }
            }
            
            leftRelated.Insert(Mathf.Clamp(indexLtr, 0, leftRelated.Count), rightSideModel);
            rightRelated.Insert(Mathf.Clamp(indexRtl, 0, rightRelated.Count), leftSideModel);
            
            TriggerChangedEvent(leftArgs);
            TriggerChangedEvent(rightArgs);

            TriggerAfterAttachedEvent(leftArgs);
            TriggerAfterAttachedEvent(rightArgs);

            if (isEstablishingRelationLtr) {
                TriggerAfterEstablishedEvent(leftArgs);
            }
            
            if (isEstablishingRelationRtl) {
                TriggerAfterEstablishedEvent(rightArgs);
            }
            
            return true;
        }

        internal static void BreakRelation(Relation genericLtr, Relation genericRtl, IModel leftSideModel, IModel rightSideModel) {
            RelationStore leftStore = leftSideModel.GetRelationStore();
            RelationStore rightStore = rightSideModel.GetRelationStore();
            if (!leftStore.ContainsRelation(genericLtr, rightSideModel)
                && !rightStore.ContainsRelation(genericRtl, leftSideModel)) {
                return;
            }

            EventTriggerArgs leftArgs = new(genericLtr, leftSideModel, rightSideModel, false);
            EventTriggerArgs rightArgs = new(genericRtl, rightSideModel, leftSideModel, false);
            
            TriggerBeforeDetachedEvent(leftArgs);
            TriggerBeforeDetachedEvent(rightArgs);

            var leftRelated = leftStore._related[genericLtr];
            var rightRelated = rightStore._related[genericRtl];

            bool isDisestablishingRelationLtr = leftRelated.Count == 1;
            bool isDisestablishingRelationRtl = rightRelated.Count == 1;
            
            if (isDisestablishingRelationLtr) {
                TriggerBeforeDisestablishedEvent(leftArgs);
            }

            if (isDisestablishingRelationRtl) {
                TriggerBeforeDisestablishedEvent(rightArgs);
            }

            leftRelated.Remove(rightSideModel);
            rightRelated.Remove(leftSideModel);

            TriggerChangedEvent(leftArgs);
            TriggerChangedEvent(rightArgs);

            TriggerAfterDetachedEvent(leftArgs);
            TriggerAfterDetachedEvent(rightArgs);

            if (isDisestablishingRelationLtr) {
                TriggerAfterDisestablishedEvent(leftArgs);
            }

            if (isDisestablishingRelationRtl) {
                TriggerAfterDisestablishedEvent(rightArgs);
            }
        }
        
        static bool RunBeforeEstablishedHooks(in EventTriggerArgs args) {
            return args.Relation.Events.BeforeEstablished.RunHooks(args.Model, args.EventData).Prevented;
        }

        static bool RunBeforeAttachedHooks(in EventTriggerArgs args) {
            return args.Relation.Events.BeforeAttached.RunHooks(args.Model, args.EventData).Prevented;
        }

        static void TriggerAfterAttachedEvent(in EventTriggerArgs args) {
            args.Model.Trigger(args.Relation.Events.AfterAttached, args.EventData);
        }

        static void TriggerAfterEstablishedEvent(in EventTriggerArgs args) {
            args.Model.Trigger(args.Relation.Events.AfterEstablished, args.EventData);
        }

        static void TriggerBeforeDisestablishedEvent(in EventTriggerArgs args) {
            args.Model.Trigger(args.Relation.Events.BeforeDisestablished, args.EventData);
        }

        static void TriggerBeforeDetachedEvent(in EventTriggerArgs args) {
            args.Model.Trigger(args.Relation.Events.BeforeDetached, args.EventData);
        }

        static void TriggerChangedEvent(in EventTriggerArgs args) {
            args.Model.Trigger(args.Relation.Events.Changed, args.EventData);
        }

        static void TriggerAfterDetachedEvent(in EventTriggerArgs args) {
            args.Model.Trigger(args.Relation.Events.AfterDetached, args.EventData);
        }

        static void TriggerAfterDisestablishedEvent(in EventTriggerArgs args) {
            args.Model.Trigger(args.Relation.Events.AfterDisestablished, args.EventData);
        }

        internal void BreakAllRelations() {
            // collect first, since the list is going to be changing
            List<(Relation, IModel)> toBreak = _related.AsEnumerable()
                .OrderBy(pair => pair.Key.Order)
                .SelectMany(pair => pair.Value.Select(model => (pair.Key, model)))
                .ToList();
            // break everything
            foreach (var (rel, model) in toBreak) {
                BreakRelation(rel, rel.GenericOpposite, _owner, model);
            }
        }

        internal IModel SingleRelatedBy(Relation rel) {
            _related.TryGetValue(rel, out var list);
            return list?.Count > 0 ? list[0] : null;
        }

        internal List<IModel> AllRelatedBy(Relation rel) {
            _related.TryGetValue(rel, out var list);
            return list;
        }

        internal bool ContainsRelation(Relation rel, IModel other) {
            return _related.TryGetValue(rel, out var list) && list.Contains(other);
        }

        // === Low-level operations on the index

        /// <summary>
        /// Revalidation removes invalid elements from relations on game load
        /// </summary>
        public void Revalidate() {
            if (_related == null) {
                return;
            }

            foreach (var key in _related.Keys) {
                List<IModel> list = _related[key];
                for (int i = list.Count - 1; i >= 0; i--) {
                    IModel model = list[i];
                    if (model == null || model.WasDiscarded) {
                        Log.Important?.Error($"Removed invalid relation, type: {key.Name}, model: {model?.ID}");
                        list.RemoveAt(i);
                    }
                }
            }
            
            // make sure pair relations have both sides. remove and log if not
            foreach (var key in _related.Keys) {
                List<IModel> list = _related[key];
                for (var index = list.Count - 1; index >= 0; index--) {
                    IModel model = list[index];
                    Relation opposite = key.GenericOpposite;
                    Dictionary<Relation, List<IModel>> dictionary = model.GetRelationStore()?._related;

                    if (dictionary == null || !dictionary.TryGetValue(opposite, out var oppositeList) || !oppositeList.Contains(_owner)) {
                        Log.Critical?.Error($"Relation inconsistency: {key.Name} -> {model.ID} does not have {opposite.Name} -> {_owner.ID}");
                        list.RemoveAt(index);
                    }
                }
            }
        }

        readonly ref struct EventTriggerArgs {
            public Relation Relation { get; }
            public IModel Model { get; }
            public RelationEventData EventData { get; }
            
            public EventTriggerArgs(Relation relation, IModel model, IModel relModel, bool newState) {
                Relation = relation;
                Model = model;
                EventData = new RelationEventData(model, relModel, relation, newState);
            }
        }
    }
}
