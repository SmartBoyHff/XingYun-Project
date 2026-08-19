using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using static ForkliftController;

public class PlacementArea : MonoBehaviour
{
    public ForkliftController forklift;
    public XRSocketInteractor forkPlacementArea;
    public float height=0.8f;
    public bool isPut = false;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Fork")&& forklift.liftKnob.value> height&&forklift.currentGear==Gear.Neutral&&!isPut)
        {
            Debug.Log(1);
            forkPlacementArea.enabled=false;
            isPut = true; 
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Fork"))
        {
            Debug.Log(2);
            forkPlacementArea.enabled = true;
        }
        if(other.CompareTag("Fork")&&isPut)
        {
            isPut = false;
        }
    }
}
