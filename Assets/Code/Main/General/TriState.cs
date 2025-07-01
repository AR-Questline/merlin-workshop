using Awaken.Utility;
using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.General {
    [Serializable]
    public partial struct TriState {
        public ushort TypeForSerialization => SavedTypes.TriState;

        [SerializeField]
        [Saved] 
        State state;

        [UnityEngine.Scripting.Preserve] public static readonly TriState NotDefined = new TriState(null);
        [UnityEngine.Scripting.Preserve] public static readonly TriState True = new TriState(true);
        [UnityEngine.Scripting.Preserve] public static readonly TriState False = new TriState(false);

        public TriState(bool? value) {
            if (value == null) {
                state = State.NotDefined;
            } else if (value.Value) {
                state = State.True;
            } else {
                state = State.False;
            }
        }

        public bool Get(bool fallback) {
            if (state == State.NotDefined) {
                return fallback;
            } else if (state == State.True) {
                return true;
            } else {
                return false;
            }
        }

        public void Set(bool? value) {
            if (value == null) {
                state = State.NotDefined;
            } else if (value.Value) {
                state = State.True;
            } else {
                state = State.False;
            }
        }

        enum State {
            NotDefined = 0,
            True = 1,
            False = 2,
        }
        
        // === Equality members
        public static bool operator ==(TriState a, TriState b) => a.Equals(b);
        public static bool operator !=(TriState a, TriState b) => !a.Equals(b);
        public override bool Equals(object obj) {
            return obj is TriState other && Equals(other);
        }
        public bool Equals(TriState other) {
            return state == other.state;
        }
        public override int GetHashCode() {
            return (int) state;
        }
    }
}