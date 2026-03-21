using System;
using UnityEngine;

namespace FSMModule.Graph
{
    [Serializable]
    public sealed class FsmBindingDefinition
    {
        [SerializeField] private string key = "Binding";
        [SerializeField] private string description;

        public string Key
        {
            get => key;
            set => key = value;
        }

        public string Description
        {
            get => description;
            set => description = value;
        }
    }
}
