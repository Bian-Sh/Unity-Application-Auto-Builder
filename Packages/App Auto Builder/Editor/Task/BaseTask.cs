using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace zFramework.AppBuilder
{
    // 约定：
    // 1. output 是 Unity 打包回调返回的路径
    // 2. BuildTaskResult.Output 使得修改路径成为可能，方便 task 内部的修改被下一个 Task 使用
    // 3. 一般来说，通过 Path.GetFileNameWithoutExtension(output)  可以获取到 ProductName
    public class BaseTask : ScriptableObject
    {
        public TaskType taskType;
        public int priority;
        internal string Description;
        
        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="output">输入的输出路径</param>
        /// <returns>任务执行结果，包含成功状态和可能修改的输出路径</returns>
        public virtual Task<BuildTaskResult> RunAsync(string output)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///  为避免运行后才发现错误，可以在这里进行一些预检查
        /// </summary>
        /// <returns></returns>
        public virtual bool Validate()
        {
            return true;
        }

        #region Report  task result 
        // 报告任务结果，将 文件路径在控制台中使用 HyperLink 的方式显示,方便用户一键直达文件 Or 文件夹
        [InitializeOnLoadMethod]
        private static void Init()
        {
#if UNITY_2021_1_OR_NEWER
            EditorGUI.hyperLinkClicked += OnLinkClicked;
            static void OnLinkClicked(EditorWindow ew, HyperLinkClickedEventArgs args)
            {
                if (args.hyperLinkData.TryGetValue("autobuilderresult", out var path))
                {
                    EditorUtility.RevealInFinder(path);
                }
            };
        }
#else
            var evt = typeof(EditorGUI).GetEvent("hyperLinkClicked", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (evt != null)
            {
                var handler = Delegate.CreateDelegate(evt.EventHandlerType, typeof(BaseTask), nameof(OnLinkClicked));
                evt.AddMethod.Invoke(null, new object[] { handler });
            }
        }
        static void OnLinkClicked(object sender, EventArgs args)
        {
            var property = args.GetType().GetProperty("hyperlinkInfos", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (property.GetValue(args) is Dictionary<string, string> infos)
            {
                if (infos.TryGetValue("autobuilderresult", out var path))
                {
                    EditorUtility.RevealInFinder(path);
                }
            }
        }
#endif

        public void ReportResult(string path, Func<string> Prefixed = null)
        {
            var prefix = Prefixed?.Invoke();
            Debug.Log($"{prefix}<a autobuilderresult=\"{path}\">{path}</a>");
        }

        #endregion


    }
}