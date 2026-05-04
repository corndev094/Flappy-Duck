using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LanguagePopup : ABasePopup {
    [SerializeField] private RectTransform panel;
    [Header("Language Settings")]
    [SerializeField] private LocalizeStepper languageStepper;
    [SerializeField] private List<LanguageReference> languageReferences = new();

    protected override void Awake()
    {
        base.Awake();
        languageStepper.OnItemSelected.AddListener(OnStepperChanged);
    }

    void OnValidate()
    {
        if(languageReferences.Count > LocalizationSettings.AvailableLocales.Locales.Count)
        {
            do
            {
                languageReferences.RemoveAt(languageReferences.Count - 1);
                Debug.LogWarning("LanguageReference count is greater than available languages, removing last element");
            } while (languageReferences.Count > LocalizationSettings.AvailableLocales.Locales.Count);
        }
    }

    protected override async UniTask PlayOpenTransition()
    {
        panel.localScale = new Vector3(0, 1, 1);
        await panel.DOScaleX(1, 0.5f).SetEase(Ease.OutElastic).AsyncWaitForCompletion();
    }

    [Button]
    private void AutoLocalsReference()
    {
        languageReferences.Clear();
        for (var i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
        {
            languageReferences.Add(new LanguageReference() { key = LocalizationSettings.AvailableLocales.Locales[i].Identifier.Code, localizeStringReference = null });
        }
        Debug.Log("Localize reference completed!");
    }

    private void OnStepperChanged(string localeCode)
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
            DataManager.Instance.SaveLanguage(locale.Identifier.Code);
        }
    }

    public async void UpdateUI()
    { 
        languageStepper.SetSelectedItem(DataManager.Instance.GetLanguage(), false);
    }

    [Button]
    void LogTest()
    {
        Debug.Log($"{0:1|2}");
        Debug.Log($"{0:choose(1|2|3):one|two|three}");
    }

    [Serializable]
    public struct LanguageReference
    {
        public string key;
        public LocalizeStringEvent localizeStringReference;
    }
}