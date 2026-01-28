using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoolStepper : Stepper<bool> {
    protected override void Awake()
    {
        items = new() { true, false };
        if (direction == Direction.Horizontal)
        {
            content.pivot = new Vector2(0, 0.5f);
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(0, 1);
            content.offsetMin = new Vector2(content.offsetMin.x, 0);
            content.offsetMax = new Vector2(content.offsetMax.x, 0);
            var contentWidth = (viewPort.rect.width * 2) + (spacing * (2 - 1));
            content.sizeDelta = new Vector2(contentWidth, content.sizeDelta.y);
        }
        else if (direction == Direction.Vertical)
        {
            content.pivot = new Vector2(0.5f, 1);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.offsetMin = new Vector2(content.offsetMin.x, 0);
            content.offsetMax = new Vector2(content.offsetMax.x, 0);
            var contentHeight = (viewPort.sizeDelta.y * 2) + (spacing * (2 - 1));
            content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        }

        content.anchoredPosition = Vector2.zero;

        if (!content.TryGetComponent(out HorizontalOrVerticalLayoutGroup layoutGroup))
        {
            if (direction == Direction.Horizontal)
            {
                layoutGroup = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            else
            {
                layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }
        }
        
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.spacing = spacing;

        var item = Instantiate(itemPrefab, content);
        item.name = "True";
        item.GetComponentInChildren<TMP_Text>().SetText("True");
        item = Instantiate(itemPrefab, content);
        item.name = "False";
        item.GetComponentInChildren<TMP_Text>().SetText("False");
    }
}