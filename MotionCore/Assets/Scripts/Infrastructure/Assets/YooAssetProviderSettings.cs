namespace MotionCore.Infrastructure
{
    /// <summary>
    /// YooAsset 运行时设置。
    /// </summary>
    public sealed class YooAssetProviderSettings
    {
        public const string DefaultPackageName = "DefaultPackage";

        public string PackageName { get; set; } = DefaultPackageName;
        public YooAssetPlayMode PlayMode { get; set; } = YooAssetPlayMode.EditorSimulate;
    }

    /// <summary>
    /// YooAsset 资源包播放模式。
    /// </summary>
    public enum YooAssetPlayMode
    {
        EditorSimulate = 0,
        Offline = 1
    }
}
