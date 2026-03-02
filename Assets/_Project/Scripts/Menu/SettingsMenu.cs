using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SettingsMenu : ABaseMenu {
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private Button returnBtn;
    [SerializeField] private Volume globalVolume;
    
    [Header("Data")]
    [SerializeField] private StringListSO screenModeList;
    [SerializeField] private StringListSO resolutionList;
    [SerializeField] private IntListSO fpsList;

    [Header("Graphic")]
    [SerializeField] private StringStepper screenModeStepper;
    [SerializeField] private StringStepper resolutionStepper;
    [SerializeField] private IntStepper fpsStepper;
    [SerializeField] private BoolStepper vSyncStepper;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Button graphicResetBtn;
    [SerializeField] private Slider contrastSlider;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Button audioResetBtn;

    private ColorAdjustments colorAdjustments;

    void Awake()
    {
        Setup();
        if (globalVolume == null) globalVolume = FindFirstObjectByType<Volume>();
        if (globalVolume != null)
        {
            if (globalVolume.profile.TryGet<ColorAdjustments>(out var ca))
            {
                colorAdjustments = ca;
            }
        }
    }

    private void OnEnable() {
        returnBtn.onClick.AddListener(ReturnHome);
        screenModeStepper.OnItemSelected.AddListener(OnScreenModeChanged);
        resolutionStepper.OnItemSelected.AddListener(OnResolutionChanged);
        fpsStepper.OnItemSelected.AddListener(OnFpsChanged);
        vSyncStepper.OnItemSelected.AddListener(OnVSyncChanged);
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        contrastSlider.onValueChanged.AddListener(OnContrastChanged);
        graphicResetBtn.onClick.AddListener(ResetGrahpicSettings);
        audioResetBtn.onClick.AddListener(ResetAudioSettings);
    }

    private void OnDisable() {
        returnBtn.onClick.RemoveListener(ReturnHome);
        screenModeStepper.OnItemSelected.RemoveListener(OnScreenModeChanged);
        resolutionStepper.OnItemSelected.RemoveListener(OnResolutionChanged);
        fpsStepper.OnItemSelected.RemoveListener(OnFpsChanged);
        vSyncStepper.OnItemSelected.RemoveListener(OnVSyncChanged);
        masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
        contrastSlider.onValueChanged.RemoveListener(OnContrastChanged);
        graphicResetBtn.onClick.RemoveListener(ResetGrahpicSettings);
        audioResetBtn.onClick.RemoveListener(ResetAudioSettings);
    }

    public async override UniTask Open(Action onOpenFinish = null)
    {
        await base.Open(onOpenFinish);
        UpdateMenu();
    }

    private void Setup()
    {
        screenModeStepper.SetupList(screenModeList.items);
        resolutionStepper.SetupList(resolutionList.items);
        fpsStepper.SetupList(fpsList.items);
    }

    public void UpdateUI()
    {
        // Graphic
        screenModeStepper.SetSelectedItem(DataManager.Instance.GetScreenMode(), false);
        resolutionStepper.SetSelectedItem(DataManager.Instance.GetResolution(), false);
        fpsStepper.SetSelectedItem(DataManager.Instance.GetFps(), false);
        vSyncStepper.SetSelectedItem(DataManager.Instance.GetVSync(), false);
        brightnessSlider.value = DataManager.Instance.GetBrightness();
        contrastSlider.value = DataManager.Instance.GetContrast();

        // Audio
        masterVolumeSlider.value = DataManager.Instance.GetMasterVolume();
        musicVolumeSlider.value = DataManager.Instance.GetMusicVolume();
        sfxVolumeSlider.value = DataManager.Instance.GetSfxVolume();
    }

    public void UpdateMenu()
    {
        OnMasterVolumeChanged(DataManager.Instance.GetMasterVolume());
        OnMusicVolumeChanged(DataManager.Instance.GetMusicVolume());
        OnSfxVolumeChanged(DataManager.Instance.GetSfxVolume());
        OnScreenModeChanged(DataManager.Instance.GetScreenMode());
        OnResolutionChanged(DataManager.Instance.GetResolution());
        OnFpsChanged(DataManager.Instance.GetFps());
        OnVSyncChanged(DataManager.Instance.GetVSync());
        OnBrightnessChanged(DataManager.Instance.GetBrightness());
        OnContrastChanged(DataManager.Instance.GetContrast());
        UpdateUI();
    }

    public void OnScreenModeChanged(string mode)
    {
        FullScreenMode m = Screen.fullScreenMode;
        switch (mode)
        {
            case "Exclusive":
                m = FullScreenMode.ExclusiveFullScreen;
                break;
            case "Windowed":
                m = FullScreenMode.Windowed;
                break;
        }
        Screen.SetResolution(Screen.width, Screen.height, m);
        DataManager.Instance.SaveScreenMode(mode);
    }

    private void OnResolutionChanged(string resolution)
    {
        var res = resolution.Split('x');
        if (res.Length != 2) return;
        if (int.TryParse(res[0].Trim(), out int width) && int.TryParse(res[1].Trim(), out int height))
        {
            Screen.SetResolution(width, height, Screen.fullScreenMode);
            DataManager.Instance.SaveResolution(resolution);
        }
    }
    private void OnFpsChanged(int fps)
    {
        Application.targetFrameRate = fps;
        DataManager.Instance.SaveFps(fps);
    }

    private void OnVSyncChanged(bool vSync)
    {
        QualitySettings.vSyncCount = vSync ? 1 : 0;
        DataManager.Instance.SaveVSync(vSync ? 1 : 0);
    }

    private async void ReturnHome()
    {
        await UIManager.Instance.SwitchToMenu(Menu.Main);
    }

    private void OnMasterVolumeChanged(float value)
    {
        SoundManager.Instance.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        SoundManager.Instance.SetMusicVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        SoundManager.Instance.SetSfxVolume(value);
    }

    private void OnBrightnessChanged(float value)
    {
        if (globalVolume != null)
        {
            float exposure = Mathf.Lerp(-1f, 1f, value);
            colorAdjustments.postExposure.value = exposure;
            DataManager.Instance.SaveBrightness(value);
        }
    }

    private void OnContrastChanged(float value)
    {
        if (contrastSlider != null)
        {
            float contrast = Mathf.Lerp(-50f, 50f, value);
            colorAdjustments.contrast.value = contrast;
            DataManager.Instance.SaveContrast(value);
        }
    }

    private void ResetGrahpicSettings()
    {
        screenModeStepper.SetSelectedItem("Exclusive", false);
        resolutionStepper.SetSelectedItem("1920x1080", false);
        fpsStepper.SetSelectedItem(60, false);
        vSyncStepper.SetSelectedItem(false, false);
        brightnessSlider.value = 0.5f;
        contrastSlider.value = 0.5f;
    }

    private void ResetAudioSettings()
    {
        OnMasterVolumeChanged(1f);
        OnMusicVolumeChanged(1f);
        OnSfxVolumeChanged(1f);
        masterVolumeSlider.value = 1f;
        musicVolumeSlider.value = 1f;
        sfxVolumeSlider.value = 1f;
    }
}