using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class D_ToolRecoveryZone : MonoBehaviour
{
    [SerializeField] private D_ItemToolbarManager toolbarManager; // 工具栏管理器引用

    private void OnTriggerEnter(Collider other)
    {
        GrabbableItem item = other.GetComponent<GrabbableItem>();
        if (item != null && !string.IsNullOrEmpty(item.itemID))
        {
            // 通知工具栏管理器放回
            if (toolbarManager.TryReturnItem(item.itemID, item.originSlot))
            {
                // 强制清除手部抓取状态
                D_GrabManager.Instance.ForceClearCurrent();
                Destroy(item.gameObject);
            }
            else
            {
                Debug.Log("放回失败，所有槽位已满或不属于此工具栏");
            }
        }
    }
}
