// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace MagicaCloth2
{
    /// <summary>
    /// Inertia and travel/rotation limits.
    /// 慣性と移動/回転制限
    /// </summary>
    public class InertiaConstraint : IDisposable
    {
        /// <summary>
        /// テレポートモード
        /// Teleport processing mode.
        /// </summary>
        public enum TeleportMode
        {
            None = 0,

            /// <summary>
            /// シミュレーションをリセットします
            /// Reset the simulation.
            /// </summary>
            Reset = 1,

            /// <summary>
            /// テレポート前の状態を継続します
            /// Continue the state before the teleport.
            /// </summary>
            Keep = 2,
        }

        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Anchor that cancels inertia.
            /// Anchor translation and rotation are excluded from simulation.
            /// This is useful if your character rides a vehicle.
            /// 慣性を打ち消すアンカー
            /// アンカーの移動と回転はシミュレーションから除外されます
            /// これはキャラクターが乗り物に乗る場合に便利です
            /// [OK] Runtime changes.
            /// [NG] Export/Import with Presets
            /// </summary>
            public Transform anchor;

            /// <summary>
            /// Anchor Influence (0.0 ~ 1.0)
            /// アンカーの影響(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [NG] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float anchorInertia;


            /// <summary>
            /// World Influence (0.0 ~ 1.0).
            /// ワールド移動影響(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [FormerlySerializedAs("movementInertia")]
            [Range(0.0f, 1.0f)]
            public float worldInertia;

            /// <summary>
            /// World Influence Smoothing (0.0 ~ 1.0).
            /// ワールド移動影響平滑化(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float movementInertiaSmoothing;

            /// <summary>
            /// World movement speed limit (m/s).
            /// ワールド移動速度制限(m/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData movementSpeedLimit;

            /// <summary>
            /// World rotation speed limit (deg/s).
            /// ワールド回転速度制限(deg/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData rotationSpeedLimit;

            /// <summary>
            /// Local Influence (0.0 ~ 1.0).
            /// ローカル慣性影響(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float localInertia;

            /// <summary>
            /// Local movement speed limit (m/s).
            /// ローカル移動速度制限(m/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData localMovementSpeedLimit;

            /// <summary>
            /// Local rotation speed limit (deg/s).
            /// ローカル回転速度制限(deg/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData localRotationSpeedLimit;

            /// <summary>
            /// depth inertia (0.0 ~ 1.0).
            /// Increasing the effect weakens the inertia near the root (makes it difficult to move).
            /// 深度慣性(0.0 ~ 1.0)
            /// 影響を大きくするとルート付近の慣性が弱くなる（動きにくくなる）
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float depthInertia;

            /// <summary>
            /// Centrifugal acceleration (0.0 ~ 1.0).
            /// 遠心力加速(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float centrifualAcceleration;

            /// <summary>
            /// Particle Velocity Limit (m/s).
            /// パーティクル速度制限(m/s)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CheckSliderSerializeData particleSpeedLimit;

            /// <summary>
            /// Teleport determination method.
            /// テレポート判定モード
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public TeleportMode teleportMode;

            /// <summary>
            /// Teleport detection distance.
            /// テレポート判定距離
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public float teleportDistance;

            /// <summary>
            /// Teleport detection angle(deg).
            /// テレポート判定回転角度(deg)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public float teleportRotation;

            public SerializeData()
            {
            }

            public SerializeData Clone()
            {
                return default;
            }

            public void DataValidate()
            {
            }
        }

        public struct InertiaConstraintParams
        {
            /// <summary>
            /// アンカー影響率(0.0 ~ 1.0)
            /// </summary>
            public float anchorInertia;

            /// <summary>
            /// ワールド慣性影響(0.0 ~ 1.0)
            /// </summary>
            public float worldInertia;

            /// <summary>
            /// ワールド慣性スムージング率(0.0 ~ 1.0)
            /// </summary>
            public float movementInertiaSmoothing;

            /// <summary>
            /// ワールド移動速度制限(m/s)
            /// 無制限時は(-1)
            /// </summary>
            public float movementSpeedLimit;

            /// <summary>
            /// ワールド回転速度制限(deg/s)
            /// 無制限時は(-1)
            /// </summary>
            public float rotationSpeedLimit;

            /// <summary>
            /// ローカル慣性影響(0.0 ~ 1.0)
            /// </summary>
            public float localInertia;

            /// <summary>
            /// ローカル移動速度制限(m/s)
            /// </summary>
            public float localMovementSpeedLimit;

            /// <summary>
            /// ローカル回転速度制限(deg/s)
            /// </summary>
            public float localRotationSpeedLimit;

            /// <summary>
            /// 深度慣性(0.0 ~ 1.0)
            /// 影響を大きくするとルート付近の慣性が弱くなる（動きにくくなる）
            /// </summary>
            public float depthInertia;

            /// <summary>
            /// 遠心力加速(0.0 ~ 1.0)
            /// </summary>
            public float centrifualAcceleration;

            /// <summary>
            /// パーティクル速度制限(m/s)
            /// </summary>
            public float particleSpeedLimit;

            /// <summary>
            /// テレポートモード
            /// </summary>
            public TeleportMode teleportMode;

            /// <summary>
            /// テレポート判定距離
            /// </summary>
            public float teleportDistance;

            /// <summary>
            /// テレポート判定角度(deg)
            /// </summary>
            public float teleportRotation;

            public void Convert(SerializeData sdata)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// センタートランスフォームのデータ
        /// </summary>
        [System.Serializable]
        public struct CenterData
        {
            /// <summary>
            /// 現在のアンカー姿勢
            /// </summary>
            public float3 anchorPosition;
            public quaternion anchorRotation;

            /// <summary>
            /// 前フレームのアンカー姿勢
            /// </summary>
            public float3 oldAnchorPosition;
            public quaternion oldAnchorRotation;

            /// <summary>
            /// アンカー空間でのコンポーネントのローカル座標
            /// </summary>
            public float3 anchorComponentLocalPosition;

            /// <summary>
            /// 参照すべきセンタートランスフォームインデックス
            /// 同期時は同期先チームのもにになる
            /// </summary>
            public int centerTransformIndex;

            /// <summary>
            /// 現フレームのコンポーネント姿勢
            /// </summary>
            public float3 componentWorldPosition;
            public quaternion componentWorldRotation;
            public float3 componentWorldScale;

            /// <summary>
            /// 前フレームのコンポーネント姿勢
            /// </summary>
            public float3 oldComponentWorldPosition;
            public quaternion oldComponentWorldRotation;
            public float3 oldComponentWorldScale;

            /// <summary>
            /// 現フレームのコンポーネント移動量
            /// </summary>
            public float3 frameComponentShiftVector;
            public quaternion frameComponentShiftRotation;

            /// <summary>
            /// 現フレームのコンポーネント移動速度と方向
            /// </summary>
            public float frameMovingSpeed;
            public float3 frameMovingDirection;

            /// <summary>
            /// 現フレームの姿勢
            /// </summary>
            public float3 frameWorldPosition;
            public quaternion frameWorldRotation;
            public float3 frameWorldScale;
            public float3 frameLocalPosition;

            /// <summary>
            /// 前フレームの姿勢
            /// </summary>
            public float3 oldFrameWorldPosition;
            public quaternion oldFrameWorldRotation;
            public float3 oldFrameWorldScale;

            /// <summary>
            /// 現ステップでの姿勢
            /// </summary>
            public float3 nowWorldPosition;
            public quaternion nowWorldRotation;
            //public float3 nowWorldScale; // ※現在未使用

            /// <summary>
            /// 前回ステップでの姿勢
            /// </summary>
            public float3 oldWorldPosition;
            public quaternion oldWorldRotation;

            /// <summary>
            /// ステップごとの移動力削減割合(0.0~1.0)
            /// </summary>
            public float stepMoveInertiaRatio;

            /// <summary>
            /// ステップごとの回転力削減割合(0.0~1.0)
            /// </summary>
            public float stepRotationInertiaRatio;

            /// <summary>
            /// ステップごとの移動ベクトル
            /// これは削減前の純粋なワールドベクトル
            /// </summary>
            public float3 stepVector;

            /// <summary>
            /// ステップごとの回転ベクトル
            /// これは削減前の純粋なワールド回転
            /// </summary>
            public quaternion stepRotation;

            /// <summary>
            /// ステップごとの慣性全体移動シフトベクトル
            /// </summary>
            public float3 inertiaVector;

            /// <summary>
            /// ステップごとの慣性全体シフト回転
            /// </summary>
            public quaternion inertiaRotation;

            /// <summary>
            /// ステップごとの慣性削減後の移動速度(m/s)
            /// </summary>
            public float stepMovingSpeed;

            /// <summary>
            /// ステップごとの慣性削減後の移動方向
            /// </summary>
            public float3 stepMovingDirection;

            /// <summary>
            /// 回転の角速度(rad/s)
            /// </summary>
            public float angularVelocity;

            /// <summary>
            /// 回転軸(角速度0の場合は(0,0,0))
            /// </summary>
            public float3 rotationAxis;

            /// <summary>
            /// 初期化時の慣性中心姿勢でのローカル重力方向
            /// 重力falloff計算で使用
            /// </summary>
            public float3 initLocalGravityDirection;

            /// <summary>
            /// スムージングされた現在のワールド慣性速度ベクトル
            /// </summary>
            public float3 smoothingVelocity; // (m/s)

            /// <summary>
            /// マイナススケールによる反転を打ち消すための変換マトリックス
            /// センター空間
            /// </summary>
            public float4x4 negativeScaleMatrix;

            internal void Initialize()
            {
            }
        }

        /// <summary>
        /// 制約データ
        /// </summary>
        [System.Serializable]
        public class ConstraintData
        {
            public ResultCode result;
            public CenterData centerData;
            public float3 initLocalGravityDirection;
        }

        /// <summary>
        /// チームごとの固定点リスト
        /// </summary>
        internal ExNativeArray<ushort> fixedArray;

        //=========================================================================================
        public InertiaConstraint()
        {
        }

        public void Dispose()
        {
        }

        public override string ToString()
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 制約データの作成
        /// </summary>
        /// <param name="cbase"></param>
        public static ConstraintData CreateData(VirtualMesh proxyMesh, in ClothParameters parameters)
        {
            return default;
        }

        internal void Register(ClothProcess cprocess)
        {
        }

        internal void Exit(ClothProcess cprocess)
        {
        }
    }
}
