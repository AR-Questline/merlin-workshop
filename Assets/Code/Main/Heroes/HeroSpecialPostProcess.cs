using System;
using Awaken.TG.Main.Rendering;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroSpecialPostProcess : Element<Skill> {
        public sealed override bool IsNotSaved => true;

        readonly float _enableBlendSpeed;
        readonly float _disableBlendSpeed;
        readonly SpecialPostProcessType _ppType;
        VolumeWrapper _postProcess;
        
        public HeroSpecialPostProcess(SpecialPostProcessType ppType, float enableBlendSpeed, float disableBlendSpeed) {
            _ppType = ppType;
            _enableBlendSpeed = enableBlendSpeed;
            _disableBlendSpeed = disableBlendSpeed;
        }
        
        protected override void OnInitialize() {
            var ppService = World.Services.Get<SpecialPostProcessService>();
            _postProcess = _ppType switch {
                SpecialPostProcessType.Drunk => ppService.VolumeDrunk,
                SpecialPostProcessType.High => ppService.VolumeHigh,
                _ => throw new ArgumentOutOfRangeException()
            };
            _postProcess.SetWeight(1.0f, _enableBlendSpeed);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _postProcess.SetWeight(0, _disableBlendSpeed);
        }
    }
    
    public enum SpecialPostProcessType : byte {
        Drunk = 0,
        High = 1
    }
}