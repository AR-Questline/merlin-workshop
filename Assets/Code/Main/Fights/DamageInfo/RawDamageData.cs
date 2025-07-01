using System.Text;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Fights.DamageInfo {
    public partial class RawDamageData {
        public ushort TypeForSerialization => SavedTypes.RawDamageData;

        public static bool showCalculationLogs;
        
        float _calculatedValue;
        [Saved] float _uncalculatedValue;
        [Saved] float _multModifier;
        [Saved] float _addedMultModifier;
        [Saved] float _linearModifier;
        bool _finalCalculated;

        StringBuilder _log;
        
        public float CalculatedValue {
            get {
                if (!_finalCalculated) {
                    _calculatedValue = (_uncalculatedValue + _linearModifier) * _multModifier * _addedMultModifier;
                }
                return _calculatedValue;
            }
        }
        
        public float UncalculatedValue => _uncalculatedValue;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        RawDamageData() { }

        public RawDamageData(float uncalculatedValue, float multModifier = 1, float linearModifier = 0) {
            _uncalculatedValue = uncalculatedValue;
            _multModifier = multModifier;
            _linearModifier = linearModifier;
            _addedMultModifier = 1f;

            CreateLog($"Created: ({_uncalculatedValue} + {_linearModifier}) * {_multModifier} * {_addedMultModifier}");
        }
        
        public RawDamageData(RawDamageData other) {
            _uncalculatedValue = other._uncalculatedValue;
            _multModifier = other._multModifier;
            _linearModifier = other._linearModifier;
            _addedMultModifier = other._addedMultModifier;
            
            CreateLog($"Created: ({_uncalculatedValue} + {_linearModifier}) * {_multModifier} * {_addedMultModifier}");
        }
        
        public void MultiplyMultModifier(float multModifier) {
            if (_finalCalculated) {
                if (_calculatedValue != 0) {
                    Log.Important?.Error("Raw Damage Data already calculated, cannot multiply mult modifier");
                }
                return;
            }
            AddToLog($"Multiplied MultModifier: ({_multModifier} * {multModifier}) = {_multModifier * multModifier}");
            _multModifier *= multModifier;
        }

        public void AddMultModifier(float multModifier) {
            if (_finalCalculated) {
                if (_calculatedValue != 0) {
                    Log.Important?.Error("Raw Damage Data already calculated, cannot add mult modifier");
                }
                return;
            }
            AddToLog($"Added to AddedMultModifier: ({_addedMultModifier} + {multModifier}) = {_addedMultModifier + multModifier}");
            _addedMultModifier += multModifier;
        }
        
        public void AddLinearModifier(float linearModifier) {
            if (_finalCalculated) {
                if (_calculatedValue != 0) {
                    Log.Important?.Error("Raw Damage Data already calculated, cannot add linear modifier");
                }
                return;
            }
            AddToLog($"Added to LinearModifier: ({_linearModifier} + {linearModifier}) = {_linearModifier + linearModifier}");
            _linearModifier += linearModifier;
        }

        public void SetToZero() {
            if (_finalCalculated) {
                return;
            }
            _calculatedValue = 0;
            ShowLog($"Negated whole damage: ({_uncalculatedValue} + {_linearModifier}) * {_multModifier} * {_addedMultModifier} = {_calculatedValue}");
            _finalCalculated = true;
        }

        public void FinalCalculation() {
            if (_finalCalculated) {
                return;
            }
            _calculatedValue = (_uncalculatedValue + _linearModifier) * _multModifier * _addedMultModifier;
            ShowLog($"Final calculation: ({_uncalculatedValue} + {_linearModifier}) * {_multModifier} * {_addedMultModifier} = {_calculatedValue}");
            _finalCalculated = true;
        }

        // --- Logs
        
        void CreateLog(string message) {
            if (!showCalculationLogs) {
                return;
            }
            _log = new StringBuilder();
            _log.Append("Raw DMG Calculations:\n");
            _log.Append("(WeaponDmg + LinearModifier) * MultModifier * AddedMultModifier:\n");
            _log.Append(message);
            _log.Append("\n");
        }
        
        void AddToLog(string message) {
            if (!showCalculationLogs || _log == null) {
                return;
            }
            _log.Append(message);
            _log.Append("\n");
        }
        
        void ShowLog(string message) {
            if (!showCalculationLogs || _log == null) {
                return;
            }
            _log.Append(message);
            Log.Important?.Warning(_log.ToString());
        }
    }
}
