using TMPro;
using UnityEngine;

public class DebugUI : Singleton<DebugUI> {
    [SerializeField] private TMP_Text fpsText;

    private float updateFpsInterval = 0.5f;

    private void Update() {
        updateFpsInterval -= Time.unscaledDeltaTime;
        if (updateFpsInterval <= 0f) {
            updateFpsInterval = 0.5f;
            UpdateFPS(1f / Time.unscaledDeltaTime);
        }
    }

    public void UpdateFPS(float fps) {
        if (fpsText != null)
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
    }
}