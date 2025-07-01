// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

namespace Animancer
{
    /// https://kybernetik.com.au/animancer/api/Animancer/ControllerState
    partial class ControllerState
    {
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns a <see cref="Drawer"/> for this state.</summary>
        protected internal override Editor.IAnimancerNodeDrawer CreateDrawer() => new Drawer(this);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public class Drawer : Editor.ParametizedAnimancerStateDrawer<ControllerState>
        {
            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Drawer"/> to manage the Inspector GUI for the `state`.</summary>
            public Drawer(ControllerState state) : base(state) {
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            protected override void DoDetailsGUI()
            {
            }

            /************************************************************************************************************************/

            private readonly List<AnimatorControllerParameter>
                Parameters = new List<AnimatorControllerParameter>();

            /// <summary>Fills the <see cref="Parameters"/> list with the current parameter details.</summary>
            private void GatherParameters()
            {
            }

            /************************************************************************************************************************/

            private AnimatorControllerParameter GetParameter(int hash)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            public override int ParameterCount => Parameters.Count;

            /// <inheritdoc/>
            public override string GetParameterName(int index) => Parameters[index].name;

            /// <inheritdoc/>
            public override AnimatorControllerParameterType GetParameterType(int index) => Parameters[index].type;

            /// <inheritdoc/>
            public override object GetParameterValue(int index)
            {
                return default;
            }

            /// <inheritdoc/>
            public override void SetParameterValue(int index, object value)
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

#endif

