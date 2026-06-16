using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;

// ============================================================
// File: VR6_DroneController
// Module: VR6 three-body flying car display
// Purpose: Rigidbody-based multi-rotor aircraft control
// Created: 2026-05-12
// Updated: 2026-05-12
// ============================================================

namespace VRHelmet.VRTeam.Manufacturing.VehicleTest.ThreeBodyCar
{
    /// <summary>
    /// Controls a multi-rotor aircraft with front lift rotors, rear lift rotors, and one tail rotor.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VR6_DroneController : MonoBehaviour
    {
        #region ==========Field==========

        [Header("Input")]
        [FormerlySerializedAs("leftStickAction")]
        [SerializeField] private InputActionReference controlStickAction;
        [SerializeField] private bool startFlyingOnAwake;
        [SerializeField] private bool useKeyboardFallback = true;
        [SerializeField] private bool invertLeftStickY = true;
        [SerializeField] private bool invertForwardMovement;
        [SerializeField] private float inputDeadZone = 0.12f;

        [Header("Rigidbody")]
        [SerializeField] private Rigidbody droneRigidbody;
        [SerializeField] private Transform centerOfMass;
        [SerializeField] private Transform flightForwardReference;
        [SerializeField] private bool applyCenterOfMassOnAwake = true;

        [Header("Lift Rotor Points")]
        [SerializeField] private List<Transform> frontRotorPoints = new List<Transform>();
        [SerializeField] private List<Transform> rearRotorPoints = new List<Transform>();
        [HideInInspector]
        [SerializeField] private bool useRotorPointUpDirection;

        [Header("Tail Rotor")]
        [SerializeField] private Transform tailRotorPoint;
        [SerializeField] private Transform tailForceDirection;
        [HideInInspector]
        [SerializeField] private float tailRotorForce = 8f;
        [HideInInspector]
        [SerializeField] private float yawTorque = 3f;

        [Header("Flight Force")]
        [HideInInspector]
        [SerializeField] private bool useAssistedPlanarFlight = true;
        [HideInInspector]
        [SerializeField] private bool autoHoverWhenFlying = true;
        [HideInInspector]
        [SerializeField] private float hoverThrottle = 0.55f;
        [HideInInspector]
        [SerializeField] private float maxRotorForce = 8f;
        [HideInInspector]
        [SerializeField] private float throttleResponse = 2.5f;
        [HideInInspector]
        [SerializeField] private float pitchPower = 0.4f;
        [HideInInspector]
        [SerializeField] private float rollPower = 0.45f;
        [SerializeField] private float maxHorizontalSpeed = 12f;
        [HideInInspector]
        [SerializeField] private float horizontalDrag = 0.8f;
        [HideInInspector]
        [SerializeField] private float verticalDrag = 0.2f;

        [Header("Assisted Flight")]
        [SerializeField] private float verticalTakeoffHeight = 1.2f;
        [SerializeField] private float verticalTakeoffClimbRate = 0.4f;
        [SerializeField] private float verticalTakeoffRampTime = 2f;
        [SerializeField] private float landedHeightTolerance = 0.12f;
        [SerializeField] private float landedSpeedThreshold = 0.18f;
        [SerializeField] private float takeoffHeight = 1.5f;
        [HideInInspector]
        [SerializeField] private float climbStartSpeed = 0.5f;
        [SerializeField] private float climbFullSpeed = 5f;
        [SerializeField] private float maxSpeedClimbRate = 0.8f;
        [HideInInspector]
        [SerializeField] private float idleLiftRatio = 0.35f;
        [HideInInspector]
        [SerializeField] private float runwayLiftRatio = 0.85f;
        [HideInInspector]
        [SerializeField] private float idlePropellerThrottle = 0.15f;
        [HideInInspector]
        [SerializeField] private float poweredLiftRatio = 1.15f;
        [HideInInspector]
        [SerializeField] private float verticalSpeedCorrection = 3f;
        [HideInInspector]
        [SerializeField] private float powerReleaseResponse = 0.45f;
        [HideInInspector]
        [SerializeField] private float gentleDescentRate = 0.22f;
        [HideInInspector]
        [SerializeField] private float airborneIdleLiftRatio = 0.72f;
        [SerializeField] private float planarMoveAcceleration = 4f;
        [SerializeField] private float yawTurnSpeed = 60f;
        [HideInInspector]
        [SerializeField] private float planarBrake = 2f;
        [HideInInspector]
        [SerializeField] private float tiltFollowSpeed = 90f;

        [Header("Stabilization")]
        [HideInInspector]
        [SerializeField] private bool autoLevel = true;
        [HideInInspector]
        [SerializeField] private float levelTorque = 8f;
        [HideInInspector]
        [SerializeField] private float angularDamping = 2.5f;
        [SerializeField] private float maxTiltAngle = 20f;

        [Header("Propeller Visuals")]
        [SerializeField] private List<VR6_AxisRotateConstraint> frontPropellerRotators = new List<VR6_AxisRotateConstraint>();
        [SerializeField] private List<VR6_AxisRotateConstraint> rearPropellerRotators = new List<VR6_AxisRotateConstraint>();
        [SerializeField] private VR6_AxisRotateConstraint tailPropellerRotator;
        [SerializeField] private float minPropellerSpinSpeed = 360f;
        [SerializeField] private float maxPropellerSpinSpeed = 3600f;
        [SerializeField] private float propellerFlightSpeedForMaxSpin = 18f;
        [HideInInspector]
        [SerializeField] private float flyingIdleSpinRatio = 0.35f;
        [SerializeField] private float tailPropellerYawSpinBoost = 1200f;
        [SerializeField] private bool invertTailPropellerSpin;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private float inputLogInterval = 0.5f;

        private InputAction cachedControlStickAction;

        private Vector2 pitchRollInput;
        private Vector2 throttleYawInput;
        private float currentThrottle;
        private bool isFlying;
        private bool loggedWaitingForStart;
        private float nextInputLogTime;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float flightStartAltitude;
        private float verticalTakeoffStartTime;
        private float smoothedFlightPower;
        private bool verticalTakeoffComplete;
        private bool waitingForForwardRetakeoff;
        private float assistedYaw;

        #endregion

        #region ==========Unity Method==========

        private void Awake()
        {
            if (droneRigidbody == null)
            {
                droneRigidbody = GetComponent<Rigidbody>();
            }

            cachedControlStickAction = controlStickAction != null ? controlStickAction.action : null;

            if (applyCenterOfMassOnAwake && centerOfMass != null && droneRigidbody != null)
            {
                droneRigidbody.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
            }

            currentThrottle = 0f;
            isFlying = startFlyingOnAwake;
            initialPosition = droneRigidbody != null ? droneRigidbody.position : transform.position;
            initialRotation = droneRigidbody != null ? droneRigidbody.rotation : transform.rotation;

            DisablePropellerAutoRotate();
            WarnIfSetupInvalid();
        }

        private void OnEnable()
        {
            cachedControlStickAction = controlStickAction != null ? controlStickAction.action : null;
            cachedControlStickAction?.Enable();
        }

        private void OnDisable()
        {
            // Do not disable shared InputAction assets here. Car and drone modes can reference
            // the same 2D axis action, and disabling it from one mode makes the other read zero.
        }

        private void Update()
        {
            if (!isFlying)
            {
                if (!loggedWaitingForStart)
                {
                    Log("Waiting for StartFly(). Flight input is ignored until flight mode starts.");
                    loggedWaitingForStart = true;
                }

                ClearInput();
                UpdatePropellerVisuals(0f);
                return;
            }

            ReadInput();
            UpdatePropellerVisuals(Mathf.Abs(currentThrottle));
        }

        private void FixedUpdate()
        {
            if (!isFlying || droneRigidbody == null)
            {
                return;
            }

            ApplyThrottleResponse();
            if (useAssistedPlanarFlight)
            {
                UpdateLandingAndRetakeoffState();
                ApplyAssistedLiftForces();
                ApplyPlanarMovement();
                ApplyAssistedRotation();
            }
            else
            {
                ApplyLiftForces();
                ApplyTailYaw();
                ApplyStabilization();
            }

            ApplyAirDrag();
            LimitHorizontalSpeed();
        }

        #endregion

        #region ==========Logic==========

        private void ClearInput()
        {
            pitchRollInput = Vector2.zero;
            throttleYawInput = Vector2.zero;
        }

        private void ReadInput()
        {
            Vector2 actionStickInput = cachedControlStickAction != null ? cachedControlStickAction.ReadValue<Vector2>() : Vector2.zero;
            Vector2 singleStickInput = actionStickInput;
            if (invertLeftStickY)
            {
                singleStickInput.y = -singleStickInput.y;
            }

            singleStickInput = ApplyDeadZone(singleStickInput);

            if (useKeyboardFallback && Keyboard.current != null)
            {
                Vector2 keyboardSingleStickInput = ReadKeyboardSingleStick();
                if (keyboardSingleStickInput.sqrMagnitude > singleStickInput.sqrMagnitude)
                {
                    singleStickInput = keyboardSingleStickInput;
                }
            }

            float forwardInput = invertForwardMovement ? -singleStickInput.y : singleStickInput.y;
            pitchRollInput = new Vector2(singleStickInput.x, forwardInput);
            throttleYawInput = new Vector2(0f, singleStickInput.y);
            LogInputSnapshot(actionStickInput, singleStickInput);
        }

        private Vector2 ReadKeyboardSingleStick()
        {
            float yaw = ReadKeyboardAxis(Keyboard.current.aKey, Keyboard.current.dKey);
            float throttle = ReadKeyboardAxis(Keyboard.current.sKey, Keyboard.current.wKey);
            return ApplyDeadZone(new Vector2(yaw, throttle));
        }

        private static float ReadKeyboardAxis(KeyControl negativeKey, KeyControl positiveKey)
        {
            float value = 0f;

            if (negativeKey.isPressed)
            {
                value -= 1f;
            }

            if (positiveKey.isPressed)
            {
                value += 1f;
            }

            return Mathf.Clamp(value, -1f, 1f);
        }

        private Vector2 ApplyDeadZone(Vector2 input)
        {
            float x = Mathf.Abs(input.x) > inputDeadZone ? input.x : 0f;
            float y = Mathf.Abs(input.y) > inputDeadZone ? input.y : 0f;
            return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
        }

        private void ApplyThrottleResponse()
        {
            float targetThrottle = GetTargetThrottle();
            currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, throttleResponse * Time.fixedDeltaTime);
        }

