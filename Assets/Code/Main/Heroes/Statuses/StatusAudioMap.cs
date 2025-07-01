using System;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Statuses {
    [InlineEditor]
    public class StatusAudioMap : ScriptableObject {
        [SerializeField] public StatusEventPair[] statusEventPairs = Array.Empty<StatusEventPair>();

        public EventReference GetEventReference(StatusDamageType statusDamageType, Gender gender = Gender.Male) =>
            statusEventPairs
                .FirstOrDefault(sep => sep.StatusDamageType == statusDamageType)
                .GetEventReference(gender);

        [Serializable]
        public struct StatusEventPair {
            [field:SerializeField, BoxGroup] public StatusDamageType StatusDamageType { get; private set; }

            [SerializeField, InlineProperty, HideLabel, BoxGroup] GenderEvent eventReference;

            public EventReference GetEventReference(Gender gender) => gender switch {
                Gender.Male => eventReference.MaleEventReference,
                Gender.Female => eventReference.FemaleEventReference,
                _ => eventReference.MaleEventReference
            };

            [Serializable]
            struct GenderEvent {
                [field: SerializeField] public EventReference MaleEventReference { get; private set; }
                [field: SerializeField] public EventReference FemaleEventReference { get; private set; }
            }
        }
    }
}