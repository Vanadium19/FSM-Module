using System;
using UnityEngine;

namespace FSMModule.Graph
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FsmBlackboardKeyAttribute : PropertyAttribute
    {
        public FsmBlackboardKeyAttribute() { }

        public FsmBlackboardKeyAttribute(BlackboardParameterType requiredType)
        {
            HasTypeFilter = true;
            RequiredType = requiredType;
        }

        public bool HasTypeFilter { get; }
        public BlackboardParameterType RequiredType { get; }
    }
}
