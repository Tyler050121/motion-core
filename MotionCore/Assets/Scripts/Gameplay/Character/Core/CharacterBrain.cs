using MotionCore.Gameplay.Cameras;
using MotionCore.Gameplay.Combat;
using MotionCore.Gameplay.Input;
using MotionCore.Gameplay.Targeting;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [DisallowMultipleComponent]
    public sealed class CharacterBrain : MonoBehaviour
    {
        [SerializeField] Character m_Character;
        [SerializeField] TargetLockController m_TargetLockController;

        InputActions m_InputActions;
        ICharacterCommandExecutor m_CommandExecutor;
        IHitReactionHandler m_HitReactionHandler;
        ICameraService m_Camera;
        bool m_IsPaused;

        void Awake()
        {
            m_CommandExecutor = GetComponentInChildren<ICharacterCommandExecutor>();
            m_HitReactionHandler = GetComponentInChildren<IHitReactionHandler>();
            m_InputActions = new InputActions();
            m_CommandExecutor.SetAttackFacingResolver(GetAttackFacingDirection);
        }

        void OnEnable()
        {
            m_InputActions?.Enable();
        }

        void OnDisable()
        {
            m_InputActions?.Disable();
        }

        void Start()
        {
            m_Camera = ServiceLocator.Resolve<ICameraService>();
        }

        void OnDestroy()
        {
            m_InputActions?.Dispose();
        }

        void Update()
        {
            UpdatePause();
            if (m_IsPaused)
                return;

            UpdateMovement();
            UpdateAction();
        }

        public void SetMoveInput(Vector2 moveInput, bool wantsRun)
        {
            bool hasMoveInput = moveInput.sqrMagnitude > 0.0001f;
            Vector2 clampedInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            Vector3 forward = m_Camera.PlanarForward;
            Vector3 right = m_Camera.PlanarRight;

            Vector3 moveDirection = hasMoveInput
                ? right.normalized * clampedInput.x + forward.normalized * clampedInput.y
                : Vector3.zero;

            m_CommandExecutor.SetMoveInput(clampedInput, moveDirection, wantsRun);
        }

        public bool TryEvade()
        {
            return m_CommandExecutor.TryEvade();
        }

        public bool TryNormalAttack()
        {
            return m_CommandExecutor.TryBasicAttack();
        }

        public void ReceiveHit(float knockbackPower)
        {
            m_HitReactionHandler.ReceiveHit(knockbackPower);
        }

        public void ToggleTargetLock()
        {
            m_TargetLockController.ToggleLock();
        }

        void UpdatePause()
        {
            if (!m_InputActions.Character.Pause.WasPressedThisFrame())
                return;

            m_IsPaused = !m_IsPaused;
            Time.timeScale = m_IsPaused ? 0f : 1f;
        }

        void UpdateMovement()
        {
            Vector2 moveInput = m_InputActions.Character.Move.ReadValue<Vector2>();
            bool wantsRun = m_InputActions.Character.Run.IsPressed();
            SetMoveInput(moveInput, wantsRun);
        }

        void UpdateAction()
        {
            if (m_InputActions.Character.Evade.WasPressedThisFrame())
                TryEvade();

            if (m_InputActions.Character.BasicAttack.WasPressedThisFrame())
                TryNormalAttack();

            if (m_InputActions.Character.CycleLockTarget.WasPressedThisFrame())
                m_TargetLockController.TryCycleLockTarget();

            if (m_InputActions.Character.Lock.WasPressedThisFrame())
                ToggleTargetLock();
        }

        Vector3 GetAttackFacingDirection()
        {
            if (m_TargetLockController.HasTarget)
                return m_TargetLockController.GetDirectionFrom(m_Character.FacingRoot.position);

            if (m_Character.Parameters.HasMoveInput)
                return m_Character.Parameters.MoveDirection;

            return m_Character.FacingRoot.forward;
        }
    }
}
