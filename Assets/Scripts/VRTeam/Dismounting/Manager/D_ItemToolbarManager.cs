using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class D_ItemToolbarManager : MonoBehaviour
{
    // 现在直接持有具体工具的数据（由管理器动态填充）
    [SerializeField] private D_ToolItemData toolItemData;      // 数据资源
    [SerializeField] private GameObject slotPrefab;           // 挂载了 ToolbarSlot 的预制体
    [SerializeField] private Transform slotContainer;         // 父对象，如一个 Horizontal Layout Group

    private List<D_ToolbarSlot> slots = new List<D_ToolbarSlot>();

    private void Start()
    {
        GenerateSlots();
    }

    private void GenerateSlots()
    {
        if (toolItemData == null || slotPrefab == null || slotContainer == null) return;

        foreach (var entry in toolItemData.tools)
        {
            GameObject go = Instantiate(slotPrefab, slotContainer);
            D_ToolbarSlot slot = go.GetComponent<D_ToolbarSlot>();
            if (slot != null)
            {
                slot.Initialize(entry);
                slots.Add(slot);
            }
        }
    }

    public bool TryReturnItem(string itemID, D_ToolbarSlot originSlot)
    {
        // 优先归还来源槽位
        if (originSlot != null && !originSlot.IsFilled && originSlot.toolData.itemID == itemID)
        {
            originSlot.ReturnItem(itemID);
            return true;
        }

        // 否则查找第一个匹配的空槽
        foreach (var slot in slots)
        {
            if (!slot.IsFilled && slot.toolData.itemID == itemID)
            {
                slot.ReturnItem(itemID);
                return true;
            }
        }
        return false;
    }
}
