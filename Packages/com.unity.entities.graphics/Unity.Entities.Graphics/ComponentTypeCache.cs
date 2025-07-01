using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Assertions;

namespace Unity.Rendering
{
    // Helper to only call GetDynamicComponentTypeHandle once per type per frame
    internal struct ComponentTypeCache
    {
        internal NativeParallelHashMap<int, int> UsedTypes;

        // Re-populated each frame with fresh objects for each used type.
        // Use C# array so we can hold SafetyHandles without problems.
        internal DynamicComponentTypeHandle[] TypeDynamics;
        internal int MaxIndex;

        public ComponentTypeCache(int initialCapacity) : this()
        {
            Reset(initialCapacity);
        }

        public void Reset(int capacity = 0)
        {
            Dispose();
            UsedTypes = new NativeParallelHashMap<int, int>(capacity, Allocator.Persistent);
            MaxIndex = 0;
        }

        public void Dispose()
        {
            if (UsedTypes.IsCreated) UsedTypes.Dispose();
            TypeDynamics = null;
        }

        public int UsedTypeCount => UsedTypes.Count();

        public void UseType(int typeIndex)
        {
            // Use indices without flags so we have a nice compact range
            int i = GetArrayIndex(typeIndex);
            Assert.IsTrue(!UsedTypes.ContainsKey(i) || UsedTypes[i] == typeIndex,
                "typeIndex is not consistent with its stored array index");
            UsedTypes[i] = typeIndex;
            MaxIndex = math.max(i, MaxIndex);
        }

        public void FetchTypeHandles(ComponentSystemBase componentSystem)
        {
            var types = UsedTypes.GetKeyValueArrays(Allocator.Temp);

            if (TypeDynamics == null || TypeDynamics.Length < MaxIndex + 1)
                // Allocate according to Capacity so we grow with the same geometric formula as NativeList
                TypeDynamics = new DynamicComponentTypeHandle[MaxIndex + 1];

            ref var keys = ref types.Keys;
            ref var values = ref types.Values;
            int numTypes = keys.Length;
            for (int i = 0; i < numTypes; ++i)
            {
                int arrayIndex = keys[i];
                int typeIndex = values[i];
                TypeDynamics[arrayIndex] = componentSystem.GetDynamicComponentTypeHandle(
                    ComponentType.ReadOnly(typeIndex));
            }

            types.Dispose();
        }

        public static int GetArrayIndex(int typeIndex) => typeIndex & TypeManager.ClearFlagsMask;

