using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler
{
    private RectTransform slotRect;

    void Awake()
    {
        slotRect = GetComponent<RectTransform>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Debug.Log("OnDrop");
        if (eventData.pointerDrag != null)
        {
            eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition = slotRect.anchoredPosition;
        }
    }
}
