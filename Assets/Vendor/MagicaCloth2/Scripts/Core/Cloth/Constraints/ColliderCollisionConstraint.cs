// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// コライダーによる衝突判定制約
    /// </summary>
    public class ColliderCollisionConstraint : IDisposable
    {
        /// <summary>
        /// Collision judgment mode.
        /// 衝突判定モード
        /// </summary>
        public enum Mode
        {
            None = 0,
            Point = 1,
            Edge = 2,
        }

        [System.Serializable]
        public class SerializeData : IDataValidate, ITransform
        {
            /// <summary>
            /// Collision judgment mode.
            /// 衝突判定モード
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public Mode mode;

            /// <summary>
            /// Friction (0.0 ~ 1.0).
            /// Dynamic friction/stationary friction combined use.
            /// 摩擦(0.0 ~ 1.0)
            /// 動摩擦／静止摩擦兼用
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 0.5f)]
            public float friction;

            /// <summary>
            /// Collider list.
            /// コライダーリスト
            /// [OK] Runtime changes.
            /// [NG] Export/Import with Presets
            /// </summary>
            public List<ColliderComponent> colliderList = new List<ColliderComponent>();

            /// <summary>
            /// List of Transforms that perform collision detection with BoneSpring.
            /// BoneSpringで衝突判定を行うTransformのリスト
            /// [OK] Runtime changes.
            /// [NG] Export/Import with Presets
            /// </summary>
            public List<Transform> collisionBones = new List<Transform>();

            /// <summary>
            /// The maximum distance from the origin that a vertex will be pushed by the collider. Currently used only with BoneSpring.
            /// コライダーにより頂点が押し出される原点からの最大距離。現在はBoneSpringのみで利用。
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData limitDistance = new CurveSerializeData(0.05f);


            public SerializeData()
            {
            }

            public void DataValidate()
            {
            }

            public SerializeData Clone()
            {
                return default;
            }

            public override int GetHashCode()
            {
                return default;
            }

            public void GetUsedTransform(HashSet<Transform> transformSet)
            {
            }

            public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
            {
            }

            public int ColliderLength => colliderList.Count;
        }

        public struct ColliderCollisionConstraintParams
        {
            /// <summary>
            /// 衝突判定モード
            /// BoneSpringではPointに固定される
            /// </summary>
            public Mode mode;

            /// <summary>
            /// 動摩擦係数(0.0 ~ 1.0)
            /// 摩擦1.0に対するステップごとの接線方向の速度減速率
            /// </summary>
            public float dynamicFriction;

            /// <summary>
            /// 静止摩擦係数(0.0 ~ 1.0)
            /// 静止速度(m/s)
            /// </summary>
            public float staticFriction;

            /// <summary>
            /// コライダーにより頂点が押し出される原点からの最大距離。現在はBoneSpringのみで利用。
            /// </summary>
            public float4x4 limitDistance;

            public void Convert(SerializeData sdata, ClothProcess.ClothType clothType)
            {
            }
        }

        NativeArray<int> tempFrictionArray;
        NativeArray<int> tempNormalArray;

        //=========================================================================================
        public ColliderCollisionConstraint()
        {
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// 作業バッファ更新
        /// </summary>
        internal void WorkBufferUpdate()
        {
        }

        public override string ToString()
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 制約の解決
        /// </summary>
        /// <param name="clothBase"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe internal JobHandle SolverConstraint(JobHandle jobHandle)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// Pointコライダー衝突判定
        /// </summary>
        [BurstCompile]
        struct PointColliderCollisionConstraintJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepParticleIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> collisionNormalArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> colliderFlagArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ColliderManager.WorkData> colliderWorkDataArray;

            // ステップ実行パーティクルごと
            public void Execute(int index)
            {
            }

            /// <summary>
            /// Point球衝突判定
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="pos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="friction"></param>
            /// <returns></returns>
            float PointSphereColliderDetection(ref float3 nextpos, in float3 basePos, float radius, in AABB aabb, in ColliderManager.WorkData cwork, bool isSpring, float maxLength, out float3 normal)
            {
                normal = default(float3);
                return default;
            }

            /// <summary>
            /// Point平面衝突判定（無限平面）
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="normal"></param>
            /// <returns></returns>
            float PointPlaneColliderDetction(ref float3 nextpos, float radius, in ColliderManager.WorkData cwork, out float3 normal)
            {
                normal = default(float3);
                return default;
            }

            /// <summary>
            /// Pointカプセル衝突判定
            /// </summary>
            /// <param name="nextpos"></param>
            /// <param name="pos"></param>
            /// <param name="radius"></param>
            /// <param name="cindex"></param>
            /// <param name="dir"></param>
            /// <param name="friction"></param>
            /// <returns></returns>
            float PointCapsuleColliderDetection(ref float3 nextpos, float radius, in AABB aabb, in ColliderManager.WorkData cwork, out float3 normal)
            {
                normal = default(float3);
                return default;
            }
        }

        //=========================================================================================
        /// <summary>
        /// Edgeコライダー衝突判定
        /// </summary>
        [BurstCompile]
        unsafe struct EdgeColliderCollisionConstraintJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepEdgeCollisionIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> edgeTeamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> collisionNormalArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> colliderFlagArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ColliderManager.WorkData> colliderWorkDataArray;

            // output
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> tempFrictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> tempNormalArray;

            // ステップ実行エッジごと
            public void Execute(int index)
            {
            }

            float EdgeSphereColliderDetection(ref float3x2 nextPosE, in float2 radiusE, in AABB aabbE, float cfr, in ColliderManager.WorkData cwork, out float3 normal)
            {
                normal = default(float3);
                return default;
            }

            float EdgeCapsuleColliderDetection(ref float3x2 nextPosE, in float2 radiusE, in AABB aabbE, float cfr, in ColliderManager.WorkData cwork, out float3 normal)
            {
                normal = default(float3);
                return default;
            }

            float EdgePlaneColliderDetection(ref float3x2 nextPosE, in float2 radiusE, in ColliderManager.WorkData cwork, out float3 normal)
            {
                normal = default(float3);
                return default;
            }
        }

        /// <summary>
        /// エッジコライダーコリジョン結果の集計
        /// </summary>
        [BurstCompile]
        struct SolveEdgeBufferAndClearJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobParticleIndexList;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> collisionNormalArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;

            // aggregate
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> tempFrictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> tempNormalArray;

            // ステップ有効パーティクルごと
            public void Execute(int index)
            {
            }
        }
    }
}
