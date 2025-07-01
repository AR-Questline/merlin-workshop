// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public static class MathUtility
    {
        /// <summary>
        /// 数値を(-1.0f～1.0f)にクランプする
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp1(float a)
        {
            return default;
        }

        /// <summary>
        /// 投影ベクトルを求める
        /// </summary>
        /// <param name="v"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Project(in float3 v, in float3 n)
        {
            return default;
        }

        /// <summary>
        /// ベクトルを平面に投影する
        /// </summary>
        /// <param name="v"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ProjectOnPlane(in float3 v, in float3 n)
        {
            return default;
        }

        /// <summary>
        /// ２つのベクトルのなす角を返す（ラジアン）
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>ラジアン</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(in float3 v1, in float3 v2)
        {
            return default;
        }

        /// <summary>
        /// ベクトルの長さをクランプする
        /// </summary>
        /// <param name="v"></param>
        /// <param name="minlength"></param>
        /// <param name="maxlength"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClampVector(float3 v, float minlength, float maxlength)
        {
            return default;
        }

        /// <summary>
        /// ベクトルの長さをクランプする
        /// </summary>
        /// <param name="v"></param>
        /// <param name="minlength"></param>
        /// <param name="maxlength"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClampVector(float3 v, float maxlength)
        {
            return default;
        }

        /// <summary>
        /// frotmからtoへの移動を最大移動距離でクランプする
        /// </summary>
        /// <param name="from">基準座標</param>
        /// <param name="to">目標座標</param>
        /// <param name="maxlength">最大移動距離</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClampDistance(float3 from, float3 to, float maxlength)
        {
            return default;
        }

        /// <summary>
        /// ベクトル(dir)を最大角度にクランプする
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="basedir"></param>
        /// <param name="maxAngle">最大角度（ラジアン）</param>
        /// <returns></returns>
        public static bool ClampAngle(in float3 dir, in float3 basedir, float maxAngle, out float3 outdir)
        {
            outdir = default(float3);
            return default;
        }

        /// <summary>
        /// fromからtoへ回転させるクォータニオンを返します
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="t">補間率(0.0-1.0)</param>
        /// <returns></returns>
        public static quaternion FromToRotation(in float3 from, in float3 to, float t = 1.0f)
        {
            return default;
        }

        /// <summary>
        /// fromからtoへ回転させるクォータニオンを返します
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion FromToRotation(in quaternion from, in quaternion to)
        {
            return default;
        }

        /// <summary>
        /// ２つのクォータニオンの角度を返します（ラジアン）
        /// 不正なクォータニオンでは結果が不定になるので注意！例:(0,0,0,0)など
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(in quaternion a, in quaternion b)
        {
            return default;
        }

        /// <summary>
        /// クォータニオンを最大角度にクランプします
        /// </summary>
        /// <param name="from">基準回転</param>
        /// <param name="to">目標回転</param>
        /// <param name="maxAngle">最大角度（ラジアン）</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ClampAngle(quaternion from, quaternion to, float maxAngle)
        {
            return default;
        }

        /// <summary>
        /// 法線と接線から回転姿勢を求める
        /// </summary>
        /// <param name="nor"></param>
        /// <param name="tan"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ToRotation(in float3 nor, in float3 tan)
        {
            return default;
        }

        /// <summary>
        /// 回転姿勢を法線と接線に分解して返す
        /// </summary>
        /// <param name="rot"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToNormalTangent(in quaternion rot, out float3 nor, out float3 tan)
        {
            nor = default(float3);
            tan = default(float3);
        }

        /// <summary>
        /// 回転姿勢から法線を取り出す
        /// </summary>
        /// <param name="rot"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToNormal(in quaternion rot)
        {
            return default;
        }

        /// <summary>
        /// 回転姿勢から接線を取り出す
        /// </summary>
        /// <param name="rot"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToTangent(in quaternion rot)
        {
            return default;
        }

        /// <summary>
        /// 回転姿勢から従法線を取り出す
        /// </summary>
        /// <param name="rot"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToBinormal(in quaternion rot)
        {
            return default;
        }

        /// <summary>
        /// 法線／接線から従法線を求めて返す
        /// </summary>
        /// <param name="nor"></param>
        /// <param name="tan"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Binormal(in float3 nor, in float3 tan)
        {
            return default;
        }

        /// <summary>
        /// 方向ベクトルをXY回転角度(ラジアン)に分離する、Z角度は常に０である
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 AxisToEuler(in float3 axis)
        {
            return default;
        }

        /// <summary>
        /// 方向ベクトルからクォータニオンを作成して返す
        /// ベクトルは一旦オイラー角に分解されてからクォータニオンへ組み立て直される
        /// XYの回転軸を安定させるため
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion AxisQuaternion(float3 dir)
        {
            return default;
        }

        /// <summary>
        /// クォータニオンから回転軸と回転角度(rad)を取得する
        /// この結果はUnity.Quaternion.ToAngleAxisと同じである（僅かに誤差あり）
        /// ただ回転角度が360度を越えると軸が逆転するので注意！（これはUnity.ToAngleAxis()でも同じ）
        /// 回転がほぼ０の場合は回転軸として(0, 0, 0)を返す（Unity.ToAngleAxisでは(1, 0, 0))
        /// </summary>
        /// <param name="q"></param>
        /// <param name="angle">回転角度(rad)</param>
        /// <param name="axis">回転軸</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToAngleAxis(in quaternion q, out float angle, out float3 axis)
        {
            angle = default(float);
            axis = default(float3);
        }

        /// <summary>
        /// 与えられた線分abおよび点cに対して、ab上の最近接点t(0.0-1.0)を計算して返す
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClosestPtPointSegmentRatio(in float3 c, in float3 a, in float3 b)
        {
            return default;
        }

        /// <summary>
        /// 与えられた線分abおよび点cに対して、ab上の最近接点tを計算して返す。tはクランプされない
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClosestPtPointSegmentRatioNoClamp(float3 c, float3 a, float3 b)
        {
            return default;
        }

        /// <summary>
        /// 与えられた線分abおよび点cに対して、ab上の最近接点座標dを計算して返す
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClosestPtPointSegment(float3 c, float3 a, float3 b)
        {
            return default;
        }

        /// <summary>
        /// 与えられた線分abおよび点cに対して、ab上の最近接点座標dを計算して返す。dはクランプされない
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClosestPtPointSegmentNoClamp(float3 c, float3 a, float3 b)
        {
            return default;
        }

        /// <summary>
        /// ２つの線分(p1-q1)(p2-q2)の最近接点(c1, c2)を計算する
        /// 戻り値として最近接点の距離の平方を返す
        /// </summary>
        /// <param name="p1">線分１の始点</param>
        /// <param name="q1">線分１の終点</param>
        /// <param name="p2">線分２の始点</param>
        /// <param name="q2">線分２の終点</param>
        /// <param name="c1">最近接点１</param>
        /// <param name="c2">最近接点２</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClosestPtSegmentSegment(in float3 p1, in float3 q1, in float3 p2, in float3 q2, out float s, out float t, out float3 c1, out float3 c2)
        {
            s = default(float);
            t = default(float);
            c1 = default(float3);
            c2 = default(float3);
            return default;
        }

        /// <summary>
        /// 三角形(abc)から点(p)への最近接点とその重心座標uvwを返す
        /// </summary>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="uvw"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClosestPtPointTriangle(in float3 p, in float3 a, in float3 b, in float3 c, out float3 uvw)
        {
            uvw = default(float3);
            return default;
        }

        /// <summary>
        /// 三角形と点の最近接点重心(uvw)から点が三角形の内部にあるか判定する
        /// </summary>
        /// <param name="uvw"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInTriangleUVW(float3 uvw)
        {
            return default;
        }


        /// <summary>
        /// トライアングルの重心を返す
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TriangleCenter(in float3 p0, in float3 p1, in float3 p2)
        {
            return default;
        }

        /// <summary>
        /// トライアングルの法線を計算して返す
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TriangleNormal(in float3 p0, in float3 p1, in float3 p2)
        {
            return default;
        }

        /// <summary>
        /// トライアングルの面積を求めて返す
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TriangleArea(in float3 p0, in float3 p1, in float3 p2)
        {
            return default;
        }

        /// <summary>
        /// 安全なトライアングルか判定する
        /// 面積が極端に小さいトライアングルは不正とする
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSafeTriangle(in float3 p0, in float3 p1, in float3 p2)
        {
            return default;
        }

        /// <summary>
        /// トライアングルの接線を計算して返す。
        /// 接線は単位化される。ただし、状況により長さ０となるケースがありその場合はベクトル０を返す。
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="uv0"></param>
        /// <param name="uv1"></param>
        /// <param name="uv2"></param>
        /// <returns></returns>
        public static float3 TriangleTangent(in float3 p0, in float3 p1, in float3 p2, in float2 uv0, in float2 uv1, in float2 uv2)
        {
            return default;
        }

        /// <summary>
        /// トライアングルの回転姿勢を返す
        /// 法線と(重心-p0)の軸からなるクォータニオン
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion TriangleRotation(float3 p0, float3 p1, float3 p2)
        {
            return default;
        }

        /// <summary>
        /// 隣接する２つのトライアングルの回転姿勢を返す
        /// 法線の平均と共通エッジからなるクォータニオン
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion TriangleCenterRotation(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            return default;
        }

        /// <summary>
        /// トライアングルペアのなす角を返す（ラジアン）
        ///   v2 +
        ///     / \
        /// v0 +---+ v1
        ///     \ /
        ///   v3 +
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>ラジアン、水平時は0となる</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TriangleAngle(in float3 v0, in float3 v1, in float3 v2, in float3 v3)
        {
            return default;
        }

        /// <summary>
        /// トライアングル重心からの距離を返す
        /// </summary>
        /// <param name="p"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceTriangleCenter(float3 p, float3 p0, float3 p1, float3 p2)
        {
            return default;
        }

        /// <summary>
        /// 点ｐがトライアングルの正負どちらの向きにあるか返す(-1/0/+1)
        /// </summary>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DirectionPointTriangle(float3 p, float3 a, float3 b, float3 c)
        {
            return default;
        }

        /// <summary>
        /// ２つのトライアングルと共通するエッジから残りの２つ頂点（対角点）を返す
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static int2 GetRestTriangleVertex(int3 tri1, int3 tri2, int2 edge)
        {
            return default;
        }

        /// <summary>
        /// ２つのトライアングルから共通する辺のインデックスを返す
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <returns>見つからない場合は(0,0)</returns>
        public static int2 GetCommonEdgeFromTrianglePair(int3 tri1, int3 tri2)
        {
            return default;
        }

        /// <summary>
        /// 共通する辺をもつ２つのトライアングルから四角を形成する４つの頂点インデックスを返す
        /// 頂点インデックスは[2][3]が共通する辺を示し、[0][1]は各トライアングルの残りのインデックス
        ///   v2 +
        ///     /|\
        /// v0 + | + v1
        ///     \|/
        ///   v3 +
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <returns></returns>
        public static int4 GetTrianglePairIndices(int3 tri1, int3 tri2)
        {
            return default;
        }

        /// <summary>
        /// トライアングルについて指定エッジ以外の頂点インデックスを返す
        /// </summary>
        /// <param name="tri"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static int GetUnuseTriangleIndex(int3 tri, int2 edge)
        {
            return default;
        }

        /// <summary>
        /// 共通するエッジをもつ２つのトライアングルのなす角を求める（ラジアン）
        ///   v2 +
        ///     /|\
        /// v0 + | + v1
        ///     \|/
        ///   v3 +
        /// </summary>
        /// <param name="pos0"></param>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <param name="pos3"></param>
        /// <returns>ラジアン</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetTrianglePairAngle(float3 pos0, float3 pos1, float3 pos2, float3 pos3)
        {
            return default;
        }

        /// <summary>
        /// トライアングルを反転させる
        /// </summary>
        /// <param name="tri"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 FlipTriangle(in int3 tri)
        {
            return default;
        }

        /// <summary>
        /// トライアングルを包む球の中心と半径を求める
        /// これは球からトライアングルがはみ出る事はないが完全に正確ではないので注意！
        /// あくまで衝突判定のブロードフェーズなどで使用することが目的のもの
        /// 正確性よりも速度を重視した実装となっている
        /// </summary>
        /// <param name="pos0"></param>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <param name="sc"></param>
        /// <param name="sr"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetTriangleSphere(float3 pos0, float3 pos1, float3 pos2, out float3 sc, out float sr)
        {
            sc = default(float3);
            sr = default(float);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 LocalToWorldMatrix(in float3 wpos, in quaternion wrot, in float3 wscl)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 WorldToLocalMatrix(in float3 wpos, in quaternion wrot, in float3 wscl)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TransformPoint(in float3 pos, in float3 wpos, in quaternion wrot, in float3 wscl)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TransformPoint(in float3 pos, in float4x4 m)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TransformVector(in float3 vec, in float4x4 m)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 TransformDirection(in float3 dir, in float4x4 m)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion TransformRotation(in quaternion rot, in float4x4 m, in float3 normalTangentFlip)
        {
            return default;
        }

        /// <summary>
        /// 距離を空間変換する
        /// 不均等スケールを考慮して各軸の平均値を返す
        /// </summary>
        /// <param name="dist"></param>
        /// <param name="localToWorldMatrix"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TransformDistance(in float dist, in float4x4 localToWorldMatrix)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TransformPositionNormalTangent(in float3 tpos, in quaternion trot, in float3 tscl, ref float3 pos, ref float3 nor, ref float3 tan)
        {
        }

        /// <summary>
        /// 長さをマトリックス空間に変換する
        /// </summary>
        /// <param name="length"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TransformLength(float length, in float4x4 matrix)
        {
            return default;
        }

        // ！！これはスケールが入るとうまく行かない
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static quaternion TransformRotation(in quaternion rot, in float4x4 localToWorldMatrix)
        //{
        //    return math.mul(rot, new quaternion(localToWorldMatrix));
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InverseTransformPoint(in float3 pos, in float4x4 worldToLocalMatrix)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InverseTransformPoint(in float3 pos, in float3 wpos, in quaternion wrot, in float3 wscl)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InverseTransformVector(in float3 vec, in float4x4 worldToLocalMatrix)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InverseTransformVector(in float3 vec, in quaternion rot)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InverseTransformDirection(in float3 dir, in float4x4 worldToLocalMatrix)
        {
            return default;
        }

        // ！！これはスケールが入るとうまく行かない
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static quaternion InverseTransformRotation(in quaternion rot, in float4x4 worldToLocalMatrix)
        //{
        //    return math.mul(new quaternion(worldToLocalMatrix), rot);
        //}

        /// <summary>
        /// fromのローカル座標をtoのローカル座標に変換するmatrixを返す
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 Transform(in float4x4 fromLocalToWorldMatrix, in float4x4 toWorldToLocalMatrix)
        {
            return default;
        }

        /// <summary>
        /// ２つのマトリックスが等しいか判定する
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareMatrix(in float4x4 m1, in float4x4 m2)
        {
            return default;
        }

        /// <summary>
        /// ２つの座標系が等しいか判定する
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="rot1"></param>
        /// <param name="scl1"></param>
        /// <param name="pos2"></param>
        /// <param name="rot2"></param>
        /// <param name="scl2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareTransform(in float3 pos1, in quaternion rot1, in float3 scl1, in float3 pos2, in quaternion rot2, in float3 scl2)
        {
            return default;
        }

        /// <summary>
        /// 線分pqおよび三角形abcに対して、線分が三角形と交差しているかどうかを返す
        /// 交差している場合は、交差点の重心(u,v,w)と線分の位置tを返す
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="doubleSide">両面判定はtrue</param>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="w"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IntersectSegmentTriangle(in float3 p, in float3 q, float3 a, float3 b, float3 c, bool doubleSide, out float u, out float v, out float w, out float t)
        {
            u = default(float);
            v = default(float);
            w = default(float);
            t = default(float);
            return default;
        }

        /// <summary>
        /// 線分pqおよび三角形abcに対して、線分が三角形と交差しているかどうかを返す
        /// 交差している場合は、交差点の重心(u,v,w)と線分の位置tを返す
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IntersectSegmentTriangle(in float3 p, in float3 q, float3 a, float3 b, float3 c)
        {
            return default;
        }

        /// <summary>
        /// 点と面の衝突判定
        /// 衝突した場合にその押し出し位置を計算して返す
        /// </summary>
        /// <param name="planePos"></param>
        /// <param name="planeDir"></param>
        /// <param name="pos"></param>
        /// <param name="outPos"></param>
        /// <returns>平面までの距離。押し出された（衝突の）場合は0.0以下(マイナス)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float IntersectPointPlaneDist(in float3 planePos, in float3 planeDir, in float3 pos, out float3 outPos)
        {
            outPos = default(float3);
            return default;
        }


        /// <summary>
        /// 光線と球が交差しているか判定する
        /// 交差している場合は交差しているtの値および交差点dを返す
        /// </summary>
        /// <param name="p">光線の始点</param>
        /// <param name="d">光線の方向|d|=1</param>
        /// <param name="sc">球の位置</param>
        /// <param name="sr">球の半径</param>
        /// <param name="t"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectRaySphere(in float3 p, in float3 d, in float3 sc, in float sr, ref float t, ref float3 q)
        {
            return default;
        }

        /// <summary>
        /// 点Cと線分abの間の距離の平方を返す
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static float SqDistPointSegment(Vector3 a, Vector3 b, Vector3 c)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(float3 v)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(float4 v)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(quaternion q)
        {
            return default;
        }

        /// <summary>
        /// 座標をPivotのローカル姿勢を保ちながらシフトさせる
        /// 主に慣性シフト用
        /// </summary>
        /// <param name="oldPos">移動前座標</param>
        /// <param name="oldPivotPosition">移動前のシフト中心座標</param>
        /// <param name="shiftVector">シフト移動量</param>
        /// <param name="shiftRotation">シフト回転量</param>
        /// <returns></returns>
        public static float3 ShiftPosition(in float3 oldPos, in float3 oldPivotPosition, in float3 shiftVector, in quaternion shiftRotation)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 深さから重量を計算して返す
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcMass(float depth)
        {
            return default;
        }

        /// <summary>
        /// 摩擦係数から逆重量を計算して返す
        /// </summary>
        /// <param name="friction"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcInverseMass(float friction)
        {
            return default;
        }

        /// <summary>
        /// 逆重量を計算して返す
        /// </summary>
        /// <param name="friction"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcInverseMass(float friction, float depth)
        {
            return default;
        }

        /// <summary>
        /// 逆重量を計算して返す
        /// </summary>
        /// <param name="friction"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcInverseMass(float friction, float depth, bool fix, float fixMass)
        {
            return default;
        }

        /// <summary>
        /// セルフコリジョン用の逆重量を計算して返す
        /// 固定パーティクルはほとんど動かなくする
        /// </summary>
        /// <param name="friction"></param>
        /// <param name="fix"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcSelfCollisionInverseMass(float friction, bool fix, float clothMass)
        {
            return default;
        }
    }
}
