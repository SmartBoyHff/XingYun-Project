using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VR.UI
{
    public class UIController : MonoBehaviour
    {
        [Tooltip("Reference to the Input Action for the A button.")]
        public InputActionReference aBtn;

        public GameObject uiCanvas;

        private void Start()
        {
            aBtn.action.performed += ToggleUI;
        }

        private void OnDestroy()
        {
            aBtn.action.performed -= ToggleUI;
        }

        private void ToggleUI(InputAction.CallbackContext context)
        {
            if (uiCanvas != null)
            Debug.Log(1);
                uiCanvas.SetActive(!uiCanvas.activeSelf);
        }
    }
}