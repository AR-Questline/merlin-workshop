// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>
    /// Bitwise flags used by <see cref="Validate.IsEnabled"/> and <see cref="Validate.Disable"/> to determine which
    /// warnings Animancer should give.
    /// <para></para>
    /// <strong>These warnings are all optional</strong>. Feel free to disable any of them if you understand the
    /// <em>potential</em> issues they are referring to.
    /// </summary>
    /// 
    /// <remarks>
    /// All warnings are enabled by default, but are compiled out of runtime builds (except development builds).
    /// <para></para>
    /// You can manually disable warnings using the Settings in the <see cref="Editor.Tools.AnimancerToolsWindow"/>
    /// (<c>Window/Animation/Animancer Tools</c>).
    /// </remarks>
    /// 
    /// <example>
    /// You can put the following method in any class to disable whatever warnings you don't want on startup:
    /// <para></para><code>
    /// #if UNITY_ASSERTIONS
    /// [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    /// private static void DisableAnimancerWarnings()
    /// {
    ///     Animancer.OptionalWarning.ProOnly.Disable();
    ///     
    ///     // You could disable OptionalWarning.All, but that is not recommended for obvious reasons.
    /// }
    /// #endif
    /// </code></example>
    /// https://kybernetik.com.au/animancer/api/Animancer/OptionalWarning
    /// 
    [Flags]
    public enum OptionalWarning
    {
        /// <summary>
        /// A <see href="https://kybernetik.com.au/animancer/docs/introduction/features">Pro-Only Feature</see> has been
        /// used in <see href="https://kybernetik.com.au/animancer/redirect/lite">Animancer Lite</see>.
        /// </summary>
        /// 
        /// <remarks>
        /// Some <see href="https://kybernetik.com.au/animancer/docs/introduction/features">Features</see> are only
        /// available in <see href="https://kybernetik.com.au/animancer/redirect/pro">Animancer Pro</see>.
        /// <para></para>
        /// <see href="https://kybernetik.com.au/animancer/redirect/lite">Animancer Lite</see> allows you to try out those
        /// features in the Unity Editor and gives this warning the first time each one is used to inform you that they
        /// will not work in runtime builds.
        /// </remarks>
        ProOnly = 1 << 0,

        /// <summary>
        /// An <see cref="AnimancerComponent.Playable"/> is being initialized while its <see cref="GameObject"/> is
        /// inactive.
        /// </summary>
        /// 
        /// <remarks>
        /// Unity will not call <see cref="AnimancerComponent.OnDestroy"/> if the <see cref="GameObject"/> is never
        /// enabled. That would prevent it from destroying the internal <see cref="PlayableGraph"/>, leading to a
        /// memory leak.
        /// <para></para>
        /// Animations usually shouldn't be played on inactive objects so you most likely just need to call
        /// <see cref="GameObject.SetActive(bool)"/> first.
        /// <para></para>
        /// If you do intend to use it while inactive, you will need to disable this warning and call
        /// <see cref="AnimancerComponent.OnDestroy"/> manually when the object is destroyed (such as when its scene is
        /// unloaded).
        /// </remarks>
        CreateGraphWhileDisabled = 1 << 1,

        /// <summary>
        /// An <see cref="AnimancerComponent.Playable"/> is being initialized during a type of GUI event that shouldn't
        /// cause side effects.
        /// </summary>
        /// 
        /// <remarks>
        /// <see cref="EventType.Layout"/> and <see cref="EventType.Repaint"/> should display the current details of
        /// things, but they should not modify things.
        /// </remarks>
        CreateGraphDuringGuiEvent = 1 << 2,

        /// <summary>
        /// The <see cref="AnimancerComponent.Animator"/> is disabled so Animancer won't be able to play animations.
        /// </summary>
        /// 
        /// <remarks>
        /// The <see cref="Animator"/> doesn't need an Animator Controller, it just needs to be enabled via the
        /// checkbox in the Inspector or by setting <c>animancerComponent.Animator.enabled = true;</c> in code.
        /// </remarks>
        AnimatorDisabled = 1 << 3,

        /// <summary>
        /// An <see cref="Animator.runtimeAnimatorController"/> is assigned but the Rig is Humanoid so it can't be
        /// blended with Animancer.
        /// </summary>
        /// 
        /// <remarks>
        /// <see href="https://kybernetik.com.au/animancer/docs/manual/animator-controllers#native">Native</see>
        /// Animator Controllers can blend with Animancer on Generic Rigs, but not on Humanoid Rigs (you can swap back
        /// and forth between the Animator Controller and Animancer, but it won't smoothly blend between them).
        /// <para></para>
        /// If you don't intend to blend between them, you can just disable this warning.
        /// </remarks>
        NativeControllerHumanoid = 1 << 4,

        /// <summary>
        /// An <see cref="Animator.runtimeAnimatorController"/> is assigned while also using a
        /// <see cref="HybridAnimancerComponent"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// Either assign the <see cref="Animator.runtimeAnimatorController"/> to use it as a Native Animator
        /// Controller or assign the <see cref="HybridAnimancerComponent.Controller"/> to use it as a Hybrid Animator
        /// Controller. The differences are explained in the
        /// <see href="https://kybernetik.com.au/animancer/docs/manual/animator-controllers">Documentation</see>
        /// <para></para>
        /// It is possible to use both, but it usually only happens when misunderstanding how the system works. If you
        /// do want both, just disable this warning.
        /// </remarks>
        NativeControllerHybrid = 1 << 5,

        /// <summary>
        /// An <see href="https://kybernetik.com.au/animancer/docs/manual/events/animancer">Animancer Event</see> is
        /// being added to an <see cref="AnimancerEvent.Sequence"/> which already contains an identical event.
        /// </summary>
        /// 
        /// <remarks>
        /// This warning often occurs due to a misunderstanding about the way events are
        /// <see href="https://kybernetik.com.au/animancer/docs/manual/events/animancer#auto-clear">Automatically
        /// Cleared</see>.
        /// <para></para>
        /// If you play an <see cref="AnimationClip"/>, its <see cref="AnimancerState.Events"/> will be empty so you
        /// can add whatever events you want.
        /// <para></para>
        /// But <see href="https://kybernetik.com.au/animancer/docs/manual/transitions">Transitions</see> store their own
        /// events, so if you play one then modify its <see cref="AnimancerState.Events"/> you are actually modifying
        /// the transition's events. Then if you play the same transition again, you will modify the events again,
        /// often leading to the same event being added multiple times.
        /// <para></para>
        /// If that is not the case, you can simply disable this warning. There is nothing inherently wrong with having
        /// multiple identical events in the same sequence.
        /// </remarks>
        DuplicateEvent = 1 << 6,

        /// <summary>
        /// An <see href="https://kybernetik.com.au/animancer/docs/manual/events/end">End Event</see> did not actually
        /// end the animation.
        /// </summary>
        /// 
        /// <remarks>
        /// <see href="https://kybernetik.com.au/animancer/docs/manual/events/end">End Events</see> are triggered every
        /// frame after their time has passed, so in this case it might be necessary to explicitly clear the event or
        /// simply use a regular <see href="https://kybernetik.com.au/animancer/docs/manual/events/animancer">Animancer Event</see>.
        /// <para></para>
        /// If you intend for the event to keep getting triggered, you can just disable this warning.
        /// </remarks>
        EndEventInterrupt = 1 << 7,

        /// <summary>
        /// An <see cref="AnimancerEvent"/> that does nothing was invoked. Most likely it was not configured correctly.
        /// </summary>
        /// 
        /// <remarks>
        /// Unused events should be removed to avoid wasting performance checking and invoking them.
        /// </remarks>
        UselessEvent = 1 << 8,

        /// <summary>
        /// An <see cref="AnimancerEvent.Sequence"/> is being modified even though its
        /// <see cref="AnimancerEvent.Sequence.ShouldNotModifyReason"/> is set.
        /// </summary>
        /// 
        /// <remarks>
        /// This is primarily used by transitions. Their events should generally be configured on startup rather
        /// than repeating the setup on the state after the transition is played because such modifications will apply
        /// back to the transition's events (which is usually not intended).
        /// </remarks>
        LockedEvents = 1 << 9,

        /// <summary>
        /// <see href="https://kybernetik.com.au/animancer/docs/manual/events/animancer">Animancer Events</see> are
        /// being used on a state that does not properly support them so they might not work as intended.
        /// </summary>
        /// 
        /// <remarks>
        /// <see href="https://kybernetik.com.au/animancer/docs/manual/events/animancer">Animancer Events</see> on a
        /// <see cref="ControllerState"/> will be triggered based on its <see cref="AnimancerState.NormalizedTime"/>,
        /// which comes from the current state of its Animator Controller regardless of which state that may be.
        /// <para></para>
        /// If you intend for the event to be associated with a specific state inside the Animator Controller, you need
        /// to use <see href="https://kybernetik.com.au/animancer/docs/manual/events/animation">Animation Events</see>
        /// instead.
        /// <para></para>
        /// But if you intend the event to be triggered by any state inside the Animator Controller, then you can
        /// simply disable this warning.
        /// </remarks>
        UnsupportedEvents = 1 << 10,

        /// <summary><see cref="AnimancerNode.Speed"/> is being used on a state that doesn't support it.</summary>
        /// 
        /// <remarks>
        /// <see cref="PlayableExtensions.SetSpeed"/> does nothing on <see cref="ControllerState"/>s so there is no
        /// way to directly control their speed. The
        /// <see href="https://kybernetik.com.au/animancer/docs/bugs/animator-controller-speed">Animator Controller Speed</see>
        /// page explains a possible workaround for this issue.
        /// <para></para>
        /// The only reason you would disable this warning is if you are setting the speed of states in general and
        /// not depending on it to actually take effect.
        /// </remarks>
        UnsupportedSpeed = 1 << 11,

        /// <summary>
        /// <see href="https://kybernetik.com.au/animancer/docs/manual/ik">Inverse Kinematics</see> cannot be
        /// dynamically enabled on some <see href="https://kybernetik.com.au/animancer/docs/manual/playing/states">States</see>
        /// Types.
        /// </summary>
        /// 
        /// <remarks>
        /// To use IK on a <see cref="ControllerState"/> you must instead enable it on the desired layer inside the
        /// Animator Controller.
        /// <para></para>
        /// IK is not supported by <see cref="PlayableAssetState"/>.
        /// <para></para>
        /// Setting <see cref="AnimancerNode.ApplyAnimatorIK"/> on such a state will simply do nothing, so feel free to
        /// disable this warning if you are enabling IK on states without checking their type.
        /// </remarks>
        UnsupportedIK = 1 << 12,

        /// <summary>
        /// A <see cref="ManualMixerState"/> is being initialized with its <see cref="AnimancerNode.ChildCount"/> &lt;= 1.
        /// </summary>
        /// 
        /// <remarks>
        /// The purpose of a mixer is to mix multiple child states so you are probably initializing it with incorrect
        /// parameters.
        /// <para></para>
        /// A mixer with only one child will simply play that child, so feel free to disable this warning if that is
        /// what you intend to do.
        /// </remarks>
        MixerMinChildren = 1 << 13,

        /// <summary>
        /// A <see cref="ManualMixerState"/> is synchronizing a child with <see cref="AnimancerState.Length"/> = 0.
        /// </summary>
        /// 
        /// <remarks>
        /// Synchronization is based on the <see cref="AnimancerState.NormalizedTime"/> which can't be calculated if
        /// the <see cref="AnimancerState.Length"/> is 0.
        /// <para></para>
        /// Some state types can change their <see cref="AnimancerState.Length"/>, in which case you can just disable
        /// this warning. But otherwise, the indicated state should not be added to the synchronization list.
        /// </remarks>
        MixerSynchronizeZeroLength = 1 << 14,

        /// <summary>
        /// A <see href="https://kybernetik.com.au/animancer/docs/manual/blending/fading#custom-fade">Custom Fade</see>
        /// is being started but its weight calculation does not go from 0 to 1.
        /// </summary>
        /// 
        /// <remarks>
        /// The <see cref="CustomFade.CalculateWeight"/> method is expected to return 0 when the parameter is 0 and
        /// 1 when the parameter is 1. It can do anything you want with other values, but violating that guideline will
        /// trigger this warning because it would likely lead to undesirable results.
        /// <para></para>
        /// If your <see cref="CustomFade.CalculateWeight"/> method is expensive you could disable this warning to save
        /// some performance, but violating the above guidelines is not recommended.
        /// </remarks>
        CustomFadeBounds = 1 << 15,

        /// <summary>
        /// A weight calculation method was not specified when attempting to start a
        /// <see href="https://kybernetik.com.au/animancer/docs/manual/blending/fading#custom-fade">Custom Fade</see>.
        /// </summary>
        /// 
        /// <remarks>
        /// Passing a <c>null</c> parameter into <see cref="CustomFade.Apply(AnimancerState, AnimationCurve)"/> and
        /// other similar methods will trigger this warning and return <c>null</c> because a <see cref="CustomFade"/>
        /// serves no purpose if it doesn't have a method for calculating the weight.
        /// </remarks>
        CustomFadeNotNull = 1 << 16,

        /// <summary>
        /// The <see cref="Animator.speed"/> property does not affect Animancer. 
        /// Use <see cref="AnimancerPlayable.Speed"/> instead.
        /// </summary>
        /// 
        /// <remarks>
        /// The <see cref="Animator.speed"/> property only works with Animator Controllers but does not affect the
        /// Playables API so Animancer has its own <see cref="AnimancerPlayable.Speed"/> property.
        /// </remarks>
        AnimatorSpeed = 1 << 17,

        /// <summary>An <see cref="AnimancerNode.Root"/> is null during finalization (garbage collection).</summary>
        /// <remarks>
        /// This probably means that node was never used for anything and should not have been created.
        /// <para></para>
        /// This warning can be prevented for a specific node by passing it into <see cref="GC.SuppressFinalize"/>.
        /// <para></para>
        /// To minimise the performance cost of checking this warning, it does not capture the stack trace of the
        /// node's creation by default. However, you can enable <see cref="AnimancerNode.TraceConstructor"/> on startup
        /// so that it can include the stack trace in the warning message for any nodes that end up being unused.
        /// </remarks>
        UnusedNode = 1 << 18,

        /// <summary>
        /// <see cref="PlayableAssetState.InitializeBindings"/> is trying to bind to the same <see cref="Animator"/>
        /// that is being used by Animancer.
        /// </summary>
        /// <remarks>
        /// Doing this will replace Animancer's output so its animations would not work anymore.
        /// </remarks>
        PlayableAssetAnimatorBinding = 1 << 19,

        /// <summary>
        /// <see cref="AnimancerLayer.GetOrCreateWeightlessState"/> is cloning a complex state such as a
        /// <see cref="ManualMixerState"/> or <see cref="ControllerState"/>. This has a larger performance cost than cloning
        /// a <see cref="ClipState"/> and these states generally have parameters that need to be controlled which may
        /// result in undesired behaviour if your scripts are only expecting to have one state to control.
        /// </summary>
        /// <remarks>
        /// The <see href="https://kybernetik.com.au/animancer/docs/manual/blending/fading/modes">Fade Modes</see> page
        /// explains why clones are created.
        /// </remarks>
        CloneComplexState = 1 << 20,

        /// <summary>All warning types.</summary>
        All = ~0,
    }

    /// https://kybernetik.com.au/animancer/api/Animancer/Validate
    public static partial class Validate
    {
        /************************************************************************************************************************/

#if UNITY_ASSERTIONS
        /// <summary>[Assert-Only] The <see cref="OptionalWarning"/> flags that are currently disabled (default none).</summary>
        private static OptionalWarning _DisabledWarnings;
#endif

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] [Assert-Conditional]
        /// Disables the specified warning type. Supports bitwise combinations.
        /// </summary>
        /// <example>
        /// You can put the following method in any class to disable whatever warnings you don't want on startup:
        /// <para></para><code>
        /// #if UNITY_ASSERTIONS
        /// [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        /// private static void DisableAnimancerWarnings()
        /// {
        ///     Animancer.OptionalWarning.EndEventInterrupt.Disable();
        ///     
        ///     // You could disable OptionalWarning.All, but that is not recommended for obvious reasons.
        /// }
        /// #endif
        /// </code></example>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public static void Disable(this OptionalWarning type)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] [Assert-Conditional]
        /// Enables the specified warning type. Supports bitwise combinations.
        /// </summary>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public static void Enable(this OptionalWarning type)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] [Assert-Conditional]
        /// Enables or disables the specified warning type. Supports bitwise combinations.
        /// </summary>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public static void SetEnabled(this OptionalWarning type, bool enable)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] [Assert-Conditional]
        /// Logs the `message` as a warning if the `type` is enabled.
        /// </summary>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public static void Log(this OptionalWarning type, string message, object context = null)
        {
        }

        /************************************************************************************************************************/
