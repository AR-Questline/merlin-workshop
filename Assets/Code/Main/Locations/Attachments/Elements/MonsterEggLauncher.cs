using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.Archers;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class MonsterEggLauncher : Element<Location>, IRefreshedByAttachment<MonsterEggLauncherAttachment>, UnityUpdateProvider.IWithUpdateGeneric {
        public override ushort TypeForSerialization => SavedModels.MonsterEggLauncher;

        struct LaunchedProjectile {
            public bool isActive;
            public ARAssetReference spawnedProjectileHandle;
            public CustomOnContactProjectile customOnContactProjectile;
            
            public void Release() {
                if (!isActive) {
                    Log.Important?.Error("Attempting to release a projectile that is not active.");
                    return;
                }

                Log.Debug?.Info($"- Releasing active projectile: {(customOnContactProjectile != null ? customOnContactProjectile.gameObject.PathInSceneHierarchy() + customOnContactProjectile.transform.position : "null")} {spawnedProjectileHandle.Address} ");
                isActive = false;
                
                spawnedProjectileHandle.ReleaseAsset();
                spawnedProjectileHandle = null;
                
                if (customOnContactProjectile != null && customOnContactProjectile.gameObject != null) {
                    Object.Destroy(customOnContactProjectile.gameObject);
                }
                customOnContactProjectile = null;
            }
        }
        
        ShareableARAssetReference _projectileAsset;
        MonsterEggTarget[] _targets;
        LaunchedProjectile[] _activeProjectiles;
        UnsafePinnableList<int> _clearedIndices;
        [Saved(false)] bool _shouldStartEnabled;
        [Saved(1)] float _externalIntervalMultiplier = 1f;
        bool _shouldUsePrediction;
        bool _highShot;
        float _projectileSpeed;
        float _launchIntervalMin;
        float _launchIntervalMax;
        float _eggLandingOffset;
        float _maxLaunchDistanceSq;
        float _nextLaunchTime;
        Vector3 _lastHeroPosition;
        Transform _launcherTransform;
        Color _debugColor;
        
        ExplosionConfig _explosionConfig;
        
        [Saved] WeakModelRef<BaseLocationSpawner>[] _spawnedSpawners;
        
        bool LocationCleared(int index) {
            if (index < 0 || index >= _spawnedSpawners.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return _spawnedSpawners[index].Exists() == false;
        }
        
        public void InitFromAttachment(MonsterEggLauncherAttachment spec, bool isRestored) {
            _projectileAsset = spec.projectileAsset;
            _targets = spec.targets;
            _projectileSpeed = spec.projectileSpeed;
            _highShot = spec.highShot;
            _activeProjectiles = new LaunchedProjectile[_targets.Length];
            _launchIntervalMin = spec.launchIntervalMin;
            _launchIntervalMax = spec.launchIntervalMax;
            _eggLandingOffset = spec.eggLandingOffset;
            _maxLaunchDistanceSq = spec.maxDistanceOfTargetFromHero * spec.maxDistanceOfTargetFromHero;
            _shouldUsePrediction = spec.shouldUsePrediction;
            _explosionConfig = spec.explosion;
            _shouldStartEnabled = spec.shouldStartEnabled;

            if (!isRestored) {
                _spawnedSpawners = new WeakModelRef<BaseLocationSpawner>[_targets.Length];
            } else if (_spawnedSpawners.Length != _targets.Length) {
                var previousSpawners = _spawnedSpawners;
                _spawnedSpawners = new WeakModelRef<BaseLocationSpawner>[_targets.Length];

                // Despawn extra spawners
                for (int i = _spawnedSpawners.Length; i < previousSpawners.Length; i++) {
                    if (previousSpawners[i].Exists()) {
                        previousSpawners[i].Get().Discard();
                    }
                }
                int copyLength = Math.Min(previousSpawners.Length, _spawnedSpawners.Length);
                Array.Copy(previousSpawners, _spawnedSpawners, copyLength);
            }
        }

        protected override void OnFullyInitialized() {
            if (_targets.Length > 0 && _projectileAsset.IsSet) {
                if (_shouldStartEnabled) {
                    UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
                }
                _clearedIndices = new UnsafePinnableList<int>(_targets.Length);
                _launcherTransform = ParentModel.LocationView.transform;
                ScheduleNextLaunch(false, false);
                _debugColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f, 1f, 1f);
            }
        }

        public void EnableLaunches() {
            if (_shouldStartEnabled) return; // already enabled
            _shouldStartEnabled = true;
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
        }
        
        public void DisableLaunches() {
            _shouldStartEnabled = false;
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
        }
        
        public void SetExternalIntervalMultiplier(float multiplier) {
            _externalIntervalMultiplier = multiplier;
        }
        
        public void UnityUpdate() {
            if (Time.time >= _nextLaunchTime) {
                Vector3 newHeroPosition = Hero.Current.Coords;
                bool spawned = false, 
                     heroFarAway = true;
                
                if (newHeroPosition.SquaredDistanceTo(_launcherTransform.position) < 300f * 300f) {
                    heroFarAway = false;
                    spawned = LaunchRandomProjectile(newHeroPosition);
                }

                _lastHeroPosition = newHeroPosition;
                ScheduleNextLaunch(spawned, heroFarAway);
            }
        }
        
        void ScheduleNextLaunch(bool spawned, bool heroFarAway) {
            float interval = Random.Range(_launchIntervalMin, _launchIntervalMax) * _externalIntervalMultiplier;
            if (!spawned) {
                interval *= 0.5f;
            } else if (heroFarAway) {
                interval *= 2f;
            }
            _nextLaunchTime = Time.time + interval;
        }
        
        bool LaunchRandomProjectile(in Vector3 newHeroPosition) {
            const int MaxPredictionDistance = 30;
            
            Vector3 heroReferencePoint = Prediction(newHeroPosition, MaxPredictionDistance, out Vector3 prediction);
            
            // Find all cleared locations
            for (int i = 0; i < _targets.Length; i++) {
                if (LocationCleared(i) && !_activeProjectiles[i].isActive && heroReferencePoint.SquaredDistanceTo(_launcherTransform.TransformPoint(_targets[i].position)) <= _maxLaunchDistanceSq) {
                    _clearedIndices.Add(i);
                }
            }
            
            if (_clearedIndices.Count == 0) return false;
            
            // Pick a random cleared location
            int randomIndex = _clearedIndices[Random.Range(0, _clearedIndices.Count)];
            _clearedIndices.Clear();
            LaunchProjectileToTarget(randomIndex).Forget();

#if UNITY_EDITOR
            if (_shouldUsePrediction) {
                float radius = Mathf.Sqrt(_maxLaunchDistanceSq);
                // add a slight random offset to future hero position so that they do not draw on the same lines every time
                heroReferencePoint += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                // Draw a 3D cross to visualize the launch radius
                Debug.DrawLine(heroReferencePoint + Vector3.right * radius, heroReferencePoint - Vector3.right * radius, _debugColor, _launchIntervalMax * 10);
                Debug.DrawLine(heroReferencePoint + Vector3.forward * radius, heroReferencePoint - Vector3.forward * radius, _debugColor, _launchIntervalMax * 10);
                Debug.DrawLine(heroReferencePoint + Vector3.up * radius, heroReferencePoint - Vector3.up * radius, _debugColor, _launchIntervalMax * 10);
                Debug.DrawLine(newHeroPosition, heroReferencePoint, prediction.magnitude >= MaxPredictionDistance ? Color.red : Color.green, _launchIntervalMax * 10);
                // draw a blue cross to show which target was launched
                Vector3 targetPoint = _launcherTransform.TransformPoint(_targets[randomIndex].position);
                const float CrossSize = 1f;
                Debug.DrawLine(targetPoint + Vector3.right * CrossSize, targetPoint - Vector3.right * CrossSize, Color.blue, _launchIntervalMax * 10);
                Debug.DrawLine(targetPoint + Vector3.forward * CrossSize, targetPoint - Vector3.forward * CrossSize, Color.blue, _launchIntervalMax * 10);
                Debug.DrawLine(targetPoint + Vector3.up * CrossSize, targetPoint - Vector3.up * CrossSize, Color.blue, _launchIntervalMax * 10);
            }
#endif
            
            return true;
        }

        Vector3 Prediction(in Vector3 newHeroPosition, int maxPredictionDistance, out Vector3 prediction) {
            if (!_shouldUsePrediction) {
                prediction = Vector3.zero;
                return newHeroPosition;
            }
            Vector3 heroDirection = (newHeroPosition - _lastHeroPosition).X0Z();
            prediction = Vector3.ClampMagnitude(heroDirection * 20, maxPredictionDistance);
            return newHeroPosition + prediction;
        }

        async UniTaskVoid LaunchProjectileToTarget(int targetIndex) {
            if (targetIndex < 0 || targetIndex >= _targets.Length) throw new ArgumentOutOfRangeException(nameof(targetIndex));

            var target = _targets[targetIndex];
            var targetWorldPos = _launcherTransform.TransformPoint(target.position);
            
            // Apply random offset to target position
            var randomOffset = (Random.insideUnitCircle * _eggLandingOffset).X0Y();
            targetWorldPos += randomOffset;

            // Load and instantiate projectile
            _activeProjectiles[targetIndex].isActive = true;
            ARAssetReference projectileToSpawn = _projectileAsset.Get();
            _activeProjectiles[targetIndex].spawnedProjectileHandle = projectileToSpawn;

            Log.Debug?.Info($"+ Loading projectile asset: {targetIndex} {projectileToSpawn.Address ?? "null"} for target index {targetIndex} to target position {targetWorldPos}");
            var result = await PrefabUtil.InstantiateAsync(projectileToSpawn, _launcherTransform.position, Quaternion.identity);
            if (result == null || HasBeenDiscarded) {
                _activeProjectiles[targetIndex].Release();
                return;
            }
            
            var projectile = result.GetComponent<CustomOnContactProjectile>();
            if (projectile == null) {
                _activeProjectiles[targetIndex].Release();
                return;
            }

            ConfigureProjectile(targetIndex, projectile, result, _launcherTransform, targetWorldPos);
        }

        void ConfigureProjectile(int targetIndex, CustomOnContactProjectile projectile, GameObject result, Transform launcherTransform, Vector3 targetWorldPos) {
            _activeProjectiles[targetIndex].customOnContactProjectile = projectile;
            projectile.gameObject.name = $"HatcheryProjectile_{targetIndex}";
            
            var projectileVisualData = result.GetComponent<ProjectileVisualData>();
            var logicData = new ProjectileLogicData {
                lifetime = 30f,
                showLifetimeStartVFX = projectileVisualData != null && projectileVisualData.lifetimeStartVFX.IsSet,
            };
            
            projectile.Setup(logicData, projectileVisualData, null, launcherTransform, default);

            var shotData = new ShotData(
                from: launcherTransform.position,
                to: targetWorldPos,
                projectileSpeed: _projectileSpeed,
                highShot: _highShot
            );

            var velocity = ArcherUtils.ShotVelocity(shotData);

            // Configure explosion if enabled
            if (_explosionConfig.enabled) {
                projectile.ConfigureExplosion(_explosionConfig, false);
                
                // Set up damage parameters for the projectile
                var rawDamageData = new RawDamageData(_explosionConfig.damage);
                var damageTypeData = new DamageTypeData(_explosionConfig.damageType);
                projectile.SetBaseDamageParams(null, null, 1.0f, damageTypeData, rawDamageData);
            }

            // Set up projectile physics
            projectile.SetVelocityAndForward(velocity);
            projectile.SetAngularVelocity(Random.onUnitSphere * Random.Range(1f, 5f));

            // Set up projectile actions
            int capturedIndex = targetIndex;
            projectile.AssignOnContactAction(() => OnProjectileContact(capturedIndex, projectile));
            projectile.AssignReleaseAction(() => {
                if (_activeProjectiles[capturedIndex].isActive) {
                    _activeProjectiles[capturedIndex].Release();
                }
            });
            projectile.FinalizeConfiguration();
        }

        void OnProjectileContact(int targetIndex, CustomOnContactProjectile projectile) {
            if (targetIndex < 0 || targetIndex >= _targets.Length) return;

            var spawnerToSpawn = _targets[targetIndex].spawnerToSpawn;

            // Spawn the corresponding spawner at the projectile's contact position
            var contactPosition = projectile.transform.position;
            var locationTemplate = spawnerToSpawn.TryGet<LocationTemplate>(ParentModel.LocationView);

            if (locationTemplate == null) {
                // Handle explosion if enabled
                if (_explosionConfig.enabled) {
                    projectile.DealExplosionDamage(null, contactPosition);
                    SpawnPersistentAoE(contactPosition);
                }
                return;
            }

            // Spawn the location containing the spawner
            var spawnedLocation = locationTemplate.SpawnLocation(contactPosition, Quaternion.identity, spawnScene: ParentModel.LocationView.gameObject.scene);

            // Find the LocationSpawner in the spawned location
            var spawner = spawnedLocation.Element<BaseLocationSpawner>();
            var manualSpawner = spawner.Element<ManualSpawner>();

            _spawnedSpawners[targetIndex] = new WeakModelRef<BaseLocationSpawner>(spawner);

            // Handle explosion if enabled
            if (_explosionConfig.enabled) {
                projectile.DealExplosionDamage(spawner.ParentModel, contactPosition);
                SpawnPersistentAoE(contactPosition);
            }
            // Force spawn enemies immediately
            manualSpawner.TriggerSpawner().Forget();
        }

        void SpawnPersistentAoE(Vector3 contactPosition) {
            var aoeTemplate = _explosionConfig.persistentAoE?.TryGet<LocationTemplate>(ParentModel.LocationView);
            if (aoeTemplate == null) return;

            aoeTemplate.SpawnLocation(contactPosition, Quaternion.identity);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            
            // Clean up active projectiles
            for (int i = 0; i < _activeProjectiles.Length; i++) {
                if (_activeProjectiles[i].isActive) {
                    _activeProjectiles[i].Release();
                }
            }
        }
    }
}