        private float GetTargetThrottle()
        {
            if (useAssistedPlanarFlight)
            {
                float targetPower = Mathf.Clamp01(Mathf.Max(Mathf.Abs(pitchRollInput.y), Mathf.Abs(pitchRollInput.x)));
                if (waitingForForwardRetakeoff)
                {
                    targetPower = 0f;
                }
                
                if (!verticalTakeoffComplete)
                {
                    targetPower = Mathf.Max(targetPower, GetVerticalTakeoffPowerRatio());
                }

                MoveSmoothedFlightPower(targetPower);
                return Mathf.Lerp(idlePropellerThrottle, 1f, smoothedFlightPower);
            }

            if (autoHoverWhenFlying)
            {
                return hoverThrottle;
            }

            if (throttleYawInput.y <= inputDeadZone)
            {
                return 0f;
            }

            return Mathf.Lerp(hoverThrottle, 1f, throttleYawInput.y);
        }

        private void MoveSmoothedFlightPower(float targetPower)
        {
            float response = targetPower > smoothedFlightPower
                ? throttleResponse
                : powerReleaseResponse;
            smoothedFlightPower = Mathf.MoveTowards(smoothedFlightPower, targetPower, response * Time.deltaTime);
        }

        private void ApplyLiftForces()
        {
            int rotorCount = frontRotorPoints.Count + rearRotorPoints.Count;
            if (rotorCount <= 0)
            {
                return;
            }

            float baseForce = currentThrottle * maxRotorForce;
            float pitchMix = pitchRollInput.y * pitchPower * maxRotorForce;
            float rollMix = pitchRollInput.x * rollPower * maxRotorForce;

            ApplyRotorGroup(frontRotorPoints, baseForce - pitchMix, rollMix);
            ApplyRotorGroup(rearRotorPoints, baseForce + pitchMix, rollMix);
        }

