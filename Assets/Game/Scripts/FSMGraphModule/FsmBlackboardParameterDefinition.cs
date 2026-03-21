using System;
using UnityEngine;

namespace FSMModule.Graph
{
    [Serializable]
    public sealed class FsmBlackboardParameterDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string key = "Parameter";
        [SerializeField] private BlackboardParameterType type = BlackboardParameterType.Bool;
        [SerializeField] private bool boolValue;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string Key
        {
            get => key;
            set => key = value;
        }

        public BlackboardParameterType Type
        {
            get => type;
            set => type = value;
        }

        public bool BoolValue
        {
            get => boolValue;
            set => boolValue = value;
        }

        public int IntValue
        {
            get => intValue;
            set => intValue = value;
        }

        public float FloatValue
        {
            get => floatValue;
            set => floatValue = value;
        }

        public bool EnsureMetadata()
        {
            if (!string.IsNullOrWhiteSpace(id))
                return false;

            id = Guid.NewGuid().ToString("N");
            return true;
        }

        public void ApplyTo(FsmParameters parameters)
        {
            if (parameters == null || string.IsNullOrWhiteSpace(key))
                return;

            switch (type)
            {
                case BlackboardParameterType.Bool:
                    parameters.RegisterBool(key);
                    parameters.SetBool(key, boolValue);
                    break;
                case BlackboardParameterType.Int:
                    parameters.RegisterInt(key);
                    parameters.SetInt(key, intValue);
                    break;
                case BlackboardParameterType.Float:
                    parameters.RegisterFloat(key);
                    parameters.SetFloat(key, floatValue);
                    break;
            }
        }
    }
}
