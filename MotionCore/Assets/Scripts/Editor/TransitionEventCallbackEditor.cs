#if UNITY_EDITOR
using Animancer;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MotionCore.Editor
{
    [CustomEditor(typeof(TransitionAsset), true), CanEditMultipleObjects]
    public sealed class TransitionEventCallbackInspector : UnityEditor.Editor
    {
        bool m_FixQueued;

        void OnEnable()
        {
            QueueFixTargets();
        }

        public override void OnInspectorGUI()
        {
            Event currentEvent = Event.current;

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck() ||
                ShouldFixAfterInspectorEvent(currentEvent))
            {
                QueueFixTargets();
            }

            if (target != null &&
                EditorApplication.isPlayingOrWillChangePlaymode &&
                EditorUtility.IsPersistent(target))
            {
                EditorGUILayout.HelpBox(
                    "This is an asset, not a scene object, which means that any changes you make to it are permanent and will NOT be undone when you exit Play Mode.",
                    MessageType.Warning);
            }
        }

        void QueueFixTargets()
        {
            if (m_FixQueued)
            {
                return;
            }

            m_FixQueued = true;
            EditorApplication.delayCall += FixTargets;
        }

        void FixTargets()
        {
            m_FixQueued = false;

            if (this == null)
            {
                return;
            }

            foreach (Object targetObject in targets)
            {
                if (targetObject is TransitionAssetBase transitionAsset)
                {
                    TransitionEventCallbackEditor.FixTransitionAsset(transitionAsset, true);
                }
            }
        }

        static bool ShouldFixAfterInspectorEvent(Event currentEvent)
        {
            if (currentEvent == null)
            {
                return false;
            }

            return currentEvent.rawType == EventType.MouseUp ||
                currentEvent.rawType == EventType.DragPerform ||
                currentEvent.rawType == EventType.KeyUp ||
                currentEvent.type == EventType.ExecuteCommand;
        }
    }

    /// <summary>
    /// Keeps MotionCore animation event callbacks wired to the expected Animancer callback type.
    /// </summary>
    public static class TransitionEventCallbackEditor
    {
        static readonly List<TimedEventIndex> s_ParameterEvents = new();

        [MenuItem("MotionCore/Animation/Fix Selected Transition Event Callbacks")]
        static void FixSelectedTransitionEventCallbacks()
        {
            int changedCount = FixObjects(Selection.objects, true);
            if (changedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            Debug.Log(
                $"MotionCore checked selected TransitionAssets. Updated {changedCount} asset(s).");
        }

        [MenuItem("MotionCore/Animation/Fix Selected Transition Event Callbacks", true)]
        static bool CanFixSelectedTransitionEventCallbacks()
        {
            foreach (Object selectedObject in Selection.objects)
            {
                if (selectedObject is TransitionAssetBase transitionAsset &&
                    IsEditableProjectAsset(transitionAsset))
                {
                    return true;
                }
            }

            return false;
        }

        static int FixObjects(Object[] objects, bool recordUndo)
        {
            int changedCount = 0;

            foreach (Object selectedObject in objects)
            {
                if (selectedObject is not TransitionAssetBase transitionAsset)
                {
                    continue;
                }

                if (FixTransitionAsset(transitionAsset, recordUndo))
                {
                    changedCount++;
                }
            }

            return changedCount;
        }

        public static bool FixTransitionAsset(TransitionAssetBase transitionAsset, bool recordUndo)
        {
            if (!transitionAsset ||
                !IsEditableProjectAsset(transitionAsset))
            {
                return false;
            }

            var serializedObject = new SerializedObject(transitionAsset);
            serializedObject.Update();

            SerializedProperty transitionProperty =
                serializedObject.FindProperty(TransitionAssetBase.TransitionField);

            if (transitionProperty == null ||
                transitionProperty.managedReferenceValue == null)
            {
                return false;
            }

            bool recordedUndo = false;
            void RecordUndoOnce()
            {
                if (!recordUndo ||
                    recordedUndo)
                {
                    return;
                }

                Undo.RecordObject(transitionAsset, "Set Transition Event Callback");
                recordedUndo = true;
            }

            bool changed = FixTransitionEvents(transitionProperty, RecordUndoOnce);
            if (!changed)
            {
                return false;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(transitionAsset);
            return true;
        }

        static bool FixTransitionEvents(SerializedProperty transitionProperty, System.Action recordUndo)
        {
            SerializedProperty eventsProperty = transitionProperty.FindPropertyRelative("_Events");
            if (eventsProperty == null)
            {
                return false;
            }

            SerializedProperty normalizedTimes =
                eventsProperty.FindPropertyRelative("_NormalizedTimes");
            SerializedProperty names = eventsProperty.FindPropertyRelative("_Names");
            SerializedProperty callbacks = eventsProperty.FindPropertyRelative("_Callbacks");

            if (names == null ||
                callbacks == null)
            {
                return false;
            }

            int eventCount = normalizedTimes != null
                ? Mathf.Max(0, normalizedTimes.arraySize - 1)
                : names.arraySize;

            int nameCount = Mathf.Min(names.arraySize, eventCount);
            bool changed = false;
            s_ParameterEvents.Clear();

            for (int i = 0; i < nameCount; i++)
            {
                SerializedProperty nameProperty = names.GetArrayElementAtIndex(i);
                var eventName = nameProperty.objectReferenceValue as StringAsset;
                CallbackRule rule = GetCallbackRule(eventName);
                if (rule == CallbackRule.ParameterInt)
                {
                    float normalizedTime = normalizedTimes != null
                        ? normalizedTimes.GetArrayElementAtIndex(i).floatValue
                        : i;

                    s_ParameterEvents.Add(new TimedEventIndex(i, normalizedTime));
                    continue;
                }

                changed |= ApplyCallbackRule(callbacks, i, rule, recordUndo);
            }

            s_ParameterEvents.Sort(CompareTimedEventIndex);
            for (int i = 0; i < s_ParameterEvents.Count; i++)
            {
                changed |= EnsureParameterIntCallback(callbacks, s_ParameterEvents[i].Index, i, recordUndo);
            }

            s_ParameterEvents.Clear();
            changed |= TrimTrailingNullCallbacks(callbacks, recordUndo);
            return changed;
        }

        static CallbackRule GetCallbackRule(StringAsset eventName)
        {
            if (!eventName)
            {
                return CallbackRule.Unchanged;
            }

            string name = eventName.name;
            if (name == GlobalConfig.AnimationEventNames.Hit ||
                name == GlobalConfig.AnimationEventNames.HitStart)
            {
                return CallbackRule.ParameterInt;
            }

            if (name == GlobalConfig.AnimationEventNames.CanAttack ||
                name == GlobalConfig.AnimationEventNames.CanCancel)
            {
                return CallbackRule.Null;
            }

            return CallbackRule.Unchanged;
        }

        static bool ApplyCallbackRule(
            SerializedProperty callbacks,
            int index,
            CallbackRule rule,
            System.Action recordUndo)
        {
            return rule switch
            {
                CallbackRule.ParameterInt => false,
                CallbackRule.Null => EnsureNullCallback(callbacks, index, recordUndo),
                _ => false,
            };
        }

        static bool EnsureParameterIntCallback(
            SerializedProperty callbacks,
            int index,
            int value,
            System.Action recordUndo)
        {
            bool changed = false;

            if (callbacks.arraySize <= index)
            {
                recordUndo();
                callbacks.arraySize = index + 1;
                changed = true;
            }

            SerializedProperty callback = callbacks.GetArrayElementAtIndex(index);
            if (callback.managedReferenceValue is AnimancerEvent.ParameterInt parameterInt)
            {
                if (parameterInt.Value == value)
                {
                    return changed;
                }

                recordUndo();
                parameterInt.Value = value;
                callback.managedReferenceValue = parameterInt;
                return true;
            }

            recordUndo();
            var parameter = new AnimancerEvent.ParameterInt
            {
                Value = value,
            };

            callback.managedReferenceValue = parameter;
            return true;
        }

        static bool EnsureNullCallback(
            SerializedProperty callbacks,
            int index,
            System.Action recordUndo)
        {
            if (callbacks.arraySize <= index)
            {
                return false;
            }

            SerializedProperty callback = callbacks.GetArrayElementAtIndex(index);
            if (callback.managedReferenceValue == null)
            {
                return false;
            }

            recordUndo();
            callback.managedReferenceValue = null;
            return true;
        }

        static bool TrimTrailingNullCallbacks(SerializedProperty callbacks, System.Action recordUndo)
        {
            int lastRequiredIndex = callbacks.arraySize - 1;
            while (lastRequiredIndex >= 0 &&
                callbacks.GetArrayElementAtIndex(lastRequiredIndex).managedReferenceValue == null)
            {
                lastRequiredIndex--;
            }

            int requiredSize = lastRequiredIndex + 1;
            if (callbacks.arraySize == requiredSize)
            {
                return false;
            }

            recordUndo();
            callbacks.arraySize = requiredSize;
            return true;
        }

        static bool IsEditableProjectAsset(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return !string.IsNullOrEmpty(path) &&
                path.StartsWith("Assets/", System.StringComparison.Ordinal);
        }

        static int CompareTimedEventIndex(TimedEventIndex a, TimedEventIndex b)
        {
            int timeComparison = a.NormalizedTime.CompareTo(b.NormalizedTime);
            return timeComparison != 0
                ? timeComparison
                : a.Index.CompareTo(b.Index);
        }

        enum CallbackRule
        {
            Unchanged,
            ParameterInt,
            Null,
        }

        readonly struct TimedEventIndex
        {
            public readonly int Index;
            public readonly float NormalizedTime;

            public TimedEventIndex(int index, float normalizedTime)
            {
                Index = index;
                NormalizedTime = normalizedTime;
            }
        }
    }
}
#endif
