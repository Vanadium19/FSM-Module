using System;
using System.Collections.Generic;

namespace FSMModule
{
    public class Blackboard
    {
        private readonly Dictionary<Type, object> _values = new();
        private readonly Dictionary<string, object> _objectValues = new();

        #region Objects

        public bool DeleteValue<T>(string key) where T : struct => GetBucket<T>().Remove(key);

        public void SetObject(string key, object value) => _objectValues[key] = value;

        public bool HasObject(string key) => _objectValues.ContainsKey(key);

        public T GetObject<T>(string key) => (T)_objectValues[key];

        public bool TryGetObject<T>(string key, out T value)
        {
            if (_objectValues.TryGetValue(key, out var result))
            {
                value = (T)result;
                return true;
            }

            value = default;
            return false;
        }

        public bool DeleteObject(string key) => _objectValues.Remove(key);

        #endregion

        #region Values

        public void SetValue<T>(string key, T value) where T : struct => GetBucket<T>()[key] = value;

        public bool HasValue<T>(string key) where T : struct => GetBucket<T>().ContainsKey(key);

        public T GetValue<T>(string key) where T : struct => GetBucket<T>()[key];

        public bool TryGetValue<T>(string key, out T value) where T : struct
        {
            var bucket = GetBucket<T>();

            if (bucket.TryGetValue(key, out var result))
            {
                value = result;
                return true;
            }

            value = default;
            return false;
        }

        private Dictionary<string, T> GetBucket<T>() where T : struct
        {
            var type = typeof(T);

            if (_values.TryGetValue(type, out var boxed))
                return (Dictionary<string, T>)boxed;

            var dictionary = new Dictionary<string, T>();
            _values[type] = dictionary;
            return dictionary;
        }

        #endregion
    }
}