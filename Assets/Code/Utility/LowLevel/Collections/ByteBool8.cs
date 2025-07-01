using System;
using Awaken.Utility.Debugging;

namespace Awaken.Utility.LowLevel.Collections {
    public struct ByteBool8
    {
        public byte value;

        public bool this[int index]
        {
            get
            {
                Asserts.IndexInRange(index, 8);

                return (value & (1 << index)) != 0;
            }
            set
            {
                Asserts.IndexInRange(index, 8);

                if (value)
                {
                    this.value |= (byte)(1 << index);
                }
                else
                {
                    this.value &= (byte)~(1 << index);
                }
            }
        }

        public bool c0
        {
            get => (value & 1) != 0;
            set => this.value = (byte)(this.value & ~1 | (value ? 1 : 0));
        }

        public bool c1
        {
            get => (value & (1 << 1)) != 0;
            set => this.value = (byte)(this.value & ~(1 << 1) | (value ? (1 << 1) : 0));
        }

        public bool c2
        {
            get => (value & (1 << 2)) != 0;
            set => this.value = (byte)(this.value & ~(1 << 2) | (value ? (1 << 2) : 0));
        }

        public bool c3
        {
            get => (value & (1 << 3)) != 0;
            set => this.value = (byte)(this.value & ~(1 << 3) | (value ? (1 << 3) : 0));
        }

        public bool c4
        {
            get => (value & (1 << 4)) != 0;
            set => this.value = (byte)(this.value & ~(1 << 4) | (value ? (1 << 4) : 0));
        }

        public bool c5
        {
            get => (value & (1 << 5)) != 0;
            set => this.value = (byte)(this.value & ~(1 << 5) | (value ? (1 << 5) : 0));
        }

        public bool c6
        {
            get => (value & (1 << 6)) != 0;
            set => this.value = (byte)(this.value & ~(1 << 6) | (value ? (1 << 6) : 0));
        }

        public bool c7
        {
            get => (value & (1 << 7)) != 0;
            set => this.value = (byte)(this.value & ~(1 << 7) | (value ? (1 << 7) : 0));
        }

        public ByteBool8 GetShiftedSubValue(int offset, int length)
        {
            Asserts.IsFalse((offset < 0 || length <= 0 || offset + length > 8), "The range specified is out of the byte boundaries.");
            
            // Create a mask with 'length' number of ones at the appropriate position
            byte mask = (byte)((1 << length) - 1);

            // Extract the bits, shift them down to the LSB, and apply the mask to clean any higher bits
            byte extracted = (byte)((value >> offset) & mask);

            return new ByteBool8 { value = extracted };
        }

        public static ByteBool8 Or(ByteBool8 a, ByteBool8 b)
        {
            return new ByteBool8 { value = (byte)(a.value | b.value) };
        }

        public override string ToString()
        {
            return System.Convert.ToString(value, 2).PadLeft(8, '0');
        }
    }
}