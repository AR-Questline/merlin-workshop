using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.AudioSystem;
using UnityEngine;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.MVC.Events;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class DigOutAction : Element<Location>, IRefreshedByAttachment<DigOutAttachment> {
        public override ushort TypeForSerialization => SavedModels.DigOutAction;

        // === Fields
        string[] _objectsToEnableAfterDigging;
        string[] _objectsToDisableAfterDigging;
        string _objectToDig;
        Vector3 _hiddenPosition;
        Quaternion _hiddenRotation;
        EventReference _digUpSound;
        ShareableARAssetReference _digUpVfx;
        StoryBookmark _storyOnDugOut;
        
        [Saved] float _diggingPercentage, _maxHp;
        [Saved] bool _dugOut;
        
        GameObject _baseGameObject;
        Transform _objectToDigOut;
        ToolInteractAction _toolInteractAction;
        // === Properties
        Transform ObjectToDigOut {
            get {
                if (_objectToDigOut == null) {
                    _objectToDigOut = _baseGameObject.FindChildRecursively(_objectToDig);
                }
                return _objectToDigOut;
            }
        }
        SearchAction SearchAction => ParentModel.TryGetElement<SearchAction>();

        // === Constructors
        public void InitFromAttachment(DigOutAttachment spec, bool isRestored) {
            _objectToDig = spec.objectToMove.name;
            _hiddenPosition = spec.objectToMove.localPosition;
            _hiddenRotation = spec.objectToMove.localRotation;
            _digUpSound = spec.digUpSound;
            _digUpVfx = spec.digUpVFX;
            _storyOnDugOut = spec.StoryOnDugOut;

            _objectsToEnableAfterDigging = spec.objectsToEnableAfterDigging?.Select(o => o.name).ToArray() ?? Array.Empty<string>();
            _objectsToDisableAfterDigging = spec.objectToDisableAfterDigging?.Select(o => o.name).ToArray() ?? Array.Empty<string>();
        }

        // === Execution
        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(AttachCallbacks);
            _toolInteractAction = ParentModel.AddElement(new ToolInteractAction(ToolType.Digging));
            _toolInteractAction.MarkedNotSaved = true;
        }

        void AttachCallbacks(Transform parentTransform) {
            _baseGameObject = parentTransform.gameObject;
            IAlive alive = ParentModel.TryGetElement<IAlive>();
            if (alive == null) {
                Log.Important?.Error($"Failed to initialize DigOutAction! IAlive is null in location: {ParentModel}");
                return;
            }
            _maxHp = alive.AliveStats.MaxHealth;
            alive.HealthElement.ListenTo(HealthElement.Events.TakingDamage, OnTakingDamage, this);
            SearchAction?.SetSearchAvailable(_dugOut);
            UpdateVisuals(true);
        }

        void OnTakingDamage(HookResult<HealthElement, Damage> damageHook) {
            if (_dugOut) {
                damageHook.Prevent();
                return;
            }

            if (damageHook.Value.Type != DamageType.Interact) {
                damageHook.Prevent();
                return;
            }

            if (ToolType.Digging != damageHook.Value.Item.TryGetElement<Tool>()?.Type) {
                damageHook.Prevent();
                return;
            }


            if (TryDig(1 / _maxHp)) {
                OnDugOut();
            }

            damageHook.Prevent();
        }

        public void DigOutOverride() {
            if (_dugOut) return;
            
            _diggingPercentage = 1;
            OnDugOut();
            UpdateVisuals(true);
            if (!_digUpSound.IsNull) {
                FMODManager.PlayOneShot(_digUpSound, ParentModel.Coords);
            }
        }

        void OnDugOut() {
            if (_objectsToEnableAfterDigging != null) {
                foreach (string go in _objectsToEnableAfterDigging) {
                    Transform obj = _baseGameObject.FindChildRecursively(go, true);
                    if (obj != null) {
                        obj.gameObject.SetActive(true);
                    }
                }
            }

            if (_objectsToDisableAfterDigging != null) {
                foreach (string go in _objectsToDisableAfterDigging) {
                    Transform obj = _baseGameObject.FindChildRecursively(go, true);
                    if (obj != null) {
                        obj.gameObject.SetActive(false);
                    }
                }
            }

            _dugOut = true;
            SearchAction?.SetSearchAvailable(true);
            
            if (_storyOnDugOut != null) {
                Story.StartStory(StoryConfig.Base(_storyOnDugOut, null));
            }
            
            RemoveToolInteractionAction();
        }

        bool TryDig(float power) {
            _diggingPercentage += power;
            if (_diggingPercentage > 1f) {
                _diggingPercentage = 1f;
            }

            UpdateVisuals();
            if (!_digUpSound.IsNull) {
                FMODManager.PlayOneShot(_digUpSound, ParentModel.Coords);
            }

            if (_digUpVfx.IsSet) {
                PrefabPool.InstantiateAndReturn(_digUpVfx, ParentModel.Coords, Quaternion.identity).Forget();
            }

            return _diggingPercentage >= 1f;
        }

        void UpdateVisuals(bool instant = false) {
            ObjectToDigOut.DOLocalMove(Vector3.Lerp(_hiddenPosition, Vector3.zero, _diggingPercentage), instant ? 0 : 0.5f).SetUpdate(instant);
            ObjectToDigOut.DOLocalRotateQuaternion(Quaternion.Lerp(_hiddenRotation, Quaternion.identity, _diggingPercentage), instant ? 0 : 0.5f).SetUpdate(instant);
        }

        void RemoveToolInteractionAction() {
            if (_toolInteractAction is { HasBeenDiscarded: false }) {
                _toolInteractAction.Discard();
            }
            _toolInteractAction = null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            RemoveToolInteractionAction();
        }
    }
}
