using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FSMModule
{
    [Serializable]
    public sealed class ComponentBinding
    {
        [SerializeField] private string key;
        [SerializeField] private Object target;

        public string Key => key;
        public Object Target => target;

        public bool IsValid => !string.IsNullOrWhiteSpace(key) && target != null;
    }
}
