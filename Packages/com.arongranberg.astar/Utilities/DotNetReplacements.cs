namespace Pathfinding.Util {
	/// <summary>
	/// Simple implementation of a GUID.
	/// Version: Since 3.6.4 this struct works properly on platforms with different endianness such as Wii U.
	/// </summary>
	public struct Guid {
		const string hex = "0123456789ABCDEF";

		public static readonly Guid zero = new Guid(new byte[16]);
		public static readonly string zeroString = new Guid(new byte[16]).ToString();

		readonly ulong _a, _b;

		public Guid (byte[] bytes) : this()
        {
        }

        public Guid (string str) : this()
        {
        }

        public static Guid Parse(string input)
        {
            return default;
        }

        /// <summary>Swaps between little and big endian</summary>
        static ulong SwapEndianness(ulong value)
        {
            return default;
        }

        private static System.Random random = new System.Random();

		public static Guid NewGuid () {
            return default;
        }

        public static bool operator ==(Guid lhs, Guid rhs)
        {
            return default;
        }

        public static bool operator !=(Guid lhs, Guid rhs)
        {
            return default;
        }

        public override bool Equals(System.Object _rhs)
        {
            return default;
        }

        public override int GetHashCode()
        {
            return default;
        }

        private static System.Text.StringBuilder text;

		public override string ToString () {
            return default;
        }
    }
}
