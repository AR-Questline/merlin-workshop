using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.DayNightSystem;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Graphics.VFX.ShaderControlling;
using Awaken.TG.Graphics.VisualsPickerTool;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.Locations.Elevator;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.UI.UITooltips;
using Awaken.TG.Main.Utility.Animations.Blendshapes;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel;
using FMODUnity;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.IL2CPP.CompilerServices;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UniversalProfiling;

namespace Awaken.TG.MVC {
    [Il2CppEagerStaticClassConstruction]
    public class UnityUpdateProvider : IService {
        const int SpawnerUpdateBatchSize = 10;
        
        static readonly UniversalProfilerMarker UnityUpdateProviderUpdate = new UniversalProfilerMarker(ProfilerCategory.Scripts, "UnityUpdateProvider.Update");
        static readonly UniversalProfilerMarker UnityUpdateProviderLateUpdate = new UniversalProfilerMarker(ProfilerCategory.Scripts, "UnityUpdateProvider.LateUpdate");
        static readonly UniversalProfilerMarker UnityUpdateProviderFixedUpdate = new UniversalProfilerMarker(ProfilerCategory.Scripts, "UnityUpdateProvider.FixedUpdate");

        static readonly UniversalProfilerMarker DynamicLocationUpdate = new(ProfilerCategory.Scripts, "VDynamicLocation.Update");
        static readonly UniversalProfilerMarker NPCInteractionUpdate = new(ProfilerCategory.Scripts, "NpcInteraction.Update");
        static readonly UniversalProfilerMarker TextLinkHandlerUpdate = new(ProfilerCategory.Scripts, "TextLinkHandler.Update");
        static readonly UniversalProfilerMarker LocationSpawnerUpdate = new(ProfilerCategory.Scripts, "BaseLocationSpawner.Update");
        static readonly UniversalProfilerMarker AssetLoadingGateUpdate = new(ProfilerCategory.Scripts, "AssetLoadingGate.Update");
        static readonly UniversalProfilerMarker StudioEventEmitterUpdate = new(ProfilerCategory.Scripts, "StudioEventEmitter.Update");
        static readonly UniversalProfilerMarker CommuteToInteractionUpdate = new(ProfilerCategory.Scripts, "Commute.Update");
        static readonly UniversalProfilerMarker RotateToInteractionUpdate = new(ProfilerCategory.Scripts, "RotateTo.Update");
        static readonly UniversalProfilerMarker LocationDiscoveryUpdate = new(ProfilerCategory.Scripts, "LocationDiscovery.Update");
        static readonly UniversalProfilerMarker CullingGroupUpdate = new(ProfilerCategory.Scripts, "ICullingGroup.Update");
        static readonly UniversalProfilerMarker VCompassElementUpdate = new(ProfilerCategory.Scripts, "VCompassElement.Update");
        static readonly UniversalProfilerMarker GenericUpdate = new(ProfilerCategory.Scripts, "IWithUpdateGeneric.Update");
        static readonly UniversalProfilerMarker SplineRepellerUpdate = new(ProfilerCategory.Scripts, "WyrdnightSplineRepeller.Update");
        static readonly UniversalProfilerMarker LightControllersActiveUpdate = new(ProfilerCategory.Scripts, "LightController.ActiveUpdate");
        static readonly UniversalProfilerMarker LightControllersCulledUpdate = new(ProfilerCategory.Scripts, "LightController.CulledUpdate");
        static readonly UniversalProfilerMarker AdvancedUnityProviderUpdate = new(ProfilerCategory.Scripts, "AdvancedShaderController.TweenUpdate");
        static readonly UniversalProfilerMarker AudioZoneUpdate = new(ProfilerCategory.Scripts, "ManualAudioZone.Update");
        static readonly UniversalProfilerMarker BodyFeaturesUpdate = new(ProfilerCategory.Scripts, "BodyFeatures.Update");

        static readonly UniversalProfilerMarker GenericLateUpdate = new(ProfilerCategory.Scripts, "IWithLateUpdateGeneric.LateUpdate");
        static readonly UniversalProfilerMarker CopyBlendshapesLateUpdate = new(ProfilerCategory.Scripts, "CopyBlendShapes.LateUpdate");
        
