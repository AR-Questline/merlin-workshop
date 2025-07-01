// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// サイズ拡張可能なNativeArray管理クラス
    /// 領域が不足すると自動でサイズを拡張する
    /// ただし領域の拡張のみで削除や領域の再利用はできない
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExSimpleNativeArray<T> : IDisposable where T : unmanaged
    {
        NativeArray<T> nativeArray;

        int count;
        int length;

        //=========================================================================================
        public ExSimpleNativeArray()
        {
        }

        /// <summary>
        /// 領域を確保する
        /// </summary>
        /// <param name="dataLength"></param>
        /// <param name="areaOnly">true=領域のみで利用カウントを進めない</param>
        public ExSimpleNativeArray(int dataLength, bool areaOnly = false) : this()
        {
        }

        public ExSimpleNativeArray(T[] dataArray) : this()
        {
        }

        public ExSimpleNativeArray(NativeArray<T> array) : this()
        {
        }

        public ExSimpleNativeArray(NativeList<T> array) : this()
        {
        }

        public ExSimpleNativeArray(SerializationData sdata)
        {
        }

        public void Dispose()
        {
        }

        public bool IsValid
        {
            get
            {
                return nativeArray.IsCreated;
            }
        }

        /// <summary>
        /// 実際に使用されている要素数
        /// </summary>
        public int Count => count;

        /// <summary>
        /// 確保されている配列の要素数
        /// </summary>
        public int Length => length;

        /// <summary>
        /// 使用配列カウントを設定する
        /// </summary>
        /// <param name="newCount"></param>
        public void SetCount(int newCount)
        {
        }

        //=========================================================================================
        /// <summary>
        /// 領域のみ拡張する
        /// </summary>
        /// <param name="capacity"></param>
        public void AddCapacity(int capacity)
        {
        }

        /// <summary>
        /// サイズ分の空データを追加する
        /// </summary>
        /// <param name="dataLength"></param>
        public void AddRange(int dataLength)
        {
        }

        /// <summary>
        /// 配列データを追加する
        /// </summary>
        /// <param name="dataArray"></param>
        public void AddRange(T[] dataArray)
        {
        }

        /// <summary>
        /// 配列データを追加する
        /// </summary>
        /// <param name="dataArray"></param>
        public void AddRange(T[] dataArray, int cnt)
        {
        }

        /// <summary>
        /// 領域を確保し設定値で埋める（それなりのコストが発生するので注意！）
        /// </summary>
        /// <param name="dataLength"></param>
        /// <param name="fillData"></param>
        public void AddRange(int dataLength, T fillData = default(T))
        {
        }

        public void AddRange(NativeArray<T> narray)
        {
        }

        public void AddRange(NativeList<T> nlist)
        {
        }

        public void AddRange(ExSimpleNativeArray<T> exarray)
        {
        }

        /// <summary>
        /// 型は異なるが型のサイズは同じ配列を追加する。Vector3->float3など。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe void AddRange<U>(U[] array) where U : struct
        {
        }

        /// <summary>
        /// 型もサイズも異なる配列を追加する。int[] -> int3[]など。
        /// データはそのままメモリコピーされる。例えばint[]からint3[]へ追加すると次のようになる。
        /// int[]{1, 2, 3, 4, 5, 6} => int3[]{{1, 2, 3}, {4, 5, 6}}
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe void AddRangeTypeChange<U>(U[] array) where U : struct
        {
        }

        public unsafe void AddRangeTypeChange<U>(NativeArray<U> array) where U : struct
        {
        }

        /// <summary>
        /// 型もサイズも異なる配列を部分的にコピーする。Vector4[] -> float3など。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public unsafe void AddRangeStride<U>(U[] array) where U : struct
        {
        }

        public void Add(T data)
        {
        }

        public T[] ToArray()
        {
            return default;
        }

        public void CopyTo(T[] array)
        {
        }

        public void CopyTo<U>(U[] array) where U : struct
        {
        }

        /// <summary>
        /// 型もサイズも異なる配列にデータをコピーする。
        /// int3 -> int[]など
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        public unsafe void CopyToWithTypeChange<U>(U[] array) where U : struct
        {
        }

        /// <summary>
        /// 型もサイズも異なる配列にデータを断片的にコピーする。
        /// float3 -> Vector4[]など。この場合はVector4にはxyzのみ書き込まれる。
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="array"></param>
        public unsafe void CopyToWithTypeChangeStride<U>(U[] array) where U : struct
        {
        }

        public void CopyFrom(NativeArray<T> array)
        {
        }

        public void CopyFrom<U>(NativeArray<U> array) where U : struct
        {
        }

        public unsafe void CopyFromWithTypeChangeStride<U>(NativeArray<U> array) where U : struct
        {
        }

        /// <summary>
        /// 設定値で埋める（それなりのコストが発生するので注意！）
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="dataLength"></param>
        /// <param name="fillData"></param>
        public void Fill(int startIndex, int dataLength, T fillData = default(T))
        {
        }

        unsafe void FillInternal(int start, int size, T fillData = default(T))
        {
        }

        public T this[int index]
        {
            get
            {
                return nativeArray[index];
            }
            set
            {
                nativeArray[index] = value;
            }
        }

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す
        /// </summary>
        /// <returns></returns>
        public NativeArray<T> GetNativeArray()
        {
            return default;
        }

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す(型変更あり)
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <returns></returns>
        public NativeArray<U> GetNativeArray<U>() where U : struct
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 領域を拡張する（必要がなければ何もしない）
        /// </summary>
        /// <param name="dataLength"></param>
        /// <param name="force">強制的に領域を追加</param>
        void Expand(int dataLength, bool force = false)
        {
        }

        public override string ToString()
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// シリアライズデータ
        /// </summary>
        [System.Serializable]
        public class SerializationData
        {
            public int count;
            public int length;
            public byte[] arrayBytes;
        }

        /// <summary>
        /// シリアライズする
        /// </summary>
        /// <returns></returns>
        public SerializationData Serialize()
        {
            return default;
        }

        /// <summary>
        /// デシリアライズする
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Deserialize(SerializationData data)
        {
            return default;
        }
    }
}
