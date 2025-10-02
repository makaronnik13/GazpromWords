using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UniRx;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour {

    public ReactiveProperty<LevelDataAsset> CurrentLevel = new ReactiveProperty<LevelDataAsset>();
    
    private CanvasGroup canvas;
    
    [SerializeField] private Transform miniMenu;
    [SerializeField] private Transform levelMenu;
    [SerializeField] private Button _startButton;
    [SerializeField] private GameObject _hint; 
    
    private void Start()
    {
        canvas = GetComponent<CanvasGroup>();
        canvas.alpha = 1;

        CurrentLevel.Subscribe(level =>
        {
            _startButton.interactable = level != null;
            _hint.SetActive(level == null);
        });

        _startButton.OnClickAsObservable().Subscribe(_ =>
        {
            StartGame();
        }).AddTo(this);
    }

    public void StartGame(){
        
        WordHunt.instance.Setup(CurrentLevel.Value.level);
        canvas.alpha = 0;
        canvas.blocksRaycasts = false;
        miniMenu.DOMoveY(0,.6f).SetEase(Ease.OutBack);
        levelMenu.DOMoveY(-200f,.6f).SetEase(Ease.OutBack);
    }

    public void Home(){
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }

}
