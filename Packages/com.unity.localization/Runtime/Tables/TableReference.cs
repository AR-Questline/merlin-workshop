using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using UnityEngine.Serialization;

namespace UnityEngine.Localization.Tables
{
    /// <summary>
    /// It is possible to reference a table via either the table collection name or the table collection name guid.
    /// The TableReference provides a flexible way to reference via either of these methods and also includes editor functionality.
    /// </summary>
    [Serializable]
    public struct TableReference : ISerializationCallbackReceiver, IEquatable<TableReference>
    {
        /// <summary>
        /// The type of reference.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// No table is referenced.
            /// </summary>
            Empty,

            /// <summary>
            /// A table is referenced by its table collection name guid.
            /// </summary>
            Guid,

            /// <summary>
            /// A table is referenced by its name.
            /// </summary>
            Name
        }

        // Cached values to reduce GC
        static readonly Dictionary<Guid, string> s_GuidToStringCache = new Dictionary<Guid, string>();
        static readonly Dictionary<string, Guid> s_StringToGuidCache = new Dictionary<string, Guid>();

        [SerializeField]
        [FormerlySerializedAs("m_TableName")]
        string m_TableCollectionName;

        bool m_Valid;

        const string k_GuidTag = "GUID:";

        /// <summary>
        /// The type of reference.
        /// </summary>
        public Type ReferenceType { get; private set; }

        /// <summary>
        /// The table collection name guid when <see cref="ReferenceType"/> is <see cref="Type.Guid"/>.
        /// </summary>
        public Guid TableCollectionNameGuid { get; private set; }

        /// <summary>
        /// The table collection name when <see cref="ReferenceType"/> is <see cref="Type.Name"/>.
        /// If the <see cref="ReferenceType"/> is not <see cref="Type.Name"/> then an attempt will be made to extract the Table Collection Name, for debugging purposes, through
        /// the AssetDatabase(in Editor) or by checking the <see cref="LocalizationSettings"/> to see if the <see cref="SharedTableData"/> has been loaded by the
        /// <see cref="LocalizedStringDatabase"/> or <see cref="LocalizedAssetDatabase"/>, if the name can not be resolved then null will be returned.
        /// </summary>
        public string TableCollectionName
        {
            get => ReferenceType == Type.Name ? m_TableCollectionName : SharedTableData?.TableCollectionName;
            private set => m_TableCollectionName = value;
        }

