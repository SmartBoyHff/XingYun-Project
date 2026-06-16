using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================
// File: VR6_MainBodyController
// Module: VR6 three-body flying car display
// Purpose: Main body module animation and mode selection
// Created: 2026-04-28
// Updated: 2026-05-11
// ============================================================

namespace VRHelmet.VRTeam.Manufacturing.VehicleTest.ThreeBodyCar
{
    /// <summary>
    /// Selectable runtime presentation modes for the VehicleTest main body.
    /// </summary>
    public enum VR6_VehicleTestMode
    {
        /// <summary>
        /// No presentation mode has been selected yet.
        /// </summary>
        None,

        /// <summary>
        /// Car mode keeps the chassis visible and starts car driving control.
        /// </summary>
        Car,

        /// <summary>
        /// Flight mode keeps the flight body visible and hides the chassis.
        /// </summary>
        Flight
    }

    /// <summary>
    /// Controls VehicleTest body modules, mode visibility, and explosion animation.
    /// </summary>
    public class VR6_MainBodyController : MonoBehaviour
    {
        #region ==========Field==========

        [Header("Modules")]
        [SerializeField] private Transform DiPan;
        [SerializeField] private Transform FeiXingQi;
        [SerializeField] private Transform JiCang;

        [Header("Explosion Targets")]
        [SerializeField] private Transform FeiXingQiTarget;
        [SerializeField] private Transform JiCangTarget;

        [Header("Mode Control")]
        [SerializeField] private VR6_CarController carController;
        [SerializeField] private VR6_DroneController droneController;
        [SerializeField] private GameObject playCamera;
        [SerializeField] private GameObject cabin;

        [Header("Reset")]
        [SerializeField] private InputActionReference resetAction;
        [SerializeField] private VR6_CameraOrbit cameraOrbit;
        [SerializeField] private Rigidbody mainBodyRigidbody;
        [SerializeField] private bool resetOnlyInPlayMode = true;

        [Header("Move Settings")]
        [SerializeField] private float duration = 1f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool lockRigidbodiesDuringExplosion = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private Coroutine moveRoutine;
        private InputAction cachedResetAction;

        private bool isExploded;
        private bool hasInitialPose;

        private ModulePose mainBodyInitialPose;
        private ModulePose diPanInitialPose;
        private ModulePose feiXingQiInitialPose;
        private ModulePose jiCangInitialPose;
        private AttachmentPose diPanInitialAttachment;
        private AttachmentPose feiXingQiInitialAttachment;
        private AttachmentPose jiCangInitialAttachment;
        private AttachmentPose playCameraInitialAttachment;
        private AttachmentPose cabinInitialAttachment;
        private readonly List<RigidbodyState> cachedRigidbodyStates = new List<RigidbodyState>();

        private VR6_VehicleTestMode currentMode = VR6_VehicleTestMode.None;
        private int lastExplosionToggleFrame = -1;
        private bool moduleRigidbodiesLocked;
        private bool isPlayModeActive;

        #endregion

        #region ==========Unity Method==========

        private void Awake()
        {
            cachedResetAction = resetAction != null ? resetAction.action : null;
            if (mainBodyRigidbody == null)
            {
                mainBodyRigidbody = GetComponent<Rigidbody>();
            }

            Log($"Awake. Modules assigned: {HasModuleReferences()}, carController assigned: {carController != null}");
        }

        private void Start()
        {
            CacheMainBodyPose();
            CacheInitialPose();
            CacheModeObjectAttachments();
            Log("Start. Initial main body, module, and mode object poses cached.");
        }

        private void OnEnable()
        {
            cachedResetAction = resetAction != null ? resetAction.action : null;
            cachedResetAction?.Enable();
        }

        private void Update()
        {
            TryResetMainBodyByAction();
        }

        #endregion

        #region ==========Logic==========

        private struct ModulePose
        {
            /// <summary>
            /// Cached world position of the module.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// Cached world rotation of the module.
            /// </summary>
            public Quaternion Rotation;

            /// <summary>
            /// Creates a pose snapshot from the given transform.
            /// </summary>
            public ModulePose(Transform source)
            {
                Position = source.position;
                Rotation = source.rotation;
            }

            /// <summary>
            /// Applies this cached pose to the given transform.
            /// </summary>
            public void ApplyTo(Transform target)
            {
                target.position = Position;
                target.rotation = Rotation;
            }
        }

        private struct AttachmentPose
        {
            /// <summary>
            /// Cached original parent transform.
            /// </summary>
            public Transform Parent;

            /// <summary>
            /// Cached local position under the original parent.
            /// </summary>
            public Vector3 LocalPosition;

            /// <summary>
            /// Cached local rotation under the original parent.
            /// </summary>
            public Quaternion LocalRotation;

