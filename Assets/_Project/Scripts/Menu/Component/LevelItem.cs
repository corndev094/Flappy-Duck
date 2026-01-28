using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelItem : MonoBehaviour {
    [SerializeField] private Image thumbnail;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button levelButton;

    private LevelSO levelData;
    private LevelMapMenu levelMapMenu;

    public void Setup(LevelSO levelData, Action<LevelSO> onClick, bool isUnlocked)
    {
        this.levelData = levelData;
        thumbnail.sprite = levelData.Thumbnail;
        nameText.SetText($"Level {levelData.ID}");
        
        thumbnail.color = isUnlocked ? Color.white : Color.gray;
        
        if (levelButton != null)
        {
            levelButton.interactable = isUnlocked;
            levelButton.onClick.RemoveAllListeners();
            
            if (isUnlocked)
            {
                levelButton.onClick.AddListener(() => onClick?.Invoke(levelData));
            }
        }
    }
    
    public void SetLevelMapMenu(LevelMapMenu menu)
    {
        levelMapMenu = menu;
    }
}