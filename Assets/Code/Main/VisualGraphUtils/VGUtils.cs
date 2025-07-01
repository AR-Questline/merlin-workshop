using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.AI.Fights.Archers;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Attachments;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Paths;
using Awaken.TG.MVC;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.MVC.Elements;
using Awaken.TG.VisualScripts.Units.Utils;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.VisualGraphUtils {
    /// <summary>
    /// Helper methods for Visual Scripting
    /// </summary>
    public static class VGUtils {
        public const string ModelVariableName = "Model";

        public static bool TryDoDamage(IAlive aliveHit, Collider colliderHit, float amount, ICharacter attacker,
            ref DamageParameters parameters, Item item = null, Projectile projectile = null, 
            StatusDamageType statusDamageType = StatusDamageType.Default, float? overridenRandomnessModifier = null) {
            var rawDamageData = new RawDamageData(amount);
            return DamageUtils.TryDoDamage(aliveHit, colliderHit, rawDamageData, attacker, ref parameters, item, projectile, statusDamageType, overridenRandomnessModifier);
        }

        public static void SendCustomEvent(GameObject target, GameObject sender, VSCustomEvent action, params object[] args) {
            if (target == null) return;
            try {
                CustomEvent.Trigger(target, action.ToString(), sender, args);
            } catch {
                Log.Important?.Error($"[SafeGraph] Exception for event: {action} for GameObject: {target.PathInSceneHierarchy()}", target);
            }
        }
        
        public static void SendCustomEvent(ScriptMachineWithSkill target, GameObject sender, VSCustomEvent action, params object[] args) {
            if (target == null) return;
            try {
                SendCustomEvent(target, action.ToString(), sender, args);
            } catch {
                Log.Important?.Error($"[SafeGraph] Exception for event: {action} for GameObject: {target.gameObject.PathInSceneHierarchy()}", target);
            }
        }

        public static void SendCustomEvent(object target, string action, params object[] args) {
            EventBus.Trigger(new EventHook(EventHooks.Custom, target), new CustomEventArgs(action, args));
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void Patrol(Vector3 startPosition, RichAI agent, Transform transform, float patrolRadius = 25) {
            Seeker seeker = agent.GetComponent<Seeker>();
            if (!agent.hasPath || Vector3.Distance(agent.destination, transform.position) <= agent.endReachedDistance) {
                bool pathIsCorrect = false;
                int pathsCount = 0, pathsLimit = 10;
                while (!pathIsCorrect && pathsCount < pathsLimit) {
                    Vector3 newDestination = Ground.SnapToGround(
                        startPosition + new Vector3(RandomUtil.UniformFloat(-patrolRadius, patrolRadius), 0, RandomUtil.UniformFloat(-patrolRadius, patrolRadius))
                    );
                    agent.destination = newDestination;
                    Path path = seeker.StartPath(agent.position, newDestination, null);
                    path.BlockUntilCalculated();
                    pathIsCorrect = path.CompleteState == PathCompleteState.Complete;
                    pathsCount++;
                }
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static List<Vector3> Waypoints(GameObject gameObject) {
            var vLocation = gameObject.GetComponentInParent<VLocation>();
            return vLocation.Target.TryGetElement<LocationPath>()?.Path.waypoints;
        }

        [UnityEngine.Scripting.Preserve]
        public static ProjectileData GetProjectileData(Item item) {
            var projectile = item?.TryGetElement<ItemProjectile>();
            if (projectile == null) {
                throw new Exception($"Item: {item} has no Item Projectile");
            }
            return projectile.Data;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static ProjectileData GetUpgradeableProjectileData(Item item, int level) {
            var projectile = item?.TryGetElement<UpgradeableItemProjectile>();
            if (projectile == null) {
                throw new Exception($"Item: {item} has no Upgradeable Item Projectile");
            }
            if (level >= 0 && level <= projectile.MaxLevel) {
                return projectile.Data(level);
            }
            Log.Minor?.Error($"Upgradeable Item Projectile in Item: {item} doesn't have level: {level}, returning level 0");
            return projectile.Data(0);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static ProjectileWrapper ShootProjectile(Transform shooterTransform, Item projectileItem, Vector3 shotPosition, Vector3 targetPosition, float velocity, bool highShot, float fireStrength = 1, DamageType? damageType = null, EquipmentSlotType projectileSlotType = null, float? damageAmount = null) {
            Vector3 arrowVelocity = ArcherUtils.ShotVelocity(new ShotData(shotPosition, targetPosition, velocity, highShot));
            return ShootProjectile(shooterTransform, projectileItem, shotPosition, arrowVelocity, fireStrength, damageType, projectileSlotType, damageAmount);
        }
        
        public static ProjectileWrapper ShootProjectile(Transform shooterTransform, Item projectileItem, Vector3 shotPosition, Vector3 arrowVelocity, float fireStrength = 1, DamageType? damageType = null, EquipmentSlotType projectileSlotType = null, float? damageAmount = null) {
            return ShootProjectile(VGUtils.GetModel<ICharacter>(shooterTransform.gameObject), projectileItem, shotPosition, arrowVelocity, fireStrength, damageType, projectileSlotType, damageAmount);
        }
        
        public static ProjectileWrapper ShootProjectile(ICharacter shooter, Item projectileItem, Vector3 shotPosition, Vector3 arrowVelocity, float fireStrength = 1, DamageType? damageType = null, EquipmentSlotType projectileSlotType = null, float? damageAmount = null) {
            ShootParams shootParams = ShootParams.Default;
            shootParams.shooter = shooter;
            shootParams.startPosition = shotPosition;
            shootParams.fireStrength = fireStrength;
            shootParams.damageTypeData = new DamageTypeData(damageType ?? DamageType.PhysicalHitSource);
            shootParams.projectileSlotType = projectileSlotType;
            shootParams.damageAmount = damageAmount;
            shootParams.WithItem(projectileItem);
            return ShootProjectile(shootParams, arrowVelocity);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static ProjectileWrapper ShootProjectile(Transform shooterTransform, ShareableARAssetReference logicRef, ProjectileLogicData? logicData, List<SkillReference> skillReferences, ShareableARAssetReference visualRef, Vector3 shotPosition, Vector3 targetPosition, float velocity, bool highShot, float fireStrength = 1, DamageType? damageType = null, EquipmentSlotType projectileSlotType = null, float? damageAmount = null) {
            Vector3 arrowVelocity = ArcherUtils.ShotVelocity(new ShotData(shotPosition, targetPosition, velocity, highShot));
            var projectileData = new ProjectileData(logicRef, visualRef, skillReferences, logicData ?? ProjectileLogicData.Default);
            return ShootProjectile(VGUtils.GetModel<ICharacter>(shooterTransform.gameObject), projectileData, shotPosition, arrowVelocity, fireStrength, damageType, projectileSlotType, damageAmount);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static ProjectileWrapper ShootProjectile(Transform shooterTransform, ShareableARAssetReference logicRef, ShareableARAssetReference visualRef, ProjectileLogicData? logicData, List<SkillReference> skillReferences, Vector3 shotPosition, Vector3 arrowVelocity, float fireStrength = 1, DamageType? damageType = null, EquipmentSlotType projectileSlotType = null, float? damageAmount = null) {
            var projectileData = new ProjectileData(logicRef, visualRef, skillReferences, logicData ?? ProjectileLogicData.Default);
            return ShootProjectile(VGUtils.GetModel<ICharacter>(shooterTransform.gameObject), projectileData, shotPosition, arrowVelocity, fireStrength, damageType, projectileSlotType, damageAmount);
        }

        [UnityEngine.Scripting.Preserve]
        public static ProjectileWrapper ShootProjectile(ICharacter shooter, ShareableARAssetReference logicRef, ShareableARAssetReference visualRef, ProjectileLogicData? logicData, List<SkillReference> skillReferences, Vector3 shotPosition, Vector3 arrowVelocity, float fireStrength = 1, DamageType? damageType = null, EquipmentSlotType projectileSlotType = null, float? damageAmount = null) {
            var projectileData = new ProjectileData(logicRef, visualRef, skillReferences, logicData ?? ProjectileLogicData.Default);
            return ShootProjectile(shooter, projectileData, shotPosition, arrowVelocity, fireStrength, damageType, projectileSlotType, damageAmount);
        }
        
        public static ProjectileWrapper ShootProjectile(ICharacter shooter, ProjectileData projectileData, Vector3 shotPosition, Vector3 arrowVelocity, float fireStrength = 1, DamageType? damageType = null, EquipmentSlotType projectileSlotType = null, float? damageAmount = null) {
            ShootParams shootParams = ShootParams.Default;
            shootParams.shooter = shooter;
            shootParams.startPosition = shotPosition;
            shootParams.fireStrength = fireStrength;
            shootParams.damageTypeData = new DamageTypeData(damageType ?? DamageType.PhysicalHitSource);
            shootParams.projectileSlotType = projectileSlotType;
            shootParams.damageAmount = damageAmount;
            shootParams = shootParams.WithCustomProjectile(projectileData);
            return ShootProjectile(shootParams, arrowVelocity);
        }
        
        public static ProjectileWrapper ShootProjectile(ShootParams shootParams, Vector3 arrowVelocity) => ShootProjectile(shootParams, arrowVelocity, null);

        public static ProjectileWrapper ShootProjectile(ShootParams shootParams, Vector3 arrowVelocity, Transform firePoint) {
            UniTask<CombinedProjectile> projectileTask;
            if (shootParams.itemProjectile != null) {
                projectileTask = shootParams.itemProjectile.GetProjectile(shootParams.startPosition, Quaternion.LookRotation(arrowVelocity, shootParams.upDirection), true, shootParams.shooter?.CharacterView.transform, firePoint, null);
            } else if (shootParams.customProjectile.logicPrefab is {IsSet: true}) {
                projectileTask = ItemProjectile.GetCustomProjectile(shootParams.customProjectile, shootParams.startPosition, Quaternion.LookRotation(arrowVelocity, shootParams.upDirection), true, shootParams.shooter?.CharacterView.transform, firePoint, null);
            } else {
                Log.Minor?.Error("Projectile prefab is not set, aborting");
                return null;
            }

            var wrapper = new ProjectileWrapper(projectileTask);
            wrapper.ConfigureShootProjectile(shootParams, arrowVelocity);
            return wrapper;
        }

        [UnityEngine.Scripting.Preserve]
        public static ProjectileWrapper ShotProjectileSimple(Transform shooterTransform, Item itemProjectile, Vector3 shotPosition, Vector3 targetPosition, float velocity, Transform firePoint = null, EquipmentSlotType slotType = null, float fireStrength = 1, ProjectileOffsetData? offsetParams = null, DamageType? damageType = null, List<VSVariable> variables = null) {
            var projectile = itemProjectile.TryGetElement<ItemProjectile>();
            if (projectile == null) {
                Log.Minor?.Error($"itemProjectile {itemProjectile} has no ItemProjectile element");
                return null;
            }
            ICharacter shooter = shooterTransform.GetComponentInParent<ICharacterView>().Character;
            Vector3 projectileVelocity = (targetPosition - shotPosition).normalized * velocity;
            var projectileTask = projectile.GetProjectile(shotPosition, Quaternion.LookRotation(projectileVelocity), true, shooterTransform, firePoint, null);
            var wrapper = ShotProjectileSimpleInternal(projectileTask, shooter, projectileVelocity, slotType, fireStrength, offsetParams, damageType);
            if (variables is { Count: > 0 }) {
                wrapper.ApplyVariables(variables);
            }
            return wrapper;
        }

        [UnityEngine.Scripting.Preserve]
        public static ProjectileWrapper ShotProjectileSimple(Transform shooterTransform, ShareableARAssetReference logicRef, ShareableARAssetReference visualRef, ProjectileLogicData? logicData, List<SkillReference> skillReferences, Vector3 shotPosition, Vector3 targetPosition, float velocity,  Transform firePoint = null, EquipmentSlotType slotType = null, float fireStrength = 1, ProjectileOffsetData? offsetParams = null, DamageType? damageType = null, List<VSVariable> variables = null) {
            var projectileData = new ProjectileData(logicRef, visualRef, skillReferences, logicData ?? ProjectileLogicData.Default);
            return ShotProjectileSimple(shooterTransform, projectileData, shotPosition, targetPosition, velocity, firePoint, slotType, fireStrength, offsetParams, damageType, variables);
        }
        
        public static ProjectileWrapper ShotProjectileSimple(Transform shooterTransform, ProjectileData projectileData, Vector3 shotPosition, Vector3 targetPosition, float velocity, Transform firePoint = null, EquipmentSlotType slotType = null, float fireStrength = 1, ProjectileOffsetData? offsetParams = null, DamageType? damageType = null, List<VSVariable> variables = null) {
            ICharacter shooter = shooterTransform.GetComponentInParent<ICharacterView>().Character;
            Vector3 projectileVelocity = (targetPosition - shotPosition).normalized * velocity;
            var projectileTask = ItemProjectile.GetCustomProjectile(projectileData, shotPosition, Quaternion.LookRotation(projectileVelocity), true, shooterTransform, firePoint, null);
            var wrapper = ShotProjectileSimpleInternal(projectileTask, shooter, projectileVelocity, slotType, fireStrength, offsetParams, damageType);
            if (variables is { Count: > 0 }) {
                wrapper.ApplyVariables(variables);
            }
            return wrapper;
        }

        static ProjectileWrapper ShotProjectileSimpleInternal(UniTask<CombinedProjectile> projectileTask, ICharacter shooter, Vector3 projectileVelocity,
            EquipmentSlotType slotType, float fireStrength, ProjectileOffsetData? offsetParams, DamageType? damageType) {
            var wrapper = new ProjectileWrapper(projectileTask);
            wrapper.ConfigureShotProjectileSimple(projectileVelocity, shooter, slotType, fireStrength, offsetParams, damageType);
            return wrapper;
        }

        [UnityEngine.Scripting.Preserve]
        public static ProjectileWrapper FireHomingProjectile(ICharacter shooter, Item shootingItem, DamageType? damageType, Transform firePoint) {
            var projectileData = shootingItem?.TryGetElement<ItemProjectile>()?.Data;
            if (projectileData == null) {
                Log.Minor?.Error($"{shootingItem?.Template} has no ItemProjectile element");
                return null;
            }
            return FireHomingProjectile(projectileData.Value, shooter, shootingItem, damageType, firePoint);
        }

        public static ProjectileWrapper FireHomingProjectile(ProjectileData projectileData, ICharacter shooter, Item shootingItem, DamageType? damageType, Transform firePoint) {
            var projectileTask = ItemProjectile.GetCustomProjectile(projectileData, shooter.Coords + Vector3.up, Quaternion.identity, true, shooter.CharacterView.transform, firePoint, null);
            var wrapper = new ProjectileWrapper(projectileTask);
            wrapper.ConfigureHomingProjectile(shooter, shootingItem, new DamageTypeData(damageType ?? DamageType.MagicalHitSource));
            return wrapper;
        }

        [UnityEngine.Scripting.Preserve]
        public static void DuplicateProjectile(DamageDealingProjectile projectile, Vector3 spawnOffset, Vector3 aimOffset, bool consumeAmmo = false, float delayMove = 0f) {
            DuplicateProjectile(projectile, spawnOffset, aimOffset, consumeAmmo, null, null, null, null, delayMove);
        }

        public static void DuplicateProjectile(DamageDealingProjectile projectile, Vector3 spawnOffset, Vector3 aimOffset, bool consumeAmmo = false, ShareableARAssetReference overrideLogicPrefab = null,
            ShareableARAssetReference overrideVisualPrefab = null, ProjectileLogicData? overrideLogicData = null, List<SkillReference> overrideSkillsRef = null, float delayMove = 0f, float? damageMultiplier = null) {
            if (!projectile.IsPrimary) {
                return;
            }
            
            var baseProjectileItem = projectile.SourceProjectile;
            ItemTemplate itemTemplate = projectile.ItemTemplate;
            if (itemTemplate == null) {
                Log.Minor?.Error("Projectile can't be duplicated, it has no SourceProjectile and ItemTemplate");
                return;
            }
            
            if (consumeAmmo && !itemTemplate.IsMagic) {
                if (baseProjectileItem == null) {
                    return;
                }
                baseProjectileItem.DecrementQuantityWithoutNotification();
            }

            ShareableARAssetReference logicAssetRef = overrideLogicPrefab ?? projectile.CreationData.logicPrefab;
            ShareableARAssetReference visualAssetRef = overrideVisualPrefab ?? projectile.CreationData.visualPrefab;
            ProjectileLogicData logicData = overrideLogicData ?? projectile.CreationData.logicData;
            IEnumerable<SkillReference> skills = overrideSkillsRef ?? projectile.CreationData.skills;
            var creationData = new ProjectileData(logicAssetRef, visualAssetRef, skills, logicData);
            
            var projectileTransform = projectile.transform;
            Vector3 shotPosition = spawnOffset + projectileTransform.position;
            Vector3 targetPos = spawnOffset + projectile.Velocity - projectile.PositionOffset.InitialVelocity;
            float velocity = targetPos.magnitude;
            targetPos.Normalize();
            targetPos += Quaternion.LookRotation(targetPos) * aimOffset;
            targetPos += shotPosition;

            Vector3 projectileVelocity = (targetPos - shotPosition).normalized * velocity;
            var shotRotation = Quaternion.LookRotation(projectileVelocity);
            var projectileTask = ItemProjectile.GetCustomProjectile(creationData, shotPosition, shotRotation, true, projectile.owner?.CharacterView.transform, null, null);
            if (delayMove <= 0f) {
                var nextProjectile = ShotProjectileSimpleInternal(projectileTask, projectile.owner, projectileVelocity,
                    null, projectile.FireStrength, projectile.PositionOffset, projectile.DamageTypeData.SourceType);
                if (damageMultiplier.HasValue) {
                    nextProjectile.MultiplyBaseDamage(damageMultiplier.Value);
                }
                nextProjectile.SetAsSecondaryProjectile();
            } else {
                DelayedShotProjectileInternal(delayMove, projectileTask, projectile.owner, projectileVelocity, projectile.FireStrength, projectile.PositionOffset, projectile.DamageTypeData.SourceType, damageMultiplier).Forget();
            }
        }

        static async UniTaskVoid DelayedShotProjectileInternal(float delay, UniTask<CombinedProjectile> projectileTask, ICharacter shooter, 
            Vector3 velocity, float fireStrength, ProjectileOffsetData? offsetParams, DamageType? damageType, float? damageMultiplier) {
            var nextProjectile = new ProjectileWrapper(projectileTask, false);
            nextProjectile.EnableLogic(false);
            if (!await AsyncUtil.DelayTime(shooter, delay)) {
                return;
            }
            nextProjectile.EnableLogic(true);
            nextProjectile.ConfigureShotProjectileSimple(velocity, shooter, null, fireStrength, offsetParams, damageType);
            if (damageMultiplier.HasValue) {
                nextProjectile.MultiplyBaseDamage(damageMultiplier.Value);
            }
            nextProjectile.SetAsSecondaryProjectile();
            nextProjectile.FinalizeConfiguration();
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool IsWyrdSkillActive() {
            return Hero.Current?.Development?.WyrdSkillActivation?.IsActive ?? false; 
        }

        [UnityEngine.Scripting.Preserve]
        public static bool CanWyrdSkillBeActivated() {
            return Hero.Current?.Development?.WyrdSkillActivation?.CanBeActivated ?? false; 
        }

        [UnityEngine.Scripting.Preserve]
        public static bool TryActivateWyrdSkill() {
            return Hero.Current?.Development?.WyrdSkillActivation?.TryActivate() ?? false;
        }
        
        public static void ToggleCrafting(TabSetConfig tabSetConfig) {
            var crafting = World.Any<CraftingTabsUI>();
            if (crafting == null) {
                World.Add(new CraftingTabsUI(tabSetConfig));
            } else {
                crafting.Discard();
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static void SpawnFloatingText(string text, Transform spawnTransform, GameObject textPrefab, float horizontalOffset = 0.75f,
            float verticalOffset = 1.5f) {
            Vector3 offset = Vector3.right * Random.Range(-horizontalOffset, horizontalOffset);
            offset.y = verticalOffset;
            var spawned = Object.Instantiate(textPrefab, spawnTransform.position + offset, spawnTransform.rotation, spawnTransform);
            TextMeshPro mText = spawned.GetComponentInChildren<TextMeshPro>();
            mText.text = text;
        }

        [UnityEngine.Scripting.Preserve]
        public static void CreateLocationSpecModel(LocationTemplate template) {
            var location = template.SpawnLocation();
            RepetitiveNpcUtils.Check(location);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool AnimatorHasParameter(Animator animator, string parameterName) {
            if (animator.runtimeAnimatorController == null || !animator.gameObject.activeInHierarchy) {
                return false;
            }
            return animator.parameters.Any(p => p.name == parameterName);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool AnimatorHasParameter(Animator animator, int parameterHash) {
            if (animator.runtimeAnimatorController == null || !animator.gameObject.activeInHierarchy) {
                return false;
            }
            return animator.parameters.Any(p => p.nameHash == parameterHash);
        }

        public static float GetTimeOfDay() {
            return Graphics.TimeOfDayPostProcessesController.dayNightCycle;
        }

        [UnityEngine.Scripting.Preserve]
        public static bool IsCurrentTimeLaterThanTime(int hour, int minute)
        {
            float testTime = hour/24f + minute/1440f;
            return GetTimeOfDay() > testTime;
        }

        public static T DefaultValue<T>(this ValueInput input) {
            return input.unit.defaultValues[input.key] is T t ? t : default;
        }

        [UnityEngine.Scripting.Preserve]
        public static void SetFlagValue(string flagName, bool state) {
            World.Services.Get<GameplayMemory>().Context().Set(flagName, state);
        }

        [UnityEngine.Scripting.Preserve]
        public static void DropItemsOnTheGround(NpcElement npc, bool dropOnlyImportantItems) => DropItemsOnTheGround(npc.ParentModel, dropOnlyImportantItems);

        public static void DropItemsOnTheGround(Location location, bool dropOnlyImportantItems) {
            location.TryGetElement<SearchAction>()?.DropAllItemsAndDiscard(dropOnlyImportantItems);
        }

        // === Script Machine With
        [UnityEngine.Scripting.Preserve]
        public static TMachine GetMachineWith<T, TMachine>(this ICharacter character, T owner, ScriptGraphAsset flowGraph) where TMachine : ScriptMachineWith<T> where T : class {
            return character.GetMachineGO()?.GetMachineWith<T, TMachine>(owner, flowGraph);
        }
        public static TMachine GetMachineWith<T, TMachine>(this GameObject gameObject, T owner, ScriptGraphAsset flowGraph) where TMachine : ScriptMachineWith<T> where T : class {
            return gameObject.GetComponents<TMachine>().FirstOrDefault(sm => sm.Owner == owner && sm.nest.macro == flowGraph);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static TMachine CreateMachineWith<T, TMachine>(this ICharacter character, T owner, ScriptGraphAsset flowGraph) where TMachine : ScriptMachineWith<T> where T : class {
            return character.GetMachineGO()?.CreateMachineWith<T, TMachine>(owner, flowGraph);
        }
        public static TMachine CreateMachineWith<T, TMachine>(this GameObject gameObject, T owner, ScriptGraphAsset flowGraph) where TMachine : ScriptMachineWith<T> where T : class {
            var machine  = gameObject.AddComponent<TMachine>();
            machine.Owner = owner;
            machine.nest.source = GraphSource.Macro;
            machine.nest.macro = flowGraph;
            return machine;
        }

        [UnityEngine.Scripting.Preserve]
        public static void DestroyMachineWith<T, TMachine>(this ICharacter character, T owner, ScriptGraphAsset flowGraph) where TMachine : ScriptMachineWith<T> where T : class {
            character.GetMachineGO()?.DestroyMachineWith<T, TMachine>(owner, flowGraph);
        }
        public static void DestroyMachineWith<T, TMachine>(this GameObject gameObject, T owner, ScriptGraphAsset flowGraph) where TMachine : ScriptMachineWith<T> where T : class {
            var machine = gameObject.GetMachineWith<T, TMachine>(owner, flowGraph);
            if (machine != null) {
                Object.Destroy(machine);
            }
        }
        
        // === Unit Values

        public static IEnumerable<T> Units<T>(this FlowGraph graph) {
            return graph.units.OfType<T>();
        }
        
        public static T Unit<T>(this FlowGraph graph) {
            return graph.Units<T>().FirstOrDefault();
        }

        [UnityEngine.Scripting.Preserve]
        public static T Value<T>(this ValueInput input, IGraphRoot root) {
            return input.Value<T>(GraphReference.New(root, false));
        }
        public static T Value<T>(this ValueInput input, GraphReference reference) {
            using var flow = Flow.New(reference);
            return flow.GetValue<T>(input);
        }

        [UnityEngine.Scripting.Preserve]
        public static void Trigger(this ControlOutput output, IGraphRoot root) {
            SafeGraph.Run(AutoDisposableFlow.New(GraphReference.New(root, false)), output);
        }
        
        public static T GetValue<T>(this ValueInput input, Flow flow, Func<Flow, T> fallback) {
            return input.hasValidConnection ? flow.GetValue<T>(input) : fallback(flow);
        }

        public static void CopyFlowVariables(Flow from, Flow to) {
            foreach (var variable in from.variables) {
                to.variables.Set(variable.name, variable.value);
            }
        }

        public static CharacterStatuses.AddResult ApplyStatus(CharacterStatuses characterStatuses, StatusTemplate template,
            StatusSourceInfo sourceInfo, float buildupStrength, IDuration duration, SkillVariablesOverride overrides) {
            CharacterStatuses.AddResult result;
            if (template.IsBuildupAble) {
                result = characterStatuses.BuildupStatus(buildupStrength, template, sourceInfo);
            } else {
                result = characterStatuses.AddStatus(template, sourceInfo, duration, overrides);
            }
            return result;
        }
        
        public static CharacterStatuses.AddResult ApplyStatus(CharacterStatuses characterStatuses, StatusTemplate template,
            StatusSourceInfo sourceInfo, float buildupStrength, float duration, SkillVariablesOverride overrides) {
            CharacterStatuses.AddResult result;
            if (template.IsBuildupAble) {
                result = characterStatuses.BuildupStatus(buildupStrength, template, sourceInfo);
            } else {
                var iduration = template.TryGetDuration();
                if (duration > 0) {
                    if (iduration == null) {
                        Log.Important?.Error($"StatusTemplate {LogUtils.GetDebugName(template)} has no Duration Attachment component but a duration change was attempted");
                    } else if (iduration is TimeDuration timeDuration) {
                        timeDuration.Renew(duration);
                    } else {
                        Log.Important?.Error($"StatusTemplate {LogUtils.GetDebugName(template)} has no TimeDuration component, cannot renew duration");
                    }
                }
                result = characterStatuses.AddStatus(template, sourceInfo, iduration, overrides);
            }
            return result;
        }

        // === VisualScripting <-> Code
        
        /// <summary>
        /// Tries it's best to convert object to given type.
        /// Designed to help designers with working on VS, so that they don't have to consider types so much. 
        /// </summary>
        public static T Convert<T>(this object o) where T : class, IModel {
            IModel model = o switch {
                GameObject go => VGUtils.GetModel(go),
                Component c => VGUtils.GetModel(c.gameObject),
                IModel m => m,
                _ => throw new ArgumentException($"Cannot convert from object of type {o?.GetType().FullName} to Model")
            };

            if (model == null) {
                throw new NullReferenceException($"Couldn't find Model from object of type {o?.GetType().FullName}");
            }
            T result = ModelUtils.FindInHierarchy<T>(model);
            if (result == null) {
                throw new NullReferenceException($"Couldn't find model of type {typeof(T).FullName} in hierarchy of Model {model.ID} (type {model.GetType()})");
            }
            return result;
        }

        public static IModel GetModel(GameObject gameObject) {
            return gameObject == null ? null : gameObject.GetComponentInParent<IModelProvider>(true)?.Model;
        }
        
        public static T GetModel<T>(GameObject gameObject) where T : class, IModel {
            IModel model = GetModel(gameObject);
            return model switch {
                T t => t,
                Location location => location.TryGetElement<T>(),
                Item item => GetModelFromItem<T>(item),
                MountElement mountElement => mountElement.MountedHero as T,
                null => null,
                Element ele => ele.GetModelInParent<T>() ?? throw new ConversionException(model, gameObject, typeof(T)),
                _ => throw new ConversionException(model, gameObject, typeof(T))
            };
        }

        public static bool TryGetModel<T>(GameObject gameObject, out T model) where T : class, IModel {
            model = TryGetModel<T>(gameObject);
            return model != null;
        }
        
        public static T TryGetModel<T>(GameObject gameObject) where T : class, IModel {
            IModel model = GetModel(gameObject);
            return model switch {
                T t => t,
                Location location => location.TryGetElement<T>(),
                Item item => GetModelFromItem<T>(item),
                MountElement mountElement => mountElement.MountedHero as T,
                Element ele => ele.GetModelInParent<T>() ?? null,
                _ => null,
            };
        }
        
        static T GetModelFromItem<T>(Item item) where T : class, IModel {
            if (item.Owner is T itemOwner) {
                return itemOwner;
            }
            if (item.Owner?.Character is T itemCharacter) {
                return itemCharacter;
            }
            return item.Owner?.TryGetElement<T>();
        }

        public static T My<T>(Flow flow) where T : class, IModel {
            return GetModel<T>(flow.stack.self);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void FastTravelTo(Portal portal, bool withTransition = true) {
            if (portal == null) {
                Log.Important?.Error("Portal is null");
                return;
            }
            Portal.FastTravel.To(Hero.Current, portal, withTransition).Forget();
        }

        class ConversionException : Exception {
            public ConversionException(IModel model, GameObject go, Type t)
                : base($"Unknown conversion from {model?.GetType()} to {t}. Invoked on GameObject: {go.name}") { }
        }

        public struct ShootParams {
            public ICharacter shooter;
            // Projectile From Item Projectile
            public ItemProjectile itemProjectile;
            // Custom Projectile
            public ProjectileData customProjectile;
            public Vector3 startPosition;
            public Vector3 upDirection;
            public float fireStrength;
            public DamageTypeData damageTypeData;
            public EquipmentSlotType projectileSlotType;
            public RawDamageData rawDamageData;
            public float? damageAmount;
            public Item item;
            
            /// <summary>
            /// Use this to get correct default values
            /// </summary>
            public static readonly ShootParams Default = new() {
                upDirection = Vector3.up,
                fireStrength = 1,
                damageTypeData = new DamageTypeData(DamageType.PhysicalHitSource),
                projectileSlotType = null,
                rawDamageData = null,
                damageAmount = null,
                item = null,
            };
            
            // === Fluent API
            public ShootParams WithItem(Item shootItem) {
                this.item = shootItem;
                if (item.TryGetElement<ItemProjectile>(out var itemProjectile)) {
                    this.itemProjectile = itemProjectile;
                }
                return this;
            }

            [UnityEngine.Scripting.Preserve]
            public ShootParams WithCustomProjectile(ShareableARAssetReference logicRef, ShareableARAssetReference visualRef, ProjectileLogicData? logicData = null, IEnumerable<SkillReference> skills = null) {
                this.customProjectile = new ProjectileData(logicRef, visualRef, skills, logicData ?? ProjectileLogicData.Default);
                return this;
            }
            
            public ShootParams WithCustomProjectile(ProjectileData data) {
                this.customProjectile = data;
                return this;
            }
        }
    }
}
