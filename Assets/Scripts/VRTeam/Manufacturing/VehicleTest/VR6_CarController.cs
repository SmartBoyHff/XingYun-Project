using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// ============================================================
// File: VR6_CarController
// Module: VR6 three-body flying car display
// Purpose: WheelCollider-based chassis driving control
// Created: 2026-05-11
// Updated: 2026-05-11
// ============================================================

namespace VRHelmet.VRTeam.Manufacturing.VehicleTest.ThreeBodyCar
{
    /// <summary>
    /// Drives the vehicle chassis with WheelCollider torque, steering, braking, and wheel visuals.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VR6_CarController : MonoBehaviour
    {
        #region ==========Field==========

        [Header("Input")]
        [SerializeField] private InputActionReference primary2DAxisAction;
        [SerializeField] private InputActionReference brakeAction;
        [SerializeField] private bool startDrivingOnAwake;
        [SerializeField] private bool useKeyboardFallback = true;
        [SerializeField] private float inputDeadZone = 0.12f;

        [Header("Rigidbody")]
        [SerializeField] private Rigidbody carRigidbody;
        [SerializeField] private Transform driveDirectionReference;
        [SerializeField] private Transform centerOfMass;
        [SerializeField] private float downForce = 35f;
        [SerializeField] private float antiRollStrength = 3500f;

        [Header("Wheel Colliders")]
        [SerializeField] private WheelCollider frontLeftWheel;
        [SerializeField] private WheelCollider frontRightWheel;
        [SerializeField] private WheelCollider rearLeftWheel;
        [SerializeField] private WheelCollider rearRightWheel;
        [SerializeField] private bool frontWheelDrive;
        [SerializeField] private bool rearWheelDrive = true;

        [Header("Wheel Collider Settings")]
        [SerializeField] private bool applyWheelColliderSettingsOnAwake = true;
        [SerializeField] private float wheelMass = 20f;
        [SerializeField] private float suspensionDistance = 0.25f;
        [SerializeField] private float suspensionSpring = 35000f;
        [SerializeField] private float suspensionDamper = 4500f;
        [SerializeField] private float suspensionTargetPosition = 0.5f;
        [SerializeField] private float forwardFrictionStiffness = 1.6f;
        [SerializeField] private float sidewaysFrictionStiffness = 1.8f;

        [Header("Drive")]
        [SerializeField] private float motorTorque = 900f;
        [SerializeField] private float reverseTorque = 500f;
        [SerializeField] private float maxForwardSpeedKmh = 55f;
        [SerializeField] private float maxReverseSpeedKmh = 18f;
        [SerializeField] private float throttleAcceleration = 2.5f;
        [SerializeField] private float throttleRelease = 4f;

        [Header("Brake")]
        [SerializeField] private float brakeTorque = 2500f;
        [SerializeField] private float parkingBrakeTorque = 5000f;
        [SerializeField] private float idleBrakeTorque = 80f;

        [Header("Restore")]
        [SerializeField] private Transform restoreTarget;

        [Header("Steering")]
        [SerializeField] private float maxSteerAngle = 28f;
        [SerializeField] private float highSpeedSteerFactor = 0.45f;
        [SerializeField] private float steerResponse = 140f;
        [SerializeField] private List<Transform> frontSteerPivots = new List<Transform>();

        [Header("Wheel Visuals")]
        [SerializeField] private List<VR6_AxisRotateConstraint> wheelRotators = new List<VR6_AxisRotateConstraint>();
        [SerializeField] private float wheelRadius = 0.35f;
        [SerializeField] private bool invertWheelSpin;

        [Header("Wheel Dust")]
        [SerializeField] private ParticleSystem frontLeftDust;
        [SerializeField] private ParticleSystem frontRightDust;
        [SerializeField] private ParticleSystem rearLeftDust;
        [SerializeField] private ParticleSystem rearRightDust;
        [SerializeField] private float minDustInputRatio = 0.05f;
        [SerializeField] private float dustMaxSpeedKmh = 35f;
        [SerializeField] private float minDustEmissionRate = 6f;
        [SerializeField] private float maxDustEmissionRate = 45f;
        [SerializeField] private float dustMinSlip = 0.18f;
        [SerializeField] private float dustSlipForMaxEmission = 0.75f;
        [SerializeField] private bool requireGroundForDust;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private float wheelSpinWarningInterval = 1f;

        private InputAction cachedMoveAxisAction;
        private InputAction cachedBrakeAction;
        private readonly List<Quaternion> frontSteerBaseRotations = new List<Quaternion>();
        private WheelCollider[] allWheelColliders = Array.Empty<WheelCollider>();

        private Vector3 restoreInitialPosition;
        private Quaternion restoreInitialRotation;
        private Vector3 restoreInitialLocalPosition;
        private Quaternion restoreInitialLocalRotation;

        private float throttleInput;
        private float steerInput;
        private float currentMotorInput;
        private float currentSteerAngle;
        private bool brakeInput;
        private bool isDriving;
        private float stalledThrottleStartTime = -1f;
        private float nextWheelSpinWarningTime;
        private bool loggedWaitingForStart;

        #endregion

        #region ==========Unity Method==========

        private void Awake()
        {
            if (carRigidbody == null)
            {
                carRigidbody = GetComponent<Rigidbody>();
            }

            cachedMoveAxisAction = primary2DAxisAction != null ? primary2DAxisAction.action : null;
            cachedBrakeAction = brakeAction != null ? brakeAction.action : null;

            // Temporarily disabled so Rigidbody Center Of Mass can be adjusted directly in the Inspector.
            // if (centerOfMass != null)
            // {
            //     carRigidbody.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
            // }

            if (driveDirectionReference == null && carRigidbody != null)
            {
                driveDirectionReference = carRigidbody.transform;
            }

            BuildWheelCache();
            ConfigureWheelColliders();
            CacheSteeringVisuals();
            CacheRestorePose();

            isDriving = startDrivingOnAwake;
            LogWheelSetup();

            if (primary2DAxisAction == null)
            {
                Debug.LogWarning("[VR6_CarController] Primary 2D Axis Action is not assigned. VR joystick input will be zero.", this);
            }

            WarnIfPositionFrozen();
            ApplyParkingWheelState();
        }

        private void OnEnable()
        {
            cachedMoveAxisAction?.Enable();
            cachedBrakeAction?.Enable();
        }

        private void OnDisable()
        {
            // Do not disable shared InputAction assets here. Vehicle modes can reuse the same
            // 2D axis action, and disabling it from one controller makes the other read zero.
            ApplyParkingWheelState();
            StopAllWheelDustEffects();
        }

        private void Update()
        {
            if (!isDriving)
            {
                if (!loggedWaitingForStart)
                {
                    Log("Waiting for StartCarMove(). Input is ignored until car mode starts.");
                    loggedWaitingForStart = true;
                }

                ClearInput();
                StopAllWheelDustEffects();
                return;
            }

            ReadInput();
            UpdateSteeringVisuals();
            UpdateWheelRollingVisuals();
            UpdateWheelDustEffects();
        }

        private void FixedUpdate()
        {
            if (!isDriving)
            {
                ApplyParkingWheelState();
                return;
            }

            ApplyWheelSteering();
            ApplyWheelDriveAndBrake();
            ApplyDownForce();
            ApplyAntiRoll(frontLeftWheel, frontRightWheel);
            ApplyAntiRoll(rearLeftWheel, rearRightWheel);
            CheckDriveWheelSpin();
            CheckPotentialStall();
        }

        #endregion

        #region ==========Logic==========

        private void BuildWheelCache()
        {
            allWheelColliders = new[]
            {
                frontLeftWheel,
                frontRightWheel,
                rearLeftWheel,
                rearRightWheel
            };
        }

        private void ConfigureWheelColliders()
        {
            if (!applyWheelColliderSettingsOnAwake)
            {
                return;
            }

            for (int i = 0; i < allWheelColliders.Length; i++)
            {
                WheelCollider wheel = allWheelColliders[i];
                if (wheel == null)
                {
                    continue;
                }

                wheel.mass = wheelMass;
                wheel.suspensionDistance = suspensionDistance;
                wheel.ConfigureVehicleSubsteps(5f, 12, 15);

                JointSpring spring = wheel.suspensionSpring;
                spring.spring = suspensionSpring;
                spring.damper = suspensionDamper;
                spring.targetPosition = Mathf.Clamp01(suspensionTargetPosition);
                wheel.suspensionSpring = spring;

                WheelFrictionCurve forwardFriction = wheel.forwardFriction;
                forwardFriction.stiffness = forwardFrictionStiffness;
                wheel.forwardFriction = forwardFriction;

                WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
                sidewaysFriction.stiffness = sidewaysFrictionStiffness;
                wheel.sidewaysFriction = sidewaysFriction;
            }
        }

        private void CacheSteeringVisuals()
        {
            frontSteerBaseRotations.Clear();
            for (int i = 0; i < frontSteerPivots.Count; i++)
            {
                frontSteerBaseRotations.Add(frontSteerPivots[i] != null ? frontSteerPivots[i].localRotation : Quaternion.identity);
            }
        }

        private void CacheRestorePose()
        {
            if (restoreTarget == null)
            {
                restoreTarget = transform;
            }

            restoreInitialPosition = restoreTarget.position;
            restoreInitialRotation = restoreTarget.rotation;
            restoreInitialLocalPosition = restoreTarget.localPosition;
            restoreInitialLocalRotation = restoreTarget.localRotation;
        }

        private void ClearInput()
        {
            throttleInput = 0f;
            steerInput = 0f;
            brakeInput = false;
        }

        private void ReadInput()
        {
            Vector2 actionMoveInput = cachedMoveAxisAction != null ? cachedMoveAxisAction.ReadValue<Vector2>() : Vector2.zero;
            Vector2 moveInput = actionMoveInput;
            Vector2 keyboardMoveInput = Vector2.zero;

            if (useKeyboardFallback && Keyboard.current != null)
            {
                float keyboardSteer = ReadKeyboardAxis(Keyboard.current.aKey, Keyboard.current.dKey)
                                    + ReadKeyboardAxis(Keyboard.current.leftArrowKey, Keyboard.current.rightArrowKey);
                float keyboardThrottle = ReadKeyboardAxis(Keyboard.current.sKey, Keyboard.current.wKey)
                                       + ReadKeyboardAxis(Keyboard.current.downArrowKey, Keyboard.current.upArrowKey);
                keyboardMoveInput = new Vector2(Mathf.Clamp(keyboardSteer, -1f, 1f), Mathf.Clamp(keyboardThrottle, -1f, 1f));

                if (Mathf.Abs(keyboardSteer) > Mathf.Abs(moveInput.x))
                {
                    moveInput.x = Mathf.Clamp(keyboardSteer, -1f, 1f);
                }

                if (Mathf.Abs(keyboardThrottle) > Mathf.Abs(moveInput.y))
                {
                    moveInput.y = Mathf.Clamp(keyboardThrottle, -1f, 1f);
                }
            }

            steerInput = Mathf.Abs(moveInput.x) > inputDeadZone ? Mathf.Clamp(moveInput.x, -1f, 1f) : 0f;
            throttleInput = Mathf.Abs(moveInput.y) > inputDeadZone ? Mathf.Clamp(moveInput.y, -1f, 1f) : 0f;

            brakeInput = cachedBrakeAction != null && cachedBrakeAction.IsPressed();
            if (useKeyboardFallback && Keyboard.current != null)
            {
                brakeInput |= Keyboard.current.spaceKey.isPressed;
            }

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

            return value;
        }

        private void ApplyWheelSteering()
        {
            float speed01 = Mathf.Clamp01(Mathf.Abs(GetForwardSpeedKmh()) / Mathf.Max(1f, maxForwardSpeedKmh));
            float steerLimit = Mathf.Lerp(maxSteerAngle, maxSteerAngle * highSpeedSteerFactor, speed01);
            float targetSteerAngle = steerInput * steerLimit;
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle, steerResponse * Time.fixedDeltaTime);

            if (frontLeftWheel != null)
            {
                frontLeftWheel.steerAngle = currentSteerAngle;
            }

            if (frontRightWheel != null)
            {
                frontRightWheel.steerAngle = currentSteerAngle;
            }
        }

