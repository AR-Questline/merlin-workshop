// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace Animancer
{
    /// https://kybernetik.com.au/animancer/api/Animancer/ControllerState
    partial class ControllerState
    {
        /************************************************************************************************************************/

        /// <summary>A wrapper for the name and hash of an <see cref="AnimatorControllerParameter"/>.</summary>
        public readonly struct ParameterID
        {
            /************************************************************************************************************************/

            /// <summary>The name of this parameter.</summary>
            public readonly string Name;

            /// <summary>The name hash of this parameter.</summary>
            public readonly int Hash;

            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="ParameterID"/> with the specified <see cref="Name"/> and uses
            /// <see cref="Animator.StringToHash"/> to calculate the <see cref="Hash"/>.
            /// </summary>
            public ParameterID(string name) : this()
            {
            }

            /// <summary>
            /// Creates a new <see cref="ParameterID"/> with the specified <see cref="Hash"/> and leaves the
            /// <see cref="Name"/> null.
            /// </summary>
            public ParameterID(int hash) : this()
            {
            }

            /// <summary>Creates a new <see cref="ParameterID"/> with the specified <see cref="Name"/> and <see cref="Hash"/>.</summary>
            /// <remarks>This constructor does not verify that the `hash` actually corresponds to the `name`.</remarks>
            public ParameterID(string name, int hash) : this()
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="ParameterID"/> with the specified <see cref="Name"/> and uses
            /// <see cref="Animator.StringToHash"/> to calculate the <see cref="Hash"/>.
            /// </summary>
            public static implicit operator ParameterID(string name) => new ParameterID(name);

            /// <summary>
            /// Creates a new <see cref="ParameterID"/> with the specified <see cref="Hash"/> and leaves the
            /// <see cref="Name"/> null.
            /// </summary>
            public static implicit operator ParameterID(int hash) => new ParameterID(hash);

            /************************************************************************************************************************/

            /// <summary>Returns the <see cref="Hash"/>.</summary>
            public static implicit operator int(ParameterID parameter) => parameter.Hash;

            /************************************************************************************************************************/

            /// <summary>[Editor-Conditional]
            /// Throws if the `controller` doesn't have a parameter with the specified <see cref="Hash"/>
            /// and `type`.
            /// </summary>
            /// <exception cref="ArgumentException"/>
            [System.Diagnostics.Conditional(Strings.UnityEditor)]
            public void ValidateHasParameter(RuntimeAnimatorController controller, AnimatorControllerParameterType type)
            {
            }

            /************************************************************************************************************************/

#if UNITY_EDITOR
            private static Dictionary<RuntimeAnimatorController, Dictionary<int, AnimatorControllerParameterType>>
                _ControllerToParameterHashAndType;

            /// <summary>[Editor-Only] Returns the hash mapped to the type of all parameters in the `controller`.</summary>
            /// <remarks>This doesn't work for if the `controller` was loaded from an Asset Bundle.</remarks>
            private static Dictionary<int, AnimatorControllerParameterType> GetParameterDetails(
                RuntimeAnimatorController controller)
            {
                return default;
            }
#endif

            /************************************************************************************************************************/

            /// <summary>Returns a string containing the <see cref="Name"/> and <see cref="Hash"/>.</summary>
            public override string ToString()
            {
                return default;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

