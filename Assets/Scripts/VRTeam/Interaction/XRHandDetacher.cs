using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRHandDetacher : MonoBehaviour
{
    [Header("手部控制器引用")]
    public Transform leftController;
    public Transform rightController;

    [Header("控制器原父节点（通常为 Camera Offset）")]
    public Transform originalParent;

    private XRDirectInteractor leftInteractor;
    private XRDirectInteractor rightInteractor;

    void Start()
    {
        if (leftController != null)
            leftInteractor = leftController.GetComponentInChildren<XRDirectInteractor>();
        if (rightController != null)
            rightInteractor = rightController.GetComponentInChildren<XRDirectInteractor>();

        if (leftInteractor != null)
        {
            leftInteractor.selectEntered.AddListener(OnGrabStartLeft);
            leftInteractor.selectExited.AddListener(OnGrabEndLeft);
        }
        if (rightInteractor != null)
        {
            rightInteractor.selectEntered.AddListener(OnGrabStartRight);
            rightInteractor.selectExited.AddListener(OnGrabEndRight);
        }
    }

    void OnGrabStartLeft(SelectEnterEventArgs args) => DetachController(leftController);
    void OnGrabEndLeft(SelectExitEventArgs args) => AttachController(leftController);
    void OnGrabStartRight(SelectEnterEventArgs args) => DetachController(rightController);
    void OnGrabEndRight(SelectExitEventArgs args) => AttachController(rightController);

    void DetachController(Transform controller)
    {
        if (controller == null || controller.parent == null) return;
        controller.SetParent(null);          // 脱离层级，变为世界空间
        // 确保追踪组件仍在运行，通常 TrackedPoseDriver 会自己处理
    }

    void AttachController(Transform controller)
    {
        if (controller == null || originalParent == null) return;
        controller.SetParent(originalParent, true);   // 重新挂回，worldPositionStays = true 保持世界位置
    }
}