        static readonly UniversalProfilerMarker ElevatorPlatformFixedUpdate = new(ProfilerCategory.Scripts, " ElevatorPlatform.FixedUpdate");


        // Here values are taken from profile sample from campaign map
        readonly List<VDynamicLocation> _dynamicLocations = new(900);
        readonly List<NpcInteractionWithUpdate> _npcInteractions = new(100);

        readonly List<TextLinkHandler> _textLinkHandlers = new(80);
        readonly List<BaseLocationSpawner> _locationSpawners = new(60);
        readonly List<AssetLoadingGate> _assetLoadingGates = new(30);
        readonly List<StudioEventEmitter> _studioEventEmitters = new(30);
        readonly List<CommuteToBase> _commuteToInteractions = new(30);
        readonly List<RotateToInteraction> _rotateToInteractions = new(30);
        readonly List<LocationDiscovery> _locationDiscoveries = new(30);
        readonly List<VCompassElement> _vCompassElements = new(800);
        readonly List<CopyBlendshapesRuntime> _copyBlendShapes = new(30);
        readonly List<IWithUpdateGeneric> _generics = new(30);
        readonly List<IWithLateUpdateGeneric> _lateGenerics = new(30);
        readonly List<BaseCullingGroup> _cullingGroups = new(4);
        readonly List<WyrdnightSplineRepeller> _splineRepellers = new(10);
        readonly List<LightController> _lightControllersActive = new(100);
        readonly List<LightController> _lightControllersCulled = new(100);
#if UNITY_EDITOR || AR_DEBUG
        readonly List<LightController> _DEBUG_lightControllersCulledStatic = new(100);
#endif
        readonly List<AdvancedShaderController> _advancedShaderControllers = new(10);
        readonly List<ManualAudioZone> _audioZones = new(10);
        readonly List<BodyFeatures> _bodyFeatures = new(100);
        
        readonly List<ElevatorPlatform> _elevatorPlatforms = new(4);

        readonly int _unscaledTimeId = Shader.PropertyToID("_UnscaledTime");

        static UnityUpdateProvider Instance { get; set; }
        
        int _spawnerUpdateCounter;

        public static void EDITOR_RuntimeReset() {
            Instance = null;
        }

        UnityUpdateProvider() {
            PlayerLoopUtils.RemoveFromPlayerLoop<UnityUpdateProvider, Update>();
            PlayerLoopUtils.RemoveFromPlayerLoop<UnityUpdateProvider, PreLateUpdate>();
            PlayerLoopUtils.RemoveFromPlayerLoop<UnityUpdateProvider, FixedUpdate>();
            PlayerLoopUtils.RegisterToPlayerLoopAfter<UnityUpdateProvider, Update, Update.ScriptRunBehaviourUpdate>(Update);
            PlayerLoopUtils.RegisterToPlayerLoopAfter<UnityUpdateProvider, PreLateUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate>(LateUpdate);
            PlayerLoopUtils.RegisterToPlayerLoopAfter<UnityUpdateProvider, FixedUpdate, FixedUpdate.ScriptRunBehaviourFixedUpdate>(FixedUpdate);
        }

        public static UnityUpdateProvider GetOrCreate() {
            if (Instance == null) {
                Instance = new UnityUpdateProvider();
            }

            return Instance;
        }

        [CanBeNull]
        public static UnityUpdateProvider TryGet() {
            return Instance;
        }

