// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MagicaCloth2
{
    /// <summary>
    /// １つのバッファに並列にデータを書き込めるようにするための構造。
    /// カウンターをアトミック操作することによりその開始インデックスを管理する。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe class ExProcessingList<T> : IDisposable, IValid where T : struct
    {
        /// <summary>
        /// バッファの現在のデータ数をカウントするためのカウンター
        /// </summary>
        public NativeReference<int> Counter;

        /// <summary>
        /// データバッファ
        /// </summary>
        public NativeArray<T> Buffer;

        //=========================================================================================
        public void Dispose()
        {
        }

        public bool IsValid()
        {
            return default;
        }

        //=========================================================================================
        public ExProcessingList()
        {
        }

        //=========================================================================================
        /// <summary>
        /// キャパシティが収まるようにバッファを拡張する。
        /// すでに容量が確保できている場合は何もしない。
        /// </summary>
        /// <param name="capacity"></param>
        public void UpdateBuffer(int capacity)
        {
        }

        /// <summary>
        /// ジョブスケジュール用のカウントintポインターを取得する
        /// </summary>
        /// <param name="counter"></param>
        /// <returns></returns>
        public int* GetJobSchedulePtr()
        {
            return default;
        }

        public override string ToString()
        {
            return default;
        }
    }
}
