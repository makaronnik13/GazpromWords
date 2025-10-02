using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Extensions;
using DG.Tweening;

public class HighlightBehaviour : MonoBehaviour
{
    public static HighlightBehaviour instance;

    public UIRoundedLineFromRects lineRendererPrefab;

    public Color[] colors;
    public int colorCounter;

    private void Awake()
    {
        instance = this;
        WordHunt.instance.FoundWord += SetLineRenderer;
    }

    void SetLineRenderer(List<LetterObjectScript> points)
    {
        UIRoundedLineFromRects line = Instantiate(lineRendererPrefab, transform);
        line.SetColor(colors[colorCounter]);
        colorCounter = (colorCounter == colors.Length - 1) ? 0 : colorCounter + 1;
        line.SetPositions(points.Select(p=>p.GetComponent<RectTransform>()).ToArray());
        //line.transform.DOScale(0, 0.3f).From().SetEase(Ease.OutBack);
    }

    private void OnDestroy()
    {
        WordHunt.instance.FoundWord -= SetLineRenderer;
    }
}