        private void ApplyAssistedLiftForces()
        {
            int rotorCount = frontRotorPoints.Count + rearRotorPoints.Count;
            if (rotorCount <= 0)
            {
                return;
            }

            if (waitingForForwardRetakeoff)
            {
                ApplyResidualIdleLift(rotorCount);
                return;
            }

            if (!verticalTakeoffComplete)
            {
                ApplyVerticalTakeoffLift(rotorCount);
                return;
            }

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(droneRigidbody.velocity, Vector3.up);
            float speedRatio = Mathf.InverseLerp(
                Mathf.Max(0f, climbStartSpeed),
                Mathf.Max(climbStartSpeed + 0.01f, climbFullSpeed),
                horizontalVelocity.magnitude);
            float inputPower = Mathf.Clamp01(Mathf.Max(Mathf.Abs(pitchRollInput.y), Mathf.Abs(pitchRollInput.x)));
            float climbRatio = Mathf.Clamp01(speedRatio);
            float targetAltitude = flightStartAltitude + Mathf.Max(0f, verticalTakeoffHeight) + Mathf.Max(0f, takeoffHeight) * climbRatio;
            float altitudeError = targetAltitude - droneRigidbody.position.y;
            float climbRate = maxSpeedClimbRate * Mathf.Clamp01(speedRatio);
            float desiredVerticalSpeed = altitudeError > 0.05f
                ? Mathf.Clamp(altitudeError, 0f, climbRate * Mathf.Max(inputPower, 0.35f))
                : -gentleDescentRate * (1f - inputPower);
            float groundRunLift = Mathf.Lerp(
                Mathf.Clamp01(airborneIdleLiftRatio),
                Mathf.Clamp01(runwayLiftRatio),
                smoothedFlightPower);
            float liftSupport = Mathf.Lerp(
                groundRunLift,
                Mathf.Max(1f, poweredLiftRatio),
                climbRatio);
            float verticalAcceleration = Physics.gravity.magnitude * liftSupport
                + (desiredVerticalSpeed - droneRigidbody.velocity.y) * verticalSpeedCorrection * Mathf.Max(0.25f, climbRatio);
            float rotorForce = Mathf.Max(0f, droneRigidbody.mass * verticalAcceleration / rotorCount);

            ApplyAssistedRotorGroup(frontRotorPoints, rotorForce);
            ApplyAssistedRotorGroup(rearRotorPoints, rotorForce);
        }

