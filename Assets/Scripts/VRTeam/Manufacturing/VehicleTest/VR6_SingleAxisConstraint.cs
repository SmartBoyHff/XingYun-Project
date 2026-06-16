using UnityEngine;

// ============================================================
// File: VR6_AxisRotateConstraint
// Module: VR6 three-body flying car display
// Purpose: Single-axis visual rotation constraint
// Created: 2026-04-28
// Updated: 2026-05-11
// ============================================================

namespace VRHelmet.VRTeam.Manufacturing.VehicleTest.ThreeBodyCar
{
    /// <summary>
    /// Constrains a visual part to rotate around one selected local axis.
    /// </summary>
    public class VR6_AxisRotateConstraint : MonoBehaviour
    {
        #region ==========Field==========

        [Header("Axis")]
        [SerializeField] private Axis freeAxis = Axis.X;

        [Header("Initial Rotation")]
        [SerializeField] private float initialAngleOffset;

        [Header("Auto Rotate")]
        [SerializeField] private bool autoRotateOnStart;
        [SerializeField] private float autoRotateSpeedDegPerSec = 180f;

        private Quaternion baseLocalRotation;
        private float axisAngle;
        private bool autoRotating;

        #endregion

        #region ==========Unity Method==========

        private void Awake()
        {
            baseLocalRotation = transform.localRotation;
            axisAngle = initialAngleOffset;
            autoRotating = autoRotateOnStart;
            ApplyRotation();
        }

        private void Update()
        {
            if (autoRotating)
            {
                axisAngle += autoRotateSpeedDegPerSec * Time.deltaTime;
            }
        }

        private void LateUpdate()
        {
            ApplyRotation();
        }

        #endregion

        #region ==========Logic==========

        private void ApplyRotation()
        {
            Vector3 localAxis = GetLocalAxisVector(freeAxis);
            Quaternion spin = Quaternion.AngleAxis(axisAngle, localAxis);
            transform.localRotation = baseLocalRotation * spin;
        }

        private static Vector3 GetLocalAxisVector(Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return Vector3.right;
                case Axis.Y:
                    return Vector3.up;
                default:
                    return Vector3.forward;
            }
        }

        #endregion

        #region ==========API==========

        /// <summary>
        /// Sets the absolute rotation angle around the selected local axis.
        /// </summary>
        public void SetAxisAngle(float angleDeg)
        {
            axisAngle = angleDeg;
            ApplyRotation();
        }

        /// <summary>
        /// Adds a delta angle to the current rotation around the selected local axis.
        /// </summary>
        public void AddAxisAngle(float deltaDeg)
        {
            axisAngle += deltaDeg;
            ApplyRotation();
        }

        /// <summary>
        /// Rebinds the current local rotation as the new base pose and resets the axis angle.
        /// </summary>
        public void RebindCurrentAsBase()
        {
            baseLocalRotation = transform.localRotation;
            axisAngle = 0f;
            ApplyRotation();
        }

        /// <summary>
        /// Enables or disables automatic rotation around the selected local axis.
        /// </summary>
        public void SetAutoRotate(bool enable)
        {
            autoRotating = enable;
        }

        /// <summary>
        /// Toggles automatic rotation and returns the enabled state after the toggle.
        /// </summary>
        public bool ToggleAutoRotate()
        {
            autoRotating = !autoRotating;
            return autoRotating;
        }

        #endregion
    }

    /// <summary>
    /// Selectable local axis for constrained visual rotation.
    /// </summary>
    public enum Axis
    {
        /// <summary>
        /// Local X axis.
        /// </summary>
        X,

        /// <summary>
        /// Local Y axis.
        /// </summary>
        Y,

        /// <summary>
        /// Local Z axis.
        /// </summary>
        Z
    }
}
