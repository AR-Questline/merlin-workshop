using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.TG.Main.Timing.ARTime;
using Cysharp.Threading.Tasks;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Universal;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Utils {
    public static class AsyncUtil {
        public static async UniTask<bool> BlockInputUntilDelay(IModel owner, float seconds, bool cancelable = true, bool ignoreTimeScale = false) {
            var modalBlocker = World.SpawnView(owner, typeof(VModalBlocker));
            CancellationTokenSource source = new CancellationTokenSource();

            if (cancelable) {
                owner.ListenTo(VModalBlocker.Events.ModalDismissed, source.Cancel, owner);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale, cancellationToken: source.Token).SuppressCancellationThrow();

            if (modalBlocker != null) {
                modalBlocker.Discard();
            }

            return source.IsCancellationRequested;
        }

        public static async UniTask UntilCancelled(CancellationToken token) {
            await UniTask.WaitUntilCanceled(token).SuppressCancellationThrow();
        }

        public static async UniTask<bool> UntilCancelled(IModel model, CancellationToken token) {
            await UntilCancelled(token);
            return model is {HasBeenDiscarded: false};
        }

        public static async UniTask<bool> UntilCancelled(UnityEngine.Object obj, CancellationToken token) {
            await UntilCancelled(token);
            return obj != null;
        }
        
        public static UniTask<bool> UntilCancelled(IModel model, CancellationTokenSource source) {
            return UntilCancelled(model, source.Token);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static UniTask<bool> UntilCancelled(UnityEngine.Object obj, CancellationTokenSource source) {
            return UntilCancelled(obj, source.Token);
        }

        public static async UniTask WaitForInput(IModel owner, CancellationTokenSource source = null) {
            var modalBlocker = World.SpawnView(owner, typeof(VWaitForInput));
            source ??= new CancellationTokenSource();
            owner.ListenTo(VModalBlocker.Events.ModalDismissed, source.Cancel, owner);
            await UntilCancelled(source.Token);
            if (modalBlocker != null) {
                modalBlocker.Discard();
            }
        }

        public static async UniTask<bool> DelayFrame(UnityEngine.Object obj, int frames = 1, CancellationToken cancellationToken = default) {
            bool wasCanceled = false;
            if (cancellationToken != default) {
                wasCanceled = await UniTask.DelayFrame(frames, cancellationToken: cancellationToken).SuppressCancellationThrow();
            } else {
                await UniTask.DelayFrame(frames);
            }

            return !wasCanceled && obj != null;
        }

        public static async UniTask<bool> DelayFrame(IModel model, int frames = 1, CancellationToken cancellationToken = default) {
            bool wasCanceled = false;
            if (cancellationToken != default) {
                wasCanceled = await UniTask.DelayFrame(frames, cancellationToken: cancellationToken).SuppressCancellationThrow();
            } else {
                await UniTask.DelayFrame(frames);
            }

            return !wasCanceled && model is {HasBeenDiscarded: false};
        }

        /// <summary>
        /// In seconds
        /// </summary>
        public static async UniTask<bool> DelayTime(IModel model, float seconds, bool ignoreTimeScale = false, CancellationTokenSource source = null) {
            bool wasCanceled = false;
            if (source != null) {
                wasCanceled = await UniTask.Delay((int) (seconds * 1000), ignoreTimeScale, cancellationToken: source.Token).SuppressCancellationThrow();
            } else {
                await UniTask.Delay((int) (seconds * 1000), ignoreTimeScale);
            }

            return !wasCanceled && model is {HasBeenDiscarded: false};
        }
        
        /// <summary>
        /// In seconds
        /// </summary>
        public static async UniTask<bool> DelayTime(IModel model, float seconds, CancellationToken token, bool ignoreTimeScale = false) {
            bool wasCanceled = await UniTask.Delay((int) (seconds * 1000), ignoreTimeScale, cancellationToken: token).SuppressCancellationThrow();

            return !wasCanceled && model is {HasBeenDiscarded: false};
        }
        
        /// <summary>
        /// In seconds
        /// </summary>
        public static async UniTask<bool> DelayTime(UnityEngine.Object obj, float seconds, CancellationToken token, bool ignoreTimeScale = false) {
            bool wasCanceled = await UniTask.Delay((int) (seconds * 1000), ignoreTimeScale, cancellationToken: token).SuppressCancellationThrow();

            return !wasCanceled && obj != null;
        }

        /// <summary>
        /// In seconds
        /// </summary>
        public static async UniTask<bool> DelayTime(UnityEngine.Object obj, float seconds, bool ignoreTimeScale = false,
            CancellationTokenSource source = null) {
            bool wasCanceled = false;
            if (source != null) {
                wasCanceled = await UniTask.Delay((int) (seconds * 1000), ignoreTimeScale, cancellationToken: source.Token).SuppressCancellationThrow();
            } else {
                await UniTask.Delay((int) (seconds * 1000), ignoreTimeScale);
            }

            return !wasCanceled && obj != null;
        }
        
        /// <summary>
        /// In seconds
        /// </summary>
        public static async UniTask<bool> DelayTimeWithModelTimeScale(IModel model, float seconds, CancellationTokenSource source = null) {
            while (seconds > 0) {
                await UniTask.DelayFrame(1);
                if (model is not { HasBeenDiscarded: false }) {
                    return false;
                }
                if (source?.IsCancellationRequested ?? false) {
                    return false;
                }
                seconds -= model.GetDeltaTime();
            }
            return true;
        }

        public static async UniTask<bool> DelayFrameOrTime(UnityEngine.Object obj, int frames = 1, int milliseconds = 50, bool ignoreTimeScale = true) {
            float time = Time.time;
            await UniTask.DelayFrame(frames);
            float timeDifference = Time.time - time;
            milliseconds -= (int) (timeDifference / 1000f);
            if (milliseconds > 0) {
                await UniTask.Delay(milliseconds, ignoreTimeScale);
            }

            return obj != null;
        }

        public static async UniTask<bool> DelayFrameOrTime(IModel model, int frames = 1, int milliseconds = 50, bool ignoreTimeScale = true) {
            float time = Time.time;
            await UniTask.DelayFrame(frames);
            float timeDifference = Time.time - time;
            milliseconds -= (int) (timeDifference / 1000f);
            if (milliseconds > 0) {
                await UniTask.Delay(milliseconds, ignoreTimeScale);
            }

            return model is { HasBeenDiscarded: false };
        }

        public static async UniTask<bool> WaitForEndOfFrame(MonoBehaviour behaviour, CancellationTokenSource source = null) {
            bool wasCanceled = false;
            if (source != null) {
                wasCanceled = await UniTask.WaitForEndOfFrame(behaviour, cancellationToken: source.Token).SuppressCancellationThrow();
            } else {
                await UniTask.WaitForEndOfFrame(behaviour);
            }

            return !wasCanceled && behaviour != null;
        }

        public static async UniTask<bool> WaitForPlayerLoopEvent(UnityEngine.Object obj, PlayerLoopTiming playerLoopTiming, CancellationTokenSource source = null) {
            bool wasCanceled = false;
            if (source != null) {
                wasCanceled = await UniTask.Yield(playerLoopTiming, cancellationToken: source.Token).SuppressCancellationThrow();
            } else {
                await UniTask.Yield(playerLoopTiming);
            }

            return !wasCanceled && obj != null;
        }

        public static async UniTask<bool> WaitWhile(GameObject gameObject, Func<bool> predicate, float? maxWaitTime = null, CancellationTokenSource source = null) {
            float initTime = Time.realtimeSinceStartup;

            bool TimeCondition() {
                return !maxWaitTime.HasValue || initTime + maxWaitTime > Time.realtimeSinceStartup;
            }

            bool WaitWhileCondition() {
                return TimeCondition() && gameObject != null && predicate.Invoke();
            }

            bool wasCanceled = false;
            if (source != null) {
                wasCanceled = await UniTask.WaitWhile(WaitWhileCondition, cancellationToken: source.Token).SuppressCancellationThrow();
            } else {
                await UniTask.WaitWhile(WaitWhileCondition);
            }

            return !wasCanceled && gameObject != null && TimeCondition();
        }

        public static async UniTask<bool> WaitWhile(IModel model, Func<bool> predicate, CancellationTokenSource source = null) {
            bool WaitWhileCondition() {
                return !model.HasBeenDiscarded && predicate.Invoke();
            }
            
            bool wasCanceled = false;
            if (source != null) {
                wasCanceled = await UniTask.WaitWhile(WaitWhileCondition, cancellationToken: source.Token).SuppressCancellationThrow();
            } else {
                await UniTask.WaitWhile(WaitWhileCondition);
            }

            return !wasCanceled && !model.HasBeenDiscarded;
        }

        public static async UniTask<bool> WaitUntil(IModel model, Func<bool> predicate, CancellationTokenSource source = null) {
            bool WaitUntilCondition() {
                return model.HasBeenDiscarded || predicate.Invoke();
            }
            bool wasCanceled = false;
            if (source != null) {
                wasCanceled = await UniTask.WaitUntil(WaitUntilCondition, cancellationToken: source.Token).SuppressCancellationThrow();
            } else {
                await UniTask.WaitUntil(WaitUntilCondition);
            }
            return !wasCanceled && !model.HasBeenDiscarded;
        }
        
        public static async UniTask<bool> WaitUntil(IModel model, Func<bool> predicate, CancellationToken token) {
            bool WaitUntilCondition() {
                return model.HasBeenDiscarded || predicate.Invoke();
            }
            
            bool wasCancelled = await UniTask.WaitUntil(WaitUntilCondition, cancellationToken: token).SuppressCancellationThrow();
            return !wasCancelled && !model.HasBeenDiscarded;
        }

        public static async UniTask<bool> WaitUntil(UnityEngine.Object unityObject, Func<bool> predicate) {
            bool WaitUntilCondition() {
                return unityObject == null || predicate.Invoke();
            }
            
            await UniTask.WaitUntil(WaitUntilCondition);
            return unityObject != null;
        }
        
        public static async UniTask<bool> WaitUntil(UnityEngine.Object unityObject, Func<bool> predicate, CancellationToken token) {
            bool WaitUntilCondition() {
                return unityObject == null || predicate.Invoke();
            }
            
            bool wasCancelled = await UniTask.WaitUntil(WaitUntilCondition, cancellationToken: token).SuppressCancellationThrow();
            return !wasCancelled && unityObject != null;
        }

        public static async UniTask CheckAndWaitUntil(Func<bool> predicate) {
            if (!predicate.Invoke()) {
                await UniTask.WaitUntil(predicate);
            }
        }
        
        public static async UniTask<bool> WaitForAll(IModel model, IEnumerable<UniTask> tasks) {
            IEnumerable<UniTask> uniTasks = tasks as UniTask[] ?? tasks.ToArray();
            foreach (var task in uniTasks) {
                if (task.Status == UniTaskStatus.Pending) {
                    await task;
                }
            }

            return !model.HasBeenDiscarded;
        }

        [UnityEngine.Scripting.Preserve]
        public static async UniTask<bool> WaitForAll(IModel model, IEnumerable<UniTask> tasks, CancellationToken token) {
            IEnumerable<UniTask> uniTasks = tasks as UniTask[] ?? tasks.ToArray();
            foreach (var task in uniTasks) {
                if (task.Status == UniTaskStatus.Pending) {
                    await task;
                }
            }
            
            return !model.HasBeenDiscarded && !token.IsCancellationRequested;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static async UniTask<bool> WaitForAll(UnityEngine.Object unityObject, IEnumerable<UniTask> tasks) {
            IEnumerable<UniTask> uniTasks = tasks as UniTask[] ?? tasks.ToArray();
            foreach (var task in uniTasks) {
                if (task.Status == UniTaskStatus.Pending) {
                    await task;
                }
            }

            return unityObject != null;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static async UniTask<bool> WaitForAll(UnityEngine.Object unityObject, IEnumerable<UniTask> tasks, CancellationToken token) {
            IEnumerable<UniTask> uniTasks = tasks as UniTask[] ?? tasks.ToArray();
            foreach (var task in uniTasks) {
                if (task.Status == UniTaskStatus.Pending) {
                    await task;
                }
            }

            return unityObject != null && !token.IsCancellationRequested;
        }

        public static async UniTask<T> WaitForElement<T>(this IModel model) where T : class, IModel {
            await UniTask.WaitUntil(() => model.HasBeenDiscarded || model.HasElement<T>());
            return model.HasBeenDiscarded ? null : model.Element<T>();
        }

        public static UniTask WaitForDiscard(IModel model) {
            return AsyncUtil.WaitWhile(model, static () => true);
        }

        public static void Forget(this Tween _) {
            //This method is empty because it is used to suppress the warning that the tween is not awaited
        }
        
        public static void CancelAndCreateToken(ref CancellationTokenSource tokenSource, out CancellationToken cancellationToken) {
            tokenSource?.Cancel();
            tokenSource = new CancellationTokenSource();
            cancellationToken = tokenSource.Token;
        }
    }
}
