using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FSMModule.Graph.Editor
{
    [CustomPropertyDrawer(typeof(FsmBlackboardKeyAttribute))]
    public sealed class FsmBlackboardKeyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String ||
                !TryGetOwningGraph(property, out var graph))
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var blackboardKeyAttribute = (FsmBlackboardKeyAttribute)attribute;
            var optionLabels = new List<string> { "<None>" };
            var optionValues = new List<string> { string.Empty };

            for (var i = 0; i < graph.BlackboardParameters.Count; i++)
            {
                var parameter = graph.BlackboardParameters[i];
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.Key))
                    continue;

                if (blackboardKeyAttribute.HasTypeFilter && parameter.Type != blackboardKeyAttribute.RequiredType)
                    continue;

                optionLabels.Add($"{parameter.Key} ({parameter.Type})");
                optionValues.Add(parameter.Key);
            }

            var currentValue = property.stringValue;
            var selectedIndex = Mathf.Max(0, optionValues.IndexOf(currentValue));

            if (!string.IsNullOrWhiteSpace(currentValue) && !optionValues.Contains(currentValue))
            {
                optionLabels.Add($"Missing: {currentValue}");
                optionValues.Add(currentValue);
                selectedIndex = optionValues.Count - 1;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, optionLabels.ToArray());

            if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < optionValues.Count)
                property.stringValue = optionValues[selectedIndex];

            EditorGUI.EndProperty();
        }

        private static bool TryGetOwningGraph(SerializedProperty property, out FsmGraphAsset graph)
        {
            graph = null;

            var targetObject = property.serializedObject.targetObject;
            if (targetObject == null)
                return false;

            if (targetObject is Component component)
            {
                var runner = component.GetComponent<FsmGraphRunner>();
                if (runner != null && runner.Graph != null)
                {
                    graph = runner.Graph;
                    return true;
                }

                var runnerProperty = property.serializedObject.FindProperty("runner");
                if (runnerProperty?.objectReferenceValue is FsmGraphRunner referencedRunner && referencedRunner.Graph != null)
                {
                    graph = referencedRunner.Graph;
                    return true;
                }
            }

            var assetPath = AssetDatabase.GetAssetPath(targetObject);
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            graph = AssetDatabase.LoadMainAssetAtPath(assetPath) as FsmGraphAsset;
            return graph != null;
        }
    }
}
