using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FSMModule.Graph.Editor
{
    [CustomEditor(typeof(FsmBlackboardTransitionBehaviour))]
    public sealed class FsmBlackboardTransitionBehaviourEditor : UnityEditor.Editor
    {
        private const string ConditionModePropertyName = "conditionMode";
        private const string ConditionsPropertyName = "conditions";
        private const string ParameterKeyPropertyName = "parameterKey";
        private const string ParameterTypePropertyName = "parameterType";
        private const string ComparisonOperatorPropertyName = "comparisonOperator";
        private const string BoolValuePropertyName = "boolValue";
        private const string IntValuePropertyName = "intValue";
        private const string FloatValuePropertyName = "floatValue";

        private SerializedProperty _conditionModeProperty;
        private SerializedProperty _conditionsProperty;
        private ReorderableList _conditionsList;

        private void OnEnable()
        {
            _conditionModeProperty = serializedObject.FindProperty(ConditionModePropertyName);
            _conditionsProperty = serializedObject.FindProperty(ConditionsPropertyName);

            _conditionsList = new ReorderableList(serializedObject, _conditionsProperty, true, true, true, true)
            {
                drawHeaderCallback = DrawConditionsHeader,
                drawElementCallback = DrawConditionElement,
                elementHeight = EditorGUIUtility.singleLineHeight + 6f,
                onAddCallback = AddCondition,
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_conditionModeProperty, new GUIContent("Mode"));

            var graph = GetOwningGraph();
            if (graph == null)
            {
                EditorGUILayout.HelpBox("Transition editor expects to be used inside an FSM graph asset.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (graph.BlackboardParameters.Count == 0)
                EditorGUILayout.HelpBox("Add blackboard parameters to the graph before configuring transition conditions.", MessageType.Info);

            _conditionsList.DoLayoutList();

            if (_conditionsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Transitions without conditions will not fire. Add at least one condition.",
                    MessageType.None);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConditionsHeader(Rect rect) =>
            EditorGUI.LabelField(rect, "Conditions");

        private void DrawConditionElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var graph = GetOwningGraph();
            var conditionProperty = _conditionsProperty.GetArrayElementAtIndex(index);
            var parameterKeyProperty = conditionProperty.FindPropertyRelative(ParameterKeyPropertyName);
            var parameterTypeProperty = conditionProperty.FindPropertyRelative(ParameterTypePropertyName);
            var comparisonOperatorProperty = conditionProperty.FindPropertyRelative(ComparisonOperatorPropertyName);
            var boolValueProperty = conditionProperty.FindPropertyRelative(BoolValuePropertyName);
            var intValueProperty = conditionProperty.FindPropertyRelative(IntValuePropertyName);
            var floatValueProperty = conditionProperty.FindPropertyRelative(FloatValuePropertyName);

            var contentRect = new Rect(rect.x, rect.y + 2f, rect.width, EditorGUIUtility.singleLineHeight);
            var parameterRect = new Rect(contentRect.x, contentRect.y, contentRect.width * 0.46f, contentRect.height);
            var operationRect = new Rect(parameterRect.xMax + 4f, contentRect.y, contentRect.width * 0.26f, contentRect.height);
            var valueRect = new Rect(operationRect.xMax + 4f, contentRect.y, contentRect.xMax - operationRect.xMax - 4f, contentRect.height);

            SyncConditionType(graph, parameterKeyProperty, parameterTypeProperty);
            DrawParameterPopup(parameterRect, graph, parameterKeyProperty, parameterTypeProperty);

            var parameterType = (BlackboardParameterType)parameterTypeProperty.enumValueIndex;
            switch (parameterType)
            {
                case BlackboardParameterType.Bool:
                    DrawBoolValueField(operationRect, valueRect, boolValueProperty);
                    break;
                case BlackboardParameterType.Int:
                    EditorGUI.PropertyField(operationRect, comparisonOperatorProperty, GUIContent.none);
                    EditorGUI.PropertyField(valueRect, intValueProperty, GUIContent.none);
                    break;
                case BlackboardParameterType.Float:
                    EditorGUI.PropertyField(operationRect, comparisonOperatorProperty, GUIContent.none);
                    EditorGUI.PropertyField(valueRect, floatValueProperty, GUIContent.none);
                    break;
            }
        }

        private void AddCondition(ReorderableList list)
        {
            var graph = GetOwningGraph();
            var index = _conditionsProperty.arraySize;
            _conditionsProperty.InsertArrayElementAtIndex(index);

            var conditionProperty = _conditionsProperty.GetArrayElementAtIndex(index);
            var parameterKeyProperty = conditionProperty.FindPropertyRelative(ParameterKeyPropertyName);
            var parameterTypeProperty = conditionProperty.FindPropertyRelative(ParameterTypePropertyName);
            var comparisonOperatorProperty = conditionProperty.FindPropertyRelative(ComparisonOperatorPropertyName);
            var boolValueProperty = conditionProperty.FindPropertyRelative(BoolValuePropertyName);
            var intValueProperty = conditionProperty.FindPropertyRelative(IntValuePropertyName);
            var floatValueProperty = conditionProperty.FindPropertyRelative(FloatValuePropertyName);

            var defaultParameter = graph != null
                ? graph.BlackboardParameters.FirstOrDefault(parameter => parameter != null && !string.IsNullOrWhiteSpace(parameter.Key))
                : null;

            parameterKeyProperty.stringValue = defaultParameter?.Key ?? string.Empty;
            parameterTypeProperty.enumValueIndex = (int)(defaultParameter?.Type ?? BlackboardParameterType.Bool);
            comparisonOperatorProperty.enumValueIndex = (int)FsmNumericComparisonOperator.Greater;
            boolValueProperty.boolValue = true;
            intValueProperty.intValue = 0;
            floatValueProperty.floatValue = 0f;
        }

        private static void DrawBoolValueField(Rect operationRect, Rect valueRect, SerializedProperty boolValueProperty)
        {
            EditorGUI.LabelField(operationRect, "Is", EditorStyles.popup);
            var boolIndex = boolValueProperty.boolValue ? 0 : 1;
            var updatedIndex = EditorGUI.Popup(valueRect, boolIndex, new[] { "true", "false" });
            boolValueProperty.boolValue = updatedIndex == 0;
        }

        private static void DrawParameterPopup(
            Rect rect,
            FsmGraphAsset graph,
            SerializedProperty parameterKeyProperty,
            SerializedProperty parameterTypeProperty)
        {
            if (graph == null)
            {
                EditorGUI.PropertyField(rect, parameterKeyProperty, GUIContent.none);
                return;
            }

            var parameters = graph.BlackboardParameters
                .Where(parameter => parameter != null && !string.IsNullOrWhiteSpace(parameter.Key))
                .ToArray();

            if (parameters.Length == 0)
            {
                EditorGUI.LabelField(rect, "<No Parameters>", EditorStyles.popup);
                parameterKeyProperty.stringValue = string.Empty;
                return;
            }

            var optionNames = parameters
                .Select(parameter => $"{parameter.Key} ({parameter.Type})")
                .ToList();
            var optionValues = parameters.Select(parameter => parameter.Key).ToList();

            var selectedIndex = optionValues.IndexOf(parameterKeyProperty.stringValue);
            if (!string.IsNullOrWhiteSpace(parameterKeyProperty.stringValue) && selectedIndex < 0)
            {
                optionNames.Add($"Missing: {parameterKeyProperty.stringValue}");
                optionValues.Add(parameterKeyProperty.stringValue);
                selectedIndex = optionValues.Count - 1;
            }

            selectedIndex = Mathf.Max(0, selectedIndex);

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUI.Popup(rect, selectedIndex, optionNames.ToArray());
            if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < optionValues.Count)
            {
                parameterKeyProperty.stringValue = optionValues[selectedIndex];
                SyncConditionType(graph, parameterKeyProperty, parameterTypeProperty);
            }
        }

        private static void SyncConditionType(
            FsmGraphAsset graph,
            SerializedProperty parameterKeyProperty,
            SerializedProperty parameterTypeProperty)
        {
            if (graph == null || string.IsNullOrWhiteSpace(parameterKeyProperty.stringValue))
                return;

            var parameter = graph.BlackboardParameters.FirstOrDefault(candidate =>
                candidate != null &&
                candidate.Key == parameterKeyProperty.stringValue);

            if (parameter != null)
                parameterTypeProperty.enumValueIndex = (int)parameter.Type;
        }

        private FsmGraphAsset GetOwningGraph()
        {
            var assetPath = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            return AssetDatabase.LoadMainAssetAtPath(assetPath) as FsmGraphAsset;
        }
    }
}
