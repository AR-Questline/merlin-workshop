﻿// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaCloth main component.
    /// </summary>
    [AddComponentMenu("MagicaCloth2/MagicaCloth")]
    [HelpURL("https://magicasoft.jp/en/mc2_magicaclothcomponent/")]
    public partial class MagicaCloth : ClothBehaviour, IValid
    {
        /// <summary>
        /// Serialize data (1).
        /// Basic parameters.
        /// Import/export target.
        /// Can be rewritten at runtime.
        /// </summary>
        [SerializeField]
        private ClothSerializeData serializeData = new ClothSerializeData();
        public ClothSerializeData SerializeData => serializeData;

        /// <summary>
        /// Serialize data (2).
        /// Hidden data that cannot be rewritten at runtime
        /// </summary>
        [SerializeField]
        internal ClothSerializeData2 serializeData2 = new ClothSerializeData2();

#if UNITY_EDITOR
        /// <summary>
        /// Gizmo display specification when editing.
        /// </summary>
        [SerializeField]
        private GizmoSerializeData gizmoSerializeData = new GizmoSerializeData();
        public GizmoSerializeData GizmoSerializeData => gizmoSerializeData;
#endif

        /// <summary>
        /// General processing.
        /// </summary>
        private ClothProcess process = new ClothProcess();
        public ClothProcess Process { get { process.cloth = this; return process; } }

        /// <summary>
        /// Cloth component transform.
        /// Proxy meshes and selection data are managed in this space.
        /// </summary>
        public Transform ClothTransform => transform;

        /// <summary>
        /// Synchronization target.
        /// </summary>
        public MagicaCloth SyncCloth => SerializeData.IsBoneSpring() ? null : SerializeData.selfCollisionConstraint.GetSyncPartner();

        /// <summary>
        /// Check if the cloth component is in a valid state.
        /// クロスコンポーネントが有効な状態か確認します。
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return default;
        }

        //=========================================================================================
        private void Reset()
        {
        }

        private void OnValidate()
        {
        }

        private void Awake()
        {
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        void Start()
        {
        }

        private void OnDestroy()
        {
        }

        /// <summary>
        /// Hash code for checking changes when editing.
        /// </summary>
        /// <returns></returns>
        public override int GetMagicaHashCode()
        {
            return default;
        }
    }
}
