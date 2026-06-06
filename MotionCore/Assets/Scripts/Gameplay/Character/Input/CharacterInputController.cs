using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [DisallowMultipleComponent]
    public sealed class CharacterInputController : MonoBehaviour
    {
        [SerializeField] CharacterBrain m_Brain;
        [SerializeField] KeyCode m_PauseKey = KeyCode.P;
        [SerializeField] KeyCode m_EvadeKey = KeyCode.LeftShift;
        [SerializeField] KeyCode m_BasicAttackKey = KeyCode.Mouse0;
        [SerializeField] KeyCode m_LockKey = KeyCode.Mouse2;
        [SerializeField] KeyCode m_RunKey = KeyCode.LeftShift;
        [SerializeField] KeyCode m_HitTestKey = KeyCode.Alpha1;
        [SerializeField] KeyCode m_KnockbackHitTestKey = KeyCode.Alpha2;
        [SerializeField, Min(0f)] float m_HitTestKnockbackPower;
        [SerializeField, Min(0f)] float m_KnockbackHitTestPower = 2f;
        bool m_IsPaused;

        void Update()
        {
            UpdatePause();
            if (m_IsPaused)
                return;

            UpdateMovement();
            UpdateAction();
        }

        void UpdatePause()
        {
            if (!Input.GetKeyDown(m_PauseKey))
                return;

            m_IsPaused = !m_IsPaused;
            Time.timeScale = m_IsPaused ? 0f : 1f;
        }

        void UpdateMovement()
        {
            Vector2 moveInput = Vector2.zero;
            if (Input.GetKey(KeyCode.W))
                moveInput.y += 1f;
            if (Input.GetKey(KeyCode.S))
                moveInput.y -= 1f;
            if (Input.GetKey(KeyCode.D))
                moveInput.x += 1f;
            if (Input.GetKey(KeyCode.A))
                moveInput.x -= 1f;

            bool wantsRun = m_RunKey != KeyCode.None && Input.GetKey(m_RunKey);
            m_Brain.SetMoveInput(moveInput, wantsRun);
        }

        void UpdateAction()
        {
            if (Input.GetKeyDown(m_EvadeKey))
                m_Brain.TryEvade();

            if (Input.GetKeyDown(m_BasicAttackKey))
                m_Brain.TryNormalAttack();

            if (Input.GetKeyDown(m_LockKey))
                m_Brain.ToggleTargetLock();

            if (Input.GetKeyDown(m_HitTestKey))
                m_Brain.ReceiveHit(m_HitTestKnockbackPower);

            if (Input.GetKeyDown(m_KnockbackHitTestKey))
                m_Brain.ReceiveHit(m_KnockbackHitTestPower);
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 300f, 172f), GUI.skin.box);
            // GUILayout.Label($"Current State: {m_Brain.CurrentStateType}");
            // GUILayout.Label($"Move Speed: {m_Brain.MoveSpeed:0.00}");
            GUILayout.Space(4f);
            GUILayout.Label("WASD Camera-Relative Move");
            GUILayout.Label("Hold Shift Run");
            GUILayout.Label("Press Shift Evade");
            GUILayout.Label("Mouse0 Basic Action");
            GUILayout.Label("Mouse2 Lock Target");
            GUILayout.Label("P Pause/Resume");
            GUILayout.Label("Alpha1 Hit Test");
            GUILayout.Label("Alpha2 Knockback Hit Test");
            GUILayout.EndArea();
        }
    }
}
