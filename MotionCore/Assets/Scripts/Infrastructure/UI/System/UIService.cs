namespace MotionCore.Infrastructure
{
    /// <summary>
    /// UI 面板显式开关服务。
    /// </summary>
    public sealed class UIService : IUIService
    {
        readonly UIPanelManager m_PanelManager;

        /// <summary>
        /// 创建面板服务代理。
        /// </summary>
        public UIService(UIPanelManager panelManager)
        {
            m_PanelManager = panelManager;
        }

        /// <summary>
        /// 打开允许的面板。
        /// </summary>
        public void Open(string panelId)
        {
            m_PanelManager.Open(panelId);
        }

        /// <summary>
        /// 关闭指定面板。
        /// </summary>
        public void Close(string panelId)
        {
            m_PanelManager.Close(panelId);
        }
    }
}
