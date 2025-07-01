using Awaken.Utility.Debugging;
using System;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Random = UnityEngine.Random;

namespace Awaken.TG.Graphics
{
    public class SpawnRandomProp : MonoBehaviour
    {
        public GameObject[] prefabsArr = Array.Empty<GameObject>();
        private GameObject _prop;
        private Vector3 _position;
        private int _randomIndex;

        private void Start() {
            SpawnRandom();
        }
        
        private void OnEnable() {
            SpawnRandom();
        }
        
        private void OnDisable() {
            Destroy(_prop);
        }
        private void SpawnRandom() {
            if (_prop == null) {
                _randomIndex = Random.Range(0, prefabsArr.Length-1);
                _position = transform.position;
                _prop = Instantiate(prefabsArr[_randomIndex],_position,Quaternion.identity);
                _prop.transform.SetParent(gameObject.transform);  
                Log.Important?.Info(_randomIndex.ToString());
            }
        }
    }
}
