namespace MotionCore.Infrastructure
{
    /// <summary>
    /// UI 面板显式开关服务。
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// 打开当前作用域允许的面板。
        /// </summary>
        void Open(string panelId);

        /// <summary>
        /// 关闭指定面板。
        /// </summary>
        void Close(string panelId);
    }
}
