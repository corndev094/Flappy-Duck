using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace FlappyDuck.Editor
{
    public class SettingsConfigWindow : EditorWindow
    {
        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;

        private string screenMode = "Exclusive";
        private string resolution = "1920x1080";
        private int fps = 60;
        private bool vSync = false;
        private float brightness = 0.5f;
        private float contrast = 0.5f;

        private string language = "en";

        // Lists for dropdowns
        private StringListSO screenModeList;
        private StringListSO resolutionList;
        private IntListSO fpsList;
        private StringListSO languageList;

        [MenuItem("Flappy Duck/Settings Config Window")]
        public static void ShowWindow()
        {
            GetWindow<SettingsConfigWindow>("Settings Config");
        }

        private void OnEnable()
        {
            LoadAssets();
            LoadCurrentValues();
        }

        private void LoadAssets()
        {
            screenModeList = AssetDatabase.LoadAssetAtPath<StringListSO>("Assets/_Project/ScriptableObjects/Data/VariableList/ScreenModeList.asset");
            resolutionList = AssetDatabase.LoadAssetAtPath<StringListSO>("Assets/_Project/ScriptableObjects/Data/VariableList/ResolutionList.asset");
            fpsList = AssetDatabase.LoadAssetAtPath<IntListSO>("Assets/_Project/ScriptableObjects/Data/VariableList/FPSList.asset");
            languageList = AssetDatabase.LoadAssetAtPath<StringListSO>("Assets/_Project/ScriptableObjects/Data/VariableList/LanguageList.asset");
        }

        private void LoadCurrentValues()
        {
            masterVolume = PlayerPrefs.GetFloat(ConstantString.SOUND_MASTER, 1f);
            musicVolume = PlayerPrefs.GetFloat(ConstantString.SOUND_MUSIC, 1f);
            sfxVolume = PlayerPrefs.GetFloat(ConstantString.SOUND_SFX, 1f);

            screenMode = PlayerPrefs.GetString(ConstantString.SCREEN_MODE, "Exclusive");
            resolution = PlayerPrefs.GetString(ConstantString.RESOLUTION, "1920x1080");
            fps = PlayerPrefs.GetInt(ConstantString.FPS, 60);
            vSync = PlayerPrefs.GetInt(ConstantString.VSYNC, 0) == 1;
            brightness = PlayerPrefs.GetFloat(ConstantString.BRIGHTNESS, 0.5f);
            contrast = PlayerPrefs.GetFloat(ConstantString.CONTRAST, 0.5f);

            language = PlayerPrefs.GetString(ConstantString.LANGUAGE, "en");
        }

        private void OnGUI()
        {
            GUILayout.Label("Default Settings Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These values are stored in PlayerPrefs and will be used when the game starts or when settings are reset.", MessageType.Info);

            EditorGUILayout.Space();
            GUILayout.Label("Audio Settings", EditorStyles.boldLabel);
            masterVolume = EditorGUILayout.Slider("Master Volume", masterVolume, 0f, 1f);
            musicVolume = EditorGUILayout.Slider("Music Volume", musicVolume, 0f, 1f);
            sfxVolume = EditorGUILayout.Slider("SFX Volume", sfxVolume, 0f, 1f);

            EditorGUILayout.Space();
            GUILayout.Label("Graphic Settings", EditorStyles.boldLabel);
            
            // Screen Mode Dropdown
            if (screenModeList != null && screenModeList.list.Count > 0)
            {
                int index = screenModeList.list.IndexOf(screenMode);
                if (index == -1) index = 0;
                index = EditorGUILayout.Popup("Screen Mode", index, screenModeList.list.ToArray());
                screenMode = screenModeList.list[index];
            }
            else
            {
                screenMode = EditorGUILayout.TextField("Screen Mode", screenMode);
            }

            // Resolution Dropdown
            if (resolutionList != null && resolutionList.list.Count > 0)
            {
                int index = resolutionList.list.IndexOf(resolution);
                if (index == -1) index = 0;
                index = EditorGUILayout.Popup("Resolution", index, resolutionList.list.ToArray());
                resolution = resolutionList.list[index];
            }
            else
            {
                resolution = EditorGUILayout.TextField("Resolution", resolution);
            }

            // FPS Dropdown
            if (fpsList != null && fpsList.list.Count > 0)
            {
                string[] options = new string[fpsList.list.Count];
                for (int i = 0; i < fpsList.list.Count; i++) options[i] = fpsList.list[i].ToString();

                int index = fpsList.list.IndexOf(fps);
                if (index == -1) index = 0;
                index = EditorGUILayout.Popup("FPS", index, options);
                fps = fpsList.list[index];
            }
            else
            {
                fps = EditorGUILayout.IntField("FPS", fps);
            }

            vSync = EditorGUILayout.Toggle("VSync", vSync);
            brightness = EditorGUILayout.Slider("Brightness", brightness, 0f, 1f);
            contrast = EditorGUILayout.Slider("Contrast", contrast, 0f, 1f);

            EditorGUILayout.Space();
            GUILayout.Label("Localization", EditorStyles.boldLabel);
            if (languageList != null && languageList.list.Count > 0)
            {
                int index = languageList.list.IndexOf(language);
                if (index == -1) index = 0;
                index = EditorGUILayout.Popup("Language", index, languageList.list.ToArray());
                language = languageList.list[index];
            }
            else
            {
                language = EditorGUILayout.TextField("Language", language);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save to PlayerPrefs", GUILayout.Height(30)))
            {
                SaveSettings();
            }

            if (GUILayout.Button("Load from PlayerPrefs", GUILayout.Height(30)))
            {
                LoadCurrentValues();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                ResetToDefaults();
            }

            EditorGUILayout.Space();
            GUI.color = Color.red;
            if (GUILayout.Button("Clear All PlayerPrefs", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear All PlayerPrefs?", "Are you sure you want to delete all PlayerPrefs data? This will clear coins, unlocked skins, and settings.", "Yes", "No"))
                {
                    PlayerPrefs.DeleteAll();
                    LoadCurrentValues();
                    Debug.Log("PlayerPrefs cleared.");
                }
            }
            GUI.color = Color.white;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(ConstantString.SOUND_MASTER, masterVolume);
            PlayerPrefs.SetFloat(ConstantString.SOUND_MUSIC, musicVolume);
            PlayerPrefs.SetFloat(ConstantString.SOUND_SFX, sfxVolume);

            PlayerPrefs.SetString(ConstantString.SCREEN_MODE, screenMode);
            PlayerPrefs.SetString(ConstantString.RESOLUTION, resolution);
            PlayerPrefs.SetInt(ConstantString.FPS, fps);
            PlayerPrefs.SetInt(ConstantString.VSYNC, vSync ? 1 : 0);
            PlayerPrefs.SetFloat(ConstantString.BRIGHTNESS, brightness);
            PlayerPrefs.SetFloat(ConstantString.CONTRAST, contrast);

            PlayerPrefs.SetString(ConstantString.LANGUAGE, language);

            PlayerPrefs.Save();
            Debug.Log("Settings saved to PlayerPrefs.");
        }

        private void ResetToDefaults()
        {
            masterVolume = 1f;
            musicVolume = 1f;
            sfxVolume = 1f;
            screenMode = "Exclusive";
            resolution = "1920x1080";
            fps = 60;
            vSync = false;
            brightness = 0.5f;
            contrast = 0.5f;
            language = "en";
        }
    }
}