        internal SharedTableData SharedTableData
        {
            get
            {
                if (ReferenceType == Type.Empty || !LocalizationSettings.HasSettings)
                    return null;

                if (ReferenceType == Type.Guid)
                {
                    #if UNITY_EDITOR
                    // We can extract the name through the shared table data GUID
                    var guid = StringFromGuid(TableCollectionNameGuid);
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var sharedTableData = UnityEditor.AssetDatabase.LoadAssetAtPath<SharedTableData>(path);
                        return sharedTableData;
                    }
                    #endif

                    // We don't actually know what type of table we refer to so we will need to check both.
                    // We only check shared table data that is loaded, we don't want to trigger any new operations as
                    // the asset may not exist and we don't want to trigger errors when creating debug info.
                    if (LocalizationSettings.StringDatabase != null && LocalizationSettings.StringDatabase.SharedTableDataOperations.TryGetValue(TableCollectionNameGuid, out var async))
                        return async.Result;
                    if (LocalizationSettings.AssetDatabase != null && LocalizationSettings.AssetDatabase.SharedTableDataOperations.TryGetValue(TableCollectionNameGuid, out async))
                        return async.Result;
                }
                else if (ReferenceType == Type.Name)
                {
                    // We don't actually know what type of table we refer to so we will need to check both.
                    // We only check shared table data that is loaded, we don't want to trigger any new operations as
                    // the asset may not exist and we don't want to trigger errors when creating debug info.
                    foreach (var sharedTableOperation in LocalizationSettings.StringDatabase?.SharedTableDataOperations)
                    {
                        if (sharedTableOperation.Value.Result?.TableCollectionName == m_TableCollectionName)
                            return sharedTableOperation.Value.Result;
                    }
                    foreach (var sharedTableOperation in LocalizationSettings.AssetDatabase?.SharedTableDataOperations)
                    {
                        if (sharedTableOperation.Value.Result?.TableCollectionName == m_TableCollectionName)
                            return sharedTableOperation.Value.Result;
                    }
                }
                return null;
            }
        }

        #pragma warning disable CA2225 // CA2225: Operator overloads have named alternates

        /// <summary>
        /// Convert a table collection name into a <see cref="TableReference"/>.
        /// </summary>
        /// <param name="tableCollectionName">The name of the table.</param>
        /// <returns></returns>
        public static implicit operator TableReference(string tableCollectionName)
        {
            return new TableReference { TableCollectionName = tableCollectionName, ReferenceType = string.IsNullOrWhiteSpace(tableCollectionName) ? Type.Empty : Type.Name };
        }

        /// <summary>
        /// Convert a table collection name guid into a <see cref="TableReference"/>.
        /// </summary>
        /// <param name="tableCollectionNameGuid">The table collection name guid.</param>
        /// <returns></returns>
        public static implicit operator TableReference(Guid tableCollectionNameGuid)
        {
            return new TableReference { TableCollectionNameGuid = tableCollectionNameGuid, ReferenceType = tableCollectionNameGuid == Guid.Empty ? Type.Empty : Type.Guid };
        }

        /// <summary>
        /// Returns <see cref="TableCollectionName"/>.
        /// </summary>
        /// <param name="tableReference"></param>
        /// <returns></returns>
        public static implicit operator string(TableReference tableReference)
        {
            return tableReference.TableCollectionName;
        }

        /// <summary>
        /// Returns <see cref="TableCollectionNameGuid"/>.
        /// </summary>
        /// <param name="tableReference"></param>
        /// <returns></returns>
        public static implicit operator Guid(TableReference tableReference)
        {
            return tableReference.TableCollectionNameGuid;
        }

        #pragma warning restore CA2225

        internal void Validate()
        {
            if (m_Valid)
                return;

            switch (ReferenceType)
            {
                case Type.Empty:
                    throw new ArgumentException("Empty Table Reference. Must contain a Guid or Table Collection Name");

                case Type.Guid:
                    if (TableCollectionNameGuid == Guid.Empty)
                        throw new ArgumentException("Must use a valid Table Collection Name Guid, can not be Empty.");
                    break;
                case Type.Name:
                    if (string.IsNullOrWhiteSpace(TableCollectionName))
                        throw new ArgumentException($"Table Collection Name can not be null or empty.");
                    break;
            }
            m_Valid = true;
        }

        internal string GetSerializedString()
        {
            switch (ReferenceType)
            {
                case Type.Guid:
                    return $"{k_GuidTag}{StringFromGuid(TableCollectionNameGuid)}";
                case Type.Name:
                    return TableCollectionName;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Returns a string representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (ReferenceType == Type.Guid)
                return $"{nameof(TableReference)}({TableCollectionNameGuid} - {TableCollectionName})";
            if (ReferenceType == Type.Name)
                return $"{nameof(TableReference)}({TableCollectionName})";
            return $"{nameof(TableReference)}(Empty)";
        }

        /// <summary>
        /// Compare the TableReference to another TableReference.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            return obj is TableReference ter && Equals(ter);
        }

        /// <summary>
        /// Returns the hash code of <see cref="TableCollectionNameGuid"/> or <see cref="TableCollectionName"/>.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (ReferenceType == Type.Guid)
                return TableCollectionNameGuid.GetHashCode();
            if (ReferenceType == Type.Name)
                return TableCollectionName.GetHashCode();
            return base.GetHashCode();
        }

        /// <summary>
        /// Compare 2 TableReferences.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(TableReference other)
        {
            if (ReferenceType != other.ReferenceType)
                return false;

            if (ReferenceType == Type.Guid)
            {
                return TableCollectionNameGuid == other.TableCollectionNameGuid;
            }
            if (ReferenceType == Type.Name)
            {
                return TableCollectionName == other.TableCollectionName;
            }
            return true;
        }

        /// <summary>
        /// Parse a string that contains uses the <see cref="k_GuidTag"/> tag.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Guid GuidFromString(string value)
        {
            if (s_StringToGuidCache.TryGetValue(value, out var result))
                return result;

            if (Guid.TryParse(value.Substring(k_GuidTag.Length, value.Length - k_GuidTag.Length), out var guid))
            {
                s_StringToGuidCache[value] = guid;
                return guid;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Returns a string version of the GUID which works with Addressables, it uses the "N" format(32 digits).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static string StringFromGuid(Guid value)
        {
            if (s_GuidToStringCache.TryGetValue(value, out var guid))
                return guid.ToString();

            var stringValue = value.ToString("N");
            s_GuidToStringCache[value] = stringValue;
            return stringValue;
        }

        /// <summary>
        /// Converts a string into a a <see cref="TableReference"/>.
        /// </summary>
        /// <param name="value">The string to convert. The string can either be a table collection name or a GUID identified by prepending the <see cref="k_GuidTag"/> tag.</param>
        /// <returns></returns>
        internal static TableReference TableReferenceFromString(string value)
        {
            if (IsGuid(value))
                return GuidFromString(value);
            return value;
        }

        /// <summary>
        /// Is the string identified as a <see cref="Guid"/> string.
        /// Strings that start with <see cref="k_GuidTag"/> are considered a Guid.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static bool IsGuid(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            return value.StartsWith(k_GuidTag, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Converts the reference into a serializable string.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_TableCollectionName = GetSerializedString();
        }

        /// <summary>
        /// Converts the serializable string into the correct reference type.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_TableCollectionName))
            {
                ReferenceType = Type.Empty;
            }
            else if (IsGuid(m_TableCollectionName))
            {
                TableCollectionNameGuid = GuidFromString(m_TableCollectionName);
                ReferenceType = Type.Guid;
            }
            else
            {
                ReferenceType = Type.Name;
            }
        }
    }

    /// <summary>
    /// Allows for referencing a table entry via key or key id.
    /// </summary>
    [Serializable]
    public struct TableEntryReference : ISerializationCallbackReceiver, IEquatable<TableEntryReference>
    {
        /// <summary>
        /// The type of reference.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// No table entry is referenced.
            /// </summary>
            Empty,

            /// <summary>
            /// The key name is referenced.
            /// </summary>
            Name,

            /// <summary>
            /// The Key Id is referenced
            /// </summary>
            Id
        }

        [SerializeField]
        long m_KeyId;

        [SerializeField]
        string m_Key;

        bool m_Valid;

        /// <summary>
        /// The type of reference.
        /// </summary>
        public Type ReferenceType { get; private set; }

        /// <summary>
        /// The Key Id when <see cref="ReferenceType"/> is <see cref="Type.Id"/>.
        /// </summary>
        public long KeyId { get => m_KeyId; private set => m_KeyId = value; }

        /// <summary>
        /// The key name when <see cref="ReferenceType"/> is <see cref="Type.Name"/>.
        /// </summary>
        public string Key { get => m_Key; private set => m_Key = value; }

        #pragma warning disable CA2225 // CA2225: Operator overloads have named alternates

        /// <summary>
        /// Converts a string name into a reference.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static implicit operator TableEntryReference(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
                return new TableEntryReference() { Key = key, ReferenceType = Type.Name };
            return new TableEntryReference(); // Empty
        }

        /// <summary>
        /// Converts a key id into a reference.
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public static implicit operator TableEntryReference(long keyId)
        {
            if (keyId != SharedTableData.EmptyId)
                return new TableEntryReference() { KeyId = keyId, ReferenceType = Type.Id };
            return new TableEntryReference(); // Empty
        }

        /// <summary>
        /// Returns <see cref="Key"/>.
        /// </summary>
        /// <param name="tableEntryReference"></param>
        /// <returns></returns>
        public static implicit operator string(TableEntryReference tableEntryReference)
        {
            return tableEntryReference.Key;
        }

        /// <summary>
        /// Returns <see cref="KeyId"/>
        /// </summary>
        /// <param name="tableEntryReference"></param>
        /// <returns></returns>
        public static implicit operator long(TableEntryReference tableEntryReference)
        {
            return tableEntryReference.KeyId;
        }

        #pragma warning restore CA2225

        internal void Validate()
        {
            if (m_Valid)
                return;

            switch (ReferenceType)
            {
                case Type.Empty:
                    throw new ArgumentException("Empty Table Entry Reference. Must contain a Name or Key Id");

                case Type.Name:
                    if (string.IsNullOrWhiteSpace(Key))
                        throw new ArgumentException("Must use a valid Key, can not be null or Empty.");
                    break;
                case Type.Id:
                    if (KeyId == SharedTableData.EmptyId)
                        throw new ArgumentException("Key Id can not be empty.");
                    break;
            }
            m_Valid = true;
        }

        /// <summary>
        /// Returns the key name.
        /// If <see cref="ReferenceType"/> is <see cref="Type.Name"/> then <see cref="Key"/> will be returned.
        /// If <see cref="ReferenceType"/> is <see cref="Type.Id"/> then <paramref name="sharedData"/> will be used to extract the name.
        /// </summary>
        /// <param name="sharedData">The <see cref="SharedTableData"/> to use if the key name is not stored in the reference or null if it could not br resolbved.</param>
        /// <returns></returns>
        public string ResolveKeyName(SharedTableData sharedData)
        {
            if (ReferenceType == Type.Name)
                return Key;
            if (ReferenceType == Type.Id)
                return sharedData != null ? sharedData.GetKey(KeyId) : $"Key Id {KeyId}";
            return null;
        }

        /// <summary>
        /// Returns a string representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            switch (ReferenceType)
            {
                case Type.Name:
                    return $"{nameof(TableEntryReference)}({Key})";
                case Type.Id:
                    return $"{nameof(TableEntryReference)}({KeyId})";
            }
            return $"{nameof(TableEntryReference)}(Empty)";
        }

        /// <summary>
        /// Returns a string representation.
        /// </summary>
        /// <param name="tableReference">The <see cref="TableReference"/> that this entry is part of. This is used to extract the <see cref="Key"/> or <see cref="KeyId"/></param>
        /// <returns></returns>
        public string ToString(TableReference tableReference)
        {
            var sharedTableData = tableReference.SharedTableData;
            if (sharedTableData != null)
            {
                long id;
                string key;

                if (ReferenceType == Type.Name)
                {
                    key = Key;
                    id = sharedTableData.GetId(key);
                }
                else if (ReferenceType == Type.Id)
                {
                    id = KeyId;
                    key = sharedTableData.GetKey(id);
                }
                else
                {
                    return ToString();
                }

                return $"{nameof(TableEntryReference)}({id} - {key})";
            }

            return ToString();
        }

        /// <summary>
        /// Compare the TableEntryReference to another TableEntryReference.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            return obj is TableEntryReference ter && Equals(ter);
        }

        /// <summary>
        /// Compare the TableEntryReference to another TableEntryReference.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(TableEntryReference other)
        {
            if (ReferenceType != other.ReferenceType)
                return false;

            if (ReferenceType == Type.Name)
            {
                return Key == other.Key;
            }
            if (ReferenceType == Type.Id)
            {
                return KeyId == other.KeyId;
            }
            return true;
        }

        /// <summary>
        /// Returns the hash code of <see cref="Key"/> or <see cref="KeyId"/>.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (ReferenceType == Type.Name)
            {
                return Key.GetHashCode();
            }
            if (ReferenceType == Type.Id)
            {
                return KeyId.GetHashCode();
            }
            return base.GetHashCode();
        }

        /// <summary>
        /// Does nothing but is required for <see cref="OnAfterDeserialize"/>.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// Determines the <see cref="ReferenceType"/>.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (KeyId != SharedTableData.EmptyId)
                ReferenceType = Type.Id;
            else if (string.IsNullOrEmpty(m_Key))
                ReferenceType = Type.Empty;
            else
                ReferenceType = Type.Name;
        }
    }
}
