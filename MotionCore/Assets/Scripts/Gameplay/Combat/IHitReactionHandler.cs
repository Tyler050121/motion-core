namespace MotionCore.Gameplay.Combat
{
    public interface IHitReactionHandler
    {
        void ReceiveHit(float knockbackPower);
    }
}
