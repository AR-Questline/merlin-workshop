// #define DEBUG_ASSERTS

using UnityEngine.Assertions;
using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Rendering
{
    /// <summary>
    /// Represents a block of memory that you can use in a HeapAllocator to manage memory.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("({begin}, {end}), Length = {Length}")]
    public struct HeapBlock : IComparable<HeapBlock>, IEquatable<HeapBlock>
    {
        /// <summary>
        /// The beginning of the allocated heap block.
        /// </summary>
        public ulong begin { get { return m_Begin; } }

        /// <summary>
        /// The end of the allocated heap block.
        /// </summary>
        public ulong end { get { return m_End; } }

        private ulong m_Begin;
        private ulong m_End;

        internal HeapBlock(ulong begin, ulong end)
        {
            m_Begin = begin;
            m_End = end;
        }

        /// <summary>
        /// Creates new HeapBlock that starts at the given index and is of given size.
        /// </summary>
        /// <param name="begin">The start index for the block.</param>
        /// <param name="size">The size of the block.</param>
        /// <returns>Returns a new instance of HeapBlock.</returns>
        internal static HeapBlock OfSize(ulong begin, ulong size)
        {
            return new HeapBlock(begin, begin + size);
        }

        /// <summary>
        /// The length of the HeapBlock.
        /// </summary>
        public ulong Length { get { return m_End - m_Begin; } }

        /// <summary>
        /// Indicates whether the HeapBlock is empty. This is true if the HeapBlock is empty and false otherwise.
        /// </summary>
        public bool Empty { get { return Length == 0; } }

        /// <inheritdoc/>
        public int CompareTo(HeapBlock other) { return m_Begin.CompareTo(other.m_Begin); }

        /// <inheritdoc/>
        public bool Equals(HeapBlock other) { return CompareTo(other) == 0; }
    }

    /// <summary>
    /// Represents a generic best-fit heap allocation algorithm that operates on abstract integer indices.
    /// </summary>
    /// <remarks>
    /// You can use this to suballocate memory, GPU buffer contents, and DX12 descriptors.
    /// This supports alignments, resizing, and coalescing of freed blocks.
    /// </remarks>
    public struct HeapAllocator : IDisposable
    {
        /// <summary>
        /// Creates a new HeapAllocator with the given initial size and alignment.
        /// </summary>
        /// <param name="size">The initial size of the allocator.</param>
        /// <param name="minimumAlignment">The initial alignment of the allocator.</param>
        /// <remarks>
        /// You can resize the allocator later.
        /// </remarks>
        public HeapAllocator(ulong size = 0, uint minimumAlignment = 1)
        {
            m_SizeBins = new NativeList<SizeBin>(Allocator.Persistent);
            m_Blocks = new NativeList<BlocksOfSize>(Allocator.Persistent);
            m_BlocksFreelist = new NativeList<int>(Allocator.Persistent);
            m_FreeEndpoints = new NativeParallelHashMap<ulong, ulong>(0, Allocator.Persistent);
            m_Size = 0;
            m_Free = 0;
            m_MinimumAlignmentLog2 = math.tzcnt(minimumAlignment);
            m_IsCreated = true;

            Resize(size);
        }

        /// <summary>
        /// Minimal HeapBlock alignment of this allocator.
        /// </summary>
        internal uint MinimumAlignment { get { return 1u << m_MinimumAlignmentLog2; } }
        
        /// <summary>
        /// The amount of available free space in the allocator.
        /// </summary>
        public ulong FreeSpace { get { return m_Free; } }

        /// <summary>
        /// The amount of used space in the allocator.
        /// </summary>
        public ulong UsedSpace { get { return m_Size - m_Free; } }

        internal ulong OnePastHighestUsedAddress { get {
            return m_FreeEndpoints.TryGetValue(m_Size, out var tailBegin) ? tailBegin : m_Size;
        } }

        /// <summary>
        /// The size of the heap that the allocator manages.
        /// </summary>
        public ulong Size { get { return m_Size; } }

        /// <summary>
        /// Indicates whether the allocator is empty. This is true if the allocator is empty and false otherwise.
        /// </summary>
        public bool Empty { get { return m_Free == m_Size; } }

        /// <summary>
        /// Indicates whether the allocator is full. This is true if the allocator is full and false otherwise.
        /// </summary>
        public bool Full { get { return m_Free == 0; } }

        /// <summary>
        /// Indicates whether the allocator has been created and not yet allocated.
        /// </summary>
        public bool IsCreated { get { return m_IsCreated; } }

        /// <summary>
        /// Clears the allocator.
        /// </summary>
        public void Clear()
        {
            var size = m_Size;

            m_SizeBins.Clear();
            m_Blocks.Clear();
            m_BlocksFreelist.Clear();
            m_FreeEndpoints.Clear();
            m_Size = 0;
            m_Free = 0;

            Resize(size);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!IsCreated)
                return;

            for (int i = 0; i < m_Blocks.Length; ++i)
                m_Blocks[i].Dispose();

            m_FreeEndpoints.Dispose();
            m_Blocks.Dispose();
            m_BlocksFreelist.Dispose();
            m_SizeBins.Dispose();
            m_IsCreated = false;
        }

        /// <summary>
        /// Attempts to grow or shrink the allocator. Growing always succeeds,
        /// but shrinking might fail if the end of the heap is allocated.
        /// </summary>
        /// <param name="newSize">The new size of the allocator.</param>
        /// <returns>Returns true if the operation is a success. Returns false otherwise.</returns>
        public bool Resize(ulong newSize)
        {
            // Same size? No need to do anything.
            if (newSize == m_Size)
            {
                return true;
            }
            // Growing? Release a block past the end.
            else if (newSize > m_Size)
            {
                ulong increase = newSize - m_Size;
                HeapBlock newSpace = HeapBlock.OfSize(m_Size, increase);
                Release(newSpace);
                m_Size = newSize;
                return true;
            }
            // Shrinking? TODO
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Attempt to allocate a block from the heap with at least the given
        /// size and alignment.
        /// </summary>
        /// <param name="size">The size of the block to allocate.</param>
        /// <param name="alignment">Alignment of the allocated block.</param>
        /// <remarks>
        /// The allocated block might be bigger than the
        /// requested size, but will never be smaller.
        /// If the allocation fails, this method returns an empty block.
        /// </remarks>
        /// <returns>Returns a new allocated HeapBlock on success. Returns an empty block on failure.</returns>
        public HeapBlock Allocate(ulong size, uint alignment = 1)
        {
            // Always use at least the minimum alignment, and round all sizes
            // to multiples of the minimum alignment.
            size = NextAligned(size, m_MinimumAlignmentLog2);
            alignment = math.max(alignment, MinimumAlignment);

            SizeBin allocBin = new SizeBin(size, alignment);

            int index = FindSmallestSufficientBin(allocBin);
            while (index < m_SizeBins.Length)
            {
                SizeBin bin = m_SizeBins[index];
                if (CanFitAllocation(allocBin, bin))
                {
                    HeapBlock block = PopBlockFromBin(bin, index);
                    return CutAllocationFromBlock(allocBin, block);
                }
                else
                {
                    ++index;
                }
            }

            return new HeapBlock();
        }

        /// <summary>
        /// Releases a given block of memory and marks it as free.
        /// </summary>
        /// <param name="block">The HeapBlock to release to the allocator.</param>
        /// <remarks>
        /// You must have wholly allocated the given block before you pass it into this method.
        /// However, it's legal to release big allocations in smaller non-overlapping sub-blocks.
        /// </remarks>
        public void Release(HeapBlock block)
        {
            // Merge the newly released block with any free blocks on either
            // side of it. Remove those blocks from the list of free blocks,
            // as they no longer exist as separate blocks.
            block = Coalesce(block);

            SizeBin bin = new SizeBin(block);
            int index = FindSmallestSufficientBin(bin);

            // If the exact bin doesn't exist, add it.
            if (index >= m_SizeBins.Length || bin.CompareTo(m_SizeBins[index]) != 0)
            {
                index = AddNewBin(ref bin, index);
            }

            m_Blocks[m_SizeBins[index].blocksId].Push(block);
            m_Free += block.Length;

#if DEBUG_ASSERTS
            Assert.IsFalse(m_FreeEndpoints.ContainsKey(block.begin));
            Assert.IsFalse(m_FreeEndpoints.ContainsKey(block.end));
#endif

            // Store both endpoints of the free block to the hashmap for
            // easy coalescing.
            m_FreeEndpoints[block.begin] = block.end;
            m_FreeEndpoints[block.end]   = block.begin;
        }

        // Do a slow exhaustive test of the allocator's internal state to verify
        // that its invariants have not been broken. Should be only used for debugging.
        internal void DebugValidateInternalState()
        {
            // Amount of size bins should be the same as the amount of block lists
            // for those size bins.
            int numBins = m_SizeBins.Length;
            int numFreeBlockLists = m_BlocksFreelist.Length;
            int numEmptyBlocks = 0;
            int numNonEmptyBlocks = 0;

            for (int i = 0; i < m_Blocks.Length; ++i)
            {
                if (m_Blocks[i].Empty)
                    ++numEmptyBlocks;
                else
                    ++numNonEmptyBlocks;
            }

            Assert.AreEqual(numBins, numNonEmptyBlocks, "There should be exactly one non-empty block list per size bin");
            Assert.AreEqual(numEmptyBlocks, numFreeBlockLists, "All empty block lists should be in the free list");

            for (int i = 0; i < m_BlocksFreelist.Length; ++i)
            {
                int freeBlock = m_BlocksFreelist[i];
                Assert.IsTrue(m_Blocks[freeBlock].Empty, "There should be only empty block lists in the free list");
            }

            ulong totalFreeSize = 0;
            int totalFreeBlocks = 0;

            for (int i = 0; i < m_SizeBins.Length; ++i)
            {
                var sizeBin = m_SizeBins[i];
                var size = sizeBin.Size;
                var align = sizeBin.Alignment;
                var blocks = m_Blocks[sizeBin.blocksId];

                Assert.IsFalse(blocks.Empty, "All block lists should be non-empty, empty lists should be removed");

                int count = blocks.Length;

                for (int j = 0; j < count; ++j)
                {
                    var b = blocks.Block(j);
                    var bin = new SizeBin(b);
                    Assert.AreEqual(size, bin.Size, "Block size should match its bin");
                    Assert.AreEqual(align, bin.Alignment, "Block alignment should match its bin");
                    totalFreeSize += b.Length;

                    if (m_FreeEndpoints.TryGetValue(b.begin, out var foundEnd))
                        Assert.AreEqual(b.end, foundEnd, "Free block end does not match stored endpoint");
                    else
                        Assert.IsTrue(false, "No end endpoint found for free block");

                    if (m_FreeEndpoints.TryGetValue(b.end, out var foundBegin))
                        Assert.AreEqual(b.begin, foundBegin, "Free block begin does not match stored endpoint");
                    else
                        Assert.IsTrue(false, "No begin endpoint found for free block");

                    ++totalFreeBlocks;
                }
            }

            // Reported free space should be equal to the total size of the free blocks
            Assert.AreEqual(totalFreeSize, FreeSpace, "Free size reported incorrectly");
            Assert.IsTrue(totalFreeSize <= Size, "Amount of free size larger than maximum");
            Assert.AreEqual(2 * totalFreeBlocks, m_FreeEndpoints.Count(),
                "Each free block should have exactly 2 stored endpoints");
        }

        internal const int MaxAlignmentLog2 = 0x3f;
        internal const int AlignmentBits = 6;

        [System.Diagnostics.DebuggerDisplay("Size = {Size}, Alignment = {Alignment}")]
        private struct SizeBin : IComparable<SizeBin>, IEquatable<SizeBin>
        {
            public ulong sizeClass;
            public int   blocksId;

            public SizeBin(ulong size, uint alignment = 1)
            {
                int alignLog2 = math.tzcnt(alignment);
                alignLog2 = math.min(MaxAlignmentLog2, alignLog2);
                sizeClass = (size << AlignmentBits) | (uint)alignLog2;
                blocksId  = -1;

#if DEBUG_ASSERTS
                Assert.AreEqual(math.countbits(alignment), 1, "Only power-of-two alignments supported");
#endif
            }

            public SizeBin(HeapBlock block)
            {
                int alignLog2 = math.tzcnt(block.begin);
                alignLog2 = math.min(MaxAlignmentLog2, alignLog2);
                sizeClass = (block.Length << AlignmentBits) | (uint)alignLog2;
                blocksId  = -1;
            }

            public int CompareTo(SizeBin other) { return sizeClass.CompareTo(other.sizeClass); }
            public bool Equals(SizeBin other) { return CompareTo(other) == 0; }

            public bool HasCompatibleAlignment(SizeBin requiredAlignment)
            {
                int myAlign = AlignmentLog2;
                int required = requiredAlignment.AlignmentLog2;
                return myAlign >= required;
            }

            public ulong Size { get { return sizeClass >> AlignmentBits; } }
            public int AlignmentLog2 { get { return (int)sizeClass & MaxAlignmentLog2; } }
            public uint Alignment { get { return 1u << AlignmentLog2; } }
        }

        private unsafe struct BlocksOfSize : IDisposable
        {
            private UnsafeList<HeapBlock>* m_Blocks;

            public BlocksOfSize(int dummy)
            {
                m_Blocks = (UnsafeList<HeapBlock>*)Memory.Unmanaged.Allocate(
                    UnsafeUtility.SizeOf<UnsafeList<HeapBlock>>(),
                    UnsafeUtility.AlignOf<UnsafeList<HeapBlock>>(),
                    Allocator.Persistent);
                UnsafeUtility.MemClear(m_Blocks, UnsafeUtility.SizeOf<UnsafeList<HeapBlock>>());
                m_Blocks->Allocator = Allocator.Persistent;
            }

            public bool Empty { get { return m_Blocks->Length == 0; } }

            // TODO: Priority queue semantics for address-ordered allocation

            public void Push(HeapBlock block)
            {
                m_Blocks->Add(block);
            }

            public HeapBlock Pop()
            {
                int len = m_Blocks->Length;

                if (len == 0)
                    return new HeapBlock();

                HeapBlock block = Block(len - 1);
                m_Blocks->Resize(len - 1);
                return block;
            }

            public bool Remove(HeapBlock block)
            {
                for (int i = 0; i < m_Blocks->Length; ++i)
                {
                    if (block.CompareTo(Block(i)) == 0)
                    {
                        m_Blocks->RemoveAtSwapBack(i);
                        return true;
                    }
                }

                return false;
            }

            public void Dispose()
            {
                m_Blocks->Dispose();
                Memory.Unmanaged.Free(m_Blocks, Allocator.Persistent);
            }

            public unsafe HeapBlock Block(int i) { return UnsafeUtility.ReadArrayElement<HeapBlock>(m_Blocks->Ptr, i); }
            public unsafe int Length => m_Blocks->Length;
        }

        private NativeList<SizeBin> m_SizeBins;
        private NativeList<BlocksOfSize> m_Blocks;
        private NativeList<int> m_BlocksFreelist;
        private NativeParallelHashMap<ulong, ulong> m_FreeEndpoints;
        private ulong m_Size;
        private ulong m_Free;
        private readonly int m_MinimumAlignmentLog2;
        private bool m_IsCreated;

        private int FindSmallestSufficientBin(SizeBin needle)
        {
            if (m_SizeBins.Length == 0)
                return 0;

            int lo = 0;                 // Low endpoint of search, inclusive
            int hi = m_SizeBins.Length; // High endpoint of search, exclusive

            for (;;)
            {
                int d2 = (hi - lo) / 2;

                // Search has terminated. If lo is large enough, return it.
                if (d2 == 0)
                {
                    if (needle.CompareTo(m_SizeBins[lo]) <= 0)
                        return lo;
                    else
                        return lo + 1;
                }

                int probe = lo + d2;
                int cmp = needle.CompareTo(m_SizeBins[probe]);

                // Needle is smaller than probe?
                if (cmp < 0)
                {
                    hi = probe;
                }
                // Needle is greater than probe?
                else if (cmp > 0)
                {
                    lo = probe;
                }
                // Found needle exactly.
                else
                {
                    return probe;
                }
            }
        }

        private unsafe int AddNewBin(ref SizeBin bin, int index)
        {
            // If there are no free block lists, make a new one
            if (m_BlocksFreelist.IsEmpty)
            {
                bin.blocksId = m_Blocks.Length;
                m_Blocks.Add(new BlocksOfSize(0));
            }
            else
            {
                int last = m_BlocksFreelist.Length - 1;
                bin.blocksId = m_BlocksFreelist[last];
                m_BlocksFreelist.ResizeUninitialized(last);
            }

#if DEBUG_ASSERTS
            Assert.IsTrue(m_Blocks[bin.blocksId].Empty);
#endif

            int tail = m_SizeBins.Length - index;
            m_SizeBins.ResizeUninitialized(m_SizeBins.Length + 1);
            SizeBin *p = (SizeBin *)m_SizeBins.GetUnsafePtr();
            UnsafeUtility.MemMove(
                p + (index + 1),
                p + index,
                tail * UnsafeUtility.SizeOf<SizeBin>());
            p[index] = bin;

            return index;
        }

        private unsafe void RemoveBinIfEmpty(SizeBin bin, int index)
        {
            if (!m_Blocks[bin.blocksId].Empty)
                return;

            int tail = m_SizeBins.Length - (index + 1);
            SizeBin* p = (SizeBin*)m_SizeBins.GetUnsafePtr();
            UnsafeUtility.MemMove(
                p + index,
                p + (index + 1),
                tail * UnsafeUtility.SizeOf<SizeBin>());
            m_SizeBins.ResizeUninitialized(m_SizeBins.Length - 1);

            m_BlocksFreelist.Add(bin.blocksId);
        }

        private unsafe HeapBlock PopBlockFromBin(SizeBin bin, int index)
        {
            HeapBlock block = m_Blocks[bin.blocksId].Pop();
            RemoveEndpoints(block);
            m_Free -= block.Length;

            RemoveBinIfEmpty(bin, index);

            return block;
        }

        private void RemoveEndpoints(HeapBlock block)
        {
            m_FreeEndpoints.Remove(block.begin);
            m_FreeEndpoints.Remove(block.end);
        }

        private void RemoveFreeBlock(HeapBlock block)
        {
            RemoveEndpoints(block);

            SizeBin bin = new SizeBin(block);
            int index = FindSmallestSufficientBin(bin);

#if DEBUG_ASSERTS
            Assert.IsTrue(index >= 0 && m_SizeBins[index].sizeClass == bin.sizeClass,
                "Expected to find exact match for size bin since block was supposed to exist");
#endif

            bool removed = m_Blocks[m_SizeBins[index].blocksId].Remove(block);
            RemoveBinIfEmpty(m_SizeBins[index], index);

#if DEBUG_ASSERTS
            Assert.IsTrue(removed, "Block was supposed to exist");
#endif

            m_Free -= block.Length;
        }

        private HeapBlock Coalesce(HeapBlock block, ulong endpoint)
        {
            if (m_FreeEndpoints.TryGetValue(endpoint, out ulong otherEnd))
            {
#if DEBUG_ASSERTS
                if (math.min(endpoint, otherEnd) == block.begin &&
                    math.max(endpoint, otherEnd) == block.end)
                {
                    UnityEngine.Debug.Log("Inconsistent free block endpoint data");
                }
                Assert.IsFalse(
                    math.min(endpoint, otherEnd) == block.begin &&
                    math.max(endpoint, otherEnd) == block.end,
                    "Block was already freed.");
#endif

                if (endpoint == block.begin)
                {
#if DEBUG_ASSERTS
                    Assert.IsTrue(otherEnd < endpoint, "Unexpected endpoints");
#endif
                    var coalesced = new HeapBlock(otherEnd, block.begin);
                    RemoveFreeBlock(coalesced);
                    return new HeapBlock(coalesced.begin, block.end);
                }
                else
                {
#if DEBUG_ASSERTS
                    Assert.IsTrue(otherEnd > endpoint, "Unexpected endpoints");
#endif
                    var coalesced = new HeapBlock(block.end, otherEnd);
                    RemoveFreeBlock(coalesced);
                    return new HeapBlock(block.begin, coalesced.end);
                }
            }
            else
            {
                return block;
            }
        }

        private HeapBlock Coalesce(HeapBlock block)
        {
            block = Coalesce(block, block.begin); // Left
            block = Coalesce(block, block.end);   // Right
            return block;
        }

        private bool CanFitAllocation(SizeBin allocation, SizeBin bin)
        {
#if DEBUG_ASSERTS
            Assert.IsTrue(bin.sizeClass >= allocation.sizeClass, "Should have compatible size classes to begin with");
#endif

            // Check that the bin is not empty.
            if (m_Blocks[bin.blocksId].Empty)
                return false;

            // If the bin meets alignment restrictions, it is usable.
            if (bin.HasCompatibleAlignment(allocation))
            {
                return true;
            }
            // Else, require one alignment worth of extra space so we can guarantee space.
            else
            {
                return bin.Size >= (allocation.Size + allocation.Alignment);
            }
        }

        private static ulong NextAligned(ulong offset, int alignmentLog2)
        {
            int toNext = (1 << alignmentLog2) - 1;
            ulong aligned = ((offset + (ulong)toNext) >> alignmentLog2) << alignmentLog2;
            return aligned;
        }

        private HeapBlock CutAllocationFromBlock(SizeBin allocation, HeapBlock block)
        {
#if DEBUG_ASSERTS
            Assert.IsTrue(block.Length >= allocation.Size, "Block is not large enough.");
#endif

            // If the match is exact, no need to cut.
            if (allocation.Size == block.Length)
                return block;

            // Otherwise, round the begin to next multiple of alignment, and then cut away the required size,
            // potentially leaving empty space on both ends.
            ulong alignedBegin = NextAligned(block.begin, allocation.AlignmentLog2);
            ulong alignedEnd = alignedBegin + allocation.Size;

            if (alignedBegin > block.begin)
                Release(new HeapBlock(block.begin, alignedBegin));

            if (alignedEnd < block.end)
                Release(new HeapBlock(alignedEnd, block.end));

            return new HeapBlock(alignedBegin, alignedEnd);
        }
    }
}
