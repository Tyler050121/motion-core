using Animancer;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// Defines the authored animation route for locomotion stages.
    /// </summary>
    [CreateAssetMenu(menuName = "MotionCore/Character/Locomotion Animation Profile")]
    public sealed class LocomotionAnimationProfile : ScriptableObject
    {
        [Tooltip("Optional transition played when entering Move from Idle.")]
        [SerializeField] LocomotionAnimationStage m_MoveStart;
        public LocomotionAnimationStage MoveStart => m_MoveStart;

        [Tooltip("Required looping transition route while the character has move input.")]
        [SerializeField] LocomotionAnimationStage m_MoveLoop;
        public LocomotionAnimationStage MoveLoop => m_MoveLoop;

        [Tooltip("Optional transition played before returning from Move to Idle.")]
        [SerializeField] LocomotionAnimationStage m_MoveEnd;
        public LocomotionAnimationStage MoveEnd => m_MoveEnd;

        [Tooltip("Optional run turn-back transition.")]
        [SerializeField] TransitionAsset m_RunTurnBack;
        public TransitionAsset RunTurnBack => m_RunTurnBack;
    }

    public enum LocomotionStageRouteMode
    {
        Single,
        SplitWalkRun
    }

    [System.Serializable]
    public sealed class LocomotionAnimationStage
    {
        [SerializeField] LocomotionStageRouteMode m_RouteMode;
        public LocomotionStageRouteMode RouteMode => m_RouteMode;

        [SerializeField, ShowIf(nameof(m_RouteMode), LocomotionStageRouteMode.Single)]
        TransitionAsset m_SingleTransition;
        public TransitionAsset SingleTransition => m_SingleTransition;

        [SerializeField, ShowIf(nameof(m_RouteMode), LocomotionStageRouteMode.SplitWalkRun)]
        TransitionAsset m_WalkTransition;
        public TransitionAsset WalkTransition => m_WalkTransition;

        [SerializeField, ShowIf(nameof(m_RouteMode), LocomotionStageRouteMode.SplitWalkRun)]
        TransitionAsset m_RunTransition;
        public TransitionAsset RunTransition => m_RunTransition;
    }
}
