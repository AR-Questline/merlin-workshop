// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class TimeManager : IManager, IValid
    {
        /// <summary>
        /// シミュレーションの更新周期
        /// １ステップで(1.0 / simulationFrequency)時間進みます
        /// </summary>
        internal int simulationFrequency = Define.System.DefaultSimulationFrequency;

        /// <summary>
        /// 1フレームに実行される最大シミュレーション回数
        /// </summary>
        internal int maxSimulationCountPerFrame = Define.System.DefaultMaxSimulationCountPerFrame;

        /// <summary>
        /// マネージャの更新場所
        /// </summary>
        public enum UpdateLocation
        {
            AfterLateUpdate = 0,
            BeforeLateUpdate = 1,
        }
        internal UpdateLocation updateLocation = UpdateLocation.AfterLateUpdate;

        //=========================================================================================
        bool isValid = false;

        /// <summary>
        /// フレームのFixedUpdate回数
        /// </summary>
        internal int FixedUpdateCount { get; private set; }

        /// <summary>
        /// グローバルタイムスケール(0.0 ~ 1.0)
        /// </summary>
        internal float GlobalTimeScale = 1.0f;

        /// <summary>
        /// シミュレーション1回の時間
        /// </summary>
        internal float SimulationDeltaTime { get; private set; }

        /// <summary>
        /// 1フレームの最大更新時間
        /// </summary>
        internal float MaxDeltaTime { get; private set; }

        /// <summary>
        /// 制約解決係数（周波数により変動）
        /// </summary>
        internal float4 SimulationPower { get; private set; }

        //=========================================================================================
        public void Dispose()
        {
        }

        public void EnterdEditMode()
        {
        }

        public void Initialize()
        {
        }

        public bool IsValid()
        {
            return default;
        }

        //=========================================================================================
        void AfterFixedUpdate()
        {
        }

        void AfterRenderring()
        {
        }

        //=========================================================================================
        internal void FrameUpdate()
        {
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
        }
    }
}