        private void ApplyWheelDriveAndBrake()
        {
            float inputStep = Mathf.Abs(throttleInput) > Mathf.Abs(currentMotorInput) ? throttleAcceleration : throttleRelease;
            currentMotorInput = Mathf.MoveTowards(currentMotorInput, throttleInput, inputStep * Time.fixedDeltaTime);

            float forwardSpeedKmh = GetForwardSpeedKmh();
            bool reversingAgainstForwardMotion = currentMotorInput < -inputDeadZone && forwardSpeedKmh > 1f;
            bool acceleratingAgainstReverseMotion = currentMotorInput > inputDeadZone && forwardSpeedKmh < -1f;
            bool inputShouldBrake = reversingAgainstForwardMotion || acceleratingAgainstReverseMotion;
            float limitedMotorTorque = inputShouldBrake ? 0f : GetLimitedMotorTorque(forwardSpeedKmh);
            float targetBrakeTorque = GetTargetBrakeTorque(inputShouldBrake);

            SetMotorTorque(frontLeftWheel, IsFrontDriveWheel() ? limitedMotorTorque : 0f);
            SetMotorTorque(frontRightWheel, IsFrontDriveWheel() ? limitedMotorTorque : 0f);
            SetMotorTorque(rearLeftWheel, IsRearDriveWheel() ? limitedMotorTorque : 0f);
            SetMotorTorque(rearRightWheel, IsRearDriveWheel() ? limitedMotorTorque : 0f);

            SetBrakeTorque(frontLeftWheel, targetBrakeTorque);
            SetBrakeTorque(frontRightWheel, targetBrakeTorque);
            SetBrakeTorque(rearLeftWheel, targetBrakeTorque);
            SetBrakeTorque(rearRightWheel, targetBrakeTorque);

        }

