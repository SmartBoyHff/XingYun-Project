using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// ============================================================
// File: VR6_CameraOrbit
// Module: VR6 three-body flying car display
// Purpose: Play-mode UI control
// Created: 2026-04-28
// Updated: 2026-05-11
// ============================================================

namespace VRHelmet.VRTeam.Manufacturing.VehicleTest.ThreeBodyCar
{
    /// <summary>
    /// Coordinates VehicleTest mode buttons and play-mode UI canvas visibility.
    /// </summary>
    public class VR6_CameraOrbit : MonoBehaviour
    {
        #region ==========Field==========

        [Header("UI")]
        [SerializeField] private GameObject controlStepsCanvas;
        [SerializeField] private GameObject playCanvas;
        [SerializeField] private Button carModeButton;
        [SerializeField] private Button flightModeButton;
        [SerializeField] private Button explosionButton;
        [SerializeField] private bool hidePlayCanvasOnStart = true;

        [Header("Mode")]
        [SerializeField] private VR6_MainBodyController mainBodyController;

        [Header("XR Rig Lock")]
        [SerializeField] private bool lockXrRigOnPlayMode = true;
        [SerializeField] private Transform xrRigRoot;
        [SerializeField] private Rigidbody xrRigRigidbody;
        [SerializeField] private CharacterController xrRigCharacterController;
        [SerializeField] private Behaviour xrRigCharacterControllerDriver;
        [SerializeField] private GameObject xrRigLocomotionSystemRoot;
        [SerializeField] private Behaviour[] xrRigMovementComponents = Array.Empty<Behaviour>();
        [SerializeField] private bool restoreXrRigPoseOnLock;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private ButtonBinding[] buttonBindings = Array.Empty<ButtonBinding>();
        private Vector3 xrRigInitialPosition;
        private Quaternion xrRigInitialRotation;

        #endregion

        #region ==========Unity Method==========

        private void Awake()
        {
            BuildButtonBindings();
            InitializeCanvasState();
            CacheXrRigPose();

            Log($"Awake. mainBodyController assigned: {mainBodyController != null}, carButton: {carModeButton != null}, flightButton: {flightModeButton != null}, explosionButton: {explosionButton != null}");
        }

        private void OnEnable()
        {
            SetButtonBindingsRegistered(true);
            Log("Button events registered.");
        }

        private void OnDisable()
        {
            SetButtonBindingsRegistered(false);
            Log("Button events unregistered.");
        }

        #endregion

        #region ==========Logic==========

