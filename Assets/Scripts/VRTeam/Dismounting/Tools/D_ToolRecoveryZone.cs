using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class D_ToolRecoveryZone : MonoBehaviour
{
    [SerializeField] private D_ItemToolbarManager toolbarManager;

    private void OnTriggerEnter(Collider other)
    {
        GrabbableItem item = other.GetComponent<GrabbableItem>();
        if (item != null && !string.IsNullOrEmpty(item.itemID))
        {
            if (toolbarManager.TryReturnItem(item.itemID, item.originSlot))
            {
                // 如果正在被手抓着，先清除抓取状态
                if (D_GrabManager.Instance.CurrentGrabbedItemID == item.itemID)
                    D_GrabManager.Instance.ClearCurrentTool();
                Destroy(item.gameObject);
            }
        }
    }
}