        private float GetLimitedMotorTorque(float forwardSpeedKmh)
        {
            if (Mathf.Abs(currentMotorInput) <= inputDeadZone)
            {
                return 0f;
            }

            if (currentMotorInput > 0f && forwardSpeedKmh >= maxForwardSpeedKmh)
            {
                return 0f;
            }

            if (currentMotorInput < 0f && forwardSpeedKmh <= -maxReverseSpeedKmh)
            {
                return 0f;
            }

            float torque = currentMotorInput > 0f ? motorTorque : reverseTorque;
            return currentMotorInput * torque;
        }

        private float GetTargetBrakeTorque(bool inputShouldBrake)
        {
            if (brakeInput)
            {
                return brakeTorque;
            }

            if (inputShouldBrake)
            {
                return brakeTorque * Mathf.Abs(currentMotorInput);
            }

            return Mathf.Abs(throttleInput) <= inputDeadZone ? idleBrakeTorque : 0f;
        }

        private void ApplyParkingWheelState()
        {
            currentMotorInput = 0f;
            currentSteerAngle = 0f;

            for (int i = 0; i < allWheelColliders.Length; i++)
            {
                WheelCollider wheel = allWheelColliders[i];
                if (wheel == null)
                {
                    continue;
                }

                wheel.motorTorque = 0f;
                wheel.brakeTorque = parkingBrakeTorque;
                wheel.steerAngle = 0f;
            }
        }