        private void ApplyResidualIdleLift(int rotorCount)
        {
            float liftSupport = Mathf.Clamp01(airborneIdleLiftRatio);
            float desiredVerticalSpeed = -gentleDescentRate;
            float verticalAcceleration = Physics.gravity.magnitude * liftSupport
                + (desiredVerticalSpeed - droneRigidbody.velocity.y) * verticalSpeedCorrection;
            float rotorForce = Mathf.Max(0f, droneRigidbody.mass * verticalAcceleration / rotorCount);

            ApplyAssistedRotorGroup(frontRotorPoints, rotorForce);
            ApplyAssistedRotorGroup(rearRotorPoints, rotorForce);
        }

        private void ApplyVerticalTakeoffLift(int rotorCount)
        {
            float targetAltitude = flightStartAltitude + Mathf.Max(0f, verticalTakeoffHeight);
            float altitudeError = targetAltitude - droneRigidbody.position.y;
            if (altitudeError <= 0.03f)
            {
                verticalTakeoffComplete = true;
                return;
            }

            float takeoffPower = GetVerticalTakeoffPowerRatio();
            float desiredVerticalSpeed = Mathf.Clamp(altitudeError, 0f, verticalTakeoffClimbRate * takeoffPower);
            float liftSupport = Mathf.Lerp(Mathf.Clamp01(runwayLiftRatio), Mathf.Max(1f, poweredLiftRatio), takeoffPower);
            float verticalAcceleration = Physics.gravity.magnitude * liftSupport
                + (desiredVerticalSpeed - droneRigidbody.velocity.y) * verticalSpeedCorrection * Mathf.Max(0.15f, takeoffPower);
            float rotorForce = Mathf.Max(0f, droneRigidbody.mass * verticalAcceleration / rotorCount);

            ApplyAssistedRotorGroup(frontRotorPoints, rotorForce);
            ApplyAssistedRotorGroup(rearRotorPoints, rotorForce);
        }

