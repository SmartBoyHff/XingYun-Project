using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabbableItem : MonoBehaviour
{
    public string itemID;
    public D_ToolbarSlot originSlot;

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (D_GrabManager.Instance != null)
            D_GrabManager.Instance.ReportGrabbed(this);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (D_GrabManager.Instance != null)
            D_GrabManager.Instance.ReportReleased(this);
    }
}
