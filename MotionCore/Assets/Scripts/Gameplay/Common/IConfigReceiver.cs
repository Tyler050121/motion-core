namespace MotionCore.Gameplay.Common
{
    public interface IConfigReceiver
    {
    }

    public interface IConfigReceiver<in TConfig> : IConfigReceiver
    {
        void Initialize(TConfig config);
    }
}