        private float GetVerticalTakeoffPowerRatio()
        {
            float rampTime = Mathf.Max(0.05f, verticalTakeoffRampTime);
            float linearRatio = Mathf.Clamp01((Time.time - verticalTakeoffStartTime) / rampTime);
            return Mathf.SmoothStep(0f, 1f, linearRatio);
        }

        private void ApplyAssistedRotorGroup(List<Transform> rotorPoints, float rotorForce)
        {
            for (int i = 0; i < rotorPoints.Count; i++)
            {
                Transform rotorPoint = rotorPoints[i];
                if (rotorPoint == null)
                {
                    continue;
                }

                droneRigidbody.AddForceAtPosition(Vector3.up * rotorForce, rotorPoint.position, ForceMode.Force);
            }
        }

        private void ApplyPlanarMovement()
        {
            if (waitingForForwardRetakeoff || !verticalTakeoffComplete)
            {
                Vector3 currentHorizontalVelocity = Vector3.ProjectOnPlane(droneRigidbody.velocity, Vector3.up);
                droneRigidbody.AddForce(-currentHorizontalVelocity * planarBrake, ForceMode.Acceleration);
                return;
            }

            Transform directionReference = flightForwardReference != null ? flightForwardReference : transform;
            Vector3 forward = Vector3.ProjectOnPlane(directionReference.forward, Vector3.up);
            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = Vector3.forward;
            }

