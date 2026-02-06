using System;
using System.Collections.Generic;
using Awaken.Kandra.AnimationPostProcess;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Animations {
    public partial class DummyLimbCutOff : DummyBoneCutOff {
        public override ushort TypeForSerialization => SavedModels.DummyLimbCutOff;

        [Saved] LimbData _data;
        Transform[] _limbs;

        public DummyLimbCutOff(LimbData data) {
            _data = data;
        }
        
        protected override IEnumerable<Transform> GetBones() {
            return _limbs;
        }
        protected override bool TryStartInit() {
            _limbs = new Transform[CountLimbBits(_data)];
            if (_limbs.Length == 0) {
                return false;
            }
            int index = 0;
            
            TryAddLimb(LimbData.LeftArm, "LeftArm");
            TryAddLimb(LimbData.LeftForeArm, "LeftForeArm");
            TryAddLimb(LimbData.RightArm, "RightArm");
            TryAddLimb(LimbData.RightForeArm, "RightForeArm");
            TryAddLimb(LimbData.LeftLeg, "LeftUpLeg");
            TryAddLimb(LimbData.LeftForeLeg, "LeftLeg");
            TryAddLimb(LimbData.RightLeg, "RightUpLeg");
            TryAddLimb(LimbData.RightForeLeg, "RightLeg");

            return index == _limbs.Length;
            
            void TryAddLimb(LimbData flag, string name) {
                if (_data.HasFlagFast(flag)) {
                    _limbs[index] = ParentModel.RootSocket.gameObject.FindChildRecursively(name);
                    if (_limbs[index] == null) {
                        Log.Important?.Error($"{name} not found in model {LogUtils.GetDebugName(ParentModel)}");
                        return;
                    }
                    index++;
                }
            }
        }

        protected override AnimationPostProcessing.Entry[] GetAdditionalEntries() {
            var entries = new AnimationPostProcessing.Entry[_limbs.Length];
            int index = 0;
            
            var commonRefs = CommonReferences.Get;
            TryAddEntry(LimbData.LeftArm, commonRefs.dummyNoLArmPP);
            TryAddEntry(LimbData.LeftForeArm, commonRefs.dummyNoLForeArmPP);
            TryAddEntry(LimbData.RightArm, commonRefs.dummyNoRArmPP);
            TryAddEntry(LimbData.RightForeArm, commonRefs.dummyNoRForeArmPP);
            TryAddEntry(LimbData.LeftLeg, commonRefs.dummyNoLLegPP);
            TryAddEntry(LimbData.LeftForeLeg, commonRefs.dummyNoLForeLegPP);
            TryAddEntry(LimbData.RightLeg, commonRefs.dummyNoRLegPP);
            TryAddEntry(LimbData.RightForeLeg, commonRefs.dummyNoRForeLegPP);

            return entries;

            void TryAddEntry(LimbData flag, AnimationPostProcessingPreset preset) {
                if (_data.HasFlagFast(flag)) {
                    entries[index] = new AnimationPostProcessing.Entry(preset);
                    index++;
                }
            }
        }

        int CountLimbBits(LimbData data) {
            int count = 0;
            byte dataByte = (byte)data;
            while (dataByte != 0) {
                count += dataByte & 1;
                dataByte >>= 1;
            }
            return count;
        }
    }

    [Flags]
    public enum LimbData : byte {
        LeftArm = 1 << 0,
        RightArm = 1 << 1,
        LeftForeArm = 1 << 2,
        RightForeArm = 1 << 3,
        LeftLeg = 1 << 4,
        RightLeg = 1 << 5,
        LeftForeLeg = 1 << 6,
        RightForeLeg = 1 << 7,
    }
}