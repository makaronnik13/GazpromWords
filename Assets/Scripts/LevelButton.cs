using UniRx;
using UnityEngine;
using Button = UnityEngine.UI.Button;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private LevelDataAsset levelDataAsset;

    [SerializeField] private GameObject selection;

    private MenuScript menu;
    
    void Start()
    {
        menu = FindObjectOfType<MenuScript>();
        
        GetComponent<Button>().onClick.AddListener(Click);
        
        menu.CurrentLevel.Subscribe(level =>
        {
            selection.SetActive(level == levelDataAsset);
        }).AddTo(this);
    }

    public void Click()
    {
        menu.CurrentLevel.Value = levelDataAsset;
    }
}
