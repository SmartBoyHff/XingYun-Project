using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OriginalZone : MonoBehaviour
{
    private HashSet<AssemblyController> objectsInside = new HashSet<AssemblyController>();

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(1);
        AssemblyController ac = other.GetComponent<AssemblyController>();
        if (ac) objectsInside.Add(ac);
    }

    private void OnTriggerExit(Collider other)
    {
        AssemblyController ac = other.GetComponent<AssemblyController>();
        if (ac) objectsInside.Remove(ac);
    }

    public bool IsObjectInside(AssemblyController ac)
    {
        return objectsInside.Contains(ac);
    }
}