#if UNITY_ASSERTIONS
        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] [Assert-Only] Are none of the specified warning types disabled?</summary>
        public static bool IsEnabled(this OptionalWarning type) => (_DisabledWarnings & type) == 0;

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] [Assert-Only] Are all of the specified warning types disabled?</summary>
        public static bool IsDisabled(this OptionalWarning type) => (_DisabledWarnings & type) == type;

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] [Assert-Only]
        /// Disables the specified warnings and returns those that were previously enabled.
        /// </summary>
        /// <remarks>Call <see cref="Enable"/> on the returned value to re-enable it.</remarks>
        public static OptionalWarning DisableTemporarily(this OptionalWarning type)
        {
            return default;
        }

        /************************************************************************************************************************/

        private const string PermanentlyDisabledWarningsKey = nameof(Animancer) + "." + nameof(PermanentlyDisabledWarnings);

        /// <summary>[Assert-Only] Warnings that are automatically disabled and stored in <see cref="PlayerPrefs"/>.</summary>
        public static OptionalWarning PermanentlyDisabledWarnings
        {
#if NO_RUNTIME_PLAYER_PREFS && ! UNITY_EDITOR
            get => default;
            set
            {
                _DisabledWarnings = value;
            }
#else
            get => (OptionalWarning)PlayerPrefs.GetInt(PermanentlyDisabledWarningsKey);
            set
            {
                _DisabledWarnings = value;
                PlayerPrefs.SetInt(PermanentlyDisabledWarningsKey, (int)value);
            }
#endif
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializePermanentlyDisabledWarnings()
        {
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

