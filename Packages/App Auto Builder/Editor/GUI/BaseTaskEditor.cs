using UnityEditor;
using UnityEngine;
using System;
namespace zFramework.AppBuilder
{
    [CustomEditor(typeof(BaseTask), true)]
    public class BaseTaskEditor : Editor
    {
        public BaseTask task;
        public GUIContent GUIContent = new GUIContent("Test Run Task", "测试用户任务，请务必对自己的操作有认知能力！");
        public virtual void OnEnable()
        {
            task = (BaseTask)target;
        }

        string arg = string.Empty;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            using (var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.selectionRect, GUILayout.Width(EditorGUIUtility.currentViewWidth)))
            {
                if (task.taskType == TaskType.PostBuild)
                {
                    arg = EditorGUILayout.TextField("Args", arg);
                    GUILayout.Space(4);
                }
                else
                {
                    arg = string.Empty;
                }
                var buttonRect = GUILayoutUtility.GetRect(GUIContent, GUI.skin.button, GUILayout.Height(24), GUILayout.Width(120));
                buttonRect.x = (EditorGUIUtility.currentViewWidth - buttonRect.width) / 2;

                if (GUI.Button(buttonRect, GUIContent))
                {
                    async void InternalTask()
                    {
                        try
                        {
                            // validate task
                            var validatePassed = task.Validate();
                            if (!validatePassed)
                            {
                                Debug.LogError("任务验证失败，请检查任务配置，更多信息见控制台！");
                                return;
                            }

                            var result = await task.RunAsync(arg);
                            if (result.Success && !string.IsNullOrEmpty(result.Output))
                            {
                                Debug.Log(result.Output);
                            }
                            else if (!result.Success)
                            {
                                Debug.LogError($"任务执行失败！{(string.IsNullOrEmpty(result.ErrorMessage) ? "" : $" 错误：{result.ErrorMessage}")}");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"任务执行错误: {e.Message}");
                        }
                    }
                    InternalTask();
                }
            }
            DrawHelpbox(task);
        }

        public void DrawHelpbox(BaseTask task)
        {
            if (!string.IsNullOrEmpty(task.Description))
            {
                GUILayout.Space(10);
                GUIStyle helpBoxStyle = new(EditorStyles.helpBox)
                {
                    fontSize = 12
                };
                EditorGUILayout.LabelField(task.Description, helpBoxStyle);
            }
        }

    }
}
