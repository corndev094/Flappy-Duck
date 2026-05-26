using Cysharp.Threading.Tasks;
using UnityEngine;

public class SettingsManager : Singleton<SettingsManager>
{
    void Start()
    {
        UIManager.Instance.TryGetMenu(Menu.Settings, out var menu);
        if (menu != null && menu is SettingsMenu settingsMenu)
        {
            settingsMenu.gameObject.SetActive(true);
            settingsMenu.UpdateMenu();
            settingsMenu.gameObject.SetActive(false);
        }
    }
}