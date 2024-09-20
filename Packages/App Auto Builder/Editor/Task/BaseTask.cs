using System.Threading.Tasks;
using UnityEngine;
namespace zFramework.AppBuilder
{
    public class BaseTask : ScriptableObject
    {
        public TaskType taskType;
        public int priority;
        internal string Description;
        public virtual Task<string> RunAsync(string output)
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


    }
}