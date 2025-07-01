using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Awaken.Utility.Animations {

    public struct LerpyFloat {

        // ========================= Fields

        float _currentValue;
        float _theta;

        // ========================= Constructors

        public LerpyFloat(float theta, float currentValue = default(float)) {
            _currentValue = currentValue;
            _theta = theta;
        }

        // ========================= Operation

        [UnityEngine.Scripting.Preserve] 
        public float UpdateAndGet(float targetValue) {
            _currentValue += (targetValue - _currentValue) * _theta;
            return _currentValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public float UpdateAndGet(float targetValue, float theta) {
            _currentValue += (targetValue - _currentValue) * theta;
            return _currentValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public void Set(float newValue) {
            _currentValue = newValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public float Get() {
            return _currentValue;
        }

        public static implicit operator float(LerpyFloat f) { return f._currentValue; }
    }

    public struct LerpyColor {

        // ========================= Fields

        Color _currentValue;
        float _theta;

        // ========================= Constructors

        public LerpyColor(float theta, Color currentValue = default(Color)) {
            _currentValue = currentValue;
            _theta = theta;
        }

        // ========================= Operation

        [UnityEngine.Scripting.Preserve] 
        public Color UpdateAndGet(Color targetValue) {
            _currentValue += (targetValue - _currentValue) * _theta;
            return _currentValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public Color UpdateAndGet(Color targetValue, float theta) {
            _currentValue += (targetValue - _currentValue) * theta;
            return _currentValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public void Set(Color newValue) {
            _currentValue = newValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public Color Get() {
            return _currentValue;
        }
    }

    public struct LerpyVector {

        // ========================= Fields

        Vector3 _currentValue;
        float _theta;

        // ========================= Constructors

        public LerpyVector(float theta, Vector3 currentValue = default(Vector3)) {
            _currentValue = currentValue;
            _theta = theta;
        }

        // ========================= Operation

        public Vector3 UpdateAndGet(Vector3 targetValue) {
            _currentValue += (targetValue - _currentValue) * _theta;
            return _currentValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public Vector3 UpdateAndGet(Vector3 targetValue, float theta) {
            _currentValue += (targetValue - _currentValue) * theta;
            return _currentValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public void Set(Vector3 newValue) {
            _currentValue = newValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public Vector3 Get() {
            return _currentValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public void Update(Vector3 targetValue) => UpdateAndGet(targetValue);

        public static implicit operator Vector3(LerpyVector f) { return f._currentValue; }
    }

    public struct LerpyQuaternion {

        // ========================= Fields

        Quaternion _currentValue;
        float _theta;

        // ========================= Constructors

        public LerpyQuaternion(float theta) : this(theta, Quaternion.identity) { }
        public LerpyQuaternion(float theta, Quaternion currentValue) {
            _currentValue = currentValue;
            _theta = theta;
        }

        // ========================= Operation

        [UnityEngine.Scripting.Preserve] 
        public Quaternion UpdateAndGet(Quaternion targetValue) {
            _currentValue = Quaternion.Slerp(_currentValue, targetValue, _theta);
            return _currentValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public Quaternion UpdateAndGet(Quaternion targetValue, float theta) {
            _currentValue = Quaternion.Slerp(_currentValue, targetValue, theta);
            return _currentValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public void Set(Quaternion newValue) {
            _currentValue = newValue;
        }

        [UnityEngine.Scripting.Preserve] 
        public Quaternion Get() {
            return _currentValue;
        }

        public static implicit operator Quaternion(LerpyQuaternion f) { return f._currentValue; }
    }
}
