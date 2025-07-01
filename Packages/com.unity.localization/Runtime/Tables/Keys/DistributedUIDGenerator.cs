using System;

namespace UnityEngine.Localization.Tables
{
    /// <summary>
    /// A Key generator that uses the current time in ms with the machine id to generate a
    /// unique value every time. This means it is safe for multiple users to add entries to the same table without
    /// suffering from conflicts due to the entries using the same key but having different values.
    ///
    /// The implementation is based on this article https://www.callicoder.com/distributed-unique-id-sequence-number-generator/
    ///
    /// The Key is made up of the following components:
    /// <list type="table">
    /// <item>
    /// <term>Sequence Number</term>
    /// <term>12 Bits(0 - 11)</term>
    /// <term>A local counter per machine that starts at 0 and is incremented by 1 for each new id request that is made during the same millisecond.
    /// The value is limited to 12 bytes so can contain 4095 items before the ids for this millisecond are exhausted and the id generator
    /// must wait until the next millisecond before it can continue.</term>
    /// </item>
    ///
    /// <item>
    /// <term>Machine Id</term>
    /// <term>10 Bits(12-21)</term>
    /// <term>The Id of the machine. By default, in the Editor, this value is generated
    /// from the machines network interface physical address however it can also be set to a user provided value. There is enough space for 1024 machines.</term>
    /// </item>
    ///
    /// <item>
    /// <term>Epoch Timestamp.</term>
    /// <term>41 Bits(22-63)</term>
    /// <term>A timestamp using a custom epoch which is the time the class was created.
    /// The maximum timestamp that can be represented is 69 years from the custom epoch, at this point the Key generator will have exhausted its possible unique Ids.</term>
    /// </item>
    /// </list>
    ///
    /// <item>
    /// <term>Signed Bit</term>
    /// <term>1 Bit(64)</term>
    /// <term>The signed bit is unused by the ID generator. If you wish to add custom Id values then using the signed bit and adding negative ids will avoid conflicts.</term>
    /// </item>
    /// </summary>
    [Serializable]
    public class DistributedUIDGenerator : IKeyGenerator
    {
        // Configured machine id - 10 bits (gives us up to 1024 machines)
        const int kMachineIdBits = 10;

        // Sequence number - 12 bits (A local counter per machine that rolls over every 4096)
        // The sequence number is used to generate multiple ids per millisecond. This means we can generate
        // 4095 ids per ms and must then wait until the next ms before we can continue generating ids.
        const int kSequenceBits = 12;

        static readonly int kMaxNodeId = (int)(Mathf.Pow(2, kMachineIdBits) - 1);
        static readonly int kMaxSequence = (int)(Mathf.Pow(2, kSequenceBits) - 1);

        /// <summary>
        /// The name of the EditorPrefs that is used to store the machine id.
        /// </summary>
        public const string MachineIdPrefKey = "KeyGenerator-MachineId";

        [SerializeField, HideInInspector]
        long m_CustomEpoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        long m_LastTimestamp = -1;
        long m_Sequence;
        int m_MachineId;

        /// <summary>
        /// The custom epoch used to generate the timestamp.
        /// </summary>
        public long CustomEpoch => m_CustomEpoch;

        /// <summary>
        /// The Id of the current machine. By default, in the Editor, this value is generated
        /// from the machines network interface physical address however it can also be set to a user provided value.
        /// There is enough space for 1024 unique machines.
        /// Set value will be clamped in the range 1-1023.
        /// The value is not serialized into the asset but stored into the EditorPrefs(Editor only).
        /// </summary>
        public int MachineId
        {
            get
            {
                if (m_MachineId == 0)
                {
                    m_MachineId = GetMachineId();
                }
                return m_MachineId;
            }

            set
            {
                m_MachineId = Mathf.Clamp(value, 1, kMaxNodeId);

                #if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetInt(MachineIdPrefKey, m_MachineId);
                #endif
            }
        }

        /// <summary>
        /// Create a default instance which uses the current time as the <see cref="CustomEpoch"/> the machines
        /// physical address as <see cref="MachineId"/>.
        /// </summary>
        public DistributedUIDGenerator()
        {
        }

        /// <summary>
        /// Creates an instance with a defined <see cref="CustomEpoch"/>.
        /// </summary>
        /// <param name="customEpoch">The custom epoch is used to calculate the timestamp by taking the
        /// current time and subtracting the <see cref="CustomEpoch"/>.
        /// The value is then stored in 41 bits giving it a maximum time of 69 years.</param>
        public DistributedUIDGenerator(long customEpoch)
        {
            m_CustomEpoch = customEpoch;
        }

        /// <summary>
        /// Returns the next Id using the current time, machine id and sequence number.
        /// </summary>
        /// <returns></returns>
        public long GetNextKey()
        {
            var currentTimestamp  = TimeStamp();

            Debug.Assert(currentTimestamp >= m_LastTimestamp, "Invalid system clock. Current time is less than previous time.");

            // If we are generating another id in the same millisecond then we need to increment the sequence
            // or wait till the next millisecond if we have exhausted our sequences.
            if (currentTimestamp == m_LastTimestamp)
            {
                m_Sequence = (m_Sequence + 1) & kMaxSequence;
                if (m_Sequence == 0)
                {
                    // Sequence Exhausted, wait till next millisecond.
                    currentTimestamp = WaitNextMillis(currentTimestamp);
                }
            }
            else
            {
                // reset sequence to start with zero for the next millisecond.
                m_Sequence = 0;
            }

            m_LastTimestamp = currentTimestamp;

            long id = currentTimestamp << (kMachineIdBits + kSequenceBits);
            id |= (uint)MachineId << kSequenceBits;
            id |= m_Sequence;
            return id;
        }

        long TimeStamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - m_CustomEpoch;

        static int GetMachineId()
        {
            #if UNITY_EDITOR
            var id = UnityEditor.EditorPrefs.GetInt(MachineIdPrefKey, 0);
            if (id != 0)
            {
                return id;
            }

            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                {
                    var address = nic.GetPhysicalAddress().ToString();
                    return address.GetHashCode() & kMaxNodeId;
                }
            }
            #endif
            return Random.Range(0, kMaxNodeId);
        }

        // Block and wait till next millisecond
        long WaitNextMillis(long currentTimestamp)
        {
            while (currentTimestamp == m_LastTimestamp)
            {
                System.Threading.Thread.Sleep(1);
                currentTimestamp = TimeStamp();
            }
            return currentTimestamp;
        }
    }
}
