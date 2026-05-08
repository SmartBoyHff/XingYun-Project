using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ============================================================
// 文件名：VR6_UIController
// 模块：模块6 - 三体式飞行汽车展示
// 功能：简易UI控制器
// 创建日期：2026-04-28
// 最后更新：2026-04-29
// ============================================================

namespace VRHelmet.VRTeam.Manufacturing.VehicleTest.ThreeBodyCar
{
    public class VR6_UIController : MonoBehaviour
    {
        [Tooltip("Reference to the Input Action for the A button.")]
        public InputActionReference aBtn;

        public GameObject uiCanvas;

        private void Start()
        {
            if (aBtn != null)
                aBtn.action.performed += ToggleUI;
        }

        private void OnDestroy()
        {
            if (aBtn != null)
                aBtn.action.performed -= ToggleUI;
        }

        private void ToggleUI(InputAction.CallbackContext context)
        {
            if (uiCanvas != null)
                uiCanvas.SetActive(!uiCanvas.activeSelf);
        }
    }
}