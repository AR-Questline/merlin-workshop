// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class MagicaCloth
    {
        /// <summary>
        /// シリアライズデータ２の取得
        /// SerializeData2クラスはシステムが利用するパラメータクラスです。
        /// そのためユーザーによる変更は推奨されていません。
        /// 
        /// Acquisition of SerializedData2.
        /// The SerializeData2 class is a parameter class used by the system.
        /// Therefore, user modification is not recommended.
        /// </summary>
        /// <returns></returns>
        public ClothSerializeData2 GetSerializeData2()
        {
            return default;
        }

        /// <summary>
        /// クロスデータ構築完了後イベント
        /// Event after completion of cloth data construction.
        /// (true = Success, false = Failure)
        /// </summary>
        public Action<bool> OnBuildComplete;


        /// <summary>
        /// 初期化を実行します
        /// すでに初期化済みの場合は何もしません。
        /// perform initialization.
        /// If already initialized, do nothing.
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// コンポーネントのStart()で実行される自動ビルドを無効にします
        /// Disable automatic builds that run on the component's Start().
        /// </summary>
        public void DisableAutoBuild()
        {
        }

        /// <summary>
        /// コンポーネントを構築し実行します
        /// すべてのデータをセットアップしたあとに呼び出す必要があります
        /// build and run the component.
        /// Must be called after setting up all data.
        /// </summary>
        /// <returns>true=start build. false=build failed.</returns>
        public bool BuildAndRun()
        {
            return default;
        }

        static bool IsScaleApproximatelyOne(Vector3 scale, float tolerance = 0.01f)
        {
            return default;
        }

        /// <summary>
        /// コンポーネントが保持するトランスフォームを置換します。
        /// 置換先のトランスフォーム名をキーとした辞書を渡します。
        /// Replaces a component's transform.
        /// Passes a dictionary keyed by the name of the transform to be replaced.
        /// </summary>
        /// <param name="targetTransformDict">Dictionary keyed by the name of the transform to be replaced.</param>
        public void ReplaceTransform(Dictionary<string, Transform> targetTransformDict)
        {
        }

        /// <summary>
        /// コンポーネントが保持するトランスフォームを置換します。
        /// 置換先のトランスフォーム名をキーとした辞書を渡します。
        /// Replaces a component's transform.
        /// Passes a dictionary keyed by the name of the transform to be replaced.
        /// </summary>
        public void ReplaceTransform(UnsafeHashMap<FixedString64Bytes, ushort> transformsMap, Transform[] rigTransforms)
        {
        }

        /// <summary>
        /// パラメータの変更を通知
        /// 実行中にパラメータを変更した場合はこの関数を呼ぶ必要があります
        /// You should call this function if you changed parameters during execution.
        /// </summary>
        public void SetParameterChange()
        {
        }

        /// <summary>
        /// タイムスケールを変更します
        /// Change the time scale.
        /// </summary>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetTimeScale(float timeScale)
        {
        }

        /// <summary>
        /// タイムスケールを取得します
        /// Get the time scale.
        /// </summary>
        /// <returns></returns>
        public float GetTimeScale()
        {
            return default;
        }

        /// <summary>
        /// シミュレーションを初期状態にリセットします
        /// Reset the simulation to its initial state.
        /// </summary>
        /// <param name="keepPose">If true, resume while maintaining posture.</param>
        public void ResetCloth(bool keepPose = false)
        {
        }

        /// <summary>
        /// 慣性の中心座標を取得します
        /// Get the center of inertia position.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCenterPosition()
        {
            return default;
        }

        /// <summary>
        /// 外力を加えます
        /// Add external force.
        /// </summary>
        /// <param name="forceDirection"></param>
        /// <param name="forceVelocity">(m/s)</param>
        /// <param name="fmode"></param>
        public void AddForce(Vector3 forceDirection, float forceVelocity, ClothForceMode fmode = ClothForceMode.VelocityAdd)
        {
        }

        /// <summary>
        /// TransformおよびMeshへの書き込みを禁止または許可します
        /// この機能を使うことでストップモーションを実装することが可能です
        /// Prevent or allow writing to Transform and Mesh.
        /// By using this function, it is possible to implement stop motion.
        /// </summary>
        /// <param name="sw">true=write disabled, false=write enabled</param>
        public void SetSkipWriting(bool sw)
        {
        }
    }
}
