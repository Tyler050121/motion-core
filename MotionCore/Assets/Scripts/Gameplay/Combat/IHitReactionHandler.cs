using MotionCore.Gameplay.Character;

namespace MotionCore.Gameplay.Combat
{
    public interface IHitReactionHandler
    {
        void ReceiveHit(StaggerLevel staggerLevel, float knockbackPower);
    }
}
