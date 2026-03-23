using System;
using System.Collections.Generic;
using UnityEngine;

namespace FSMModule
{
    public sealed class FsmParameters
    {
        private readonly Dictionary<string, bool> _boolValues = new();
        private readonly Dictionary<string, int> _intValues = new();
        private readonly Dictionary<string, float> _floatValues = new();
        private readonly Dictionary<string, Type> _declaredTypes = new();
        private readonly HashSet<string> _reportedIssues = new();

        public void RegisterBool(string key) => Register(key, typeof(bool));

        public void RegisterInt(string key) => Register(key, typeof(int));

        public void RegisterFloat(string key) => Register(key, typeof(float));

        public bool HasKey(string key) =>
            !string.IsNullOrWhiteSpace(key) && _declaredTypes.ContainsKey(key);

        public void SetBool(string key, bool value)
        {
            if (!CanWrite(key, typeof(bool), nameof(SetBool)))
                return;

            _boolValues[key] = value;
        }

        public void SetInt(string key, int value)
        {
            if (!CanWrite(key, typeof(int), nameof(SetInt)))
                return;

            _intValues[key] = value;
        }

        public void SetFloat(string key, float value)
        {
            if (!CanWrite(key, typeof(float), nameof(SetFloat)))
                return;

            _floatValues[key] = value;
        }

        public bool GetBool(string key) =>
            TryGetBool(key, out var value) ? value : default;

        public int GetInt(string key) =>
            TryGetInt(key, out var value) ? value : default;

        public float GetFloat(string key) =>
            TryGetFloat(key, out var value) ? value : default;

        public bool TryGetBool(string key, out bool value) =>
            _boolValues.TryGetValue(key, out value);

        public bool TryGetInt(string key, out int value) =>
            _intValues.TryGetValue(key, out value);

        public bool TryGetFloat(string key, out float value) =>
            _floatValues.TryGetValue(key, out value);

        private void Register(string key, Type valueType)
        {
            if (string.IsNullOrWhiteSpace(key) || valueType == null)
                return;

            if (_declaredTypes.TryGetValue(key, out var existingType))
            {
                if (existingType != valueType)
                {
                    Debug.LogWarning(
                        $"FSM parameter '{key}' was declared multiple times with different types: '{existingType.Name}' and '{valueType.Name}'.");
                }

                _declaredTypes[key] = valueType;
                return;
            }

            _declaredTypes.Add(key, valueType);
        }

        private bool CanWrite(string key, Type valueType, string methodName)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (!_declaredTypes.TryGetValue(key, out var declaredType))
            {
                WarnOnce(
                    $"{methodName}:missing:{key}",
                    $"FSM parameter '{key}' is not declared in the graph. Add it in the graph window before calling '{methodName}'.");
                return false;
            }

            if (declaredType == valueType)
                return true;

            WarnOnce(
                $"{methodName}:type:{key}:{declaredType.FullName}:{valueType.FullName}",
                $"FSM parameter '{key}' is declared as '{declaredType.Name}', but '{methodName}' tried to write '{valueType.Name}'.");
            return false;
        }

        private void WarnOnce(string issueId, string message)
        {
            if (!_reportedIssues.Add(issueId))
                return;

            Debug.LogWarning(message);
        }
    }
}