        void Update() {
            UnityUpdateProviderUpdate.Begin();
            Shader.SetGlobalFloat(_unscaledTimeId, Time.unscaledTime);

            float deltaTime = Time.deltaTime;

            try {
                DynamicLocationUpdate.Begin(_dynamicLocations.Count);
                for (int i = _dynamicLocations.Count - 1; i >= 0; i--) {
                    _dynamicLocations[i].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                DynamicLocationUpdate.End();
            }

            try {
                NPCInteractionUpdate.Begin(_npcInteractions.Count);
                for (int i = _npcInteractions.Count - 1; i >= 0; i--) {
                    _npcInteractions[i].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                NPCInteractionUpdate.End();
            }

            try {
                TextLinkHandlerUpdate.Begin(_textLinkHandlers.Count);
                for (int i = _textLinkHandlers.Count - 1; i >= 0; i--) {
                    _textLinkHandlers[i].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                TextLinkHandlerUpdate.End();
            }

            try {
                LocationSpawnerUpdate.Begin(_locationSpawners.Count);
                // update batch size amount of spawners
                int lastCountOrBatch = math.min(_locationSpawners.Count, _spawnerUpdateCounter + SpawnerUpdateBatchSize);
                for (int i = lastCountOrBatch - 1; i >= _spawnerUpdateCounter; i--) {
                    _locationSpawners[i].UnityUpdate();
                }
                _spawnerUpdateCounter += SpawnerUpdateBatchSize;
                if (_spawnerUpdateCounter >= _locationSpawners.Count) {
                    _spawnerUpdateCounter = 0;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                LocationSpawnerUpdate.End();
            }

            try {
                AssetLoadingGateUpdate.Begin(_assetLoadingGates.Count);
                for (int i = _assetLoadingGates.Count - 1; i >= 0; i--) {
                    _assetLoadingGates[i].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                AssetLoadingGateUpdate.End();
            }

            try {
                StudioEventEmitterUpdate.Begin(_studioEventEmitters.Count);
                for (int i = _studioEventEmitters.Count - 1; i >= 0; i--) {
                    _studioEventEmitters[i].UpdatePauseTracking();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                StudioEventEmitterUpdate.End();
            }

            try {
                CommuteToInteractionUpdate.Begin(_commuteToInteractions.Count);
                for (int i = _commuteToInteractions.Count - 1; i >= 0; i--) {
                    _commuteToInteractions[i].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                CommuteToInteractionUpdate.End();
            }

            try {
                RotateToInteractionUpdate.Begin(_rotateToInteractions.Count);
                for (int i = _rotateToInteractions.Count - 1; i >= 0; i--) {
                    _rotateToInteractions[i].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                RotateToInteractionUpdate.End();
            }

            var hero = Hero.Current;
            if (hero != null && LoadingScreenUI.IsLoading == false) {
                try {
                    LocationDiscoveryUpdate.Begin(_locationDiscoveries.Count);
                    for (int i = _locationDiscoveries.Count - 1; i >= 0; i--) {
                        _locationDiscoveries[i].UnityUpdate(hero);
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                } finally {
                    LocationDiscoveryUpdate.End();
                }
            }

            try {
                VCompassElementUpdate.Begin(_vCompassElements.Count);
                for (int i = _vCompassElements.Count - 1; i >= 0; i--) {
                    _vCompassElements[i].UnityUpdate(deltaTime);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                VCompassElementUpdate.End();
            }

            try {
                GenericUpdate.Begin(_generics.Count);
                for (int i = _generics.Count - 1; i >= 0; i--) {
                    _generics[i].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                GenericUpdate.End();
            }

            try {
                CullingGroupUpdate.Begin(_cullingGroups.Count);
                for (int i = _cullingGroups.Count - 1; i >= 0; i--) {
                    _cullingGroups[i].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                CullingGroupUpdate.End();
            }

            World.Services?.TryGet<NpcGrid>()?.Update(deltaTime);

            SplineRepellerUpdate.Begin(_splineRepellers.Count);
            try {
                for (int i = _splineRepellers.Count - 1; i >= 0; i--) {
                    _splineRepellers[i].UnityUpdate(deltaTime);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }

            SplineRepellerUpdate.End();
            int culledLightControllersCount = _lightControllersCulled.Count;
            LightControllersCulledUpdate.Begin(culledLightControllersCount);
            try {
                for (int i = culledLightControllersCount - 1; i >= 0; i--) {
                    _lightControllersCulled[i].CulledLightUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
            LightControllersCulledUpdate.End();

            var activeLightControllersCount = _lightControllersActive.Count;
            LightControllersActiveUpdate.Begin(activeLightControllersCount);
            try {
#if UNITY_EDITOR
                if (Application.isPlaying == false && LightController.EditorPreviewUpdates) {
                    for (int i = activeLightControllersCount - 1; i >= 0; i--) {
                        _lightControllersActive[i].EditorUpdate();
                    }
                } else 
#endif
                {
                    for (int i = activeLightControllersCount - 1; i >= 0; i--) {
                        _lightControllersActive[i].ActiveLightUpdate();
                    }
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
            LightControllersActiveUpdate.End();

#if UNITY_EDITOR || AR_DEBUG
            int culledStaticLightControllersCount = _DEBUG_lightControllersCulledStatic.Count;
            try {
                for (int i = culledStaticLightControllersCount - 1; i >= 0; i--) {
                    _DEBUG_lightControllersCulledStatic[i].DEBUG_CheckAndFixIfStaticIsMoving();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
#endif
#if UNITY_EDITOR
            try {
                EditorUpdate();
            } catch (Exception e) {
                Debug.LogException(e);
            }
#endif

            AdvancedUnityProviderUpdate.Begin(_advancedShaderControllers.Count);
            try {
                for (var index = _advancedShaderControllers.Count - 1; index >= 0; index--) {
                    _advancedShaderControllers[index].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                AdvancedUnityProviderUpdate.End();
            }
            
            try {
                AudioZoneUpdate.Begin(_audioZones.Count);
                for (int i = _audioZones.Count - 1; i >= 0; i--) {
                    _audioZones[i].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                AudioZoneUpdate.End();
            }
            
            try {
                BodyFeaturesUpdate.Begin(_bodyFeatures.Count);
                for (int i = _bodyFeatures.Count - 1; i >= 0; i--) {
                    _bodyFeatures[i].UnityUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                BodyFeaturesUpdate.End();
            }
            
            UnityUpdateProviderUpdate.End();
        }

        void LateUpdate() {
            UnityUpdateProviderLateUpdate.Begin();
            float deltaTime = Time.deltaTime;

            try {
                CopyBlendshapesLateUpdate.Begin(_copyBlendShapes.Count);
                for (int i = _copyBlendShapes.Count - 1; i >= 0; i--) {
                    _copyBlendShapes[i].UnityLateUpdate();
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                CopyBlendshapesLateUpdate.End();
            }

            try {
                GenericLateUpdate.Begin(_lateGenerics.Count);
                for (int i = _lateGenerics.Count - 1; i >= 0; i--) {
                    _lateGenerics[i].UnityLateUpdate(deltaTime);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                GenericLateUpdate.End();
            }

#if UNITY_EDITOR
            try {
                EditorLateUpdate();
            } catch (Exception e) {
                Debug.LogException(e);
            }
#endif
            UnityUpdateProviderLateUpdate.End();
        }

        void FixedUpdate() {
            UnityUpdateProviderFixedUpdate.Begin();
            float fixedDeltaTime = Time.fixedDeltaTime;
            
            try {
                ElevatorPlatformFixedUpdate.Begin(_elevatorPlatforms.Count);
                for (int i = _elevatorPlatforms.Count - 1; i >= 0; i--) {
                    _elevatorPlatforms[i].FixedUpdate(fixedDeltaTime);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                ElevatorPlatformFixedUpdate.End();
            }
            
            UnityUpdateProviderFixedUpdate.End();
        }
 
        public void RegisterDynamicLocation(VDynamicLocation dynamicLocation) {
            _dynamicLocations.AddUnique(dynamicLocation);
        }

        public void UnregisterDynamicLocation(VDynamicLocation dynamicLocation) {
            _dynamicLocations.Remove(dynamicLocation);
        }

        public void RegisterNpcInteraction(NpcInteractionWithUpdate npcInteraction) {
            _npcInteractions.Add(npcInteraction);
        }

        public void UnregisterNpcInteraction(NpcInteractionWithUpdate npcInteraction) {
            _npcInteractions.Remove(npcInteraction);
        }

        public void RegisterTextLinkHandler(TextLinkHandler textLinkHandler) {
            _textLinkHandlers.Add(textLinkHandler);
        }

        public void UnregisterTextLinkHandler(TextLinkHandler textLinkHandler) {
            _textLinkHandlers.Remove(textLinkHandler);
        }

        public void RegisterLocationSpawner(BaseLocationSpawner locationSpawner) {
            _locationSpawners.Add(locationSpawner);
        }

        public void UnregisterLocationSpawner(BaseLocationSpawner locationSpawner) {
            _locationSpawners.Remove(locationSpawner);
        }

        public void RegisterAssetLoadingGate(AssetLoadingGate assetLoadingGate) {
            _assetLoadingGates.Add(assetLoadingGate);
        }

        public void UnregisterAssetLoadingGate(AssetLoadingGate assetLoadingGate) {
            _assetLoadingGates.Remove(assetLoadingGate);
        }

        public void RegisterStudioEventEmitter(StudioEventEmitter studioEventEmitter) {
            // There won't be much emitters but single emitter could try to register multiple times
            // So the best is to register only if no present already, the same time contains shouldn't take much time
            if (!_studioEventEmitters.Contains(studioEventEmitter)) {
                _studioEventEmitters.Add(studioEventEmitter);
            }
        }

        public void UnregisterStudioEventEmitter(StudioEventEmitter studioEventEmitter) {
            _studioEventEmitters.Remove(studioEventEmitter);
        }

        public void RegisterCommuteToInteraction(CommuteToBase interaction) {
            _commuteToInteractions.Add(interaction);
        }

        public void UnregisterCommuteToInteraction(CommuteToBase interaction) {
            _commuteToInteractions.Remove(interaction);
        }

        public void RegisterRotateToInteraction(RotateToInteraction interaction) {
            _rotateToInteractions.Add(interaction);
        }

        public void UnregisterRotateToInteraction(RotateToInteraction interaction) {
            _rotateToInteractions.Remove(interaction);
        }

        public void RegisterLocationDiscovery(LocationDiscovery discovery) {
            _locationDiscoveries.Add(discovery);
        }

        public void UnregisterLocationDiscovery(LocationDiscovery discovery) {
            _locationDiscoveries.Remove(discovery);
        }

        public void RegisterCullingGroup(BaseCullingGroup cullingGroup) {
            _cullingGroups.Add(cullingGroup);
        }

        public void UnregisterCullingGroup(BaseCullingGroup cullingGroup) {
            _cullingGroups.Remove(cullingGroup);
        }

        public void RegisterVCompassElement(VCompassElement vCompassElement) {
            vCompassElement.UpdateIndex = _vCompassElements.Count;
            _vCompassElements.Add(vCompassElement);
        }

        public void UnregisterVCompassElement(VCompassElement vCompassElement) {
            int indexToChange = vCompassElement.UpdateIndex;

            if (indexToChange == -1) {
                Log.Critical?.Error("Trying to unregister VCompassElement that is not registered");
                return;
            }

            if (indexToChange == -2) return; // element not yet initialized

            _vCompassElements[^1].UpdateIndex = indexToChange;
            _vCompassElements.RemoveAtSwapBack(indexToChange);
            vCompassElement.UpdateIndex = -1;
        }

        public void RegisterCopyBlendshapes(CopyBlendshapesRuntime copyBlendshapesEditor) {
            _copyBlendShapes.Add(copyBlendshapesEditor);
        }

        public void UnregisterCopyBlendshapes(CopyBlendshapesRuntime copyBlendshapesEditor) {
            _copyBlendShapes.Remove(copyBlendshapesEditor);
        }

        public void RegisterSplineRepeller(WyrdnightSplineRepeller splineRepeller) {
            _splineRepellers.Add(splineRepeller);
        }

        public void UnregisterSplineRepeller(WyrdnightSplineRepeller splineRepeller) {
            _splineRepellers.Remove(splineRepeller);
        }

        public void RegisterGeneric(IWithUpdateGeneric generic) {
            _generics.Add(generic);
        }

        public void UnregisterGeneric(IWithUpdateGeneric generic) {
            _generics.Remove(generic);
        }

        public void RegisterLateGeneric(IWithLateUpdateGeneric generic) {
            _lateGenerics.Add(generic);
        }

        public void UnregisterLateGeneric(IWithLateUpdateGeneric generic) {
            _lateGenerics.Remove(generic);
        }

        public void RegisterLightControllerActive(LightController lightController) {
            _lightControllersActive.Add(lightController);
        }

        public void UnregisterLightControllerActive(LightController lightController) {
            _lightControllersActive.RemoveSwapBack(lightController);
        }
        
        public void RegisterLightControllerCulled(LightController lightController) {
            _lightControllersCulled.Add(lightController);
        }

        public void UnregisterLightControllerCulled(LightController lightController) {
            _lightControllersCulled.RemoveSwapBack(lightController);
        }
        
        public void RegisterAudioZone(ManualAudioZone audioZone) {
            _audioZones.Add(audioZone);
        }
        
        public void UnregisterAudioZone(ManualAudioZone audioZone) {
            _audioZones.RemoveSwapBack(audioZone);
        }

        public void RegisterBodyFeatures(BodyFeatures bodyFeatures) {
            _bodyFeatures.Add(bodyFeatures);
        }

        public void UnRegisterBodyFeatures(BodyFeatures bodyFeatures) {
            _bodyFeatures.RemoveSwapBack(bodyFeatures);
        }

#if UNITY_EDITOR || AR_DEBUG
        public void DEBUG_RegisterLightControllerCulledStatic(LightController lightController) {
            _DEBUG_lightControllersCulledStatic.Add(lightController);
        }

        public void DEBUG_UnregisterLightControllerCulledStatic(LightController lightController) {
            _DEBUG_lightControllersCulledStatic.RemoveSwapBack(lightController);
        }
#endif
        public void RegisterAdvancedShaderController(AdvancedShaderController controller) {
            _advancedShaderControllers.Add(controller);
        }

        public void UnregisterAdvancedShaderController(AdvancedShaderController controller) {
            _advancedShaderControllers.RemoveSwapBack(controller);
        }
        
        public void RegisterElevatorPlatform(ElevatorPlatform platform) {
            _elevatorPlatforms.Add(platform);
        }

        public void UnregisterElevatorPlatform(ElevatorPlatform platform) {
            _elevatorPlatforms.RemoveSwapBack(platform);
        }

        public interface IWithUpdateGeneric {
            void UnityUpdate();
        }

        public interface IWithLateUpdateGeneric {
            void UnityLateUpdate(float deltaTime);
        }

        // === Editor
#if UNITY_EDITOR
        static readonly ProfilerMarker LocationSpecLateUpdate = new(ProfilerCategory.Scripts, "LocationSpec.LateUpdate");
        static readonly ProfilerMarker VisualPickerLateUpdate = new(ProfilerCategory.Scripts, "VisualPicker.LateUpdate");
        static readonly ProfilerMarker LocationSpawnerAttachmentLateUpdate = new(ProfilerCategory.Scripts, "LocationSpawnerAttachment.LateUpdate");
        static readonly ProfilerMarker GroupSpawnerAttachmentLateUpdate = new(ProfilerCategory.Scripts, "GroupSpawnerAttachment.LateUpdate");

        readonly List<LocationSpec> _EDITOR_LocationSpecs = new(100);
        readonly List<VisualsPicker> _EDITOR_VisualPickers = new(100);
        readonly List<LocationSpawnerAttachment> _EDITOR_LocationSpawnerAttachment = new(100);
        readonly List<GroupSpawnerAttachment> _EDITOR_GroupSpawnerAttachment = new(100);

        public void EDITOR_Register(LocationSpec registree) {
            _EDITOR_LocationSpecs.Add(registree);
        }

        public void EDITOR_Unregister(LocationSpec registree) {
            _EDITOR_LocationSpecs.Remove(registree);
        }

        public void EDITOR_Register(VisualsPicker registree) {
            _EDITOR_VisualPickers.Add(registree);
        }

        public void EDITOR_Unregister(VisualsPicker registree) {
            _EDITOR_VisualPickers.Remove(registree);
        }

        public void EDITOR_Register(LocationSpawnerAttachment registree) {
            _EDITOR_LocationSpawnerAttachment.Add(registree);
        }

        public void EDITOR_Unregister(LocationSpawnerAttachment registree) {
            _EDITOR_LocationSpawnerAttachment.Remove(registree);
        }

        public void EDITOR_Register(GroupSpawnerAttachment registree) {
            _EDITOR_GroupSpawnerAttachment.Add(registree);
        }

        public void EDITOR_Unregister(GroupSpawnerAttachment registree) {
            _EDITOR_GroupSpawnerAttachment.Remove(registree);
        }

        void EditorUpdate() {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }
        }

        void EditorLateUpdate() {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            var i = 0;
            var selection = UnityEditor.Selection.activeGameObject?.GetHashCode();
            try {
                LocationSpecLateUpdate.Begin(_EDITOR_LocationSpecs.Count);

                for (i = _EDITOR_LocationSpecs.Count - 1; i >= 0; i--) {
                    _EDITOR_LocationSpecs[i].UnityEditorLateUpdate(selection == _EDITOR_LocationSpecs[i].EDITOR_GameObject.GetHashCode());
                }
            } catch (Exception e) {
                Log.Critical?.Error($"Exception in LocationSpec.LateUpdate at index {i} - {_EDITOR_LocationSpecs[i]}");
                Debug.LogException(e);
                _EDITOR_LocationSpecs.RemoveAtSwapBack(i);
            } finally {
                LocationSpecLateUpdate.End();
            }

            try {
                VisualPickerLateUpdate.Begin(_EDITOR_VisualPickers.Count);
                if (!Application.isPlaying) {
                    for (i = _EDITOR_VisualPickers.Count - 1; i >= 0; i--) {
                        if (selection == _EDITOR_VisualPickers[i].EDITOR_GameObject.GetHashCode()) {
                            _EDITOR_VisualPickers[i].UnityEditorSelectedLateUpdate();
                            break;
                        }
                    }
                }
            } catch (Exception e) {
                Log.Critical?.Error($"Exception in VisualPicker.LateUpdate at index {i} - {_EDITOR_VisualPickers[i]}");
                Debug.LogException(e);
                _EDITOR_VisualPickers.RemoveAtSwapBack(i);
            } finally {
                VisualPickerLateUpdate.End();
            }

            try {
                LocationSpawnerAttachmentLateUpdate.Begin(_EDITOR_LocationSpawnerAttachment.Count);
                for (i = _EDITOR_LocationSpawnerAttachment.Count - 1; i >= 0; i--) {
                    _EDITOR_LocationSpawnerAttachment[i].UnityEditorLateUpdate();
                }
            } catch (Exception e) {
                Log.Critical?.Error($"Exception in VisualPicker.LateUpdate at index {i} - {_EDITOR_LocationSpawnerAttachment[i]}");
                Debug.LogException(e);
                _EDITOR_LocationSpawnerAttachment.RemoveAtSwapBack(i);
            } finally {
                LocationSpawnerAttachmentLateUpdate.End();
            }

            try {
                GroupSpawnerAttachmentLateUpdate.Begin(_EDITOR_GroupSpawnerAttachment.Count);
                for (i = _EDITOR_GroupSpawnerAttachment.Count - 1; i >= 0; i--) {
                    _EDITOR_GroupSpawnerAttachment[i].UnityEditorLateUpdate();
                }
            } catch (Exception e) {
                Log.Critical?.Error($"Exception in VisualPicker.LateUpdate at index {i} - {_EDITOR_GroupSpawnerAttachment[i]}");
                Debug.LogException(e);
                _EDITOR_GroupSpawnerAttachment.RemoveAtSwapBack(i);
            } finally {
                GroupSpawnerAttachmentLateUpdate.End();
            }
        }
#endif
    }
}