        private void ApplyDownForce()
        {
            if (carRigidbody == null)
            {
                return;
            }

            float speed = carRigidbody.velocity.magnitude;
            carRigidbody.AddForce(-GetDriveUp() * downForce * speed, ForceMode.Force);
        }

        private void ApplyAntiRoll(WheelCollider leftWheel, WheelCollider rightWheel)
        {
            if (carRigidbody == null || leftWheel == null || rightWheel == null || antiRollStrength <= 0f)
            {
                return;
            }

            float leftTravel = GetSuspensionTravel(leftWheel);
            float rightTravel = GetSuspensionTravel(rightWheel);
            float antiRollForce = (leftTravel - rightTravel) * antiRollStrength;

            if (leftWheel.isGrounded)
            {
                carRigidbody.AddForceAtPosition(leftWheel.transform.up * -antiRollForce, leftWheel.transform.position);
            }

            if (rightWheel.isGrounded)
            {
                carRigidbody.AddForceAtPosition(rightWheel.transform.up * antiRollForce, rightWheel.transform.position);
            }
        }

        private float GetSuspensionTravel(WheelCollider wheel)
        {
            if (!wheel.GetGroundHit(out WheelHit hit))
            {
                return 1f;
            }

            float localHitY = wheel.transform.InverseTransformPoint(hit.point).y;
            return (-localHitY - wheel.radius) / Mathf.Max(0.001f, wheel.suspensionDistance);
        }

