// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public static class JobUtility
    {
        //=========================================================================================
        /// <summary>
        /// 配列をValueで埋めるジョブを発行します
        /// ジェネリック型ジョブは明示的に型を<T>で指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="length"></param>
        /// <param name="value"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle Fill(NativeArray<int> array, int length, int value, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static JobHandle Fill(NativeArray<Vector4> array, int length, Vector4 value, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static JobHandle Fill(NativeArray<VirtualMeshBoneWeight> array, int length, VirtualMeshBoneWeight value, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static JobHandle Fill(NativeArray<byte> array, int length, byte value, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static void FillRun(NativeArray<int> array, int length, int value)
        {
        }

        public static void FillRun(NativeArray<Vector4> array, int length, Vector4 value)
        {
        }

        public static void FillRun(NativeArray<quaternion> array, int length, quaternion value)
        {
        }

        public static void FillRun(NativeArray<VirtualMeshBoneWeight> array, int length, VirtualMeshBoneWeight value)
        {
        }

        [BurstCompile]
        struct FillJob<T> : IJobParallelFor where T : unmanaged
        {
            public T value;

            [Unity.Collections.WriteOnly]
            public NativeArray<T> array;

            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// 配列をValueで埋めるジョブを発行します(startIndexあり)
        /// ジェネリック型ジョブは明示的に型を<T>で指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <param name="value"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle Fill(NativeArray<int> array, int startIndex, int length, int value, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        [BurstCompile]
        struct FillJob2<T> : IJobParallelFor where T : unmanaged
        {
            public T value;
            public int startIndex;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<T> array;

            public void Execute(int index)
            {
            }
        }

        public static JobHandle Fill(NativeReference<int> reference, int value, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        [BurstCompile]
        struct FillRefJob<T> : IJob where T : unmanaged
        {
            public T value;

            [Unity.Collections.WriteOnly]
            public NativeReference<T> reference;

            public void Execute()
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// 配列に連番を格納するジョブを発行します
        /// </summary>
        /// <param name="array"></param>
        /// <param name="length"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle SerialNumber(NativeArray<int> array, int length, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static void SerialNumberRun(NativeArray<int> array, int length)
        {
        }

        [BurstCompile]
        struct SerialNumberJob : IJobParallelFor
        {
            [Unity.Collections.WriteOnly]
            public NativeArray<int> array;

            public void Execute(int index)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// NativeHashSetのキーをNativeListに変換するジョブを発行します
        /// ジェネリック型ジョブは明示的に型を<T>で指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashSet"></param>
        /// <param name="list"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle ConvertHashSetToNativeList(NativeParallelHashSet<int> hashSet, NativeList<int> list, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        [BurstCompile]
        struct ConvertHashSetToListJob<T> : IJob where T : unmanaged, IEquatable<T>
        {
            [Unity.Collections.ReadOnly]
            public NativeParallelHashSet<T> hashSet;
            [Unity.Collections.WriteOnly]
            public NativeList<T> list;

            public void Execute()
            {
            }
        }

        //=========================================================================================
#if false
        /// <summary>
        /// NativeMultiHashMapのキーをNativeListに変換するジョブを発行する
        /// ジェネリック型ジョブは明示的に型を<T>で指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="hashMap"></param>
        /// <param name="keyList"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle ConvertMultiHashMapKeyToNativeList(
            NativeParallelMultiHashMap<int2, int> hashMap,
            NativeList<int2> keyList,
            JobHandle dependsOn = new JobHandle())
        {
            // todo:この処理は重い
            var job = new ConvertMultiHashMapKeyToListJob<int2, int>()
            {
                hashMap = hashMap,
                list = keyList,
            };
            return job.Schedule(dependsOn);
        }

        [BurstCompile]
        struct ConvertMultiHashMapKeyToListJob<T, U> : IJob where T : unmanaged, IEquatable<T> where U : unmanaged
        {
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<T, U> hashMap;
            [Unity.Collections.WriteOnly]
            public NativeList<T> list;

            public void Execute()
            {
                var keySet = new NativeParallelHashSet<T>(hashMap.Count(), Allocator.Temp); // ここが問題となる可能性がある(unity2023.1.5事件)
                var keyArray = hashMap.GetKeyArray(Allocator.Temp); // ここが問題となる可能性がある(unity2023.1.5事件)
                // GetKeyArray()の結果はキーが重複しまた順不同なので注意！
                for (int i = 0; i < keyArray.Length; i++)
                    keySet.Add(keyArray[i]);

                foreach (var key in keySet)
                    list.Add(key);
            }
        }
#endif

        //=========================================================================================
        /// <summary>
        /// NativeHashSetの内容をNativeListに変換するジョブを発行する
        /// ジェネリック型ジョブは明示的に型を<T>で指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <param name="hashSet"></param>
        /// <param name="keyList"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle ConvertHashSetKeyToNativeList(
            NativeParallelHashSet<int2> hashSet,
            NativeList<int2> keyList,
            JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static JobHandle ConvertHashSetKeyToNativeList(
            NativeParallelHashSet<int4> hashSet,
            NativeList<int4> keyList,
            JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        [BurstCompile]
        struct ConvertHashSetKeyToListJob<T> : IJob where T : unmanaged, IEquatable<T>
        {
            [Unity.Collections.ReadOnly]
            public NativeParallelHashSet<T> hashSet;
            [Unity.Collections.WriteOnly]
            public NativeList<T> list;

            public void Execute()
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// AABBを計算して返すジョブを発行する(NativeArray)
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="length"></param>
        /// <param name="outAABB"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle CalcAABB(NativeArray<float3> positions, int length, NativeReference<AABB> outAABB, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static void CalcAABBRun(NativeArray<float3> positions, int length, NativeReference<AABB> outAABB)
        {
        }

        /// <summary>
        /// AABBを計算して返すジョブを発行する(NativeList)
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="outAABB"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle CalcAABB(NativeList<float3> positions, NativeReference<AABB> outAABB, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static void CalcAABBRun(NativeList<float3> positions, NativeReference<AABB> outAABB)
        {
        }

        [BurstCompile]
        struct CalcAABBJob : IJob
        {
            public int length;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;

            public NativeReference<AABB> outAABB;

            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct CalcAABBDeferJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeList<float3> positions;

            public NativeReference<AABB> outAABB;

            public void Execute()
            {
            }
        }

        static AABB CalcAABBInternal(in NativeArray<float3> positions, int length)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// スフィアマッピングを行いUVを算出するジョブを発行する
        /// このUVは接線計算用でありテクスチャ用ではないので注意！
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="length"></param>
        /// <param name="aabb"></param>
        /// <param name="outUVs"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle CalcUVWithSphereMapping(NativeArray<float3> positions, int length, NativeReference<AABB> aabb, NativeArray<float2> outUVs, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static void CalcUVWithSphereMappingRun(NativeArray<float3> positions, int length, NativeReference<AABB> aabb, NativeArray<float2> outUVs)
        {
        }

        [BurstCompile]
        struct CalcUVJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeReference<AABB> aabb;

            [Unity.Collections.WriteOnly]
            public NativeArray<float2> uvs;

            public void Execute(int index)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// intデータを加算して新しい領域にコピーする
        /// </summary>
        [BurstCompile]
        public struct AddIntDataCopyJob : IJobParallelFor
        {
            public int dstOffset;
            public int addData;

            // src
            [Unity.Collections.ReadOnly]
            public NativeArray<int> srcData;

            // dst
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> dstData;

            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// int2データを加算して新しい領域にコピーする
        /// </summary>
        [BurstCompile]
        public struct AddInt2DataCopyJob : IJobParallelFor
        {
            public int dstOffset;
            public int2 addData;

            // src
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> srcData;

            // dst
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int2> dstData;

            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// int3データを加算して新しい領域にコピーする
        /// </summary>
        [BurstCompile]
        public struct AddInt3DataCopyJob : IJobParallelFor
        {
            public int dstOffset;
            public int3 addData;

            // src
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> srcData;

            // dst
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int3> dstData;

            public void Execute(int index)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// 座標を変換するジョブを発行します
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="length"></param>
        /// <param name="toM"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle TransformPosition(NativeArray<float3> positions, int length, in float4x4 toM, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static void TransformPositionRun(NativeArray<float3> positions, int length, in float4x4 toM)
        {
        }

        public static JobHandle TransformPosition(NativeArray<float3> srcPositions, NativeArray<float3> dstPositions, int length, in float4x4 toM, JobHandle dependsOn = new JobHandle())
        {
            return default;
        }

        public static void TransformPositionRun(NativeArray<float3> srcPositions, NativeArray<float3> dstPositions, int length, in float4x4 toM)
        {
        }

        [BurstCompile]
        public struct TransformPositionJob : IJobParallelFor
        {
            public float4x4 toM;
            public NativeArray<float3> positions;

            public void Execute(int vindex)
            {
            }
        }

        [BurstCompile]
        public struct TransformPositionJob2 : IJobParallelFor
        {
            public float4x4 toM;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srcPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> dstPositions;

            public void Execute(int vindex)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// スタートインデックス＋データ数とデータの２つの配列からHashMapを構築して返す
        /// </summary>
        /// <param name="indexArray"></param>
        /// <param name="dataArray"></param>
        /// <returns></returns>
        public static NativeParallelMultiHashMap<int, ushort> ToNativeMultiHashMap(in NativeArray<uint> indexArray, in NativeArray<ushort> dataArray)
        {
            return default;
        }

        [BurstCompile]
        struct ConvertArrayToMapJob<TData> : IJob where TData : unmanaged
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> indexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<TData> dataArray;

            [Unity.Collections.WriteOnly]
            public NativeParallelMultiHashMap<int, TData> map;

            public void Execute()
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// NativeReferenceをクリアするジョブを発行する
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public static JobHandle ClearReference(NativeReference<int> reference, JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct ClearReferenceJob : IJob
        {
            public NativeReference<int> reference;

            public void Execute()
            {
            }
        }
    }
}
