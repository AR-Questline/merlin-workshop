using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    public class FMODManager : MonoBehaviour {
        // === Properties and fields
        static int s_audioSourceID;

        // === Singleton management
        static FMODManager s_instance;

        [UnityEngine.Scripting.Preserve]
        public static FMODManager Instance {
            get {
                if (s_instance == null) {
                    FMODManager audioManager = FindAnyObjectByType<FMODManager>();
                    if (audioManager == null) {
                        GameObject audioSystemGameObject = new GameObject
                            { name = $"{typeof(FMODManager)} [FMODManager]" };
                        DontDestroyOnLoad(audioSystemGameObject);
                        s_instance = audioSystemGameObject.AddComponent<FMODManager>();
                    } else {
                        s_instance = audioManager;
                    }
                }
                return s_instance;
            }
        }

        public static void PlayOneShot(EventReference eventReference, Vector3 position = new Vector3(), params FMODParameter[] parameters) {
            //PlayOneShot(eventReference.Guid, position, parameters);
        }

        public static void PlayOneShot(EventReference eventReference, Vector3 position = new Vector3(), UnityEngine.Object debugObject = null,
            params FMODParameter[] parameters) {
            //PlayOneShot(eventReference.Guid, position, parameters, new EventDebugData(eventReference.PathOrGuid, debugObject));
        }

        public static void PlayOneShot(EventReference eventReference, Vector3 position = new Vector3(), ICollection<FMODParameter> parameters = null,
            UnityEngine.Object debugObject = null) {
            //PlayOneShot(eventReference.Guid, position, parameters, new EventDebugData(eventReference.PathOrGuid, debugObject));
        }

        // static void PlayOneShot(GUID guid, Vector3 position = new Vector3(), FMODParameter[] parameters = null, EventDebugData debugData = default) {
        //     // if (RuntimeManager.TryCreateInstance(guid, out var instance, out var eventDescription, debugData) == false) {
        //     //     return;
        //     // }
        //     // instance.set3DAttributes(position.To3DAttributes());
        //     // SetEventInstanceParams(guid, instance, eventDescription, parameters);
        //     // instance.start();
        //     // RuntimeManager.ReleaseInstance(instance);
        // }

        // static void PlayOneShot(GUID guid, Vector3 position = new Vector3(), ICollection<FMODParameter> parameters = null, EventDebugData debugData = default) {
        //     // if (RuntimeManager.TryCreateInstance(guid, out var instance, debugData) == false) {
        //     //     return;
        //     // }
        //     // instance.set3DAttributes(position.To3DAttributes());
        //     // if (parameters != null) {
        //     //     foreach (var param in parameters) {
        //     //         instance.setParameterByName(param.name, param.value);
        //     //     }
        //     // }
        //     // instance.start();
        //     // RuntimeManager.ReleaseInstance(instance);
        // }

        public static async UniTask PlayOneShotAfter(EventReference eventReference, EventReference previousEventReference, UnityEngine.Object debugObject) {
            if (!previousEventReference.IsNull) {
                int length;
                // if (RuntimeManager.TryGetEventDescription(previousEventReference, out var desc, debugObject)) {
                //     desc.getLength(out length);
                // } else {
                //     length = 0;
                // }
                // if (length > 0) {
                //     await UniTask.Delay(length, DelayType.UnscaledDeltaTime);
                // }
            }

            PlayOneShot(eventReference);
        }

        [UnityEngine.Scripting.Preserve]
        public static void PlayAttachedOneShotWithParameters(EventReference eventReference, GameObject gameObject, params FMODParameter[] eventParams) {
            PlayAttachedOneShotWithParameters(eventReference, gameObject, null, eventParams);
        }

        public static void PlayAttachedOneShotWithParameters(EventReference eventReference, GameObject gameObject, UnityEngine.Object debugObject,
            params FMODParameter[] eventParams) {
            if (eventReference.IsNull) {
                return;
            }
            // try {
            //     if (RuntimeManager.TryCreateInstance(eventReference, out var instance, out var eventDescription, debugObject) == false) {
            //         return;
            //     }
            //
            //     SetEventInstanceParams(eventReference.Guid, instance, eventDescription, eventParams);
            //
            //     RuntimeManager.AttachInstanceToGameObject(instance, gameObject.transform);
            //     instance.start();
            //     RuntimeManager.ReleaseInstance(instance);
            // } catch (EventNotFoundException) {
            //     string gameObjectName = gameObject == null ? "Null gameObject" : gameObject.name;
            //     Log.Minor?.Error("[FMOD] Event not found: " + eventReference + " source: " + gameObjectName, gameObject);
            // }
        }

//        static void SetEventInstanceParams(GUID eventGuid, EventInstance instance, EventDescription eventDescription, FMODParameter[] eventParams) {
//             if (eventParams == null || eventParams.Length == 0) {
//                 return;
//             }
//
// #if UNITY_EDITOR || AR_DEBUG
//             var eventPath = RuntimeManager.DEBUG_GetEventPath(eventGuid);
// #endif
//             eventDescription.getParameterDescriptionCount(out int eventParamsRealCount);
//             if (eventParamsRealCount != 0) {
//                 foreach (FMODParameter param in eventParams) {
//                     if (param.name == "") {
//                         continue;
//                     }
//                     if (instance.getParameterByName(param.name, out var value) == RESULT.OK) {
//                         if (value != param.value) {
//                             instance.setParameterByName(param.name, param.value);
//                         }
//                     } else {
// #if UNITY_EDITOR || AR_DEBUG
//                         Log.Minor?.Warning($"[FMOD] Trying to set param {param.name} for fmod event {eventPath} but event does not have this param");
// #endif
//                     }
//                 }
//             }
// #if UNITY_EDITOR || AR_DEBUG
//             else {
//                 foreach (FMODParameter param in eventParams) {
//                     if (param.name == "") {
//                         continue;
//                     }
//                     Log.Minor?.Warning($"[FMOD] Trying to set param {param.name} for fmod event {eventPath} but event does not have this param");
//                 }
//             }
// #endif
//        }

        public static void PlayBlockAudio(Item item, IAlive damageReceiver, Item itemDealingDamage, bool isParry = false) {
            if (item == null) {
                return;
            }

            EventReference eventReference = ItemAudioType.BlockDamage.RetrieveFrom(item);
            if (eventReference.IsNull) {
                return;
            }

            FMODParameter[] eventParams;
            if (itemDealingDamage != null) {
                FMODParameter surface = itemDealingDamage.Template.DamageSurfaceType;
                FMODParameter parry = new("Parry", isParry);
                eventParams = new[] { surface, parry };
            } else {
                eventParams = Array.Empty<FMODParameter>();
            }
            damageReceiver.PlayAudioClip(eventReference, true, eventParams);
        }

        public static void PlayBodyMovement(IEnumerable<ArmorAudioType> armorAudio, ICharacter owner) {
            Item equippedTorso = owner.Inventory.EquippedItem(EquipmentSlotType.Cuirass);
            if (equippedTorso == null) {
                return;
            }

            foreach (ArmorAudioType armorAudioType in armorAudio.Where(a => a == ArmorAudioType.BodyMovement || a == ArmorAudioType.BodyMovementFast)) {
                SurfaceType surfaceType = equippedTorso.TryGetElement<ItemAudio>()?.ArmorSurfaceType;

                EventReference eventReference = armorAudioType.RetrieveFrom(equippedTorso);
                if (eventReference.IsNull) {
                    continue;
                }

                owner.PlayAudioClip(eventReference, true);
            }
        }
    }
}