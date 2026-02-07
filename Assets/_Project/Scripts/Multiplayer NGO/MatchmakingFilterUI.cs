namespace Multiplayer
{
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// UI panel for custom matchmaking with filters
    /// </summary>
    public class MatchmakingFilterUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Filter Inputs")]
        [SerializeField] private TMP_Dropdown gameModeDropdown;
        [SerializeField] private TMP_InputField levelInput;
        [SerializeField] private Slider levelRangeSlider;
        [SerializeField] private TMP_Text levelRangeText;
        [SerializeField] private TMP_InputField regionInput;

        [Header("Buttons")]
        [SerializeField] private Button findMatchButton;
        [SerializeField] private Button cancelButton;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;

        private MatchmakingManager matchmaking;

        void Awake()
        {
            matchmaking = MatchmakingManager.Instance;

            // Setup buttons
            findMatchButton?.onClick.AddListener(OnFindMatchClicked);
            cancelButton?.onClick.AddListener(OnCancelClicked);

            // Setup slider
            if (levelRangeSlider != null)
            {
                levelRangeSlider.minValue = 0;
                levelRangeSlider.maxValue = 50;
                levelRangeSlider.value = 5;
                levelRangeSlider.wholeNumbers = true;
                levelRangeSlider.onValueChanged.AddListener(OnLevelRangeChanged);
            }

            // Setup dropdown
            SetupGameModeDropdown();

            // Initial state
            Hide();
        }

        #region Public Methods

        public void Show()
        {
            panel?.SetActive(true);
            LoadPlayerPreferences();
        }

        public void Hide()
        {
            panel?.SetActive(false);
        }

        #endregion

        #region Button Handlers

        private async void OnFindMatchClicked()
        {
            if (matchmaking == null)
            {
                SetStatus("Matchmaking manager not found!");
                return;
            }

            // Parse inputs
            if (!TryGetFilter(out var filter))
            {
                SetStatus("Invalid input values!");
                return;
            }

            // Save preferences
            SavePlayerPreferences(filter);

            // Start matchmaking
            SetStatus("Finding match...");
            SetButtonsInteractable(false);

            var result = await matchmaking.StartMatchmaking(filter);

            if (result != MatchingResult.Success)
            {
                SetButtonsInteractable(true);
            }
        }

        private void OnLevelRangeChanged(float value)
        {
            if (levelRangeText != null)
            {
                levelRangeText.text = $"±{value:F0}";
            }
        }

        private async void OnCancelClicked()
        {
            SetStatus("Cancelling...");
            SetButtonsInteractable(false);

            if (matchmaking != null)
            {
                await matchmaking.CancelMatchmaking();
            }

            SetButtonsInteractable(true);
            Hide();
        }

        #endregion

        #region Setup

        private void SetupGameModeDropdown()
        {
            if (gameModeDropdown == null) return;

            var modeNames = System.Enum.GetNames(typeof(GameMode));
            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var name in modeNames)
            {
                options.Add(new TMP_Dropdown.OptionData(name));
            }

            gameModeDropdown.ClearOptions();
            gameModeDropdown.AddOptions(options);
        }

        #endregion

        #region Helper Methods

        private bool TryGetFilter(out MatchmakingFilter filter)
        {
            filter = default;

            // Game Mode
            if (gameModeDropdown != null)
            {
                filter.GameMode = (GameMode)gameModeDropdown.value;
            }
            else
            {
                filter.GameMode = GameMode.Normal;
            }

            // Player Level
            if (levelInput != null && int.TryParse(levelInput.text, out int level))
            {
                filter.PlayerLevel = Mathf.Clamp(level, 1, 999);
            }
            else
            {
                Debug.LogWarning("[MatchmakingFilterUI] Invalid level input, using 1");
                filter.PlayerLevel = 1;
            }

            // Level Range
            if (levelRangeSlider != null)
            {
                filter.LevelRange = Mathf.RoundToInt(levelRangeSlider.value);
            }
            else
            {
                filter.LevelRange = 5;
            }

            // Region (optional)
            if (regionInput != null)
            {
                filter.Region = regionInput.text.Trim().ToLower();
            }
            else
            {
                filter.Region = "";
            }

            return true;
        }

        private void SetStatus(string text)
        {
            if (statusText != null)
            {
                statusText.text = text;
            }
            Debug.Log($"[MatchmakingFilterUI] {text}");
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (findMatchButton != null)
                findMatchButton.interactable = interactable;
            if (cancelButton != null)
                cancelButton.interactable = interactable;
        }

        #endregion

        #region Player Preferences

        private void SavePlayerPreferences(MatchmakingFilter filter)
        {
            PlayerPrefs.SetInt("MM_GameMode", (int)filter.GameMode);
            PlayerPrefs.SetInt("MM_Level", filter.PlayerLevel);
            PlayerPrefs.SetInt("MM_LevelRange", filter.LevelRange);
            PlayerPrefs.SetString("MM_Region", filter.Region);
            PlayerPrefs.Save();
        }

        private void LoadPlayerPreferences()
        {
            // Game Mode
            if (gameModeDropdown != null && PlayerPrefs.HasKey("MM_GameMode"))
            {
                gameModeDropdown.value = PlayerPrefs.GetInt("MM_GameMode", 0);
            }

            // Level
            if (levelInput != null)
            {
                int savedLevel = PlayerPrefs.GetInt("MM_Level", 1);
                levelInput.text = savedLevel.ToString();
            }

            // Level Range
            if (levelRangeSlider != null && PlayerPrefs.HasKey("MM_LevelRange"))
            {
                levelRangeSlider.value = PlayerPrefs.GetInt("MM_LevelRange", 5);
            }

            // Region
            if (regionInput != null && PlayerPrefs.HasKey("MM_Region"))
            {
                regionInput.text = PlayerPrefs.GetString("MM_Region", "");
            }
        }

        #endregion
    }
}
