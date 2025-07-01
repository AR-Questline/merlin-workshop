using Animancer;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARTransitions {
    public interface IARMixerState {
        ARMixerTransition.Properties Properties { get; }
    }
    
    public class CartesianARMixerState : CartesianMixerState, IARMixerState {
        public ARMixerTransition.Properties Properties { get; }
        
        public CartesianARMixerState(ARMixerTransition.Properties properties) {
            Properties = properties;
        }
    }
    
    public class DirectionalARMixerState : DirectionalMixerState, IARMixerState {
        public ARMixerTransition.Properties Properties { get; }
        
        public DirectionalARMixerState(ARMixerTransition.Properties properties) {
            Properties = properties;
        }
    }
}