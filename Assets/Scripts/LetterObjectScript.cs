using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LetterObjectScript : MonoBehaviour, IPointerDownHandler,IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {

    public delegate void ClickAction();
    public event ClickAction MouseDown;
    public event ClickAction MouseExit;
    public event ClickAction MouseEnter;

    [SerializeField] private TMP_Text _text;
    [SerializeField] private Image _image;
    public string Symbol => _text.text;

    public void OnPointerDown(PointerEventData eventData)
    {
        WordHunt.instance.LetterClick((int)pos().x, (int)pos().y, true);

        MouseDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        WordHunt.instance.LetterClick((int)pos().x, (int)pos().y, false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

        WordHunt.instance.LetterHover((int)pos().x, (int)pos().y);

        MouseEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MouseExit();
    }

    Vector2 pos()
    {
        string[] numbers = transform.name.Split('-');
        int x = int.Parse(numbers[0]);
        int y = int.Parse(numbers[1]);

        Vector2 pos = new Vector2(x, y);

        return pos;
    }

    public void Set(string str)
    {
        _text.text = str;
    }

    public void Hilight(bool move)
    {
        _image.color = Color.white;
        if (move)
        {
            transform.DOPunchScale(-Vector3.one, 0.2f, 10, 1);   
        }
    }
}
