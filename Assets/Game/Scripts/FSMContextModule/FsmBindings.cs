using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FSMModule
{
    public sealed class FsmBindings
    {
        private readonly Dictionary<string, Object> _objects = new();

        public void Set<T>(string key, T value) where T : Object => SetObject(key, value);

        public void SetObject(string key, Object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Binding key cannot be null or whitespace.", nameof(key));

            _objects[key] = value;
        }

        public bool Has(string key) => !string.IsNullOrWhiteSpace(key) && _objects.ContainsKey(key);

        public bool Delete(string key) => !string.IsNullOrWhiteSpace(key) && _objects.Remove(key);

        public Object GetObject(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Binding key cannot be null or whitespace.", nameof(key));

            if (_objects.TryGetValue(key, out var value))
                return value;

            throw new KeyNotFoundException($"Binding '{key}' was not found.");
        }

        public bool TryGetObject(string key, out Object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = null;
                return false;
            }

            return _objects.TryGetValue(key, out value);
        }

        public T Get<T>(string key) where T : Object
        {
            if (TryGet(key, out T value))
                return value;

            var storedType = TryGetObject(key, out var rawValue) && rawValue != null ? rawValue.GetType().Name : "missing";
            throw new InvalidOperationException($"Binding '{key}' could not be resolved as {typeof(T).Name}. Stored value: {storedType}.");
        }

        public bool TryGet<T>(string key, out T value) where T : Object
        {
            value = null;

            if (!TryGetObject(key, out var rawValue) || rawValue == null)
                return false;

            value = Resolve<T>(rawValue);
            return value != null;
        }

        public void Clear() => _objects.Clear();

        private static T Resolve<T>(Object rawValue) where T : Object
        {
            if (rawValue is T typedValue)
                return typedValue;

            return rawValue switch
            {
                GameObject gameObject => ResolveFromGameObject<T>(gameObject),
                Component component => ResolveFromComponent<T>(component),
                _ => null,
            };
        }

        private static T ResolveFromGameObject<T>(GameObject gameObject) where T : Object
        {
            var targetType = typeof(T);

            if (targetType == typeof(GameObject))
                return gameObject as T;

            if (targetType == typeof(Transform))
                return gameObject.transform as T;

            return typeof(Component).IsAssignableFrom(targetType)
                ? gameObject.GetComponent(targetType) as T
                : null;
        }

        private static T ResolveFromComponent<T>(Component component) where T : Object
        {
            var targetType = typeof(T);

            if (targetType == typeof(GameObject))
                return component.gameObject as T;

            if (targetType == typeof(Transform))
                return component.transform as T;

            return typeof(Component).IsAssignableFrom(targetType)
                ? component.GetComponent(targetType) as T
                : null;
        }
    }
}
