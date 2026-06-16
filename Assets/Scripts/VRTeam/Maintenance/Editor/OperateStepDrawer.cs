using UnityEditor;
using UnityEngine;

namespace VRHelmet.VRTeam.Maintenance.Editor
{
    [CustomPropertyDrawer(typeof(OperateStep))]
    public class OperateStepDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + VerticalSpacing;
            SerializedProperty childProperty = property.Copy();
            SerializedProperty endProperty = childProperty.GetEndProperty();
            bool enterChildren = true;

            while (childProperty.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(childProperty, endProperty))
            {
                height += EditorGUI.GetPropertyHeight(childProperty, true) + VerticalSpacing;
                enterChildren = false;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            float y = foldoutRect.yMax + VerticalSpacing;

            SerializedProperty childProperty = property.Copy();
            SerializedProperty endProperty = childProperty.GetEndProperty();
            bool enterChildren = true;

            while (childProperty.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(childProperty, endProperty))
            {
                float childHeight = EditorGUI.GetPropertyHeight(childProperty, true);
                Rect childRect = new Rect(position.x, y, position.width, childHeight);
                EditorGUI.PropertyField(childRect, childProperty, true);

                y += childHeight + VerticalSpacing;
                enterChildren = false;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}
