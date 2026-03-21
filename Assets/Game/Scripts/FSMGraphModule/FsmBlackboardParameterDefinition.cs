using System;
using UnityEngine;

namespace FSMModule.Graph
{
    [Serializable]
    public sealed class FsmBlackboardParameterDefinition
    {
        [SerializeField] private string key = "Parameter";
        [SerializeField] private BlackboardParameterType type = BlackboardParameterType.Bool;
        [SerializeField] private bool boolValue;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;

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

        public void ApplyTo(Blackboard blackboard)
        {
            if (blackboard == null || string.IsNullOrWhiteSpace(key))
                return;

            switch (type)
            {
                case BlackboardParameterType.Bool:
                    blackboard.SetValue(key, boolValue);
                    break;
                case BlackboardParameterType.Int:
                    blackboard.SetValue(key, intValue);
                    break;
                case BlackboardParameterType.Float:
                    blackboard.SetValue(key, floatValue);
                    break;
            }
        }
    }
}
