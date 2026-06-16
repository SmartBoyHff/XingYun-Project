using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
public class D_GrabManager : MonoBehaviour
{
    public static D_GrabManager Instance { get; private set; }

    private string currentToolID = string.Empty;
    private GrabbableItem currentItem;

    public bool IsHandOccupied => !string.IsNullOrEmpty(currentToolID);
    public string CurrentGrabbedItemID => currentToolID;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 工具被抓住时由 GrabbableItem 调用
    /// </summary>
    public void ReportGrabbed(GrabbableItem item)
    {
        currentToolID = item.itemID;
        currentItem = item;
    }

    /// <summary>
    /// 工具被释放时由 GrabbableItem 调用
    /// </summary>
    public void ReportReleased(GrabbableItem item)
    {
        if (currentItem == item)
        {
            currentToolID = string.Empty;
            currentItem = null;
        }
    }

    /// <summary>
    /// 回收区销毁工具时强制清除状态
    /// </summary>
    public void ClearCurrentTool()
    {
        if (currentItem != null)
        {
            // 如果工具仍被某只手抓着，先释放
            if (currentItem.TryGetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>(out var interactable) && interactable.isSelected)
            {
                var manager = interactable.interactionManager;
                if (manager != null)
                    manager.SelectExit(interactable.selectingInteractor, interactable);
            }
        }
        currentToolID = string.Empty;
        currentItem = null;
    }
}
