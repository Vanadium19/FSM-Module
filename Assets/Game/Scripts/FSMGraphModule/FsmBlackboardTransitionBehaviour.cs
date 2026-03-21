using System;
using System.Collections.Generic;
using UnityEngine;

namespace FSMModule.Graph
{
    public enum FsmTransitionConditionMode
    {
        All = 0,
        Any = 1,
    }

    public enum FsmNumericComparisonOperator
    {
        Greater = 0,
        GreaterOrEqual = 1,
        Less = 2,
        LessOrEqual = 3,
        Equal = 4,
        NotEqual = 5,
    }

    [Serializable]
    public sealed class FsmBlackboardTransitionCondition
    {
        [SerializeField] private string parameterId;
        [SerializeField] private string parameterKey;
        [SerializeField] private BlackboardParameterType parameterType = BlackboardParameterType.Bool;
        [SerializeField] private FsmNumericComparisonOperator comparisonOperator = FsmNumericComparisonOperator.Greater;
        [SerializeField] private bool boolValue = true;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;

        public string ParameterId
        {
            get => parameterId;
            set => parameterId = value;
        }

        public string ParameterKey
        {
            get => parameterKey;
            set => parameterKey = value;
        }

        public BlackboardParameterType ParameterType
        {
            get => parameterType;
            set => parameterType = value;
        }

        public FsmNumericComparisonOperator ComparisonOperator
        {
            get => comparisonOperator;
            set => comparisonOperator = value;
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

        public bool Evaluate(Blackboard blackboard, FsmGraphAsset graph)
        {
            if (blackboard == null || !TryResolveParameter(graph, out var parameter))
                return false;

            var resolvedKey = parameter.Key;

            return parameterType switch
            {
                BlackboardParameterType.Bool => blackboard.TryGetValue(resolvedKey, out bool boolResult) && boolResult == boolValue,
                BlackboardParameterType.Int => blackboard.TryGetValue(resolvedKey, out int intResult) && CompareNumeric(intResult, intValue),
                BlackboardParameterType.Float => blackboard.TryGetValue(resolvedKey, out float floatResult) && CompareNumeric(floatResult, floatValue),
                _ => false,
            };
        }

        public bool TryResolveParameter(FsmGraphAsset graph, out FsmBlackboardParameterDefinition parameter)
        {
            parameter = null;

            if (graph == null)
                return false;

            var hasStableId = !string.IsNullOrWhiteSpace(parameterId);
            if (hasStableId)
                parameter = graph.FindBlackboardParameterById(parameterId);

            if (!hasStableId && parameter == null && !string.IsNullOrWhiteSpace(parameterKey))
                parameter = graph.FindBlackboardParameterByKey(parameterKey);

            if (parameter == null)
                return false;

            parameterId = parameter.Id;
            parameterKey = parameter.Key;
            parameterType = parameter.Type;
            return true;
        }

        private bool CompareNumeric(int currentValue, int expectedValue) =>
            comparisonOperator switch
            {
                FsmNumericComparisonOperator.Greater => currentValue > expectedValue,
                FsmNumericComparisonOperator.GreaterOrEqual => currentValue >= expectedValue,
                FsmNumericComparisonOperator.Less => currentValue < expectedValue,
                FsmNumericComparisonOperator.LessOrEqual => currentValue <= expectedValue,
                FsmNumericComparisonOperator.Equal => currentValue == expectedValue,
                FsmNumericComparisonOperator.NotEqual => currentValue != expectedValue,
                _ => false,
            };

        private bool CompareNumeric(float currentValue, float expectedValue) =>
            comparisonOperator switch
            {
                FsmNumericComparisonOperator.Greater => currentValue > expectedValue,
                FsmNumericComparisonOperator.GreaterOrEqual => currentValue >= expectedValue,
                FsmNumericComparisonOperator.Less => currentValue < expectedValue,
                FsmNumericComparisonOperator.LessOrEqual => currentValue <= expectedValue,
                FsmNumericComparisonOperator.Equal => Mathf.Approximately(currentValue, expectedValue),
                FsmNumericComparisonOperator.NotEqual => !Mathf.Approximately(currentValue, expectedValue),
                _ => false,
            };
    }

    public sealed class FsmBlackboardTransitionBehaviour : FsmTransitionBehaviour
    {
        [SerializeField] private FsmTransitionConditionMode conditionMode = FsmTransitionConditionMode.All;
        [SerializeField] private List<FsmBlackboardTransitionCondition> conditions = new();

        public FsmTransitionConditionMode ConditionMode => conditionMode;
        public IReadOnlyList<FsmBlackboardTransitionCondition> Conditions => conditions;
        public string DisplayName => conditions.Count == 0 ? "Transition" : $"{conditionMode} ({conditions.Count})";

        public override bool CanTransition()
        {
            if (conditions.Count == 0)
                return false;

            if (conditionMode == FsmTransitionConditionMode.All)
            {
                for (var i = 0; i < conditions.Count; i++)
                {
                    var condition = conditions[i];
                    if (condition == null || !condition.Evaluate(Blackboard, Graph))
                        return false;
                }

                return true;
            }

            for (var i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                if (condition != null && condition.Evaluate(Blackboard, Graph))
                    return true;
            }

            return false;
        }
    }
}