        public DynamicComponentTypeHandle Type(int typeIndex)
        {
            return TypeDynamics[GetArrayIndex(typeIndex)];
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BurstCompatibleTypeArray
        {
            public const int kMaxTypes = 128;

            [NativeDisableParallelForRestriction] public NativeArray<int> TypeIndexToArrayIndex;

            [ReadOnly] public DynamicComponentTypeHandle t0;
            [ReadOnly] public DynamicComponentTypeHandle t1;
            [ReadOnly] public DynamicComponentTypeHandle t2;
            [ReadOnly] public DynamicComponentTypeHandle t3;
            [ReadOnly] public DynamicComponentTypeHandle t4;
            [ReadOnly] public DynamicComponentTypeHandle t5;
            [ReadOnly] public DynamicComponentTypeHandle t6;
            [ReadOnly] public DynamicComponentTypeHandle t7;
            [ReadOnly] public DynamicComponentTypeHandle t8;
            [ReadOnly] public DynamicComponentTypeHandle t9;
            [ReadOnly] public DynamicComponentTypeHandle t10;
            [ReadOnly] public DynamicComponentTypeHandle t11;
            [ReadOnly] public DynamicComponentTypeHandle t12;
            [ReadOnly] public DynamicComponentTypeHandle t13;
            [ReadOnly] public DynamicComponentTypeHandle t14;
            [ReadOnly] public DynamicComponentTypeHandle t15;
            [ReadOnly] public DynamicComponentTypeHandle t16;
            [ReadOnly] public DynamicComponentTypeHandle t17;
            [ReadOnly] public DynamicComponentTypeHandle t18;
            [ReadOnly] public DynamicComponentTypeHandle t19;
            [ReadOnly] public DynamicComponentTypeHandle t20;
            [ReadOnly] public DynamicComponentTypeHandle t21;
            [ReadOnly] public DynamicComponentTypeHandle t22;
            [ReadOnly] public DynamicComponentTypeHandle t23;
            [ReadOnly] public DynamicComponentTypeHandle t24;
            [ReadOnly] public DynamicComponentTypeHandle t25;
            [ReadOnly] public DynamicComponentTypeHandle t26;
            [ReadOnly] public DynamicComponentTypeHandle t27;
            [ReadOnly] public DynamicComponentTypeHandle t28;
            [ReadOnly] public DynamicComponentTypeHandle t29;
            [ReadOnly] public DynamicComponentTypeHandle t30;
            [ReadOnly] public DynamicComponentTypeHandle t31;
            [ReadOnly] public DynamicComponentTypeHandle t32;
            [ReadOnly] public DynamicComponentTypeHandle t33;
            [ReadOnly] public DynamicComponentTypeHandle t34;
            [ReadOnly] public DynamicComponentTypeHandle t35;
            [ReadOnly] public DynamicComponentTypeHandle t36;
            [ReadOnly] public DynamicComponentTypeHandle t37;
            [ReadOnly] public DynamicComponentTypeHandle t38;
            [ReadOnly] public DynamicComponentTypeHandle t39;
            [ReadOnly] public DynamicComponentTypeHandle t40;
            [ReadOnly] public DynamicComponentTypeHandle t41;
            [ReadOnly] public DynamicComponentTypeHandle t42;
            [ReadOnly] public DynamicComponentTypeHandle t43;
            [ReadOnly] public DynamicComponentTypeHandle t44;
            [ReadOnly] public DynamicComponentTypeHandle t45;
            [ReadOnly] public DynamicComponentTypeHandle t46;
            [ReadOnly] public DynamicComponentTypeHandle t47;
            [ReadOnly] public DynamicComponentTypeHandle t48;
            [ReadOnly] public DynamicComponentTypeHandle t49;
            [ReadOnly] public DynamicComponentTypeHandle t50;
            [ReadOnly] public DynamicComponentTypeHandle t51;
            [ReadOnly] public DynamicComponentTypeHandle t52;
            [ReadOnly] public DynamicComponentTypeHandle t53;
            [ReadOnly] public DynamicComponentTypeHandle t54;
            [ReadOnly] public DynamicComponentTypeHandle t55;
            [ReadOnly] public DynamicComponentTypeHandle t56;
            [ReadOnly] public DynamicComponentTypeHandle t57;
            [ReadOnly] public DynamicComponentTypeHandle t58;
            [ReadOnly] public DynamicComponentTypeHandle t59;
            [ReadOnly] public DynamicComponentTypeHandle t60;
            [ReadOnly] public DynamicComponentTypeHandle t61;
            [ReadOnly] public DynamicComponentTypeHandle t62;
            [ReadOnly] public DynamicComponentTypeHandle t63;
            [ReadOnly] public DynamicComponentTypeHandle t64;
            [ReadOnly] public DynamicComponentTypeHandle t65;
            [ReadOnly] public DynamicComponentTypeHandle t66;
            [ReadOnly] public DynamicComponentTypeHandle t67;
            [ReadOnly] public DynamicComponentTypeHandle t68;
            [ReadOnly] public DynamicComponentTypeHandle t69;
            [ReadOnly] public DynamicComponentTypeHandle t70;
            [ReadOnly] public DynamicComponentTypeHandle t71;
            [ReadOnly] public DynamicComponentTypeHandle t72;
            [ReadOnly] public DynamicComponentTypeHandle t73;
            [ReadOnly] public DynamicComponentTypeHandle t74;
            [ReadOnly] public DynamicComponentTypeHandle t75;
            [ReadOnly] public DynamicComponentTypeHandle t76;
            [ReadOnly] public DynamicComponentTypeHandle t77;
            [ReadOnly] public DynamicComponentTypeHandle t78;
            [ReadOnly] public DynamicComponentTypeHandle t79;
            [ReadOnly] public DynamicComponentTypeHandle t80;
            [ReadOnly] public DynamicComponentTypeHandle t81;
            [ReadOnly] public DynamicComponentTypeHandle t82;
            [ReadOnly] public DynamicComponentTypeHandle t83;
            [ReadOnly] public DynamicComponentTypeHandle t84;
            [ReadOnly] public DynamicComponentTypeHandle t85;
            [ReadOnly] public DynamicComponentTypeHandle t86;
            [ReadOnly] public DynamicComponentTypeHandle t87;
            [ReadOnly] public DynamicComponentTypeHandle t88;
            [ReadOnly] public DynamicComponentTypeHandle t89;
            [ReadOnly] public DynamicComponentTypeHandle t90;
            [ReadOnly] public DynamicComponentTypeHandle t91;
            [ReadOnly] public DynamicComponentTypeHandle t92;
            [ReadOnly] public DynamicComponentTypeHandle t93;
            [ReadOnly] public DynamicComponentTypeHandle t94;
            [ReadOnly] public DynamicComponentTypeHandle t95;
            [ReadOnly] public DynamicComponentTypeHandle t96;
            [ReadOnly] public DynamicComponentTypeHandle t97;
            [ReadOnly] public DynamicComponentTypeHandle t98;
            [ReadOnly] public DynamicComponentTypeHandle t99;
            [ReadOnly] public DynamicComponentTypeHandle t100;
            [ReadOnly] public DynamicComponentTypeHandle t101;
            [ReadOnly] public DynamicComponentTypeHandle t102;
            [ReadOnly] public DynamicComponentTypeHandle t103;
            [ReadOnly] public DynamicComponentTypeHandle t104;
            [ReadOnly] public DynamicComponentTypeHandle t105;
            [ReadOnly] public DynamicComponentTypeHandle t106;
            [ReadOnly] public DynamicComponentTypeHandle t107;
            [ReadOnly] public DynamicComponentTypeHandle t108;
            [ReadOnly] public DynamicComponentTypeHandle t109;
            [ReadOnly] public DynamicComponentTypeHandle t110;
            [ReadOnly] public DynamicComponentTypeHandle t111;
            [ReadOnly] public DynamicComponentTypeHandle t112;
            [ReadOnly] public DynamicComponentTypeHandle t113;
            [ReadOnly] public DynamicComponentTypeHandle t114;
            [ReadOnly] public DynamicComponentTypeHandle t115;
            [ReadOnly] public DynamicComponentTypeHandle t116;
            [ReadOnly] public DynamicComponentTypeHandle t117;
            [ReadOnly] public DynamicComponentTypeHandle t118;
            [ReadOnly] public DynamicComponentTypeHandle t119;
            [ReadOnly] public DynamicComponentTypeHandle t120;
            [ReadOnly] public DynamicComponentTypeHandle t121;
            [ReadOnly] public DynamicComponentTypeHandle t122;
            [ReadOnly] public DynamicComponentTypeHandle t123;
            [ReadOnly] public DynamicComponentTypeHandle t124;
            [ReadOnly] public DynamicComponentTypeHandle t125;
            [ReadOnly] public DynamicComponentTypeHandle t126;
            [ReadOnly] public DynamicComponentTypeHandle t127;

            // Need to accept &t0 as input, because 'fixed' must be in the callsite.
            public unsafe DynamicComponentTypeHandle Type(DynamicComponentTypeHandle* fixedT0,
                int typeIndex)
            {
                return fixedT0[TypeIndexToArrayIndex[GetArrayIndex(typeIndex)]];
            }

            public JobHandle Dispose(JobHandle disposeDeps)
            {
                if (TypeIndexToArrayIndex.IsCreated)
                    return TypeIndexToArrayIndex.Dispose(disposeDeps);
                else
                    return new JobHandle();
            }
        }

        public unsafe BurstCompatibleTypeArray ToBurstCompatible(Allocator allocator)
        {
            BurstCompatibleTypeArray typeArray = default;

            Assert.IsTrue(UsedTypeCount > 0, "No types have been registered");
            Assert.IsTrue(UsedTypeCount <= BurstCompatibleTypeArray.kMaxTypes, "Maximum supported amount of types exceeded");

            typeArray.TypeIndexToArrayIndex = new NativeArray<int>(
                MaxIndex + 1,
                allocator,
                NativeArrayOptions.UninitializedMemory);
            ref var toArrayIndex = ref typeArray.TypeIndexToArrayIndex;

            // Use an index guaranteed to cause a crash on invalid indices
            uint GuaranteedCrashOffset = 0x80000000;
            for (int i = 0; i < toArrayIndex.Length; ++i)
                toArrayIndex[i] = (int)GuaranteedCrashOffset;

            var typeIndices = UsedTypes.GetValueArray(Allocator.Temp);
            int numTypes = math.min(typeIndices.Length, BurstCompatibleTypeArray.kMaxTypes);
            var fixedT0 = &typeArray.t0;

            for (int i = 0; i < numTypes; ++i)
            {
                int typeIndex = typeIndices[i];
                fixedT0[i] = Type(typeIndex);
                toArrayIndex[GetArrayIndex(typeIndex)] = i;
            }

            // TODO: Is there a way to avoid this?
            // We need valid type objects in each field.
            {
                var someType = Type(typeIndices[0]);
                for (int i = numTypes; i < BurstCompatibleTypeArray.kMaxTypes; ++i)
                    fixedT0[i] = someType;
            }

            typeIndices.Dispose();

            return typeArray;
        }
    }
}
