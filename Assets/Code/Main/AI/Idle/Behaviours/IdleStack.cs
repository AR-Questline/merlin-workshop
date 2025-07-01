using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle.Behaviours {
    public class IdleStack {
        Stack<IdleStackItem> _stack = new ();
        IdleBehaviours _behaviours;
        NpcInteractor _interactor;
        INpcInteraction _poppingInteraction;
        bool _shouldDropLater;
        bool _isDropping;
        IInteractionSource _currentSource;
        InteractionStartReason _startReason;
        
        List<IdleStackItem> _pushWaitingItems = new();
        bool _activateNextItem;
        
        NpcElement Npc => _behaviours.ParentModel;
        public IInteractionSource Source => _currentSource;
        public bool IsCurrentStackEmpty => !_stack.Any(i => i.IsValid);
        public bool IsWaitingStackEmpty => _pushWaitingItems.Count == 0;
        public bool WaitingStackHasAnchor => _pushWaitingItems.Any(i => i.isAnchor);
        [UnityEngine.Scripting.Preserve] public bool IsEmpty => IsCurrentStackEmpty && IsWaitingStackEmpty;
        public bool IsDropping => _isDropping;
        public int MaxPriority => Mathf.Max(IsCurrentStackEmpty ? -1 : _stack.Max(i => i.interaction.Priority), IsWaitingStackEmpty ? -1 : _pushWaitingItems.Max(i => i.interaction.Priority));
        public bool HasAnchor => _stack.Any(i => i.isAnchor) || _pushWaitingItems.Any(i => i.isAnchor);
        
        public IdleStack(IdleBehaviours behaviours) {
            _behaviours = behaviours;
            _interactor = _behaviours.Npc.Interactor;
        }

        public bool TryDrop(IInteractionSource source, bool ignoreEquality = false, InteractionStopReason reason = InteractionStopReason.ChangeInteraction) {
            if (_isDropping) {
                return false;
            }
            
            if (!ignoreEquality && source.Equals(_currentSource)) {
                if (_interactor.CurrentInteraction == null) {
                    UpdateActiveInteraction(false);
                }
                return false;
            }
            
            if (CanBeDropped(source)) {
                Drop(source, reason).Forget();
                return true;
            }

            _shouldDropLater = true;
            return false;
        }

        public async UniTask Drop(IInteractionSource source, InteractionStopReason reason = InteractionStopReason.ChangeInteraction, bool toAnchor = false) {
            _pushWaitingItems.Clear();
            if (_isDropping) {
                return;
            }
            _isDropping = true;
            bool ignoreDelay = reason is InteractionStopReason.Death 
                or InteractionStopReason.MySceneUnloading 
                or InteractionStopReason.NPCPresenceDisabled 
                or InteractionStopReason.StoppedIdlingInstant;
            while (_stack.Count > 0) {
                if (toAnchor && _stack.Peek().isAnchor) {
                    break;
                }
                var item = Pop(reason);
                if (!ignoreDelay) {
                    var npc = _behaviours.Npc;
                    while (item.interaction?.IsStopping(npc) ?? false) {
                        if (!await AsyncUtil.DelayFrame(npc, 1)) {
                            _isDropping = false;
                            return;
                        }
                    }
                }
            }

            _isDropping = false;
            _shouldDropLater = false;
            _currentSource = source;
        }
        
        public INpcInteraction Peek() {
            while (_stack.Count > 0) {
                if (_stack.Peek().IsValid) {
                    return _stack.Peek().interaction;
                }
                Pop();
            }
            return null;
        }

        public void Push(INpcInteraction interaction, bool activate = true, bool isAnchor = false, InteractionStartReason reason = InteractionStartReason.ChangeInteraction) {
            if (interaction == null) {
                return;
            }

            if (interaction == _poppingInteraction) {
                _poppingInteraction = null;
            }

            _startReason = reason;
            Book(interaction);
            var stackItem = new IdleStackItem(interaction, isAnchor);
            
            _pushWaitingItems.Add(stackItem);
            _activateNextItem = _activateNextItem || activate;
            if (_pushWaitingItems.Count > 1) {
                return;
            }
            DelayPush().Forget();
        }

        async UniTaskVoid DelayPush() {
            var npc = _behaviours.Npc;
            while (IsDropping || _poppingInteraction != null) {
                if (!await AsyncUtil.DelayFrame(npc, 1)) {
                    return;
                }
            }
            
            foreach (var item in _pushWaitingItems) {
                _stack.Push(item);
            }
            _pushWaitingItems.Clear();
            
            if (_activateNextItem) {
                _activateNextItem = false;
                UpdateActiveInteraction(true);
            }
        }

        bool CanBeDropped(IInteractionSource source) {
            return (_currentSource == null && !HasAnchor) || CanBeInterrupted();
        }
        
        bool CanBeInterrupted() {
            return _stack.All(item => item.interaction.CanBeInterrupted && !item.isAnchor) && 
                   _pushWaitingItems.All(item => item.interaction.CanBeInterrupted && !item.isAnchor);
        }

        void InteractionHasEnded() {
            PopAndPerform(waitForEnd: true).Forget();
        }

        async UniTask PopAndPerform(InteractionStopReason stopReason = InteractionStopReason.ChangeInteraction, bool waitForEnd = false) {
            _poppingInteraction = Pop(stopReason).interaction;
            
            if (_shouldDropLater && CanBeInterrupted()) {
                _behaviours.RefreshCurrentBehaviour();
            } else {
                InteractionStartReason startReason;
                if (_poppingInteraction is ITempInteraction tempInteraction) {
                    startReason = tempInteraction.FastStart ? InteractionStartReason.NPCActivated : InteractionStartReason.ChangeInteraction;
                } else {
                    startReason = InteractionStartReason.ResumeInteraction;
                }
                UpdateActiveInteraction(false, startReason);
            }

            if (!waitForEnd) {
                _poppingInteraction = null;
                return;
            }
            
            var npc = _behaviours.Npc;
            while (_poppingInteraction?.IsStopping(npc) ?? false) {
                if (!await AsyncUtil.DelayFrame(npc, 1)) {
                    break;
                }
            }
            _poppingInteraction = null;
        }
        
        IdleStackItem Pop(InteractionStopReason reason = InteractionStopReason.ChangeInteraction) {
            try {
                IdleStackItem stackItem = _stack.Pop();
                Npc.Interactor.TryStop(stackItem.interaction, reason, false);
                Unbook(stackItem.interaction);
                return stackItem;
            } catch (InvalidOperationException invalidOperationException) {
                LogWarningInfo();
                Debug.LogWarning(invalidOperationException);
                return new IdleStackItem();
            } catch (Exception e) {
                LogErrorInfo();
                Debug.LogException(e);
                return new IdleStackItem();
            }
        }

        public InteractionBookingResult Book(INpcInteraction interaction) {
            var result = interaction.Book(_behaviours.Npc);
            if (result is InteractionBookingResult.ProperlyBooked) {
                interaction.OnInternalEnd += InteractionHasEnded;
            } else if (result is InteractionBookingResult.CannotBeBooked or InteractionBookingResult.AlreadyBookedByOtherNpc) {
                Log.Important?.Error($"Interaction Booking Failed - {interaction} cannot be booked by {_behaviours.Npc}");
            }
            return result;
        }

        void Unbook(INpcInteraction interaction) {
            if (_stack.Any(s => s.interaction == interaction)) {
                Log.Minor?.Info($"Unbooking Failed - {interaction} Interaction still in stack");
                return;
            }
            if (_pushWaitingItems.Any(s => s.interaction == interaction)) {
                Log.Minor?.Info($"Unbooking Failed - {interaction} Interaction still in waiting stack");
                return;
            }
            interaction.Unbook(_behaviours.Npc);
            interaction.OnInternalEnd -= InteractionHasEnded;
        }

        public bool TryToPerformAgain(InteractionStartReason startReason) {
            bool forceResume = startReason is InteractionStartReason.NPCActivated or InteractionStartReason.NPCEndedCombat;
            INpcInteraction interaction = Peek();
            bool result = _interactor.TryToPerformAgain(interaction, forceResume, out bool useRotateFirst);
            if (result && useRotateFirst) {
                var tempInteractions = _behaviours.SelectTemporaryInteraction(interaction, _currentSource.Finder, startReason);
                if (tempInteractions != null) {
                    _behaviours.PushTemporaryInteractions(tempInteractions, InteractionStartReason.ResumeInteraction);
                }
            }
            return result;
        }

        void UpdateActiveInteraction(bool fromPush, InteractionStartReason? reason = null) {
            if (IsCurrentStackEmpty) {
                if (IsWaitingStackEmpty) {
                    _behaviours.RefreshCurrentBehaviour();
                }
                return;
            }
            INpcInteraction interaction = Peek();
            if (interaction == null) {
                _behaviours.RefreshCurrentBehaviour();
                return;
            }
            
            reason ??= fromPush ? _startReason : InteractionStartReason.ResumeInteraction;
            _interactor.Perform(interaction, reason.Value);
        }
        
        // --- Anchors
        
        public void SetAsAnchor(bool active) {
            if (_stack.Count != 0 && _stack.Peek().isAnchor != active) {
                var oldItem = _stack.Pop();
                _stack.Push(new IdleStackItem(oldItem.interaction, active));
            }
        }

        public async UniTask DropAnchor(InteractionStopReason reason = InteractionStopReason.ChangeInteraction) {
            if (_isDropping || _stack.Count == 0) {
                ClearWaitingStackToAnchor(true);
                return;
            }
            await DropToAnchor(reason, false);
            await PopAndPerform(reason);
        }

        public async UniTask DropToAnchor(InteractionStopReason reason = InteractionStopReason.ChangeInteraction, bool updateActive = true) {
            if (_isDropping) {
                ClearWaitingStackToAnchor(false);
                return;
            }
            int amountBefore = _stack.Count;
            await Drop(_currentSource, reason, toAnchor: true);
            
            if (amountBefore != _stack.Count && updateActive) {
                UpdateActiveInteraction(false);
            }
        }

        void ClearWaitingStackToAnchor(bool deleteAnchor) {
            if (!WaitingStackHasAnchor) {
                _pushWaitingItems.Clear();
                return;
            }
            
            int maxIndex = _pushWaitingItems.Count - 1;
            for (int i = maxIndex; i >= 0; i--) {
                if (_pushWaitingItems[i].isAnchor) {
                    if (deleteAnchor) {
                        _pushWaitingItems.RemoveRange(i, maxIndex - i + 1);
                    } else {
                        if (i + 1 >= maxIndex) {
                            return;
                        }
                        _pushWaitingItems.RemoveRange(i + 1, maxIndex - i);
                    }
                    return;
                }
            }
            _pushWaitingItems.Clear();
        }

        // --- Debug
        string LogInfo => $"Exception below happened for {Npc} {Npc.Name}.\nCurrent Interaction {Npc.Interactor.CurrentInteraction}\nStack {string.Join("\n", _stack.Select(s => s.interaction.ToString()))}\nSource {_currentSource}";

        void LogWarningInfo() {
            Log.Important?.Warning(LogInfo);
        }
        void LogErrorInfo() {
            Log.Important?.Error(LogInfo);
        }
    }
}
