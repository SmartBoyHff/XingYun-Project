using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class D_ToolbarSlot : MonoBehaviour, IPointerClickHandler
{
    public D_ToolItemData.ToolEntry toolData;
    public Image iconImage;
    public bool IsFilled { get; private set; } = true;

    public void Initialize(D_ToolItemData.ToolEntry entry)
    {
        toolData = entry;
        IsFilled = true;
        if (iconImage != null)
            iconImage.sprite = entry.icon;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsFilled || toolData.prefab == null) return;
        if (D_GrabManager.Instance.IsHandOccupied())
        {
            Debug.Log("手中有工具，请先放回");
            return;
        }

        GameObject instance = Instantiate(toolData.prefab);
        D_GrabManager.Instance.GrabObject(instance, toolData.itemID, this);
        SetFilled(false);
    }

    public void ReturnItem(string returnedItemID)
    {
        if (IsFilled) return;
        if (toolData.itemID == returnedItemID)
        {
            SetFilled(true);
        }
    }

    public void SetFilled(bool filled)
    {
        IsFilled = filled;
        if (iconImage != null)
        {
            iconImage.sprite = filled ? toolData.icon : null;
            iconImage.enabled = filled;
        }
    }
}
