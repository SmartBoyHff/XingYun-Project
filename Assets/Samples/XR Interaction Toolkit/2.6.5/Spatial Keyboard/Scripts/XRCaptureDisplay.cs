using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR.Interaction.Toolkit;

public class XRCaptureDisplay : MonoBehaviour
{
    [Header("左手抓取事件")]
    public UnityEngine.Events.UnityEvent OnLeftHandGrab;
    [Header("右手抓取事件")]
    public UnityEngine.Events.UnityEvent OnRightHandGrab;
    [Header("左手放开事件")]
    public UnityEngine.Events.UnityEvent OnLeftHandExit;
    [Header("右手放开事件")]
    public UnityEngine.Events.UnityEvent OnRightHandExit;

    private XRBaseInteractable xRBase;
    private void Awake()
    {
        xRBase = this.GetComponent<XRBaseInteractable>();
        xRBase.selectEntered.AddListener(OnGrabEnter);
        xRBase.selectExited.AddListener(OnGrabExit);
    }

    private void OnGrabExit(SelectExitEventArgs arg0)
    {
        var interactor = arg0.interactorObject as XRBaseInteractor;
        if (interactor == null) return;

        if (interactor.transform.CompareTag("LeftHand"))
        {
            OnLeftHandExit?.Invoke();

        }
        else if (interactor.transform.CompareTag("RightHand"))
        {
            OnRightHandExit?.Invoke();

        }
    }

    // Update is called once per frame
    private void OnGrabEnter(SelectEnterEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == null) return;

        if (interactor.transform.CompareTag("LeftHand"))
        {
            OnLeftHandGrab?.Invoke();

        }
        else if (interactor.transform.CompareTag("RightHand"))
        {
            OnRightHandGrab?.Invoke();

        }

    }
    
}
