namespace MotionCore.Infrastructure
{
    public interface IVfxService
    {
        PooledVfx Play(VfxPreset preset, in VfxSpawnRequest request);
    }
}
