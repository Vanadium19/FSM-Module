using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FSMModule.Graph.Editor
{
    [CustomEditor(typeof(FsmGraphRunner))]
    public sealed class FsmGraphRunnerEditor : UnityEditor.Editor
    {
        private const string GraphPropertyName = "graph";
        private const string InjectedBindingsPropertyName = "injectedBindings";
        private const string OwnerKindPropertyName = "ownerKind";
        private const string OwnerIdPropertyName = "ownerId";
        private const string FieldNamePropertyName = "fieldName";
        private const string TargetPropertyName = "target";

        private SerializedProperty _graphProperty;
        private SerializedProperty _injectedBindingsProperty;

        private void OnEnable()
        {
            _graphProperty = serializedObject.FindProperty(GraphPropertyName);
            _injectedBindingsProperty = serializedObject.FindProperty(InjectedBindingsPropertyName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_graphProperty);

            var graph = _graphProperty.objectReferenceValue as FsmGraphAsset;
            if (graph == null)
            {
                EditorGUILayout.HelpBox("Assign an FSM graph to configure injected scene references.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            SyncInjectedBindings(graph, _injectedBindingsProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Injected References", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Fields marked with [FsmInject] inside state and transition behaviours are assigned here per graph element.",
                MessageType.None);

            var hasInjectedFields = false;
            hasInjectedFields |= DrawStateBindings(graph, _injectedBindingsProperty);
            hasInjectedFields |= DrawTransitionBindings(graph, _injectedBindingsProperty);

            if (!hasInjectedFields)
            {
                EditorGUILayout.HelpBox(
                    "No [FsmInject] UnityEngine.Object fields were found in this graph.",
                    MessageType.Info);
            }

            if (Application.isPlaying)
            {
                var runner = (FsmGraphRunner)target;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Current State", string.IsNullOrWhiteSpace(runner.CurrentStateId) ? "<None>" : runner.CurrentStateId);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static bool DrawStateBindings(FsmGraphAsset graph, SerializedProperty injectedBindingsProperty)
        {
            var hasInjectedFields = false;

            for (var i = 0; i < graph.States.Count; i++)
            {
                var state = graph.States[i];
                if (state?.State == null)
                    continue;

                var injectableFields = FsmBehaviourInjectionUtility.GetInjectableFields(state.State.GetType());
                if (injectableFields.Count == 0)
                    continue;

                hasInjectedFields = true;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"State: {state.Name}", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField(state.State.GetType().Name, EditorStyles.centeredGreyMiniLabel);

                    for (var fieldIndex = 0; fieldIndex < injectableFields.Count; fieldIndex++)
                        DrawInjectedField(injectedBindingsProperty, FsmGraphBehaviourOwnerKind.State, state.Id, injectableFields[fieldIndex]);
                }
            }

            return hasInjectedFields;
        }

        private static bool DrawTransitionBindings(FsmGraphAsset graph, SerializedProperty injectedBindingsProperty)
        {
            var hasInjectedFields = false;

            for (var i = 0; i < graph.Transitions.Count; i++)
            {
                var transition = graph.Transitions[i];
                if (transition?.Transition == null)
                    continue;

                var injectableFields = FsmBehaviourInjectionUtility.GetInjectableFields(transition.Transition.GetType());
                if (injectableFields.Count == 0)
                    continue;

                hasInjectedFields = true;

                var fromState = graph.FindState(transition.FromStateId);
                var toState = graph.FindState(transition.ToStateId);
                var title = $"Transition: {fromState?.Name ?? transition.FromStateId} -> {toState?.Name ?? transition.ToStateId}";

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField(transition.Transition.GetType().Name, EditorStyles.centeredGreyMiniLabel);

                    for (var fieldIndex = 0; fieldIndex < injectableFields.Count; fieldIndex++)
                        DrawInjectedField(injectedBindingsProperty, FsmGraphBehaviourOwnerKind.Transition, transition.Id, injectableFields[fieldIndex]);
                }
            }

            return hasInjectedFields;
        }

        private static void DrawInjectedField(
            SerializedProperty injectedBindingsProperty,
            FsmGraphBehaviourOwnerKind ownerKind,
            string ownerId,
            FieldInfo field)
        {
            var bindingProperty = FindBindingProperty(injectedBindingsProperty, ownerKind, ownerId, field.Name);
            if (bindingProperty == null)
                return;

            var targetProperty = bindingProperty.FindPropertyRelative(TargetPropertyName);
            var currentValue = targetProperty.objectReferenceValue;
            var updatedValue = EditorGUILayout.ObjectField(
                ObjectNames.NicifyVariableName(field.Name),
                currentValue,
                field.FieldType,
                true);

            if (updatedValue != currentValue)
                targetProperty.objectReferenceValue = updatedValue;
        }

        private static void SyncInjectedBindings(FsmGraphAsset graph, SerializedProperty injectedBindingsProperty)
        {
            var desiredKeys = BuildDesiredBindingKeys(graph);
            var seenKeys = new HashSet<string>();

            for (var i = injectedBindingsProperty.arraySize - 1; i >= 0; i--)
            {
                var bindingProperty = injectedBindingsProperty.GetArrayElementAtIndex(i);
                var bindingKey = BuildBindingKey(
                    (FsmGraphBehaviourOwnerKind)bindingProperty.FindPropertyRelative(OwnerKindPropertyName).enumValueIndex,
                    bindingProperty.FindPropertyRelative(OwnerIdPropertyName).stringValue,
                    bindingProperty.FindPropertyRelative(FieldNamePropertyName).stringValue);

                if (!desiredKeys.Contains(bindingKey) || !seenKeys.Add(bindingKey))
                    injectedBindingsProperty.DeleteArrayElementAtIndex(i);
            }

            foreach (var bindingInfo in EnumerateInjectableFields(graph))
            {
                if (FindBindingProperty(injectedBindingsProperty, bindingInfo.OwnerKind, bindingInfo.OwnerId, bindingInfo.Field.Name) != null)
                    continue;

                var index = injectedBindingsProperty.arraySize;
                injectedBindingsProperty.InsertArrayElementAtIndex(index);

                var bindingProperty = injectedBindingsProperty.GetArrayElementAtIndex(index);
                bindingProperty.FindPropertyRelative(OwnerKindPropertyName).enumValueIndex = (int)bindingInfo.OwnerKind;
                bindingProperty.FindPropertyRelative(OwnerIdPropertyName).stringValue = bindingInfo.OwnerId;
                bindingProperty.FindPropertyRelative(FieldNamePropertyName).stringValue = bindingInfo.Field.Name;
                bindingProperty.FindPropertyRelative(TargetPropertyName).objectReferenceValue = null;
            }
        }

        private static HashSet<string> BuildDesiredBindingKeys(FsmGraphAsset graph)
        {
            var keys = new HashSet<string>();
            foreach (var bindingInfo in EnumerateInjectableFields(graph))
                keys.Add(BuildBindingKey(bindingInfo.OwnerKind, bindingInfo.OwnerId, bindingInfo.Field.Name));

            return keys;
        }

        private static IEnumerable<BindingInfo> EnumerateInjectableFields(FsmGraphAsset graph)
        {
            for (var i = 0; i < graph.States.Count; i++)
            {
                var state = graph.States[i];
                if (state?.State == null)
                    continue;

                var fields = FsmBehaviourInjectionUtility.GetInjectableFields(state.State.GetType());
                for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
                    yield return new BindingInfo(FsmGraphBehaviourOwnerKind.State, state.Id, fields[fieldIndex]);
            }

            for (var i = 0; i < graph.Transitions.Count; i++)
            {
                var transition = graph.Transitions[i];
                if (transition?.Transition == null)
                    continue;

                var fields = FsmBehaviourInjectionUtility.GetInjectableFields(transition.Transition.GetType());
                for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
                    yield return new BindingInfo(FsmGraphBehaviourOwnerKind.Transition, transition.Id, fields[fieldIndex]);
            }
        }

        private static SerializedProperty FindBindingProperty(
            SerializedProperty injectedBindingsProperty,
            FsmGraphBehaviourOwnerKind ownerKind,
            string ownerId,
            string fieldName)
        {
            for (var i = 0; i < injectedBindingsProperty.arraySize; i++)
            {
                var bindingProperty = injectedBindingsProperty.GetArrayElementAtIndex(i);
                if ((FsmGraphBehaviourOwnerKind)bindingProperty.FindPropertyRelative(OwnerKindPropertyName).enumValueIndex != ownerKind)
                    continue;

                if (bindingProperty.FindPropertyRelative(OwnerIdPropertyName).stringValue != ownerId)
                    continue;

                if (bindingProperty.FindPropertyRelative(FieldNamePropertyName).stringValue != fieldName)
                    continue;

                return bindingProperty;
            }

            return null;
        }

        private static string BuildBindingKey(FsmGraphBehaviourOwnerKind ownerKind, string ownerId, string fieldName) =>
            $"{(int)ownerKind}:{ownerId}:{fieldName}";

        private readonly struct BindingInfo
        {
            public BindingInfo(FsmGraphBehaviourOwnerKind ownerKind, string ownerId, FieldInfo field)
            {
                OwnerKind = ownerKind;
                OwnerId = ownerId;
                Field = field;
            }

            public FsmGraphBehaviourOwnerKind OwnerKind { get; }
            public string OwnerId { get; }
            public FieldInfo Field { get; }
        }
    }
}
