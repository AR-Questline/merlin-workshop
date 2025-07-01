using UnityEngine;

namespace Awaken.TG.Graphics.VFX
{
    public class VFXShurikenController : MonoBehaviour{
        public bool enableEmissionModules = true;

        private ParticleSystem[] _childrenParticleSytems;
        void Start() {
            _childrenParticleSytems = gameObject.GetComponentsInChildren<ParticleSystem>();
        }
 
        void Update(){
            if( !enableEmissionModules ){
                foreach( ParticleSystem childPS in _childrenParticleSytems ){
                    ParticleSystem.EmissionModule childPSEmissionModule = childPS.emission;
                    childPSEmissionModule.enabled = false;
                    ParticleSystem[] grandchildrenParticleSystems = childPS.GetComponentsInChildren<ParticleSystem>();
 
                    foreach( ParticleSystem grandchildPS in grandchildrenParticleSystems ) {
                        ParticleSystem.EmissionModule grandchildPSEmissionModule = grandchildPS.emission;
                        grandchildPSEmissionModule.enabled = false;
                    }
                }
            }else{
                foreach( ParticleSystem childPS in _childrenParticleSytems ){
                    ParticleSystem.EmissionModule childPSEmissionModule = childPS.emission;
                    childPSEmissionModule.enabled = true;
                    
                    ParticleSystem[] grandchildrenParticleSystems = childPS.GetComponentsInChildren<ParticleSystem>();
 
                    foreach( ParticleSystem grandchildPS in grandchildrenParticleSystems ) {
                        ParticleSystem.EmissionModule grandchildPSEmissionModule = grandchildPS.emission;
                        grandchildPSEmissionModule.enabled = true;
                    }
                }
            }
        }
    }
}