            /// <summary>
            /// Cached local scale under the original parent.
            /// </summary>
            public Vector3 LocalScale;

            /// <summary>
            /// Creates an attachment snapshot from the given transform.
            /// </summary>
            public AttachmentPose(Transform source)
            {
                Parent = source.parent;
                LocalPosition = source.localPosition;
                LocalRotation = source.localRotation;
                LocalScale = source.localScale;
            }

            /// <summary>
            /// Restores the cached parent and local transform.
            /// </summary>
            public void ApplyTo(Transform target)
            {
                target.SetParent(Parent, false);
                target.localPosition = LocalPosition;
                target.localRotation = LocalRotation;
                target.localScale = LocalScale;
            }
        }

        private struct ModuleAnimationFrame
        {
            /// <summary>
            /// Module transform being animated.
            /// </summary>
            public Transform Module;

            /// <summary>
            /// Pose at the beginning of the current animation.
            /// </summary>
            public ModulePose StartPose;

            /// <summary>
            /// Pose at the end of the current animation.
            /// </summary>
            public ModulePose EndPose;

            /// <summary>
            /// Creates one animation frame record for a module.
            /// </summary>
            public ModuleAnimationFrame(Transform module, ModulePose startPose, ModulePose endPose)
            {
                Module = module;
                StartPose = startPose;
                EndPose = endPose;
            }

            /// <summary>
            /// Applies interpolated position and rotation to the module.
            /// </summary>
            public void Apply(float t)
            {
                Module.position = Vector3.Lerp(StartPose.Position, EndPose.Position, t);
                Module.rotation = Quaternion.Slerp(StartPose.Rotation, EndPose.Rotation, t);
            }

            /// <summary>
            /// Snaps the module to the final pose.
            /// </summary>
            public void Complete()
            {
                EndPose.ApplyTo(Module);
            }
        }

        private struct RigidbodyState
        {
            /// <summary>
            /// Rigidbody being locked for module animation.
            /// </summary>
            public Rigidbody Rigidbody;

            /// <summary>
            /// Original kinematic state.
            /// </summary>
            public bool IsKinematic;

            /// <summary>
            /// Original gravity state.
            /// </summary>
            public bool UseGravity;

            /// <summary>
            /// Original constraints.
            /// </summary>
            public RigidbodyConstraints Constraints;

            /// <summary>
            /// Caches the physical state of a Rigidbody.
            /// </summary>
            public RigidbodyState(Rigidbody source)
            {
                Rigidbody = source;
                IsKinematic = source.isKinematic;
                UseGravity = source.useGravity;
                Constraints = source.constraints;
            }

            /// <summary>
            /// Restores the cached physical state.
            /// </summary>
            public void Restore()
            {
                if (Rigidbody == null)
                {
                    return;
                }

                Rigidbody.isKinematic = IsKinematic;
                Rigidbody.useGravity = UseGravity;
                Rigidbody.constraints = Constraints;
            }
        }

        private void CacheMainBodyPose()
        {
            mainBodyInitialPose = new ModulePose(transform);
        }

        private void CacheInitialPose()
        {
            if (!HasModuleReferences())
            {
                return;
            }

            diPanInitialPose = new ModulePose(DiPan);
            feiXingQiInitialPose = new ModulePose(FeiXingQi);
            jiCangInitialPose = new ModulePose(JiCang);
            diPanInitialAttachment = new AttachmentPose(DiPan);
            feiXingQiInitialAttachment = new AttachmentPose(FeiXingQi);
            jiCangInitialAttachment = new AttachmentPose(JiCang);

            hasInitialPose = true;
            Log("Initial module poses cached.");
        }

        private void CacheModeObjectAttachments()
        {
            if (playCamera != null)
            {
                playCameraInitialAttachment = new AttachmentPose(playCamera.transform);
            }

            if (cabin != null)
            {
                cabinInitialAttachment = new AttachmentPose(cabin.transform);
            }
        }

        private bool CanRunExplosion()
        {
            if (!HasModuleReferences())
            {
                Debug.LogError("Please assign DiPan, FeiXingQi, and JiCang modules.");
                return false;
            }

            if (FeiXingQiTarget == null || JiCangTarget == null)
            {
                Debug.LogError("Please assign FeiXingQiTarget and JiCangTarget.");
                return false;
            }

            return true;
        }

        private bool HasModuleReferences()
        {
            return DiPan != null && FeiXingQi != null && JiCang != null;
        }

