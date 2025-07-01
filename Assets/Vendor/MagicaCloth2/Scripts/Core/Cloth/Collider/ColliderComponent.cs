// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace MagicaCloth2
{
    public abstract class ColliderComponent : ClothBehaviour, IDataValidate
    {
        /// <summary>
        /// トランスフォームからの中心ローカルオフセット
        /// Center local offset from transform.
        /// </summary>
        public Vector3 center;

        /// <summary>
        /// Size
        /// Sphere(x:radius)
        /// Capsule(x:start radius, y:end radius, z:length)
        /// Box(x:size x, y:size y, z:size z)
        /// </summary>
        [SerializeField]
        protected Vector3 size;


        //=========================================================================================
        /// <summary>
        /// Collider type.
        /// </summary>
        /// <returns></returns>
        public abstract ColliderManager.ColliderType GetColliderType();

        /// <summary>
        /// パラメータの検証
        /// </summary>
        public abstract void DataValidate();

        //=========================================================================================
        /// <summary>
        /// 登録チーム
        /// </summary>
        private HashSet<int> teamIdSet = new HashSet<int>();

        //=========================================================================================
        /// <summary>
        /// Get collider size.
        /// 
        /// Sphere(x:radius)
        /// Capsule(x:start radius, y:end radius, z:length)
        /// Box(x:size x, y:size y, z:size z)
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetSize()
        {
            return default;
        }

        public void SetSize(Vector3 size)
        {
        }

        /// <summary>
        /// スケール値を取得
        /// </summary>
        /// <returns></returns>
        public float GetScale()
        {
            return default;
        }

        /// <summary>
        /// チームへのコライダー登録通知
        /// </summary>
        /// <param name="teamId"></param>
        internal void Register(int teamId)
        {
        }

        /// <summary>
        /// チームからのコライダー解除通知
        /// </summary>
        /// <param name="teamId"></param>
        internal void Exit(int teamId)
        {
        }

        /// <summary>
        /// パラメータの反映
        /// すでに実行状態の場合はこの関数を呼び出さないとプロパティの変更が反映されません。
        /// Reflection of parameters.
        /// If it is already running, property changes will not be reflected unless this function is called.
        /// </summary>
        public void UpdateParameters()
        {
        }

        //=========================================================================================
        protected virtual void Start()
        {
        }

        protected virtual void OnValidate()
        {
        }

        void OnEnable()
        {
        }

        void OnDisable()
        {
        }

        void OnDestroy()
        {
        }

        void OnEnableImpl()
        {
        }

        void OnDisableImpl()
        {
        }

        void OnDestroyImpl()
        {
        }

        [Il2CppEagerStaticClassConstruction]
        public static class ModificationPostpone {
            static readonly HashSet<ColliderComponent> _toEnable = new();
            static readonly HashSet<ColliderComponent> _toDisable = new();
            static readonly List<ColliderComponent> _toDestroy = new();

            public static void OnEnable(ColliderComponent collider) {
            }

            public static void OnDisable(ColliderComponent collider) {
            }

            public static void OnDestroy(ColliderComponent collider) {
            }

            public static void FinishPostpone() {
            }
        }
    }
}
