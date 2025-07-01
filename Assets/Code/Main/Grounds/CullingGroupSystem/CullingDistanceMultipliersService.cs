using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Unity.Mathematics;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    public class CullingDistanceMultiplierService : IService, UnityUpdateProvider.IWithUpdateGeneric {
        public event Action<float, bool> OnCullingDistanceMultiplierChanged;
        public float Multiplier { get; private set; } = 1f;
        public bool ClampMultiplier { get; private set; } = true;

        readonly UnsafePinnableList<AreaModifierData> _areaModifiers = new(5);
        readonly UnsafePinnableList<GlobalModifierData> _globalModifiers = new(2);
        
        int _cachedAreaIndex = -1;
        ModifierData _cachedAreaData = new (1f, true);
        ModifierData _cachedGlobalData = new (1f, true);

        public CullingDistanceMultiplierService() {
            World.Services.Get<UnityUpdateProvider>().RegisterGeneric(this);
        }

        public void RegisterAreaModifier(IAreaCullingDistanceModifier modifier) {
            _areaModifiers.Add(new AreaModifierData(modifier));
        }
        
        public void UnregisterAreaModifier(IAreaCullingDistanceModifier modifier) {
            RemoveSwapBack(_areaModifiers, modifier);
        }

        public void RegisterGlobalModifier(ICullingDistanceModifier modifier) {
            _globalModifiers.Add(new GlobalModifierData(modifier));
            _cachedGlobalData = GetGlobalDistanceCullingData();
            AnnounceMultiplierChange();
        }

        public void UnregisterGlobalModifier(ICullingDistanceModifier modifier) {
            RemoveSwapBack(_globalModifiers, modifier);
            _cachedGlobalData = GetGlobalDistanceCullingData();
            AnnounceMultiplierChange();
        }

        public void UnityUpdate() {
            var hero = Hero.Current;
            if (hero == null) {
                return;
            }

            var heroPosition2d = (float2)hero.Coords.XZ();
            var previousIndex = _cachedAreaIndex;
            var currentData = GetAreaDistanceCullingData(heroPosition2d, ref _cachedAreaIndex);
            if ((previousIndex != _cachedAreaIndex) | !currentData.Equals(_cachedAreaData)) {
                _cachedAreaData = currentData;
                AnnounceMultiplierChange();
            }
        }
        
        void AnnounceMultiplierChange() {
            Multiplier = _cachedAreaData.multiplier * _cachedGlobalData.multiplier;
            ClampMultiplier = _cachedAreaData.allowClamp && _cachedGlobalData.allowClamp;
            OnCullingDistanceMultiplierChanged?.Invoke(Multiplier, ClampMultiplier);
        }

        ModifierData GetAreaDistanceCullingData(float2 position2d, ref int cachedIndex) {
            int count = _areaModifiers.Count;
            if (cachedIndex >= 0 && cachedIndex < count && _areaModifiers[cachedIndex].ContainsPosition(position2d)) {
                return new(_areaModifiers[cachedIndex].Multiplier, _areaModifiers[cachedIndex].AllowMultiplierClamp);
            }
            for (int i = 0; i < count; i++) {
                var areaData = _areaModifiers[i];
                if (areaData.ContainsPosition(position2d)) {
                    cachedIndex = i;
                    return new(areaData.Multiplier, areaData.AllowMultiplierClamp);
                }
            }
            cachedIndex = -1;
            return new(1f, true);
        }

        ModifierData GetGlobalDistanceCullingData() {
            int count = _globalModifiers.Count;
            float modifier = 1f;
            bool allowClamp = true;
            for (int i = 0; i < count; i++) {
                modifier *= _globalModifiers[i].Multiplier;
                allowClamp &= _globalModifiers[i].AllowMultiplierClamp;
            }
            return new(modifier, allowClamp);
        }

        static void RemoveSwapBack<T>(UnsafePinnableList<T> modifiers, ICullingDistanceModifier modifier) where T : struct, IModifierData {
            var count = modifiers.Count;
            for (int i = count - 1; i >= 0; i--) {
                if (modifiers[i].Modifier == modifier) {
                    modifiers[i].BeforeRemove();
                    modifiers.SwapBackRemove(i);
                    return;
                }
            }
        }

        struct AreaModifierData : IModifierData {
            Polygon2D _polygonArea;
            readonly IAreaCullingDistanceModifier _modifier;

            public ICullingDistanceModifier Modifier => _modifier;
            public float Multiplier => _modifier.ModifierValue;
            public bool AllowMultiplierClamp => _modifier.AllowMultiplierClamp;
            
            public AreaModifierData(IAreaCullingDistanceModifier modifier) {
                _polygonArea = modifier.ToPolygon(ARAlloc.Persistent);
                if (_polygonArea.Equals(Polygon2D.Invalid)) {
                    Log.Important?.Error($"Area {modifier.name} yields invalid polygon", modifier.gameObject);
                }
                this._modifier = modifier;
            }

            public bool ContainsPosition(float2 position2d) {
                if (_polygonArea.bounds.Contains(position2d) == false) {
                    return false;
                }
                Polygon2DUtils.IsInPolygon(position2d, _polygonArea, out bool isInside);
                return isInside;
            }

            public void BeforeRemove() {
                _polygonArea.CheckedDispose();
            }
        }

        readonly struct GlobalModifierData : IModifierData {
            public ICullingDistanceModifier Modifier { get; }
            public float Multiplier => Modifier.ModifierValue;
            public bool AllowMultiplierClamp => Modifier.AllowMultiplierClamp;

            public GlobalModifierData(ICullingDistanceModifier modifier) {
                Modifier = modifier;
            }

            public void BeforeRemove() { }
        }

        interface IModifierData {
            public ICullingDistanceModifier Modifier { get; }
            public float Multiplier { get; }
            public void BeforeRemove();
        }

        public readonly struct ModifierData : IEquatable<ModifierData> {
            public readonly float multiplier;
            public readonly bool allowClamp;
            
            public ModifierData(float multiplier, bool allowClamp) {
                this.multiplier = multiplier;
                this.allowClamp = allowClamp;
            }

            public bool Equals(ModifierData other) {
                return multiplier.Equals(other.multiplier) && allowClamp == other.allowClamp;
            }

            public override bool Equals(object obj) {
                return obj is ModifierData other && Equals(other);
            }

            public override int GetHashCode() {
                return HashCode.Combine(multiplier, allowClamp);
            }
        }
    }
}