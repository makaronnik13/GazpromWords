using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DG.Tweening;
using UnityEngine;
using TMPro;

public class LevelHint : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private List<Color> colors;

    private LevelData _level;
    private string _originalText;
    private readonly HashSet<string> _highlighted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private void Start()
    {
        WordHunt.instance.OnLevelStarted += LevelStarted;
        WordHunt.instance.FoundWord += WordFound;
    }

    private void OnDestroy()
    {
        if (WordHunt.instance != null)
        {
            WordHunt.instance.OnLevelStarted -= LevelStarted;
            WordHunt.instance.FoundWord -= WordFound;
        }
    }

    private void WordFound(List<LetterObjectScript> letters)
    {
        string foundWord = string.Concat(letters.Select(l => l.Symbol)).ToLower();
        _highlighted.Add(_level.HilightedWords[_level.Words.IndexOf(foundWord)]);
        RebuildText();
    }

    private void LevelStarted(LevelData level)
    {
        (transform as RectTransform).DOAnchorPosY(0,.6f).SetEase(Ease.OutExpo);
        _level = level;
        _originalText = level.Text ?? string.Empty;
        _highlighted.Clear();
        text.text = _originalText;
    }

    [ContextMenu("Highlight all")]
    public void HighlightAll()
    {
        if (_level == null) return;
        foreach (var w in _level.HilightedWords) _highlighted.Add(w);
        RebuildText();
    }

    private void RebuildText()
    {
        if (_level == null || _highlighted.Count == 0)
        {
            text.text = _originalText ?? string.Empty;
            return;
        }

        var words = _highlighted
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .OrderByDescending(w => w.Length)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (words.Count == 0)
        {
            text.text = _originalText ?? string.Empty;
            return;
        }

        string pattern = string.Join("|", words.Select(Regex.Escape));
        string result = Regex.Replace(
            _originalText,
            pattern,
            m =>
            {
                string w = m.Value;
                var c = GetColorFor(w);
                string hex = ColorUtility.ToHtmlStringRGB(c);
                return $"<b><size=115%><color=#{hex}>{w}</color></size></b>";
            },
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );

        text.text = result;
    }

    private Color GetColorFor(string word)
    {
        if (_level == null || _level.HilightedWords == null || _level.HilightedWords.Count == 0 || colors == null || colors.Count == 0)
            return Color.yellow;
        
        return colors[_level.HilightedWords.IndexOf(word)];
    }
}
