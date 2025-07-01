// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class DataUtility
    {
        /// <summary>
        /// ２つのintをint2にパックする
        /// データは昇順にソートされる
        /// </summary>
        /// <param name="d0"></param>
        /// <param name="d1"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 PackInt2(int d0, int d1)
        {
            return default;
        }

        /// <summary>
        /// int2をデータの昇順にソートして格納し直す
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 PackInt2(in int2 d) => PackInt2(d.x, d.y);

        /// <summary>
        /// ３つのintをint3にパックする
        /// データは昇順にソートされる
        /// </summary>
        /// <param name="d0"></param>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static int3 PackInt3(int d0, int d1, int d2)
        {
            return default;
        }

        public static int3 PackInt3(in int3 d) => PackInt3(d.x, d.y, d.z);

        /// <summary>
        /// ４つのintをint4にパックする
        /// データは昇順にソートされる
        /// </summary>
        /// <param name="d0"></param>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <param name="d3"></param>
        /// <returns></returns>
        public static int4 PackInt4(int d0, int d1, int d2, int d3)
        {
            return default;
        }

        public static int4 PackInt4(int4 d) => PackInt4(d.x, d.y, d.z, d.w);


        /// <summary>
        /// ２つのintをushortに変換し１つのuintにパッキングする
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Pack32(int hi, int low)
        {
            return default;
        }

        /// <summary>
        /// ２つのintをushortに変換し１つのuintにパッキングする
        /// データの小さいほうが上位に格納されるようにソートされる
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Pack32Sort(int a, int b)
        {
            return default;
        }

        /// <summary>
        /// uintパックデータから上位16bitをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack32Hi(uint pack)
        {
            return default;
        }

        /// <summary>
        /// uintパックデータから下位16bitをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack32Low(uint pack)
        {
            return default;
        }

#if false
        /// <summary>
        /// ２つのintをhi(10bit)とlow(22bit)に切り詰めて１つのuintにパッキングする
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Pack10_22(int hi, int low)
        {
            return (uint)hi << 22 | (uint)low & 0x3fffff;
        }

        /// <summary>
        /// uint10-22パックデータから上位10bitデータをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack10_22Hi(uint pack)
        {
            return (int)((pack >> 22) & 0x3ff);
        }

        /// <summary>
        /// uint10-22パックデータから下位22bitデータをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack10_22Low(uint pack)
        {
            return (int)(pack & 0x3fffff);
        }

        /// <summary>
        /// uint10-22パックデータを分解して２つのintとして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unpack10_22(uint pack, out int hi, out int low)
        {
            hi = (int)((pack >> 22) & 0x3ff);
            low = (int)(pack & 0x3fffff);
        }
#endif

        /// <summary>
        /// ２つのintをhi(12bit)とlow(20bit)に切り詰めて１つのuintにパッキングする
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Pack12_20(int hi, int low)
        {
            return default;
        }

        /// <summary>
        /// uint12-20パックデータから上位12bitデータをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack12_20Hi(uint pack)
        {
            return default;
        }

        /// <summary>
        /// uint12-20パックデータから下位20bitデータをintにして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack12_20Low(uint pack)
        {
            return default;
        }

        /// <summary>
        /// uint12-20パックデータを分解して２つのintとして返す
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="hi"></param>
        /// <param name="low"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unpack12_20(uint pack, out int hi, out int low)
        {
            hi = default(int);
            low = default(int);
        }

        /// <summary>
        /// ４つのintをushortに変換し１つのulongにパッキングする
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Pack64(int x, int y, int z, int w)
        {
            return default;
        }

        /// <summary>
        /// int4をushortに変換し１つのulongにパッキングする
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Pack64(in int4 a)
        {
            return default;
        }

        /// <summary>
        /// ulongパックデータからint4に展開して返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 Unpack64(in ulong pack)
        {
            return default;
        }

        /// <summary>
        /// ulongパックデータからx値を取り出す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack64X(in ulong pack)
        {
            return default;
        }

        /// <summary>
        /// ulongパックデータからy値を取り出す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack64Y(in ulong pack)
        {
            return default;
        }

        /// <summary>
        /// ulongパックデータからz値を取り出す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack64Z(in ulong pack)
        {
            return default;
        }

        /// <summary>
        /// ulongパックデータからw値を取り出す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Unpack64W(in ulong pack)
        {
            return default;
        }

        /// <summary>
        /// ４つのintをbyteに変換し１つのuintにパッキングする
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Pack32(int x, int y, int z, int w)
        {
            return default;
        }

        /// <summary>
        /// int4をbyteに変換し１つのuintにパッキングする
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Pack32(in int4 a)
        {
            return default;
        }

        /// <summary>
        /// uintパックデータからint4に展開して返す
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 Unpack32(in uint pack)
        {
            return default;
        }

        /// <summary>
        /// int3のうちuse(int2)で使われていない残りの１つのデータを返す
        /// </summary>
        /// <param name="data"></param>
        /// <param name="use"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RemainingData(in int3 data, in int2 use)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// AnimationCurveを16個のfloatのリスト(float4x4)に変換する
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static float4x4 ConvertAnimationCurve(AnimationCurve curve)
        {
            return default;
        }

        /// <summary>
        /// AnimationCurveが格納されたfloat4x4からデータを取得する
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EvaluateCurve(in float4x4 curve, float time)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 8bitフラグからコライダータイプを取得する
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ColliderManager.ColliderType GetColliderType(in ExBitFlag8 flag)
        {
            return default;
        }

        /// <summary>
        /// 8bitフラグにコライダータイプを設定する
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="ctype"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExBitFlag8 SetColliderType(ExBitFlag8 flag, ColliderManager.ColliderType ctype)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 配列をDeepコピーする
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        public static void ArrayCopy<T>(T[] src, ref T[] dst)
        {
        }
    }
}
