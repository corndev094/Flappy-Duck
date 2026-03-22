using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocalizationManager : Singleton<LocalizationManager> {
    IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        var savedLanguage = DataManager.Instance.GetLanguage();
        var local = LocalizationSettings.AvailableLocales.GetLocale(savedLanguage);
        if (local != null) LocalizationSettings.SelectedLocale = local;
    }
}