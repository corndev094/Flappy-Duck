using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettingsManager : Singleton<SettingsManager>
{
    [SerializeField] private Volume globalVolume;

    protected override void Awake()
    {
        base.Awake();
        globalVolume ??= FindFirstObjectByType<Volume>();
    }

    void Start()
    {
        globalVolume.profile.TryGet<ColorAdjustments>(out var colorAdjustments);
        // screen mode
        // resolution
        Application.targetFrameRate = DataManager.Instance.GetFps();
        QualitySettings.vSyncCount = DataManager.Instance.GetVSync() ? 1 : 0;
        colorAdjustments.contrast.value = DataManager.Instance.GetContrast();
        colorAdjustments.postExposure.value = DataManager.Instance.GetBrightness();
        SoundManager.Instance.SetMasterVolume(DataManager.Instance.GetMasterVolume());
        SoundManager.Instance.SetMusicVolume(DataManager.Instance.GetMusicVolume());
        SoundManager.Instance.SetSfxVolume(DataManager.Instance.GetSfxVolume());
    }
}