        private void UpdateSteeringVisuals()
        {
            int count = Mathf.Min(frontSteerPivots.Count, frontSteerBaseRotations.Count);

            for (int i = 0; i < count; i++)
            {
                if (frontSteerPivots[i] == null)
                {
                    continue;
                }

                frontSteerPivots[i].localRotation = frontSteerBaseRotations[i] * Quaternion.Euler(0f, currentSteerAngle, 0f);
            }
        }

        private void UpdateWheelRollingVisuals()
        {
            if (wheelRadius <= 0.001f)
            {
                return;
            }

            float fallbackDeltaAngle = GetForwardSpeedMps() / (2f * Mathf.PI * wheelRadius) * 360f * Time.deltaTime;
            float spinDirection = invertWheelSpin ? -1f : 1f;

            for (int i = 0; i < wheelRotators.Count; i++)
            {
                if (wheelRotators[i] == null)
                {
                    continue;
                }

                WheelCollider wheel = GetWheelByVisualIndex(i);
                float deltaAngle = wheel != null ? wheel.rpm * 6f * Time.deltaTime : fallbackDeltaAngle;
                wheelRotators[i].AddAxisAngle(deltaAngle * spinDirection);
            }
        }

        private void UpdateWheelDustEffects()
        {
            UpdateWheelDustEffect(frontLeftWheel, frontLeftDust);
            UpdateWheelDustEffect(frontRightWheel, frontRightDust);
            UpdateWheelDustEffect(rearLeftWheel, rearLeftDust);
            UpdateWheelDustEffect(rearRightWheel, rearRightDust);
        }

        private void UpdateWheelDustEffect(WheelCollider wheel, ParticleSystem dustEffect)
        {
            if (dustEffect == null)
            {
                return;
            }

            WheelHit hit = default;
            bool hasGroundHit = wheel != null && wheel.GetGroundHit(out hit);
            if (requireGroundForDust && !hasGroundHit)
            {
                StopDustEffect(dustEffect);
                return;
            }

            float speedKmh = Mathf.Abs(GetForwardSpeedKmh());
            float slip = hasGroundHit ? Mathf.Max(Mathf.Abs(hit.forwardSlip), Mathf.Abs(hit.sidewaysSlip)) : 0f;
            float throttleRatio = Mathf.Abs(currentMotorInput);
            float speedRatio = Mathf.InverseLerp(0f, Mathf.Max(0.1f, dustMaxSpeedKmh), speedKmh);
            float slipRatio = Mathf.InverseLerp(dustMinSlip, Mathf.Max(dustMinSlip + 0.01f, dustSlipForMaxEmission), slip);
            float brakeRatio = brakeInput ? 0.5f : 0f;
            float emissionFactor = Mathf.Clamp01(Mathf.Max(throttleRatio, speedRatio, slipRatio, brakeRatio));

            if (emissionFactor <= minDustInputRatio)
            {
                StopDustEffect(dustEffect);
                return;
            }

            float emissionRate = Mathf.Lerp(minDustEmissionRate, maxDustEmissionRate, emissionFactor);

            SetDustEmissionRate(dustEffect, emissionRate);
            EnsureDustEffectCanPlay(dustEffect);

            if (!dustEffect.isPlaying)
            {
                dustEffect.Play();
            }
        }

        private void StopAllWheelDustEffects()
        {
            StopDustEffect(frontLeftDust);
            StopDustEffect(frontRightDust);
            StopDustEffect(rearLeftDust);
            StopDustEffect(rearRightDust);
        }

