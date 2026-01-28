using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class TMP_CurvedText : MonoBehaviour
{
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 0);
    public float curveHeight = 50f;
    public float curveScale = 1f;

    TMP_Text text;

    void OnEnable()
    {
        text = GetComponent<TMP_Text>();
        ApplyCurve();
    }

    void OnValidate()
    {
        if (!text) text = GetComponent<TMP_Text>();
        ApplyCurve();
    }

    void LateUpdate()
    {
        ApplyCurve();
    }

    void ApplyCurve()
    {
        if (!text) return;

        text.ForceMeshUpdate();
        var textInfo = text.textInfo;

        if (textInfo.characterCount == 0) return;

        float boundsMinX = text.bounds.min.x;
        float boundsMaxX = text.bounds.max.x;
        float width = boundsMaxX - boundsMinX;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

            Vector3 center = (verts[vertIndex] + verts[vertIndex + 2]) * 0.5f;

            for (int j = 0; j < 4; j++)
                verts[vertIndex + j] -= center;

            float normalizedX = (center.x - boundsMinX) / width;
            float yOffset = curve.Evaluate(normalizedX * curveScale) * curveHeight;

            Vector3 offset = new Vector3(0, yOffset, 0);

            for (int j = 0; j < 4; j++)
                verts[vertIndex + j] += center + offset;
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}