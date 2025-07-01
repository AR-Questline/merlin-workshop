using System.Collections.Generic;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public partial class ProjectileWrapper {
        CombinedProjectile _projectileInstance;
        Projectile _projectile;
        List<IApplicableToProjectile> _applicableToProjectileCache = new();
        bool _isLoaded = false;
        bool _finalizeConfigurationOnLoad;
        
        public ProjectileWrapper(UniTask<CombinedProjectile> projectileInstanceUniTask, bool finalizeConfigurationOnLoad = true) {
            LoadProjectileInstance(projectileInstanceUniTask).Forget();
            _finalizeConfigurationOnLoad = finalizeConfigurationOnLoad;
        }

        public UniTask WaitForProjectileInstanceToLoad() {
            return AsyncUtil.CheckAndWaitUntil(() => _isLoaded);
        }
        
        public void FinalizeConfiguration() {
            if (!_isLoaded) {
                _finalizeConfigurationOnLoad = true;
            } else if (!_projectile.IsProjectileInitialized) {
                _projectile.FinalizeConfiguration();
            }
        }

        void ApplyToProjectile(IApplicableToProjectile applicableToProjectile) {
            if (_projectileInstance.logic == null) {
                _applicableToProjectileCache.Add(applicableToProjectile);
                return;
            }
            
            applicableToProjectile.ApplyToProjectile(_projectileInstance.logic, _projectile);
        }

        async UniTaskVoid LoadProjectileInstance(UniTask<CombinedProjectile> projectileInstanceUniTask) {
            _projectileInstance = await projectileInstanceUniTask;
            
            if (_projectileInstance.logic != null) {
                FinalizeProjectileLogicLoading();
            }
            
            _isLoaded = true;
        }

        void FinalizeProjectileLogicLoading() {
            _projectile = _projectileInstance.logic.GetComponent<Projectile>();

            foreach (var applicable in _applicableToProjectileCache) {
                applicable.ApplyToProjectile(_projectileInstance.logic, _projectile);
            }
            _applicableToProjectileCache.Clear();

            if (_finalizeConfigurationOnLoad && !_projectile.IsProjectileInitialized) {
                _projectile.FinalizeConfiguration();
            }
        }
    }
}