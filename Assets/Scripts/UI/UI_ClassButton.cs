// Scripts/UI/UI_ClassButton.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // Para o Action

public class UI_ClassButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI classNameText;
    [SerializeField] private Image classIconImage;
    [SerializeField] private Button button;

    private PlayerClass _playerClass;

    // Um evento para avisar o UIManager que este botão foi clicado
    public event Action<PlayerClass> OnClassSelected;

    public void Setup(PlayerClass playerClass, Action<PlayerClass> onClickAction)
    {
        this._playerClass = playerClass;
        this.OnClassSelected = onClickAction;

        classNameText.text = playerClass.className;
        if (playerClass.icon)
        {
            classIconImage.sprite = playerClass.icon;
        }

        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        OnClassSelected?.Invoke(_playerClass);
    }
}