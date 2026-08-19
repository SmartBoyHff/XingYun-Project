using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class VRInteractionHandlerManager : MonoBehaviour
{
    [Header("射线交互器")]
    public XRRayInteractor rayInteractor;
    public XRRayInteractor lefInteractor;

    [Header("扳机输入")]
    public InputActionReference triggerAction;
    public InputActionReference leftriggerAction;
    public float triggerThreshold = 0.5f;       

    private ItemInfo currentItem;

    void OnEnable()
    {
        if (rayInteractor)
        {
            rayInteractor.hoverEntered.AddListener(OnHoverEnter);
            rayInteractor.hoverExited.AddListener(OnHoverExit);
        }
        if (lefInteractor)
        {
            lefInteractor.hoverEntered.AddListener(OnHoverEnter);
            lefInteractor.hoverExited.AddListener(OnHoverExit);
        }
    }

    void OnDisable()
    {
        if (rayInteractor)
        {
            rayInteractor.hoverEntered.RemoveListener(OnHoverEnter);
            rayInteractor.hoverExited.RemoveListener(OnHoverExit);
        }
        if (lefInteractor)
        {
            lefInteractor.hoverEntered.RemoveListener(OnHoverEnter);
            lefInteractor.hoverExited.RemoveListener(OnHoverExit);
        }
    }

    void Update()
    {
        if (currentItem && triggerAction != null)
        {
          
            // 读取扳机当前值（通常 0~1）
            float triggerValue = triggerAction.action.ReadValue<float>();
            if (triggerValue >= triggerThreshold)
            {
               
                currentItem.PlayVideo();
                // 可在此加入防连发冷却
            }
        }
        if (currentItem && leftriggerAction != null)
        {

            // 读取扳机当前值（通常 0~1）
            float triggerValue = leftriggerAction.action.ReadValue<float>();
            if (triggerValue >= triggerThreshold)
            {

                currentItem.PlayVideo();
                // 可在此加入防连发冷却
            }
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        ItemInfo info = args.interactableObject.transform.GetComponent<ItemInfo>();
        if (!info) return;

        if (currentItem)
        {
            currentItem.SetHighlight(false);
            if (!ExamManager.Instance || !ExamManager.Instance.examActive)
                currentItem.HideName();
        }

        currentItem = info;
        currentItem.SetHighlight(true);

        bool showName = !(ExamManager.Instance != null && ExamManager.Instance.examActive);
        if (showName) currentItem.ShowName();
        else currentItem.HideName();
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        ItemInfo info = args.interactableObject.transform.GetComponent<ItemInfo>();
        if (info == null || info != currentItem) return;

        currentItem.SetHighlight(false);
        currentItem.HideName();
        currentItem = null;
    }
    
}
