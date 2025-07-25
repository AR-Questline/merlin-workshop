using System;
using UnityEngine;

namespace Unity.VisualScripting
{
    public abstract class LudiqScriptableObject : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField, DoNotSerialize] // Serialize with Unity, but not with FullSerializer.
        protected SerializationData _data;

        internal event Action OnDestroyActions;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Ignore the FullSerializer callback, but still catch the Unity callback
            if (Serialization.isCustomSerializing)
            {
                return;
            }

            SerializeImpl();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Ignore the FullSerializer callback, but still catch the Unity callback
            if (Serialization.isCustomSerializing)
            {
                return;
            }

            DeserializeImpl();
        }

        /// <summary> Awaken Hack for lazy SkillGraphs </summary>
        public void Deserialize(string json, UnityEngine.Object[] objects) 
        {
            _data = new SerializationData(json, objects);
            DeserializeImpl();
        }

        /// <summary> Awaken Hack for lazy SkillGraphs </summary>
        public void Serialize(out string json, out UnityEngine.Object[] objects) 
        {
            SerializeImpl();
            json = _data.json;
            objects = _data.objectReferences;
        }

        void DeserializeImpl() 
        {
            Serialization.isUnitySerializing = true;

            try
            {
                object @this = this;
                OnBeforeDeserialize();
                _data.DeserializeInto(ref @this, true);
                OnAfterDeserialize();

                _data.Clear();

                UnityThread.EditorAsync(OnPostDeserializeInEditor);
            }
            catch (Exception ex)
            {
                // Don't abort the whole deserialization thread because this one object failed
                Debug.LogError($"Failed to deserialize scriptable object.\n{ex}", this);
            }

            Serialization.isUnitySerializing = false;
        }

        void SerializeImpl() 
        {
            Serialization.isUnitySerializing = true;

            try
            {
                OnBeforeSerialize();
                _data = this.Serialize(true);
                OnAfterSerialize();
            }
            catch (Exception ex)
            {
                // Don't abort the whole serialization thread because this one object failed
                Debug.LogError($"Failed to serialize scriptable object.\n{ex}", this);
            }

            Serialization.isUnitySerializing = false;
        }

        protected virtual void OnBeforeSerialize() { }

        protected virtual void OnAfterSerialize() { }

        protected virtual void OnBeforeDeserialize() { }

        protected virtual void OnAfterDeserialize() { }

        protected virtual void OnPostDeserializeInEditor() { }

        private void OnDestroy()
        {
            OnDestroyActions?.Invoke();
        }

        protected virtual void ShowData()
        {
            var data = this.Serialize(true);
            data.ShowString(ToString());

            data.Clear();
        }

        public override string ToString()
        {
            return this.ToSafeString();
        }
    }
}
