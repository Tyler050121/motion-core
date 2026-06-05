namespace MotionCore.Infrastructure
{
    public interface IVfxService
    {
        PooledVfx Play(in VfxSpawnRequest request);
    }
}