        private void StopMoveRoutine()
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }
        }

        private void TryResetMainBodyByAction()
        {
            if (cachedResetAction == null)
            {
                return;
            }

            if (resetOnlyInPlayMode && !isPlayModeActive)
            {
                return;
            }

            if (cachedResetAction.WasPressedThisFrame())
            {
                ResetMainBody();
            }
        }

        private void ResetRuntimeControllers()
        {
            if (carController != null)
            {
                carController.ResetCarController();
            }

            if (droneController != null)
            {
                droneController.ResetDroneController();
            }
        }

        private void RestoreMainBodyPose()
        {
            mainBodyInitialPose.ApplyTo(transform);

            if (mainBodyRigidbody != null)
            {
                mainBodyRigidbody.velocity = Vector3.zero;
                mainBodyRigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void RestoreModulePoses()
        {
            if (!hasInitialPose)
            {
                Debug.LogWarning("[VR6_MainBodyController] Initial module poses were not cached. Reset cannot restore module transforms.", this);
                return;
            }

            SetModuleActive(DiPan, true);
            SetModuleActive(FeiXingQi, true);
            SetModuleActive(JiCang, true);

            RestoreModuleTransform(DiPan, diPanInitialAttachment, diPanInitialPose);
            RestoreModuleTransform(FeiXingQi, feiXingQiInitialAttachment, feiXingQiInitialPose);
            RestoreModuleTransform(JiCang, jiCangInitialAttachment, jiCangInitialPose);
        }

        private void RestoreModuleTransform(Transform module, AttachmentPose attachmentPose, ModulePose modulePose)
        {
            if (module == null)
            {
                return;
            }

            attachmentPose.ApplyTo(module);
            modulePose.ApplyTo(module);
        }

        private void RestoreModeObjectAttachments()
        {
            if (playCamera != null)
            {
                playCameraInitialAttachment.ApplyTo(playCamera.transform);
            }

            if (cabin != null)
            {
                cabinInitialAttachment.ApplyTo(cabin.transform);
            }
        }

        private void ExitPlayMode()
        {
            if (cameraOrbit != null)
            {
                cameraOrbit.SetPlayMode(false);
            }
        }

        private void ApplyMode(VR6_VehicleTestMode mode)
        {
            currentMode = mode;
            isPlayModeActive = currentMode != VR6_VehicleTestMode.None;
            StopMoveRoutine();
            Log($"ApplyMode: {currentMode}");

            switch (currentMode)
            {
                case VR6_VehicleTestMode.Car:
                    SetModuleActive(DiPan, true);
                    SetModuleActive(FeiXingQi, false);
                    AttachModeObjectsTo(DiPan);
                    StartCarController();
                    break;
                case VR6_VehicleTestMode.Flight:
                    SetModuleActive(FeiXingQi, true);
                    SetModuleActive(DiPan, false);
                    AttachModeObjectsTo(FeiXingQi);
                    StartDroneController();
                    break;
                default:
                    break;
            }
        }

        private void AttachModeObjectsTo(Transform parent)
        {
            if (parent == null)
            {
                Debug.LogWarning("[VR6_MainBodyController] Target mode parent is not assigned. Mode objects cannot be reparented.", this);
                return;
            }

            AttachGameObjectTo(playCamera, parent, "PlayCamera", true);
            AttachGameObjectTo(cabin, parent, "Cabin", false);
        }

        private void AttachGameObjectTo(GameObject targetObject, Transform parent, string objectName, bool keepWorldTransform)
        {
            if (targetObject == null)
            {
                Debug.LogWarning($"[VR6_MainBodyController] {objectName} is not assigned. It cannot be reparented for the selected mode.", this);
                return;
            }

            Transform targetTransform = targetObject.transform;
            Vector3 localPosition = targetTransform.localPosition;
            Quaternion localRotation = targetTransform.localRotation;
            Vector3 localScale = targetTransform.localScale;

            targetTransform.SetParent(parent, keepWorldTransform);

            if (!keepWorldTransform)
            {
                targetTransform.localPosition = localPosition;
                targetTransform.localRotation = localRotation;
                targetTransform.localScale = localScale;
            }

            Log($"{objectName} parent changed to: {parent.name}");
        }

        private void StartCarController()
        {
            if (carController != null)
            {
                Log("Calling carController.StartCarMove().");
                carController.StartCarMove();
            }
            else
            {
                Debug.LogWarning("[VR6_MainBodyController] CarController is not assigned. Car mode selected, but driving cannot start.", this);
            }
        }

        private void StartDroneController()
        {
            if (droneController != null)
            {
                Log("Calling droneController.StartFly().");
                droneController.StartFly();
            }
            else
            {
                Debug.LogWarning("[VR6_MainBodyController] DroneController is not assigned. Flight mode selected, but flight control cannot start.", this);
            }
        }

        private static void SetModuleActive(Transform module, bool active)
        {
            if (module != null)
            {
                module.gameObject.SetActive(active);
            }
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[VR6_MainBodyController] {message}", this);
            }
        }

        private ModuleAnimationFrame[] CreateExplosionFrames(bool moveToExplosion)
        {
            return new[]
            {
                new ModuleAnimationFrame(FeiXingQi, new ModulePose(FeiXingQi), moveToExplosion ? new ModulePose(FeiXingQiTarget) : feiXingQiInitialPose),
                new ModuleAnimationFrame(JiCang, new ModulePose(JiCang), moveToExplosion ? new ModulePose(JiCangTarget) : jiCangInitialPose)
            };
        }

        private IEnumerator MoveRoutine(ModuleAnimationFrame[] frames, bool targetExploded)
        {
            SetModuleRigidbodiesLocked(true);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = moveCurve.Evaluate(t);

                for (int i = 0; i < frames.Length; i++)
                {
                    frames[i].Apply(curveT);
                }

                yield return null;
            }

            for (int i = 0; i < frames.Length; i++)
            {
                frames[i].Complete();
            }

            moveRoutine = null;

            if (!targetExploded)
            {
                RestoreModuleRigidbodies();
            }
        }

        private void SetModuleRigidbodiesLocked(bool locked)
        {
            if (!lockRigidbodiesDuringExplosion)
            {
                return;
            }

            if (locked)
            {
                CacheAndLockModuleRigidbodies();
            }
        }

        private void CacheAndLockModuleRigidbodies()
        {
            if (!moduleRigidbodiesLocked)
            {
                cachedRigidbodyStates.Clear();
            }

            CacheAndLockRigidbodiesIn(FeiXingQi);
            CacheAndLockRigidbodiesIn(JiCang);
            moduleRigidbodiesLocked = true;
        }

        private void CacheAndLockRigidbodiesIn(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody body = rigidbodies[i];
                if (body == null)
                {
                    continue;
                }

                CacheRigidbodyStateIfNeeded(body);
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
            }
        }

        private void CacheRigidbodyStateIfNeeded(Rigidbody body)
        {
            if (moduleRigidbodiesLocked)
            {
                return;
            }

            cachedRigidbodyStates.Add(new RigidbodyState(body));
        }

        private void RestoreModuleRigidbodies()
        {
            for (int i = 0; i < cachedRigidbodyStates.Count; i++)
            {
                cachedRigidbodyStates[i].Restore();
            }

            cachedRigidbodyStates.Clear();
            moduleRigidbodiesLocked = false;
        }

        #endregion

        #region ==========API==========

        /// <summary>
        /// Gets the currently selected VehicleTest presentation mode.
        /// </summary>
        public VR6_VehicleTestMode CurrentMode => currentMode;

        /// <summary>
        /// Switches to car mode, hides the flight module, and starts car driving control.
        /// </summary>
        public void ActivateCarMode()
        {
            ApplyMode(VR6_VehicleTestMode.Car);
        }

        /// <summary>
        /// Switches to flight mode, hides the chassis module, and shows the flight module.
        /// </summary>
        public void ActivateFlightMode()
        {
            ApplyMode(VR6_VehicleTestMode.Flight);
        }

        /// <summary>
        /// Resets the VehicleTest main body, restores module poses, stops play controllers, and closes the play canvas.
        /// </summary>
        [ContextMenu("Reset Main Body")]
        public void ResetMainBody()
        {
            Log("ResetMainBody requested.");
            StopMoveRoutine();
            RestoreModuleRigidbodies();
            ResetRuntimeControllers();
            RestoreMainBodyPose();
            RestoreModulePoses();
            RestoreModeObjectAttachments();
            ExitPlayMode();

            currentMode = VR6_VehicleTestMode.None;
            isPlayModeActive = false;
            isExploded = false;
            lastExplosionToggleFrame = -1;
        }

        /// <summary>
        /// Toggles the three body modules between their assembled and exploded poses.
        /// </summary>
        [ContextMenu("Toggle Explosion")]
        public void ToggleExplosion()
        {
            if (lastExplosionToggleFrame == Time.frameCount)
            {
                Log("Duplicate ToggleExplosion call ignored in the same frame.");
                return;
            }

            lastExplosionToggleFrame = Time.frameCount;
            Log($"ToggleExplosion requested. Current exploded state: {isExploded}");

            if (!CanRunExplosion())
            {
                return;
            }

            if (!hasInitialPose)
            {
                Debug.LogWarning("[VR6_MainBodyController] Initial module poses were not cached. Explosion cannot run safely.", this);
                return;
            }

            StopMoveRoutine();

            isExploded = !isExploded;
            Log($"Starting explosion animation. Target exploded state: {isExploded}");
            moveRoutine = StartCoroutine(MoveRoutine(CreateExplosionFrames(isExploded), isExploded));
        }

        #endregion
    }
}
