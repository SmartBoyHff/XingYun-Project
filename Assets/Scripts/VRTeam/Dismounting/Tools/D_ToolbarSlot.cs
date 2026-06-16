using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class D_ToolbarSlot : MonoBehaviour
{
    [Header("工具栏数据")]
    public D_ToolItemData.ToolEntry toolData;
    [SerializeField] private Image iconImage;
    [SerializeField] private string handTag = "Hand";

    [Header("生成位置配置")]
    [SerializeField] private Transform customSpawnPoint;   // 自定义生成点（优先使用，可挂在图标上方）
    [SerializeField] private Vector3 spawnOffset = Vector3.zero; // 若未设 spawnPoint，则在图标位置基础上叠加此偏移

    public bool IsFilled { get; private set; } = true;

    // 防重复触发用的集合
    private HashSet<Collider> enteredHands = new HashSet<Collider>();

    public void Initialize(D_ToolItemData.ToolEntry entry)
    {
        toolData = entry;
        IsFilled = true;
        if (iconImage != null)
            iconImage.sprite = entry.icon;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 杜绝重复触发
        if (!IsFilled || !other.CompareTag(handTag) || enteredHands.Contains(other))
            return;
        enteredHands.Add(other);

        // 若想限制手中有工具时不生成，取消下行注释
        // if (GrabManager.Instance.IsHandOccupied) return;

        // 计算生成位置与旋转
        Vector3 spawnPos;
        Quaternion spawnRot;

        if (customSpawnPoint != null)
        {
            spawnPos = customSpawnPoint.position;
            spawnRot = customSpawnPoint.rotation;
        }
        else
        {
            spawnPos = iconImage.transform.position + iconImage.transform.TransformDirection(spawnOffset);
            spawnRot = iconImage.transform.rotation;
        }

        // 在世界坐标生成物体
        GameObject instance = Instantiate(toolData.prefab, spawnPos, spawnRot);

        // 确保挂载 GrabbableItem
        GrabbableItem item = instance.GetComponent<GrabbableItem>();
        if (item == null)
            item = instance.AddComponent<GrabbableItem>();
        item.itemID = toolData.itemID;
        item.originSlot = this;

        SetFilled(false);
    }

    private void OnTriggerExit(Collider other)
    {
        enteredHands.Remove(other);
    }

    public void ReturnItem(string returnedItemID)
    {
        if (!IsFilled && toolData.itemID == returnedItemID)
            SetFilled(true);
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
