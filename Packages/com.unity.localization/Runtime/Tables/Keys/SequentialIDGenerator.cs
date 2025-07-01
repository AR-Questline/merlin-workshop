namespace UnityEngine.Localization.Tables
{
    /// <summary>
    /// Simple Key generator that increments the next key by 1 each time.
    /// </summary>
    public class SequentialIDGenerator : IKeyGenerator
    {
        [SerializeField]
        long m_NextAvailableId = 1;

        /// <summary>
        /// The next id value that will be provided by <see cref="GetNextKey"/>
        /// </summary>
        public long NextAvailableId => m_NextAvailableId;

        /// <summary>
        /// Create a new instance that starts at 1.
        /// </summary>
        public SequentialIDGenerator()
        {
        }

        /// <summary>
        /// Creates a new instance starting from <paramref name="startingId"/>.
        /// </summary>
        /// <param name="startingId"></param>
        public SequentialIDGenerator(long startingId)
        {
            m_NextAvailableId = startingId;
        }

        /// <summary>
        /// Returns <see cref="NextAvailableId"/> and increments it by 1.
        /// </summary>
        /// <returns></returns>
        public long GetNextKey() => m_NextAvailableId++;
    }
}
