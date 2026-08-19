using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRSimpleInteractable2 : XRSimpleInteractable
{
    [SerializeField]
    [Tooltip("射线抓取位置更改")]
    private Transform m_H;

    public Transform rotH
    {
        get => m_H;
        set => m_H = value;
    }
    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        if (m_H == null)
        {
            return this.transform;
        }
        else
        {
            return m_H.transform;
        }

    }
}
