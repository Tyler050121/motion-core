using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [DisallowMultipleComponent]
    public sealed class CharacterInputController : MonoBehaviour
    {
        [SerializeField] CharacterBrain m_Brain;
        [SerializeField] KeyCode m_EvadeKey = KeyCode.LeftShift;
        [SerializeField] KeyCode m_BasicAttackKey = KeyCode.Mouse0;
        [SerializeField] KeyCode m_RunKey = KeyCode.LeftShift;
        [SerializeField] ActionDefinition m_BasicAttackAction;

        void Update()
        {
            UpdateMovement();
            UpdateAction();
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
                m_Brain.TryAction(m_BasicAttackAction);
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 300f, 112f), GUI.skin.box);
            // GUILayout.Label($"Current State: {m_Brain.CurrentStateType}");
            // GUILayout.Label($"Move Speed: {m_Brain.MoveSpeed:0.00}");
            GUILayout.Space(4f);
            GUILayout.Label("WASD Camera-Relative Move");
            GUILayout.Label("Hold Shift Run");
            GUILayout.Label("Press Shift Evade");
            GUILayout.Label("Mouse0 Basic Action");
            GUILayout.EndArea();
        }
    }
}