        private readonly struct ButtonBinding
        {
            private readonly Button button;
            private readonly UnityAction action;

            /// <summary>
            /// Creates a button binding from a Unity UI button and click action.
            /// </summary>
            public ButtonBinding(Button button, UnityAction action)
            {
                this.button = button;
                this.action = action;
            }

            /// <summary>
            /// Registers the stored action to the stored button when both are valid.
            /// </summary>
            public void Register()
            {
                if (button != null)
                {
                    button.onClick.AddListener(action);
                }
            }

            /// <summary>
            /// Unregisters the stored action from the stored button when both are valid.
            /// </summary>
            public void Unregister()
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(action);
                }
            }
        }

        private void BuildButtonBindings()
        {
            buttonBindings = new[]
            {
                new ButtonBinding(carModeButton, OnCarModeButtonClicked),
                new ButtonBinding(flightModeButton, OnFlightModeButtonClicked),
                new ButtonBinding(explosionButton, OnExplosionButtonClicked)
            };
        }

        private void InitializeCanvasState()
        {
            if (playCanvas != null && hidePlayCanvasOnStart)
            {
                playCanvas.SetActive(false);
                Log("Play canvas hidden on start.");
            }
        }

        private void CacheXrRigPose()
        {
            if (xrRigRoot == null)
            {
                return;
            }

            xrRigInitialPosition = xrRigRoot.position;
            xrRigInitialRotation = xrRigRoot.rotation;
        }

        private void SetButtonBindingsRegistered(bool registered)
        {
            for (int i = 0; i < buttonBindings.Length; i++)
            {
                if (registered)
                {
                    buttonBindings[i].Register();
                }
                else
                {
                    buttonBindings[i].Unregister();
                }
            }
        }

        private void OnCarModeButtonClicked()
        {
            Log("Car mode button clicked.");
            SetPlayMode(true);

            if (mainBodyController != null)
            {
                mainBodyController.ActivateCarMode();
            }
            else
            {
                Debug.LogWarning("[VR6_CameraOrbit] MainBodyController is not assigned. Car mode cannot start.", this);
            }
        }

        private void OnFlightModeButtonClicked()
        {
            Log("Flight mode button clicked.");
            SetPlayMode(true);

            if (mainBodyController != null)
            {
                mainBodyController.ActivateFlightMode();
            }
            else
            {
                Debug.LogWarning("[VR6_CameraOrbit] MainBodyController is not assigned. Flight mode cannot start.", this);
            }
        }

        private void OnExplosionButtonClicked()
        {
            Log("Explosion button clicked.");

            if (mainBodyController != null)
            {
                mainBodyController.ToggleExplosion();
            }
            else
            {
                Debug.LogWarning("[VR6_CameraOrbit] MainBodyController is not assigned. Explosion cannot toggle.", this);
            }
        }

        private static void SetCanvasActive(GameObject canvas, bool active)
        {
            if (canvas != null)
            {
                canvas.SetActive(active);
            }
        }

        private void SetXrRigMovementLocked(bool locked)
        {
            if (!locked)
            {
                SetXrRigLocomotionSystemActive(true);
            }

            for (int i = 0; i < xrRigMovementComponents.Length; i++)
            {
                Behaviour movementComponent = xrRigMovementComponents[i];

                if (movementComponent == null)
                {
                    continue;
                }

                movementComponent.enabled = !locked;
                Log($"XR Rig movement component {(locked ? "disabled" : "enabled")}: {movementComponent.GetType().Name}");
            }

            if (xrRigRigidbody != null)
            {
                xrRigRigidbody.velocity = Vector3.zero;
                xrRigRigidbody.angularVelocity = Vector3.zero;
                xrRigRigidbody.isKinematic = locked;
                Log($"XR Rig Rigidbody locked: {locked}");
            }

            if (xrRigCharacterController != null)
            {
                xrRigCharacterController.enabled = !locked;
                Log($"XR Rig CharacterController {(locked ? "disabled" : "enabled")}.");
            }

            if (xrRigCharacterControllerDriver != null)
            {
                xrRigCharacterControllerDriver.enabled = !locked;
                Log($"XR Rig CharacterControllerDriver {(locked ? "disabled" : "enabled")}: {xrRigCharacterControllerDriver.GetType().Name}");
            }

            if (locked)
            {
                SetXrRigLocomotionSystemActive(false);
            }

            if (locked && restoreXrRigPoseOnLock && xrRigRoot != null)
            {
                xrRigRoot.position = xrRigInitialPosition;
                xrRigRoot.rotation = xrRigInitialRotation;
                Log("XR Rig pose restored while locking movement.");
            }
        }

        private void SetXrRigLocomotionSystemActive(bool active)
        {
            if (xrRigLocomotionSystemRoot == null)
            {
                return;
            }

            if (xrRigRoot != null && xrRigLocomotionSystemRoot == xrRigRoot.gameObject)
            {
                Debug.LogWarning("[VR6_CameraOrbit] XR Rig Locomotion System Root should not be the XR Rig root itself. Assign the child Locomotion System object instead.", this);
                return;
            }

            xrRigLocomotionSystemRoot.SetActive(active);
            Log($"XR Rig Locomotion System root active: {active}");
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[VR6_CameraOrbit] {message}", this);
            }
        }

        #endregion

        #region ==========API==========

        /// <summary>
        /// Switches VehicleTest UI between setup/control-step mode and play mode.
        /// </summary>
        public void SetPlayMode(bool isPlayMode)
        {
            SetCanvasActive(controlStepsCanvas, !isPlayMode);
            SetCanvasActive(playCanvas, isPlayMode);
            SetXrRigMovementLocked(isPlayMode && lockXrRigOnPlayMode);
            Log($"SetPlayMode: {isPlayMode}. controlStepsCanvas: {!isPlayMode}, playCanvas: {isPlayMode}, XR rig locked: {isPlayMode && lockXrRigOnPlayMode}");
        }

        #endregion
    }
}
