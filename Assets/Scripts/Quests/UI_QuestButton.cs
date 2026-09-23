// Cliente/Scripts/UI/Quests/UI_QuestButton.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class UI_QuestButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private Image questIcon; // Opcional, para o '!' ou '?'

    private Quest _quest;
    private Action _onClickAction;
    private Action<Quest> _onClickActionWithQuest;

    public void Setup(Quest quest, Sprite icon, Action onClickCallback)
    {
        _quest = quest;
        _onClickAction = onClickCallback;
        _onClickActionWithQuest = null; // Garan

        questNameText.text = quest.QuestName;
        if (questIcon != null)
        {
            questIcon.sprite = icon;
            questIcon.enabled = icon != null;
        }

        GetComponent<Button>().onClick.AddListener(OnClick);
    }

     public void Setup(Quest quest, Sprite icon, Action<Quest> onClickCallback)
    {
        _quest = quest;
        _onClickAction = null;
        _onClickActionWithQuest = onClickCallback;

        questNameText.text = quest.QuestName;
        if (questIcon != null)
        {
            questIcon.sprite = icon;
            questIcon.enabled = icon != null;
        }

        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        _onClickAction?.Invoke();
        _onClickActionWithQuest?.Invoke(_quest);
    }
}