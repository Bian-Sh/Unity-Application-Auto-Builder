namespace zFramework.AppBuilder
{
    /// <summary>
    /// 构建任务执行结果
    /// </summary>
    public class BuildTaskResult
    {
        /// <summary>
        /// 任务是否执行成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 修改后的输出路径，如果任务修改了路径，下一个任务将使用这个新路径
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// 可选的错误消息
        /// </summary>
        public string Message { get; set; }

        public BuildTaskResult(bool success, string output, string message = null)
        {
            Success = success;
            Output = output;
            Message = message;
        }

        /// <summary>
        /// 创建成功的结果
        /// </summary>
        public static BuildTaskResult Successful(string output) => new BuildTaskResult(true, output);

        /// <summary>
        /// 创建失败的结果
        /// </summary>
        public static BuildTaskResult Failed(string output, string message = null) => new BuildTaskResult(false, output, message);
    }
}