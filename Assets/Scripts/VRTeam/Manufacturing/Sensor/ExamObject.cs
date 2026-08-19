using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ExamObject : MonoBehaviour
{
    [Header("考核模式缩比（仅在槽内生效）")]
    public Vector3 grabScale = new Vector3(1f, 1f,1f);

    [HideInInspector] public string itemName;
    public PlacementSlot currentSlot { get; private set; }

    private Vector3 originalScale;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        originalScale = transform.localScale;
        grabInteractable = GetComponent<XRGrabInteractable>();
        ItemInfo info = GetComponent<ItemInfo>();
        if (info) itemName = info.itemName;

        if (grabInteractable)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    // 抓取时什么都不做（保持原大小）
    private void OnGrab(SelectEnterEventArgs args) { }

    // 释放时也不主动缩放，由槽位接管
    private void OnRelease(SelectExitEventArgs args)
    {
        this.transform.GetChild(0).localScale = originalScale;
    }

    public void EnterSlot(PlacementSlot slot)
    {
        currentSlot = slot;
        // 进入槽位 → 缩小
        this.transform.GetChild(0).localScale = Vector3.Scale(originalScale, grabScale);
    }

    public void ExitSlot(PlacementSlot slot)
    {
        if (currentSlot == slot)
        {
            currentSlot = null;
            // 离开槽位 → 恢复原大小
            this.transform.GetChild(0).localScale = originalScale;
        }
    }
}
