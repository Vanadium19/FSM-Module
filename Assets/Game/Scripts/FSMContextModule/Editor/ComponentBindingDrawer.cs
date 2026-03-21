using UnityEditor;
using UnityEngine;

namespace FSMModule.Editor
{
    [CustomPropertyDrawer(typeof(ComponentBinding))]
    public sealed class ComponentBindingDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var keyProperty = property.FindPropertyRelative("key");
            var targetProperty = property.FindPropertyRelative("target");

            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var fieldsRect = new Rect(
                position.x,
                headerRect.yMax + Spacing,
                position.width,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(headerRect, label);

            var contentRect = EditorGUI.IndentedRect(fieldsRect);
            var keyWidth = Mathf.Min(180f, contentRect.width * 0.4f);
            var keyRect = new Rect(contentRect.x, contentRect.y, keyWidth, contentRect.height);
            var targetRect = new Rect(
                keyRect.xMax + Spacing,
                contentRect.y,
                contentRect.width - keyWidth - Spacing,
                contentRect.height);

            EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);
            EditorGUI.PropertyField(targetRect, targetProperty, GUIContent.none);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight * 2f + Spacing;
    }
}
