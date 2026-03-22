using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

public class LocalizeStepper : Stepper<string> {
    protected override void InvokeEvent()
    {
        var child = content.GetChild(_currentSelectedItem);
        var ls = child.GetComponentInChildren<LocalizeStringEvent>()?.StringReference;
        if (child && ls != null)
        {
            var table = LocalizationSettings.StringDatabase.GetTable(ls.TableReference);
            var entry = table.SharedData.GetEntry(ls.TableEntryReference.KeyId);
            OnItemSelected?.Invoke(entry.Key);
        }
    }
}