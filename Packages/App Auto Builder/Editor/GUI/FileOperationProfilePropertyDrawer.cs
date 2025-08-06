using UnityEngine;
using UnityEditor;

namespace zFramework.AppBuilder
{
    [CustomPropertyDrawer(typeof(FileOperationProfile))]
    public class FileOperationProfilePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var typeProperty = property.FindPropertyRelative("type");
            var sourcePathProperty = property.FindPropertyRelative("sourcePath");
            var destinationPathProperty = property.FindPropertyRelative("destinationPath");
            var newNameProperty = property.FindPropertyRelative("newName");

            var operationType = (FileOperationType)typeProperty.enumValueIndex;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float currentY = position.y;

            // 绘制操作类型
            var typeRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(typeRect, typeProperty, new GUIContent("操作类型"));
            currentY += lineHeight + spacing;

            // 绘制源路径 (总是显示)
            var sourceRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(sourceRect, sourcePathProperty, new GUIContent("源路径"));
            currentY += lineHeight + spacing;

            // 根据操作类型绘制相应的字段
            switch (operationType)
            {
                case FileOperationType.Copy:
                case FileOperationType.Move:
                    var destRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(destRect, destinationPathProperty, new GUIContent("目标路径"));
                    break;

                case FileOperationType.Rename:
                    var nameRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(nameRect, newNameProperty, new GUIContent("新名称"));
                    break;

                case FileOperationType.Delete:
                    // 删除操作只需要源路径
                    break;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProperty = property.FindPropertyRelative("type");
            var operationType = (FileOperationType)typeProperty.enumValueIndex;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // 操作类型 + 源路径 (总是显示)
            float height = 2 * lineHeight + spacing;

            // 根据操作类型添加额外字段的高度
            switch (operationType)
            {
                case FileOperationType.Copy:
                case FileOperationType.Move:
                case FileOperationType.Rename:
                    height += lineHeight + spacing;
                    break;

                case FileOperationType.Delete:
                    // 删除操作不需要额外字段
                    break;
            }

            return height;
        }
    }
}