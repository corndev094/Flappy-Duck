using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FpsSetter
{
    public class FpsSetter : EditorWindow
    {
        int targetFps;
        int selectedFpsIndex;
        string[] fpsOptions = new string[] { "Custom", "15", "30", "60", "90", "120", "144", "240", "360", "Max" };
        int[] fpsValues = new int[] {0, 15, 30, 60, 90, 120, 144, 240, 360, -1 };

        const string targetFpsKey = "FpsSetter_TargetFps";
        const int buttonHeight = 30; // Button height
        const int minButtonWidth = 70;
        const int inputFieldWidth = 185; // Input field width
        const int scrollMenuThresholdHorizontal = 220;
        const int scrollMenuThresholdVertical = 120;

        bool buttonPressed = false;

        [MenuItem("Window/General/FPS Setter")]
        public static void ShowWindow()
        {
            GetWindow(typeof(FpsSetter), false, "FPS Setter");
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            targetFps = EditorPrefs.GetInt(targetFpsKey, 0);
            UpdateScrollbarIndex();
        }

        private void UpdateScrollbarIndex()
        {
            if (fpsValues.Contains(targetFps))
                selectedFpsIndex = System.Array.IndexOf(fpsValues, targetFps);
            else
                selectedFpsIndex = 0;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                UpdateApplicationTargetFramerate();
        }

        private void OnGUI()
        {
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            targetFps = EditorGUILayout.IntField("Target FPS", targetFps, GUILayout.Width(inputFieldWidth));
            if (position.width < scrollMenuThresholdHorizontal || position.height < scrollMenuThresholdVertical)
            {
                GUILayout.Space(-75);
                DisplayScrollbar();
            }
            GUILayout.EndHorizontal();

            if (position.width >= scrollMenuThresholdHorizontal && position.height >= scrollMenuThresholdVertical)
                DisplayButtons();

            if(buttonPressed)
            {
                GUI.FocusControl(null);
                buttonPressed = false;
            }


            targetFps = Mathf.Clamp(targetFps, -1, 360);

            if (GUI.changed)
                EditorPrefs.SetInt(targetFpsKey, targetFps);

            if (EditorApplication.isPlaying)
                UpdateApplicationTargetFramerate();
        }

        private void DisplayButtons()
        {
            EditorGUILayout.Space();
            int buttonsPerRow = 3;
            float buttonWidth = Mathf.Max((position.width - (buttonsPerRow + 1) * 5) / buttonsPerRow, minButtonWidth);

            GUILayoutOption[] buttonSize = { GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight) };

            for(int i = 1; i < fpsOptions.Length; i += 3)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < i + 3; j++)
                    if (GUILayout.Button(fpsOptions[j], buttonSize))
                    {
                        targetFps = fpsValues[j];
                        buttonPressed = true;
                    }
                GUILayout.EndHorizontal();
            }
        }

        private void DisplayScrollbar()
        {
            UpdateScrollbarIndex();
            EditorGUILayout.LabelField("", GUILayout.Width(75));
            selectedFpsIndex = EditorGUILayout.Popup(selectedFpsIndex, fpsOptions, GUILayout.Width(75));
            if(selectedFpsIndex != 0)
                targetFps = fpsValues[selectedFpsIndex];
        }

        private void UpdateApplicationTargetFramerate()
        {
            if (Application.targetFrameRate != targetFps)
                Application.targetFrameRate = targetFps;
        }

        private void OnDisable()
        {
            EditorPrefs.SetInt(targetFpsKey, targetFps);
        }
    }
}