            Vector3 planarInput = forward.normalized * pitchRollInput.y;
            planarInput = Vector3.ClampMagnitude(planarInput, 1f);
            float inputPower = Mathf.Clamp01(Mathf.Max(Mathf.Abs(pitchRollInput.y), Mathf.Abs(pitchRollInput.x)));
            MoveSmoothedFlightPower(inputPower);
            droneRigidbody.AddForce(planarInput * planarMoveAcceleration * smoothedFlightPower, ForceMode.Acceleration);

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(droneRigidbody.velocity, Vector3.up);
            if (planarInput.sqrMagnitude <= 0.001f)
            {
                droneRigidbody.AddForce(-horizontalVelocity * planarBrake, ForceMode.Acceleration);
            }
        }

        private void ApplyAssistedRotation()
        {
            if (waitingForForwardRetakeoff || !verticalTakeoffComplete)
            {
                assistedYaw = droneRigidbody.rotation.eulerAngles.y;
                Quaternion levelRotation = Quaternion.Euler(0f, assistedYaw, 0f);
                droneRigidbody.MoveRotation(Quaternion.RotateTowards(
                    droneRigidbody.rotation,
                    levelRotation,
                    tiltFollowSpeed * Time.fixedDeltaTime));
                droneRigidbody.angularVelocity = Vector3.zero;
                return;
            }

            assistedYaw += pitchRollInput.x * yawTurnSpeed * Time.fixedDeltaTime;
            float horizontalSpeed = Vector3.ProjectOnPlane(droneRigidbody.velocity, Vector3.up).magnitude;
            float lowSpeedPitchUp = Mathf.Lerp(1f, 0.35f, Mathf.InverseLerp(climbStartSpeed, climbFullSpeed, horizontalSpeed));
            float targetPitch = Mathf.Clamp(-pitchRollInput.y * maxTiltAngle * lowSpeedPitchUp, -maxTiltAngle, maxTiltAngle);
            float targetRoll = Mathf.Clamp(-pitchRollInput.x * maxTiltAngle * 0.35f, -maxTiltAngle, maxTiltAngle);
            Quaternion targetRotation = Quaternion.Euler(targetPitch, assistedYaw, targetRoll);

            droneRigidbody.MoveRotation(Quaternion.RotateTowards(
                droneRigidbody.rotation,
                targetRotation,
                tiltFollowSpeed * Time.fixedDeltaTime));
            droneRigidbody.angularVelocity = Vector3.zero;
        }

        private void UpdateLandingAndRetakeoffState()
        {
            if (droneRigidbody == null)
            {
                return;
            }

            if (waitingForForwardRetakeoff)
            {
                if (pitchRollInput.y > inputDeadZone)
                {
                    BeginVerticalTakeoff();
                }

                return;
            }

            if (!verticalTakeoffComplete)
            {
                return;
            }

            float heightFromStart = droneRigidbody.position.y - flightStartAltitude;
            float horizontalSpeed = Vector3.ProjectOnPlane(droneRigidbody.velocity, Vector3.up).magnitude;
            bool isSlowEnough = horizontalSpeed <= landedSpeedThreshold && Mathf.Abs(droneRigidbody.velocity.y) <= landedSpeedThreshold;
            if (heightFromStart <= landedHeightTolerance && isSlowEnough)
            {
                waitingForForwardRetakeoff = true;
            }
        }

        private void BeginVerticalTakeoff()
        {
            flightStartAltitude = droneRigidbody != null ? droneRigidbody.position.y : transform.position.y;
            verticalTakeoffStartTime = Time.time;
            assistedYaw = droneRigidbody != null ? droneRigidbody.rotation.eulerAngles.y : transform.rotation.eulerAngles.y;
            smoothedFlightPower = 0f;
            currentThrottle = 0f;
            verticalTakeoffComplete = false;
            waitingForForwardRetakeoff = false;
        }

        private void ApplyRotorGroup(List<Transform> rotorPoints, float groupForce, float rollMix)
        {
            for (int i = 0; i < rotorPoints.Count; i++)
            {
                Transform rotorPoint = rotorPoints[i];
                if (rotorPoint == null)
                {
                    continue;
                }

                float sideSign = Mathf.Sign(Vector3.Dot(transform.right, rotorPoint.position - transform.position));
                float rotorForce = Mathf.Max(0f, groupForce - rollMix * sideSign);
                Vector3 forceDirection = useRotorPointUpDirection ? rotorPoint.up : transform.up;
                droneRigidbody.AddForceAtPosition(forceDirection * rotorForce, rotorPoint.position, ForceMode.Force);
            }
        }

        private void ApplyTailYaw()
        {
            float yawInput = throttleYawInput.x;
            if (Mathf.Abs(yawInput) <= inputDeadZone)
            {
                return;
            }

            if (tailRotorPoint != null)
            {
                Transform directionSource = tailForceDirection != null ? tailForceDirection : tailRotorPoint;
                droneRigidbody.AddForceAtPosition(directionSource.right * yawInput * tailRotorForce, tailRotorPoint.position, ForceMode.Force);
            }

            droneRigidbody.AddTorque(transform.up * yawInput * yawTorque, ForceMode.Force);
        }

        private void ApplyStabilization()
        {
            if (!autoLevel)
            {
                ApplyAngularDamping();
                return;
            }

            Vector3 localUp = transform.InverseTransformDirection(Vector3.up);
            Vector3 levelCorrection = new Vector3(localUp.z, 0f, -localUp.x) * levelTorque;
            droneRigidbody.AddRelativeTorque(levelCorrection, ForceMode.Force);
            ApplyAngularDamping();

            float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
            if (tiltAngle > maxTiltAngle)
            {
                Vector3 correctionAxis = Vector3.Cross(transform.up, Vector3.up);
                droneRigidbody.AddTorque(correctionAxis.normalized * levelTorque, ForceMode.Force);
            }
        }

        private void ApplyAngularDamping()
        {
            droneRigidbody.AddTorque(-droneRigidbody.angularVelocity * angularDamping, ForceMode.Force);
        }

        private void ApplyAirDrag()
        {
            Vector3 verticalVelocity = Vector3.Project(droneRigidbody.velocity, Vector3.up);
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(droneRigidbody.velocity, Vector3.up);

            droneRigidbody.AddForce(-horizontalVelocity * horizontalDrag, ForceMode.Force);
            droneRigidbody.AddForce(-verticalVelocity * verticalDrag, ForceMode.Force);
        }

        private void LimitHorizontalSpeed()
        {
            Vector3 verticalVelocity = Vector3.Project(droneRigidbody.velocity, Vector3.up);
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(droneRigidbody.velocity, Vector3.up);

            if (horizontalVelocity.magnitude <= maxHorizontalSpeed)
            {
                return;
            }

            droneRigidbody.velocity = horizontalVelocity.normalized * maxHorizontalSpeed + verticalVelocity;
        }

        private void UpdatePropellerVisuals(float throttleRatio)
        {
            float flightSpeedRatio = droneRigidbody != null
                ? Mathf.Clamp01(droneRigidbody.velocity.magnitude / Mathf.Max(0.1f, propellerFlightSpeedForMaxSpin))
                : 0f;
            float idleSpinRatio = isFlying ? Mathf.Clamp01(flyingIdleSpinRatio) : 0f;
            if (isFlying && !verticalTakeoffComplete)
            {
                idleSpinRatio *= Mathf.Max(smoothedFlightPower, GetVerticalTakeoffPowerRatio());
            }

            float spinRatio = Mathf.Clamp01(Mathf.Max(throttleRatio, flightSpeedRatio, idleSpinRatio));

            if (spinRatio <= 0.001f)
            {
                return;
            }

            float spinSpeed = Mathf.Lerp(minPropellerSpinSpeed, maxPropellerSpinSpeed, spinRatio);
            float deltaAngle = spinSpeed * Time.deltaTime;

            RotatePropellers(frontPropellerRotators, deltaAngle);
            RotatePropellers(rearPropellerRotators, deltaAngle);

            if (tailPropellerRotator != null)
            {
                float tailDirection = invertTailPropellerSpin ? -1f : 1f;
                float yawBoost = Mathf.Abs(throttleYawInput.x) * tailPropellerYawSpinBoost * Time.deltaTime;
                tailPropellerRotator.AddAxisAngle((deltaAngle + yawBoost) * tailDirection);
            }
        }

        private void DisablePropellerAutoRotate()
        {
            SetPropellerAutoRotate(frontPropellerRotators, false);
            SetPropellerAutoRotate(rearPropellerRotators, false);

            if (tailPropellerRotator != null)
            {
                tailPropellerRotator.SetAutoRotate(false);
            }
        }

        private static void SetPropellerAutoRotate(List<VR6_AxisRotateConstraint> propellers, bool enable)
        {
            for (int i = 0; i < propellers.Count; i++)
            {
                if (propellers[i] != null)
                {
                    propellers[i].SetAutoRotate(enable);
                }
            }
        }

        private void RotatePropellers(List<VR6_AxisRotateConstraint> propellers, float deltaAngle)
        {
            for (int i = 0; i < propellers.Count; i++)
            {
                if (propellers[i] != null)
                {
                    propellers[i].AddAxisAngle(deltaAngle);
                }
            }
        }

        private void WarnIfSetupInvalid()
        {
            if (droneRigidbody == null)
            {
                Debug.LogWarning("[VR6_DroneController] Rigidbody is not assigned.", this);
            }

            if (controlStickAction == null)
            {
                Debug.LogWarning("[VR6_DroneController] Control Stick Action is not assigned. The controller stick cannot drive the aircraft.", this);
            }

            if (frontRotorPoints.Count == 0 || rearRotorPoints.Count == 0)
            {
                Debug.LogWarning("[VR6_DroneController] Front and rear rotor points should be assigned before flying.", this);
            }

            if (tailRotorPoint == null)
            {
                Debug.LogWarning("[VR6_DroneController] Tail rotor point is not assigned. Yaw will only use torque.", this);
            }

            if (useRotorPointUpDirection)
            {
                Debug.LogWarning("[VR6_DroneController] useRotorPointUpDirection is enabled. If the rotor point local Up axis is tilted, throttle can push the aircraft forward/backward instead of straight upward.", this);
            }
        }

        private void LogInputSnapshot(Vector2 actionStickInput, Vector2 finalSingleStickInput)
        {
            if (!enableDebugLogs || Time.time < nextInputLogTime)
            {
                return;
            }

            nextInputLogTime = Time.time + Mathf.Max(0.1f, inputLogInterval);
            string actionName = cachedControlStickAction != null ? cachedControlStickAction.name : "None";
            bool actionEnabled = cachedControlStickAction != null && cachedControlStickAction.enabled;
            Log($"Input snapshot. action: {actionName}, actionEnabled: {actionEnabled}, actionStick: {actionStickInput}, invertLeftStickY: {invertLeftStickY}, finalStick: {finalSingleStickInput}, throttle: {throttleYawInput.y:F2}, yaw: {throttleYawInput.x:F2}, currentThrottle: {currentThrottle:F2}");
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[VR6_DroneController] {message}", this);
            }
        }

        #endregion

        #region ==========API==========

        /// <summary>
        /// Enables aircraft flight control and begins reading flight input.
        /// </summary>
        public void StartFly()
        {
            cachedControlStickAction = controlStickAction != null ? controlStickAction.action : null;
            cachedControlStickAction?.Enable();
            isFlying = true;
            loggedWaitingForStart = false;
            BeginVerticalTakeoff();
            ClearInput();
            Log("StartFly called. Drone flight control is now enabled.");
        }

        /// <summary>
        /// Disables aircraft flight control and clears input.
        /// </summary>
        public void StopFly()
        {
            isFlying = false;
            currentThrottle = 0f;
            smoothedFlightPower = 0f;
            verticalTakeoffComplete = false;
            waitingForForwardRetakeoff = false;
            ClearInput();
            Log("StopFly called. Drone flight control is now disabled.");
        }

        /// <summary>
        /// Fully resets the drone controller, aircraft pose, physics velocity, propeller input state, and flight state.
        /// </summary>
        public void ResetDroneController()
        {
            StopFly();

            if (droneRigidbody != null)
            {
                droneRigidbody.position = initialPosition;
                droneRigidbody.rotation = initialRotation;
                droneRigidbody.velocity = Vector3.zero;
                droneRigidbody.angularVelocity = Vector3.zero;
            }
            else
            {
                transform.position = initialPosition;
                transform.rotation = initialRotation;
            }

            flightStartAltitude = initialPosition.y;
            verticalTakeoffStartTime = Time.time;
            assistedYaw = initialRotation.eulerAngles.y;
            UpdatePropellerVisuals(0f);
            Log("ResetDroneController called. Drone controller has been fully reset.");
        }

        #endregion
    }
}
