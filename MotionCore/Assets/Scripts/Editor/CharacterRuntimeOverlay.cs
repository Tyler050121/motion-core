using System;
using Animancer;
using MotionCore.Gameplay.Character;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MotionCore.Editor
{
    /// <summary>
    /// 在 Game 视图右上角显示角色运行信息。
    /// </summary>
    [InitializeOnLoad]
    public static class CharacterRuntimeOverlay
    {
        const string PanelName = "motion-core-character-runtime-overlay";
        const string ContentName = "motion-core-character-runtime-content";
        const double SampleInterval = 0.25d;

        static readonly Type s_GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");

        static Character s_Character;
        static double s_NextSampleTime;

        static CharacterRuntimeOverlay()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            double time = EditorApplication.timeSinceStartup;
            if (time < s_NextSampleTime)
                return;

            s_NextSampleTime = time + SampleInterval;
            string content = Application.isPlaying ? CaptureContent() : null;

            foreach (EditorWindow gameView in FindGameViews())
            {
                VisualElement panel = GetOrCreatePanel(gameView);
                panel.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;

                if (Application.isPlaying)
                    panel.Q<Label>(ContentName).text = content;
            }

            if (!Application.isPlaying)
                s_Character = null;
        }

        static EditorWindow[] FindGameViews()
        {
            if (s_GameViewType == null)
                return Array.Empty<EditorWindow>();

            Object[] objects = Resources.FindObjectsOfTypeAll(s_GameViewType);
            EditorWindow[] windows = new EditorWindow[objects.Length];
            for (int i = 0; i < objects.Length; i++)
                windows[i] = (EditorWindow)objects[i];

            return windows;
        }

        static VisualElement GetOrCreatePanel(EditorWindow gameView)
        {
            VisualElement root = gameView.rootVisualElement;
            VisualElement panel = root.Q<VisualElement>(PanelName);
            if (panel != null)
                return panel;

            panel = new VisualElement
            {
                name = PanelName,
                pickingMode = PickingMode.Ignore
            };
            panel.style.position = Position.Absolute;
            panel.style.top = 34f;
            panel.style.right = 10f;
            panel.style.width = 480f;
            panel.style.maxWidth = new Length(80f, LengthUnit.Percent);
            panel.style.height = StyleKeyword.Auto;
            panel.style.paddingTop = 10f;
            panel.style.paddingRight = 12f;
            panel.style.paddingBottom = 10f;
            panel.style.paddingLeft = 12f;
            panel.style.backgroundColor = new Color(0.04f, 0.04f, 0.04f, 0.82f);
            panel.style.borderTopLeftRadius = 4f;
            panel.style.borderTopRightRadius = 4f;
            panel.style.borderBottomLeftRadius = 4f;
            panel.style.borderBottomRightRadius = 4f;

            Label title = new("角色运行信息")
            {
                pickingMode = PickingMode.Ignore
            };
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14f;
            title.style.color = Color.white;
            title.style.marginBottom = 6f;

            Label content = new()
            {
                name = ContentName,
                pickingMode = PickingMode.Ignore
            };
            content.style.fontSize = 13f;
            content.style.color = Color.white;
            content.style.whiteSpace = WhiteSpace.Normal;
            content.style.unityTextAlign = TextAnchor.UpperLeft;

            panel.Add(title);
            panel.Add(content);
            root.Add(panel);
            return panel;
        }

        static string CaptureContent()
        {
            float frameTime = Time.unscaledDeltaTime;
            float framesPerSecond = frameTime > 0f ? 1f / frameTime : 0f;

            s_Character = ResolveCharacter();
            if (s_Character == null)
            {
                return $"FPS：{framesPerSecond:0}    Frame：{frameTime * 1000f:0.0} ms    " +
                       $"Time Scale：{Time.timeScale:0.00}\n等待活动角色...";
            }

            CharacterController controller = s_Character.GetComponent<CharacterController>();
            Vector3 velocity = controller != null ? controller.velocity : Vector3.zero;
            float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            AnimancerState animation = ResolveAnimation(s_Character);
            CharacterState state = s_Character.StateMachine.CurrentState;
            float moveSpeed = s_Character.Parameters != null ? s_Character.Parameters.MoveSpeed : 0f;

            return
                $"FPS：{framesPerSecond:0}    Frame：{frameTime * 1000f:0.0} ms    " +
                $"Time Scale：{Time.timeScale:0.00}\n" +
                $"角色：{s_Character.name}\n" +
                $"FSM 状态：{ResolveStateName(state)}\n" +
                $"移动参数速度：{moveSpeed:0.00} m/s\n" +
                $"实际水平速度：{planarSpeed:0.00} m/s    实际总速度：{velocity.magnitude:0.00} m/s\n" +
                $"Root Motion XZ：{ResolveRootMotionSpeed(s_Character):0.00} m/s\n" +
                $"动画：{ResolveAnimationName(animation)}\n" +
                $"动画进度：{ResolveAnimationProgress(animation)}    " +
                $"速度：{ResolveAnimationSpeed(animation)}    权重：{ResolveAnimationWeight(animation)}";
        }

        static Character ResolveCharacter()
        {
            if (s_Character != null && s_Character.isActiveAndEnabled)
                return s_Character;

            CharacterBrain playerBrain = Object.FindFirstObjectByType<CharacterBrain>();
            if (playerBrain != null)
            {
                Character playerCharacter = playerBrain.GetComponentInParent<Character>();
                if (playerCharacter == null)
                    playerCharacter = playerBrain.GetComponentInChildren<Character>();
                if (playerCharacter != null)
                    return playerCharacter;
            }

            return Object.FindFirstObjectByType<Character>();
        }

        static AnimancerState ResolveAnimation(Character character)
        {
            AnimancerComponent animancer = character.Animancer;
            if (animancer == null || !animancer.IsGraphInitialized)
                return null;

            return animancer.Layers[0].CurrentState;
        }

        static string ResolveStateName(CharacterState state)
        {
            return state != null ? $"{state.Type} / {state.GetType().Name}" : "--";
        }

        static string ResolveAnimationName(AnimancerState animation)
        {
            if (animation == null)
                return "--";

            if (animation.MainObject != null)
                return animation.MainObject.name;

            return animation.DebugName?.ToString() ?? animation.GetType().Name;
        }

        static string ResolveAnimationProgress(AnimancerState animation)
        {
            return animation != null
                ? $"{animation.Time:0.00}s / {animation.NormalizedTime:0.00}"
                : "--";
        }

        static string ResolveAnimationSpeed(AnimancerState animation)
        {
            return animation != null ? animation.EffectiveSpeed.ToString("0.00") : "--";
        }

        static string ResolveAnimationWeight(AnimancerState animation)
        {
            return animation != null ? animation.EffectiveWeight.ToString("0.00") : "--";
        }

        static float ResolveRootMotionSpeed(Character character)
        {
            if (Time.deltaTime <= 0f || character.Animancer == null || character.Animancer.Animator == null)
                return 0f;

            Vector3 delta = character.Animancer.Animator.deltaPosition;
            return new Vector2(delta.x, delta.z).magnitude / Time.deltaTime;
        }
    }
}
