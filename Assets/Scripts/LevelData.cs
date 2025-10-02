using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelData
{
    [TextArea]
    public string Text;
    public List<string> Words;
    public List<string> HilightedWords;
}
