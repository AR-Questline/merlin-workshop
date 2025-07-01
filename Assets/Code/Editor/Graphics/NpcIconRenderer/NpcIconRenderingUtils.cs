using System;
using System.IO;
using Animancer;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Main.Heroes.Items;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Editor.Graphics.NpcIconRenderer {
    public static class NpcIconRenderingUtils {
        internal const string Scene = "NPCIconRendering";
        const string RenderCameraName = "RenderCamera";
        const string RenderTextureGuid = "26126bf3dd3286e4ea096aecada35e6c";
        const string IconsOutputDir = "Assets/2DAssets/RawRenderedIcons/NpcIconsRenderOutput";

        static Entry s_currentEntry;
        static LocationTemplate s_currentLocationTemplate;
        static Location s_currentLocation;
        static AnimancerState s_currentAnimancerState;
        public static Action<LocationTemplate> locationIconRenderReady;
        public static Action iconRenderComplete;

        static bool s_isLoadingPreview;

        static CinemachineVirtualCamera s_renderCamera;
        static CinemachineVirtualCamera RenderCamera => s_renderCamera == null ? GameObject.Find(RenderCameraName).GetComponent<CinemachineVirtualCamera>() : s_renderCamera;

        static RenderTexture s_renderTexture;
        static RenderTexture RenderTexture => s_renderTexture ??= AssetDatabase.LoadAssetAtPath<RenderTexture>(AssetDatabase.GUIDToAssetPath(RenderTextureGuid));

        public static void TryUpdatePreview(Entry entry) {
            if (s_isLoadingPreview) {
                Log.Important?.Error("A preview is loading, please wait for it to finish");
                return;
            }

            PreviewEntry(entry);
        }

        public static void TryUpdateRotation() {
            if (s_currentLocation != null && s_currentLocation.Rotation != s_currentEntry.GetRotation()) {
                s_currentLocation.ViewParent.parent.rotation = s_currentEntry.GetRotation();
            }
        }

        public static void TryUpdateAnimDelta() {
            if (s_currentEntry != null && s_currentLocation != null && s_currentLocation.TryGetElement(out NpcElement npcElement)) {
                SetAnimatorState(s_currentEntry, npcElement);
            }
        }

        public static void TryUpdateCamera() {
            if (RenderCamera != null) {
                RenderCamera.transform.localPosition = s_currentEntry.CameraOffset;
            }
        }

        public static void TryUpdateState() {
            if (s_currentEntry != null && s_currentLocation != null && s_currentLocation.TryGetElement(out NpcElement npcElement)) {
                PreviewEntry(s_currentEntry);
            }
        }
        
        public static void TryUpdateWeaponRenderers() {
            if (s_currentEntry != null && s_currentLocation != null && s_currentLocation.TryGetElement(out NpcElement npcElement)) {
                if (s_currentEntry.RenderWeapons) {
                    if (npcElement.MainHandEqSlot) npcElement.MainHandEqSlot.localScale = Vector3.one;
                    if (npcElement.OffHandEqSlot) npcElement.OffHandEqSlot.localScale = Vector3.one;
                    if (npcElement.BackWeaponEqSlot) npcElement.BackWeaponEqSlot.localScale = Vector3.one;
                    if (npcElement.BackEqSlot) npcElement.BackEqSlot.localScale = Vector3.one;
                } else {
                    if (npcElement.MainHandEqSlot) npcElement.MainHandEqSlot.localScale = Vector3.zero;
                    if (npcElement.OffHandEqSlot) npcElement.OffHandEqSlot.localScale = Vector3.zero;
                    if (npcElement.BackWeaponEqSlot) npcElement.BackWeaponEqSlot.localScale = Vector3.zero;
                    if (npcElement.BackEqSlot) npcElement.BackEqSlot.localScale = Vector3.zero;
                }
            }
        }
        
        public static void TryUpdateBackgroundColor(Color color) {
            if (Camera.main != null && Camera.main.TryGetComponent(out HDAdditionalCameraData cameraData)) {
                cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                cameraData.backgroundColorHDR = color;
            }
        }

        public static async void PreviewEntry(Entry entry) {
            if (s_isLoadingPreview) {
                Log.Important?.Error("A preview is already loading, please wait for it to finish");
                return;
            }

            CleanAll();

            await UniTask.WaitUntil(() => s_currentLocation == null);

            RenderCamera.transform.localPosition = entry.CameraOffset;
            s_currentLocationTemplate = entry.GetLocationTemplate();
            s_currentEntry = entry;

            if (s_currentLocationTemplate == null) {
                Log.Important?.Error("Trying to spawn null template, are you sure Encounters Cache is properly baked?");
                return;
            }

            Vector3 spawnPoint = Vector3.zero;

            s_isLoadingPreview = true;
            s_currentLocation = s_currentLocationTemplate.SpawnLocation(spawnPoint);
            s_currentLocation.OnVisualLoaded(_ => {
                if (!s_currentLocation.TryGetElement(out NpcElement npcElement)) {
                    return;
                }

                SetAnimatorState(entry, npcElement);
                TryUpdateWeaponRenderers();
            });

            EnsureHudInvisibility();
        }

        static void SetAnimatorState(Entry entry, NpcElement npcElement) {
            npcElement.SetAnimatorState(NpcFSMType.GeneralFSM, entry.StateType, Mathf.Infinity);
            var substateMachine = npcElement.GetAnimatorSubstateMachine(NpcFSMType.GeneralFSM);
            SetAndPauseAnimAsync(substateMachine, entry.AnimDeltaTime, s_currentLocationTemplate).Forget();
        }

        static async UniTask SetAndPauseAnimAsync(NpcAnimatorSubstateMachine substateMachine, float animDeltaTime, LocationTemplate locationTemplate) {
            await UniTask.WaitUntil(() => substateMachine.CurrentAnimatorState != null && substateMachine.CurrentAnimatorState.CurrentState != null);

            // Set and pause animation
            s_currentAnimancerState = substateMachine.CurrentAnimatorState.CurrentState;
            s_currentAnimancerState.FadeSpeed = 0;
            s_currentAnimancerState.Weight = 1;
            s_currentAnimancerState.NormalizedTime = animDeltaTime;
            s_currentAnimancerState.IsPlaying = false;

            // Speed up time for a one-second so animation is set and clothes are not moving
            Time.timeScale = 10;
            await UniTask.Delay(1000, DelayType.Realtime);

            // Reset time scale
            Time.timeScale = 1;

            // update rotation
            s_currentLocation.ViewParent.parent.rotation = s_currentEntry.GetRotation();

            // Notify that preview is ready
            s_isLoadingPreview = false;
            locationIconRenderReady?.Invoke(locationTemplate);
        }

        public static void RenderAndAssignIcon(LocationTemplate locationTemplate) {
            if (!locationTemplate.TryGetComponent(out NpcAttachment npcAttachment)) {
                Log.Important?.Error("Tried to render icon for location without NpcAttachment");
                return;
            }

            Render(out string assetPath);
            TryAssignIconToTemplate(assetPath);

            iconRenderComplete?.Invoke();

            void Render(out string path) {
                // render
                Camera.main.targetTexture = RenderTexture;
                Camera.main.Render();
                RenderTexture.active = RenderTexture;
                Texture2D texture2D = new(RenderTexture.width, RenderTexture.height);
                texture2D.Apply(false);
                texture2D.alphaIsTransparency = true;
                texture2D.ReadPixels(new Rect(0, 0, Camera.main.pixelWidth, Camera.main.pixelHeight), 0, 0);
                RenderTexture.active = null;
                Camera.main.targetTexture = null;
                RenderTexture.DiscardContents();
                byte[] bytes = texture2D.EncodeToPNG();
                path = $"{IconsOutputDir}/{locationTemplate.name}_icon.png";
                File.WriteAllBytes(path, bytes);
                UnityEngine.Object.DestroyImmediate(texture2D);
            }

            void TryAssignIconToTemplate(string iconPath) {
                AssetDatabase.ImportAsset(iconPath);
                var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
                if (importer == null) {
                    Log.Important?.Error($"NpcIconRenderer: Couldn't import icon ({iconPath}) with {nameof(TextureImporter)}");
                    return;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.CompressedLQ;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (sprite == null) {
                    var obj = AssetDatabase.LoadMainAssetAtPath(iconPath);
                    Log.Important?.Error($"NpcIconRenderer: Couldn't load icon ({iconPath}) or load it as Sprite", obj);
                    return;
                }

                string guid = AddressableHelper
                    .AddEntry(new AddressableEntryDraft.Builder(sprite)
                        .InGroup("NpcIcons")
                        .WithAddressProvider((_, a) => ItemTemplateEditor.GetIconAddressName(a.MainAsset))
                        .WithLabels(ItemTemplateEditor.Labels)
                        .Build());

                npcAttachment.EDITOR_SetRenderIcon(new ShareableSpriteReference(guid));
            }
        }

        public static void CleanAll() {
            s_isLoadingPreview = false;
            s_currentLocationTemplate = null;
            s_currentAnimancerState = null;
            s_currentLocation?.Discard();
            s_currentLocation = null;
        }

        static void EnsureHudInvisibility() {
            HUD hud = World.Only<HUD>();
            hud.EDITOR_DEBUG_ShowHUD(false);

            if (Hero.Current.TryGetElement(out Awaken.TG.Main.Maps.Compasses.Compass compass)) {
                compass.EDITOR_DEBUG_ShowCompass(false);
            }
        }
    }
}