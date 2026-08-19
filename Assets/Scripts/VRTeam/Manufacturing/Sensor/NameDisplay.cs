using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NameDisplay : MonoBehaviour
{
    public TextMeshProUGUI nameText;
  private Transform mainCameraTransform;
  public int lookAtMethod = 1;
    public void Show(string itemName)
    {
        nameText.text = itemName;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void LateUpdate()
{
    if (mainCameraTransform == null)
    {
        mainCameraTransform = Camera.main.transform;
    }
    else
    {
        if (lookAtMethod == 1)
        {
            this.transform.rotation = Quaternion.LookRotation(this.transform.position - mainCameraTransform.position);
        }
        else if(lookAtMethod == 2)
        {
            transform.forward = mainCameraTransform.forward;
        }
        else if (lookAtMethod == 3)
        {
            transform.LookAt(mainCameraTransform);
        }
    }
}
}