        private static void StopDustEffect(ParticleSystem dustEffect)
        {
            if (dustEffect != null && dustEffect.isPlaying)
            {
                dustEffect.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private static void SetDustEmissionRate(ParticleSystem dustEffect, float emissionRate)
        {
            ParticleSystem.EmissionModule emission = dustEffect.emission;
            emission.enabled = emissionRate > 0f;
            emission.rateOverTime = emissionRate;
        }

        private static void EnsureDustEffectCanPlay(ParticleSystem dustEffect)
        {
            if (!dustEffect.gameObject.activeSelf)
            {
                dustEffect.gameObject.SetActive(true);
            }

            ParticleSystem.MainModule main = dustEffect.main;
            main.loop = true;
        }

        private WheelCollider GetWheelByVisualIndex(int index)
        {
            return index >= 0 && index < allWheelColliders.Length ? allWheelColliders[index] : null;
        }

        private void SetMotorTorque(WheelCollider wheel, float torque)
        {
            if (wheel != null)
            {
                wheel.motorTorque = torque;
            }
        }

        private void SetBrakeTorque(WheelCollider wheel, float torque)
        {
            if (wheel != null)
            {
                wheel.brakeTorque = torque;
            }
        }

        private bool IsFrontDriveWheel()
        {
            return frontWheelDrive || !rearWheelDrive;
        }

        private bool IsRearDriveWheel()
        {
            return rearWheelDrive;
        }

        private float GetForwardSpeedMps()
        {
            if (carRigidbody == null)
            {
                return 0f;
            }

            return Vector3.Dot(carRigidbody.velocity, GetDriveForward());
        }

        private float GetForwardSpeedKmh()
        {
            return GetForwardSpeedMps() * 3.6f;
        }

        private Transform GetDriveTransform()
        {
            if (driveDirectionReference != null)
            {
                return driveDirectionReference;
            }

            return carRigidbody != null ? carRigidbody.transform : transform;
        }

        private Vector3 GetDriveForward()
        {
            Vector3 forward = Vector3.ProjectOnPlane(GetDriveTransform().forward, Vector3.up);

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = GetDriveTransform().forward;
            }

            return forward.normalized;
        }

        private Vector3 GetDriveUp()
        {
            return GetDriveTransform().up;
        }

        private int CountGroundedWheels()
        {
            int groundedCount = 0;

            for (int i = 0; i < allWheelColliders.Length; i++)
            {
                if (allWheelColliders[i] != null && allWheelColliders[i].isGrounded)
                {
                    groundedCount++;
                }
            }

            return groundedCount;
        }

        private void LogWheelSetup()
        {
            if (frontLeftWheel == null || frontRightWheel == null || rearLeftWheel == null || rearRightWheel == null)
            {
                Debug.LogWarning("[VR6_CarController] Four WheelCollider references are required for real wheel-based driving.", this);
            }
        }

        private void WarnIfPositionFrozen()
        {
            if (carRigidbody == null)
            {
                return;
            }

            RigidbodyConstraints constraints = carRigidbody.constraints;
            bool freezePosition = (constraints & RigidbodyConstraints.FreezePositionX) != 0
                               || (constraints & RigidbodyConstraints.FreezePositionY) != 0
                               || (constraints & RigidbodyConstraints.FreezePositionZ) != 0;

            if (freezePosition)
            {
                Debug.LogWarning($"[VR6_CarController] Rigidbody has frozen position constraints: {constraints}. If X/Z are frozen, the car cannot move forward.", this);
            }
        }

        private void CheckPotentialStall()
        {
            if (!enableDebugLogs || carRigidbody == null)
            {
                return;
            }

            if (Mathf.Abs(throttleInput) <= inputDeadZone)
            {
                stalledThrottleStartTime = -1f;
                return;
            }

            if (Mathf.Abs(GetForwardSpeedKmh()) > 0.5f)
            {
                stalledThrottleStartTime = -1f;
                return;
            }

            if (stalledThrottleStartTime < 0f)
            {
                stalledThrottleStartTime = Time.time;
                return;
            }

            if (Time.time - stalledThrottleStartTime > 1f)
            {
                stalledThrottleStartTime = Time.time;
                Debug.LogWarning($"[VR6_CarController] Throttle is non-zero but forward speed is still near zero. Grounded wheels: {CountGroundedWheels()}, drive wheels enabled: front={IsFrontDriveWheel()}, rear={IsRearDriveWheel()}, constraints: {carRigidbody.constraints}. Check WheelCollider references, wheel contact with ground, and whether another script is resetting the vehicle transform.", this);
            }
        }

        private void CheckDriveWheelSpin()
        {
            if (!enableDebugLogs || Mathf.Abs(throttleInput) <= inputDeadZone || Time.time < nextWheelSpinWarningTime)
            {
                return;
            }

            bool hasSpinningDriveWheel = false;
            hasSpinningDriveWheel |= IsFrontDriveWheel() && IsWheelSpinningWithoutTraction(frontLeftWheel);
            hasSpinningDriveWheel |= IsFrontDriveWheel() && IsWheelSpinningWithoutTraction(frontRightWheel);
            hasSpinningDriveWheel |= IsRearDriveWheel() && IsWheelSpinningWithoutTraction(rearLeftWheel);
            hasSpinningDriveWheel |= IsRearDriveWheel() && IsWheelSpinningWithoutTraction(rearRightWheel);

            if (!hasSpinningDriveWheel)
            {
                return;
            }

            nextWheelSpinWarningTime = Time.time + Mathf.Max(0.2f, wheelSpinWarningInterval);
            Debug.LogWarning("[VR6_CarController] Drive wheel is spinning without traction. The wheel is rotating fast but not effectively pushing the vehicle. Check WheelCollider grounding force, parent scale/rotation, wheel placement, and forward friction.", this);
        }

        private bool IsWheelSpinningWithoutTraction(WheelCollider wheel)
        {
            if (wheel == null || !wheel.GetGroundHit(out WheelHit hit))
            {
                return false;
            }

            bool wheelRpmTooHigh = Mathf.Abs(wheel.rpm) > 2500f;
            bool wheelSlipTooHigh = Mathf.Abs(hit.forwardSlip) > 0.8f;
            bool carStillSlow = Mathf.Abs(GetForwardSpeedKmh()) < 8f;

            return wheelRpmTooHigh && wheelSlipTooHigh && carStillSlow;
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[VR6_CarController] {message}", this);
            }
        }

