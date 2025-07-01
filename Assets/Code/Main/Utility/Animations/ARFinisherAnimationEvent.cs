using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.RichEnums;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Utility.Animations {
    public class ARFinisherAnimationEvent : ScriptableObject {
        public ARFinisherEffectsData arFinisherEffectsData;
    }

    [Serializable]
    public struct ARFinisherEffectsData {
        public SfxData[] sfxData;
        public VfxData[] vfxData;

        public void Play(Location target, Hero hero) {
            foreach (var vfx in vfxData) {
                vfx.Spawn(target, hero);
            }

            foreach (var sfx in sfxData) {
                sfx.Play(target, hero);
            }
        }
    }

    [Serializable]
    public struct VfxData {
        [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, group: AddressableGroup.VFX)] public ShareableARAssetReference vfxRef;
        [ShowIf(nameof(VfxSet))] public SpawnPosition spawnPosition;
        [ShowIf(nameof(VfxSet))] public Vector3 offset;
        [ShowIf(nameof(VfxSet))] public Vector3 rotationOffset;
        
        bool VfxSet => vfxRef is { IsSet: true };
        
        public void Spawn(Location target, Hero hero) {
            if (!VfxSet) {
                return;
            }

            Vector3 position = spawnPosition switch {
                SpawnPosition.TargetCoords => target.Coords,
                SpawnPosition.TargetHead => target.TryGetElement<NpcElement>()?.Head.position ?? target.Element<NpcDummy>().Head.position,
                SpawnPosition.TargetTorso => target.TryGetElement<NpcElement>()?.Torso.position ?? target.Element<NpcDummy>().Torso.position,
                SpawnPosition.MainHand => hero.MainHand.position,
                SpawnPosition.OffHand => hero.OffHand.position,
                SpawnPosition.FirePoint => hero.VHeroController.FirePoint.position,
                _ => throw new ArgumentOutOfRangeException()
            };

            Quaternion rotation = Quaternion.LookRotation(target.Coords - hero.Coords);
            if (offset != Vector3.zero) {
                position += rotation * offset;
            }
            if (rotationOffset != Vector3.zero) {
                rotation *= Quaternion.Euler(rotationOffset);
            }

            HandleVfxInSlowMotion(PrefabPool.Instantiate(vfxRef, position, rotation)).Forget();

            async UniTaskVoid HandleVfxInSlowMotion(UniTask<IPooledInstance> uniTask) {
                var result = await uniTask;
                if (result == null || result.Instance == null) {
                    return;
                }

                var VFXes = result.Instance.GetComponentsInChildren<VisualEffect>();
                float length = 5f;
                while (length > 0) {
                    float heroDeltaTime = hero.GetDeltaTime();
                    float deltaTimeDiff = heroDeltaTime - Time.deltaTime;
                    if (deltaTimeDiff > 0) {
                        foreach (var vfx in VFXes) {
                            vfx.Simulate(deltaTimeDiff);
                        }
                    }
                    length -= heroDeltaTime;
                    if (!await AsyncUtil.DelayFrame(hero)) {
                        result.Return();
                        return;
                    }
                }
                result.Return();
            }
        }
        
        [Serializable]
        public enum SpawnPosition {
            TargetCoords,
            TargetHead,
            TargetTorso,
            MainHand,
            OffHand,
            FirePoint,
        }
    }
    
    [Serializable]
    public struct SfxData {
        public SFXType sfxType;
        [ShowIf(nameof(CustomSfx))] public EventReference sfxRef;
        [ShowIf(nameof(MeleeHit)), RichEnumExtends(typeof(SurfaceType))] public RichEnumReference hitSurfaceType;
        
        bool CustomSfx => sfxType == SFXType.Custom;
        bool MeleeHit => sfxType is SFXType.MainHandMeleeHit or SFXType.OffHandMeleeHit;
        
        public void Play(Location target, Hero hero) {
            switch (sfxType) {
                case SFXType.MainHandMeleeHit:
                    PlayMeleeHitAudio(target, hero.MainHandItem);
                    break;
                case SFXType.OffHandMeleeHit:
                    PlayMeleeHitAudio(target, hero.OffHandItem);
                    break;
                case SFXType.Custom:
                    if (sfxRef is { IsNull: false }) {
                        target.LocationView.PlayAudioClip(sfxRef, true);
                    }
                    break;
                case SFXType.Hurt:
                    target.LocationView.PlayAudioClip(AliveAudioType.Hurt, true);
                    break;
                case SFXType.Die:
                    target.LocationView.PlayAudioClip(AliveAudioType.Die, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void PlayMeleeHitAudio(Location target, Item item) {
            if (item == null) {
                return;
            }
            EventReference eventReference = ItemAudioType.MeleeHit.RetrieveFrom(item);
            FMODParameter[] parameters = { hitSurfaceType.EnumAs<SurfaceType>(), new("Heavy", false) };
            item.PlayAudioClip(eventReference, true, parameters);
        }
        
        [Serializable]
        public enum SFXType {
            MainHandMeleeHit,
            OffHandMeleeHit,
            Custom,
            Hurt,
            Die,
        }
    }
}