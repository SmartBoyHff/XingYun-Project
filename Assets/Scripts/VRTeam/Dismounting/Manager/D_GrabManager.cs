using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
public class D_GrabManager : MonoBehaviour
{
   public static D_GrabManager Instance { get; private set; }

    [SerializeField] private XRRayInteractor rightRayInteractor; // 分配给右手的射线交互器
    private XRGrabInteractable currentGrabbedObject;
    private GrabbableItem currentGrabbableItem;

    private void Awake()
    {
        Instance = this;
    }

    public bool IsHandOccupied() => currentGrabbedObject != null;

    public string CurrentGrabbedItemID
    {
        get
        {
            if (currentGrabbableItem != null)
                return currentGrabbableItem.itemID;
            return string.Empty;
        }
    }

    // 由工具栏调用：生成物体并立即抓取到右手
    public void GrabObject(GameObject instance, string itemID, D_ToolbarSlot sourceSlot)
    {
        if (IsHandOccupied())
        {
            Destroy(instance);
            return;
        }

        // 确保物体有XRGrabInteractable和GrabbableItem组件
        XRGrabInteractable grabInteractable = instance.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
            grabInteractable = instance.AddComponent<XRGrabInteractable>();

        GrabbableItem item = instance.GetComponent<GrabbableItem>();
        if (item == null)
            item = instance.AddComponent<GrabbableItem>();
        item.itemID = itemID;
        item.originSlot = sourceSlot;

        // 使用交互器抓取
        rightRayInteractor.interactionManager.SelectEnter(rightRayInteractor, grabInteractable);
        currentGrabbedObject = grabInteractable;
        currentGrabbableItem = item;

        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        // 物体被放下
        if (args.interactableObject == currentGrabbedObject)
        {
            currentGrabbedObject = null;
            currentGrabbableItem = null;
        }
    }

    // 当物体被回收区销毁时调用
    public void ForceClearCurrent()
    {
        if (currentGrabbedObject != null)
        {
            currentGrabbedObject.interactionManager.SelectExit(rightRayInteractor, currentGrabbedObject);
        }
        currentGrabbedObject = null;
        currentGrabbableItem = null;
    }
}
