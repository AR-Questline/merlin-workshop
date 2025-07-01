#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using System.Runtime.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;


namespace Unity.Rendering.Occlusion.Masked
{
    static class IntrinsicUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int _vmovemask_f32(v128 a)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                //https://github.com/jratcliff63367/sse2neon/blob/master/SSE2NEON.h#L518
                // TODO: this version should work but need to revisit the callsites and see if we can get rid of it altogether
                v128 movemask = new v128(1u, 2u, 4u, 8u);
                v128 highbit = new v128(0x80000000u);

                v128 t0 = Arm.Neon.vtstq_u32(a, highbit);
                v128 t1 = Arm.Neon.vandq_u32(t0, movemask);
                return Arm.Neon.vaddvq_s32(t1);
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static v128 _vtranspose_s8(v128 a)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                v128 v0 = Arm.Neon.vcopyq_laneq_u32(new v128(0), 0, a, 0);
                v128 v1 = Arm.Neon.vcopyq_laneq_u32(new v128(0), 0, a, 1);
                v128 v2 = Arm.Neon.vcopyq_laneq_u32(new v128(0), 0, a, 2);
                v128 v3 = Arm.Neon.vcopyq_laneq_u32(new v128(0), 0, a, 3);

                v128 v4 = Arm.Neon.vzip1q_s8(v0, v1);
                v128 v5 = Arm.Neon.vzip1q_s8(v2, v3);
                return Arm.Neon.vzip1q_u16(v4, v5);
            }
            return new v128();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static v128 _vsllv_ones(v128 ishift)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                v128 shift = Arm.Neon.vminq_s32(ishift, new v128(32));
                return Arm.Neon.vshlq_s32(new v128(~0), shift);
            }
            return new v128();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static v128 _vblendq_f32(v128 mask, v128 a, v128 b)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                // set 32-bit element according to the sign bit
                // to emulate intel blendv behavior
                v128 swapMask = Arm.Neon.vcgezq_s32(mask);
                return Arm.Neon.vbslq_s8(swapMask, a, b);
            }
            return new v128();
        }

        // read access
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int getIntLane(v128 vector, uint laneIdx)
        {
            //Debug.Assert(laneIdx >= 0 && laneIdx < 4);

            // eat the modulo cost to not let it overflow
            switch (laneIdx % 4)
            {
                default:    // DS: incorrect, but works with modulo and silences compiler (CS0161)
                case 0: { return vector.SInt0; }
                case 1: { return vector.SInt1; }
                case 2: { return vector.SInt2; }
                case 3: { return vector.SInt3; }
            }
        }

        // read access
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte getByteLane(v128 vector, uint laneIdx)
        {
            //Debug.Assert(laneIdx >= 0 && laneIdx < 4);

            // eat the modulo cost to not let it overflow
            switch (laneIdx % 16)
            {
                default:    // DS: incorrect, but works with modulo and silences compiler (CS0161)
                case 0: { return vector.Byte0; }
                case 1: { return vector.Byte1; }
                case 2: { return vector.Byte2; }
                case 3: { return vector.Byte3; }
                case 4: { return vector.Byte4; }
                case 5: { return vector.Byte5; }
                case 6: { return vector.Byte6; }
                case 7: { return vector.Byte7; }
                case 8: { return vector.Byte8; }
                case 9: { return vector.Byte9; }
                case 10: { return vector.Byte10; }
                case 11: { return vector.Byte11; }
                case 12: { return vector.Byte12; }
                case 13: { return vector.Byte13; }
                case 14: { return vector.Byte14; }
                case 15: { return vector.Byte15; }
            }
        }

        // used for "write" access (returns copy, requires assignment afterwards)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static v128 getCopyWithIntLane(v128 vector, uint laneIdx, int laneVal)
        {
            //Debug.Assert(laneIdx >= 0 && laneIdx < 4);

            // eat the modulo cost to not let it overflow
            switch (laneIdx % 4)
            {
                default:    // DS: incorrect fallthrough, but works with modulo and silences compiler (CS0161)
                case 0: { vector.SInt0 = laneVal; break; }
                case 1: { vector.SInt1 = laneVal; break; }
                case 2: { vector.SInt2 = laneVal; break; }
                case 3: { vector.SInt3 = laneVal; break; }
            }

            return vector;
        }

        // read access
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float getFloatLane(v128 vector, uint laneIdx)
        {
            //Debug.Assert(laneIdx >= 0 && laneIdx < 4);

            // eat the modulo cost to not let it overflow
            switch (laneIdx % 4)
            {
                default:    // DS: incorrect fallthrough, but works with modulo and silences compiler (CS0161)
                case 0: { return vector.Float0; }
                case 1: { return vector.Float1; }
                case 2: { return vector.Float2; }
                case 3: { return vector.Float3; }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static v128 _mmw_fmadd_ps(v128 a, v128 b, v128 c)
        {
            if (X86.Fma.IsFmaSupported)
                return X86.Fma.fmadd_ps(a, b, c);
            else if (X86.Sse.IsSseSupported)
                return X86.Sse.add_ps(X86.Sse.mul_ps(a, b), c);
            return new v128();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static v128 _mmw_fmsub_ps(v128 a, v128 b, v128 c)
        {
            if (X86.Fma.IsFmaSupported)
                return X86.Fma.fmsub_ps(a, b, c);
            else if (X86.Sse.IsSseSupported)
                return X86.Sse.sub_ps(X86.Sse.mul_ps(a, b), c);
            return new v128();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static v128 _mmw_transpose_epi8(v128 a)
        {
            if (X86.Ssse3.IsSsse3Supported)
            {
                v128 shuff = X86.Sse2.setr_epi8(0x0, 0x4, 0x8, 0xC, 0x1, 0x5, 0x9, 0xD, 0x2, 0x6, 0xA, 0xE, 0x3, 0x7, 0xB, 0xF);
                return X86.Ssse3.shuffle_epi8(a, shuff);
            }
            return new v128();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static v128 _mmw_sllv_ones(v128 ishift)
        {
            if (X86.Sse4_1.IsSse41Supported)
            {
                v128 shift = X86.Sse4_1.min_epi32(ishift, X86.Sse2.set1_epi32(32));

                // Uses lookup tables and _mm_shuffle_epi8 to perform _mm_sllv_epi32(~0, shift)
                v128 byteShiftLUT;
                unchecked
                {
                    byteShiftLUT = X86.Sse2.setr_epi8((sbyte)0xFF, (sbyte)0xFE, (sbyte)0xFC, (sbyte)0xF8, (sbyte)0xF0, (sbyte)0xE0, (sbyte)0xC0, (sbyte)0x80, 0, 0, 0, 0, 0, 0, 0, 0);
                }
                v128 byteShiftOffset = X86.Sse2.setr_epi8(0, 8, 16, 24, 0, 8, 16, 24, 0, 8, 16, 24, 0, 8, 16, 24);
                v128 byteShiftShuffle = X86.Sse2.setr_epi8(0x0, 0x0, 0x0, 0x0, 0x4, 0x4, 0x4, 0x4, 0x8, 0x8, 0x8, 0x8, 0xC, 0xC, 0xC, 0xC);

                v128 byteShift = X86.Ssse3.shuffle_epi8(shift, byteShiftShuffle);

                // DS: TODO: change once we get Burst fix for X86.Sse2.set1_epi8()
                const sbyte val = 8;
                byteShift = X86.Sse4_1.min_epi8(X86.Sse2.subs_epu8(byteShift, byteShiftOffset), new v128(val) /*X86.Sse2.set1_epi8(8)*/);

                v128 retMask = X86.Ssse3.shuffle_epi8(byteShiftLUT, byteShift);

                return retMask;
            }
            return new v128();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong find_clear_lsb(ref uint mask)
        {
            ulong idx = (ulong)math.tzcnt(mask);
            mask &= mask - 1;
            return idx;
        }
    }
}
#endif
