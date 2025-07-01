// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// Nativeバッファへのインターロック書き込み制御関連
    /// </summary>
    public static class InterlockUtility
    {
        /// <summary>
        /// 固定小数点への変換倍率
        /// </summary>
        const int ToFixed = 100000;

        /// <summary>
        /// 少数への復元倍率
        /// </summary>
        const float ToFloat = 0.00001f;


        //=========================================================================================
        /// <summary>
        /// 集計バッファの指定インデックスにfloat3を固定小数点として加算しカウンタをインクリメントする
        /// </summary>
        /// <param name="index"></param>
        /// <param name="add"></param>
        /// <param name="cntPt"></param>
        /// <param name="sumPt"></param>
        unsafe internal static void AddFloat3(int index, float3 add, int* cntPt, int* sumPt)
        {
        }

        /// <summary>
        /// 集計バッファの指定インデックスにfloat3を固定小数点として加算する（カウントは操作しない）
        /// </summary>
        /// <param name="index"></param>
        /// <param name="add"></param>
        /// <param name="sumPt"></param>
        unsafe internal static void AddFloat3(int index, float3 add, int* sumPt)
        {
        }

        /// <summary>
        /// 集計バッファのカウンタのみインクリメントする
        /// </summary>
        /// <param name="index"></param>
        /// <param name="cntPt"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static void Increment(int index, int* cntPt)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static void Max(int index, float value, int* pt)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 ReadAverageFloat3(int index, in NativeArray<int> countArray, in NativeArray<int> sumArray)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 ReadFloat3(int index, in NativeArray<int> bufferArray)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float ReadFloat(int index, in NativeArray<int> bufferArray)
        {
            return default;
        }

#if false
        /// <summary>
        /// 指定アドレスにfloat値を加算する
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        unsafe public static void AddFloat(int* pt, int index, float value)
        {
            float current = UnsafeUtility.ReadArrayElement<float>(pt, index);
            int currenti = math.asint(current);
            while (true)
            {
                float next = current + value;
                int nexti = math.asint(next);
                int prev = Interlocked.CompareExchange(ref pt[index], nexti, currenti);
                if (prev == currenti)
                    return;
                else
                {
                    currenti = prev;
                    current = math.asfloat(prev);
                }
            }
        }
#endif

        //=========================================================================================
        /// <summary>
        /// 加算集計バッファを平均化してnextPosに加算する
        /// </summary>
        /// <param name="particleList"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal static JobHandle SolveAggregateBufferAndClear(in NativeList<int> particleList, float velocityAttenuation, JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct AggregateJob : IJobParallelForDefer
        {
            // 速度制限
            //public float velocityLimit;

            [Unity.Collections.ReadOnly]
            public NativeList<int> jobParticleIndexList;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            // aggregate

            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // 集計パーティクルごと
            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// 速度影響あり
        /// </summary>
        [BurstCompile]
        struct AggregateWithVelocityJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeList<int> jobParticleIndexList;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;

            // aggregate
            public float velocityAttenuation;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // 集計パーティクルごと
            public void Execute(int index)
            {
            }
        }

        internal static JobHandle SolveAggregateBufferAndClear(in ExProcessingList<int> processingList, float velocityAttenuation, JobHandle jobHandle)
        {
            return default;
        }

        unsafe internal static JobHandle SolveAggregateBufferAndClear(in NativeArray<int> particleArray, in NativeReference<int> counter, float velocityAttenuation, JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct AggregateJob2 : IJobParallelForDefer
        {
            // 速度制限
            //public float velocityLimit;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> particleIndexArray;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            // aggregate

            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // 集計パーティクルごと
            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// 速度影響あり
        /// </summary>
        [BurstCompile]
        struct AggregateWithVelocityJob2 : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> particleIndexArray;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;

            // aggregate
            public float velocityAttenuation;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // 集計パーティクルごと
            public void Execute(int index)
            {
            }
        }


        /// <summary>
        /// 集計バッファのカウンタのみゼロクリアする
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal static JobHandle ClearCountArray(JobHandle jobHandle)
        {
            return default;
        }
    }
}
