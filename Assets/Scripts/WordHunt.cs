using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WordHunt : MonoBehaviour
{
    public static WordHunt instance;

    public Action<List<LetterObjectScript>> FoundWord;
    public Action Finish;

    public Action<LevelData> OnLevelStarted;
    
    private string[,] lettersGrid;
    private LetterObjectScript[,] letters;
    private string alphabet = "абвгдеёжзийклмнопрстуфхцчэюяьъшщы";

    [Header("Settings")] public bool invertedWordsAreValid;

    [Header("List of Words")] public List<string> words = new List<string>();
    public List<string> insertedWords = new List<string>();

    [Header("Grid Settings")] public Vector2 gridSize;

    [Header("Cell Settings")] public Vector2 cellSize;
    public Vector2 cellSpacing;

    [Header("Public References")] public LetterObjectScript letterPrefab;
    public Transform gridTransform;

    [Header("Game Detection")] public string word;
    public Vector2 orig;
    public Vector2 dir;
    public bool activated;

    [HideInInspector] public List<LetterObjectScript> highlightedObjects = new List<LetterObjectScript>();

    private LevelData currentLevel;
    
    private void Awake()
    {
        instance = this;
    }

    public void Setup(LevelData data)
    {
        currentLevel = data;
        OnLevelStarted?.Invoke(data);
        words = currentLevel.Words.OrderBy(_=>Guid.NewGuid()).ToList();
        InitializeGrid();
        InsertWordsOnGrid();
        RandomizeEmptyCells();
    }
    
    private void InitializeGrid()
    {
        lettersGrid = new string[(int)gridSize.x, (int)gridSize.y];
        letters = new LetterObjectScript[(int)gridSize.x, (int)gridSize.y];
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                lettersGrid[x, y] = "";
                var letter = Instantiate(letterPrefab, transform.GetChild(0));
                letter.gameObject.name = x + "-" + y;
                letters[x, y] = letter;
            }
        }

        ApplyGridSettings();
    }

    private void ApplyGridSettings()
    {
        var gridLayout = gridTransform.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = cellSpacing;
        int cellSizeX = (int)gridLayout.cellSize.x + (int)gridLayout.spacing.x;
        transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(cellSizeX * gridSize.x, 0);
    }

    private void InsertWordsOnGrid()
    {
        var rng = new System.Random();
        foreach (var raw in words)
        {
            string w = raw.Trim().ToLower();
            if (string.IsNullOrEmpty(w)) continue;
            if (invertedWordsAreValid && rng.NextDouble() < 0.5) w = Reverse(w);

            bool placed = TryPlaceWordWithBends(w, rng);
            if (placed) insertedWords.Add(w);
        }
    }

    private bool TryPlaceWordWithBends(string w, System.Random rng)
    {
        var starts = new List<Vector2Int>();
        for (int x = 0; x < gridSize.x; x++)
        for (int y = 0; y < gridSize.y; y++)
            starts.Add(new Vector2Int(x, y));
        for (int i = starts.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (starts[i], starts[j]) = (starts[j], starts[i]);
        }

        foreach (var start in starts)
        {
            if (!CellMatches(start.x, start.y, w[0])) continue;
            var path = new List<Vector2Int> { start };
            var visited = new HashSet<(int, int)>();
            visited.Add((start.x, start.y));
            if (DFSPlace(w, 1, start, path, visited, rng))
            {
                CommitWordPath(w, path);
                return true;
            }
        }

        return false;
    }

    private static readonly Vector2Int[] DIRS =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private bool DFSPlace(string w, int index, Vector2Int cur, List<Vector2Int> path, HashSet<(int, int)> visited,
        System.Random rng)
    {
        if (index >= w.Length) return true;

        var dirs = DIRS.ToList();
        for (int i = dirs.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }

        foreach (var d in dirs)
        {
            var nxt = new Vector2Int(cur.x + d.x, cur.y + d.y);
            if (!InBounds(nxt)) continue;
            if (visited.Contains((nxt.x, nxt.y))) continue;
            if (!CellMatches(nxt.x, nxt.y, w[index])) continue;

            path.Add(nxt);
            visited.Add((nxt.x, nxt.y));

            if (DFSPlace(w, index + 1, nxt, path, visited, rng)) return true;

            path.RemoveAt(path.Count - 1);
            visited.Remove((nxt.x, nxt.y));
        }

        return false;
    }

    private bool InBounds(Vector2Int p)
    {
        return p.x >= 0 && p.y >= 0 && p.x < (int)gridSize.x && p.y < (int)gridSize.y;
    }

    private bool CellMatches(int x, int y, char c)
    {
        var cell = lettersGrid[x, y];
        return string.IsNullOrEmpty(cell) || cell.Equals(c.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private void CommitWordPath(string w, List<Vector2Int> path)
    {
        for (int i = 0; i < w.Length; i++)
        {
            var p = path[i];
            string ch = w[i].ToString();
            lettersGrid[p.x, p.y] = ch;
            letters[p.x, p.y].Set(ch.ToUpper());
        }
    }

    private void RandomizeEmptyCells()
    {
        var rn = new System.Random();
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (lettersGrid[x, y] == string.Empty)
                {
                    lettersGrid[x, y] = alphabet[rn.Next(alphabet.Length)].ToString();
                    letters[x, y].Set(lettersGrid[x, y].ToUpper());
                }
            }
        }
    }

    public void LetterClick(int x, int y, bool state)
    {
        activated = state;
        if (state)
        {
            ClearWordSelection();
            orig = new Vector2(x, y);
            dir = new Vector2(-1, -1);
            highlightedObjects.Add(letters[x, y]);
            letters[x, y].GetComponent<Image>().color =
                HighlightBehaviour.instance.colors[HighlightBehaviour.instance.colorCounter];
        }
        else
        {
            ValidateWord();
        }
    }

    public void LetterHover(int x, int y)
    {
        if (!activated) return;
        if (highlightedObjects.Count == 0)
        {
            highlightedObjects.Add(letters[x, y]);
            letters[x, y].GetComponent<Image>().color =
                HighlightBehaviour.instance.colors[HighlightBehaviour.instance.colorCounter];
            return;
        }

        var last = highlightedObjects[highlightedObjects.Count - 1];
        var parts = last.name.Split('-');
        int lx = int.Parse(parts[0]);
        int ly = int.Parse(parts[1]);

        if (lx == x && ly == y) return;

        if (Mathf.Abs(lx - x) <= 1 && Mathf.Abs(ly - y) <= 1 &&
            !(Mathf.Abs(lx - x) == 1 && Mathf.Abs(ly - y) == 1 && false))
        {
            bool isAdjacent4 = (Mathf.Abs(lx - x) + Mathf.Abs(ly - y)) == 1;
            if (!isAdjacent4) return;

            var nextLetter = letters[x, y];

            if (highlightedObjects.Count >= 2)
            {
                var prev = highlightedObjects[highlightedObjects.Count - 2];
                var prevParts = prev.name.Split('-');
                int px = int.Parse(prevParts[0]);
                int py = int.Parse(prevParts[1]);
                if (px == x && py == y)
                {
                    last.Hilight(false);
                    highlightedObjects.RemoveAt(highlightedObjects.Count - 1);
                    return;
                }
            }

            if (!highlightedObjects.Contains(nextLetter))
            {
                nextLetter.GetComponent<Image>().color =
                    HighlightBehaviour.instance.colors[HighlightBehaviour.instance.colorCounter];
                highlightedObjects.Add(nextLetter);
            }
        }
    }

    private void ValidateWord()
    {
        word = string.Empty;
        foreach (var letter in highlightedObjects) word += letter.Symbol.ToLower();

        if (insertedWords.Contains(word) || insertedWords.Contains(Reverse(word)))
        {
            foreach (var letter in highlightedObjects) letter.Hilight(true);
            FoundWord?.Invoke(highlightedObjects);
            Debug.Log("<b>" + word.ToUpper() + "</b> найдено!");
            insertedWords.Remove(word);
            insertedWords.Remove(Reverse(word));
            if (insertedWords.Count <= 0) Finish?.Invoke();
        }
        else
        {
            ClearWordSelection();
        }
    }

    private void HighlightSelectedLetters(int x, int y) { }

    private void ClearWordSelection()
    {
        foreach (var letter in highlightedObjects) letter.Hilight(false);
        highlightedObjects.Clear();
    }
    
    public static string Reverse(string s)
    {
        var a = s.ToCharArray();
        Array.Reverse(a);
        return new string(a);
    }
}
