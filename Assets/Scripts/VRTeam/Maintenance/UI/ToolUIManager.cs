using UnityEngine;
using UnityEngine.UI;

namespace VRHelmet.VRTeam.Maintenance
{
    public class ToolUIManager : MonoBehaviour
    {
        [Header("Canvas")]
        public GameObject toolCanvas;

        [Header("Tool")]
        public GameObject tirePressureGauge;
        public GameObject depthGauge;

        [Header("Button")]
        public Button tirePressureGaugeButton;
        public Button depthGaugeButton;

        private void OnEnable()
        {
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            UnbindEvents();

            if (tirePressureGaugeButton != null)
            {
                tirePressureGaugeButton.onClick.AddListener(ShowTirePressureGauge);
            }

            if (depthGaugeButton != null)
            {
                depthGaugeButton.onClick.AddListener(ShowDepthGauge);
            }
        }

        private void UnbindEvents()
        {
            if (tirePressureGaugeButton != null)
            {
                tirePressureGaugeButton.onClick.RemoveListener(ShowTirePressureGauge);
            }

            if (depthGaugeButton != null)
            {
                depthGaugeButton.onClick.RemoveListener(ShowDepthGauge);
            }
        }

        public void ShowToolCanvas()
        {
            SetToolCanvasActive(true);
        }

        public void ShowTirePressureGauge()
        {
            SetActiveTool(tirePressureGauge);
        }

        public void ShowDepthGauge()
        {
            SetActiveTool(depthGauge);
        }

        private void SetActiveTool(GameObject selectedTool)
        {
            if (tirePressureGauge != null)
            {
                tirePressureGauge.SetActive(tirePressureGauge == selectedTool);
            }

            if (depthGauge != null)
            {
                depthGauge.SetActive(depthGauge == selectedTool);
            }
        }

        private void SetToolCanvasActive(bool isActive)
        {
            if (toolCanvas != null)
            {
                toolCanvas.SetActive(isActive);
            }
        }
    }
}