        #endregion

        #region ==========API==========

        /// <summary>
        /// Enables vehicle driving and begins processing wheel torque input.
        /// </summary>
        public void StartCarMove()
        {
            cachedMoveAxisAction = primary2DAxisAction != null ? primary2DAxisAction.action : null;
            cachedBrakeAction = brakeAction != null ? brakeAction.action : null;
            cachedMoveAxisAction?.Enable();
            cachedBrakeAction?.Enable();

            isDriving = true;
            loggedWaitingForStart = false;
            currentMotorInput = 0f;
            currentSteerAngle = 0f;
            stalledThrottleStartTime = -1f;

            for (int i = 0; i < allWheelColliders.Length; i++)
            {
                if (allWheelColliders[i] == null)
                {
                    continue;
                }

                allWheelColliders[i].brakeTorque = 0f;
                allWheelColliders[i].motorTorque = 0f;
            }

            Log("StartCarMove called. WheelCollider driving is now enabled.");
        }

        /// <summary>
        /// Restores the vehicle to its cached start pose and clears all physics velocity and wheel torque.
        /// </summary>
        public void RestoreCar()
        {
            ClearInput();
            currentMotorInput = 0f;
            currentSteerAngle = 0f;
            stalledThrottleStartTime = -1f;
            Log("RestoreCar called.");

            if (carRigidbody != null && restoreTarget == transform)
            {
                transform.localPosition = restoreInitialLocalPosition;
                transform.localRotation = restoreInitialLocalRotation;
            }
            else if (restoreTarget != null)
            {
                restoreTarget.localPosition = restoreInitialLocalPosition;
                restoreTarget.localRotation = restoreInitialLocalRotation;
                restoreTarget.position = restoreInitialPosition;
                restoreTarget.rotation = restoreInitialRotation;
            }

            RestoreCarRigidbodyPose();

            ApplyParkingWheelState();
            StopAllWheelDustEffects();
        }

        private void RestoreCarRigidbodyPose()
        {
            if (carRigidbody == null)
            {
                return;
            }

            carRigidbody.position = restoreInitialPosition;
            carRigidbody.rotation = restoreInitialRotation;
            carRigidbody.velocity = Vector3.zero;
            carRigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Disables vehicle driving and parks the wheel colliders.
        /// </summary>
        public void StopCarMove()
        {
            isDriving = false;
            ClearInput();
            currentMotorInput = 0f;
            currentSteerAngle = 0f;
            stalledThrottleStartTime = -1f;
            ApplyParkingWheelState();
            StopAllWheelDustEffects();
            Log("StopCarMove called. WheelCollider driving is now disabled.");
        }

        /// <summary>
        /// Fully resets the car controller, vehicle pose, physics velocity, wheel torque, and drive state.
        /// </summary>
        public void ResetCarController()
        {
            StopCarMove();
            RestoreCar();
            Log("ResetCarController called. Car controller has been fully reset.");
        }

        #endregion
    }
}
