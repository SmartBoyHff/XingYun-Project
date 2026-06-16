using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// ============================================================
// 文件名：VR4_GrabObject
// 模块：模块4 - 维护保养
// 功能：抓取放置任务物体，负责在射线命中目标物并按下 Grab 时完成放置。
// 创建日期：2026-05-5
// 最后更新：2026-06-03
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// PickTask 的抓取物体完成源。
    /// 当手柄射线命中目标物体，并按下 Grab 时，将当前物体放入目标 Socket 并完成步骤。
    /// </summary>
    public class VR4_GrabObject : VR4_BaseObject
    {
        #region ==========Field==========
        public XRSocketInteractor targetSocket;
        public GameObject targetObject;

        public Action OnCorrectPlacement;

        [SerializeField] private bool enableGrabDebugLogs = true;

        private XRGrabInteractable grabInteractable;
        private Coroutine completePlacementCoroutine;
        private Transform lastRightRayHit;
        private Transform lastLeftRayHit;
        #endregion

        #region ==========Unity Method==========
        private void Awake()
        {
            CacheGrabInteractable();
        }

        private void OnEnable()
        {
            CacheGrabInteractable();
        }

        private void OnDisable()
        {
            StopCompletePlacementCoroutine();
        }

        protected override void OnDestroy()
        {
            StopCompletePlacementCoroutine();
            OnCorrectPlacement = null;
            base.OnDestroy();
        }

        private void Update()
        {
            LogRayHitChange();

            if (IsAnyControllerGrabbing())
            {
                LogGrabDebug($"Grab pressed. Object={name}, StepCompleted={IsStepCompleted}, TargetSocket={GetObjectName(targetSocket)}, TargetObject={GetObjectName(targetObject)}");
                TryCompletePlacementByRayGrab();
            }
        }
        #endregion

        #region ==========Logic==========
        private void CacheGrabInteractable()
        {
            if (grabInteractable == null)
            {
                grabInteractable = GetComponent<XRGrabInteractable>();
            }
        }

        private bool IsAnyControllerGrabbing()
        {
            VR4_ExperimentManager manager = VR4_ExperimentManager.Instance;
            return manager != null && manager.IsAnyControllerGrabbing();
        }

        private void TryCompletePlacementByRayGrab()
        {
            if (IsStepCompleted)
            {
                LogGrabDebug("Grab placement ignored: step already completed.");
                return;
            }

            if (targetSocket == null)
            {
                LogGrabDebug("Grab placement ignored: targetSocket is null.");
                return;
            }

            if (!IsAnyRayHittingTargetObject())
            {
                LogGrabDebug("Grab placement ignored: no ray is hitting targetObject.");
                return;
            }

            LogGrabDebug("Grab placement accepted: start placing interactive object into target socket.");
            StopCompletePlacementCoroutine();
            completePlacementCoroutine = StartCoroutine(CompletePlacementByRayGrab());
        }

        private bool IsAnyRayHittingTargetObject()
        {
            VR4_ExperimentManager manager = VR4_ExperimentManager.Instance;
            if (manager == null)
            {
                LogGrabDebug("Ray check failed: VR4_ExperimentManager.Instance is null.");
                return false;
            }

            bool rightHitTarget = IsRayHittingTargetObject(manager.rightRayInteractor, "RightRay");
            bool leftHitTarget = IsRayHittingTargetObject(manager.leftRayInteractor, "LeftRay");
            return rightHitTarget || leftHitTarget;
        }

        private bool IsRayHittingTargetObject(XRRayInteractor rayInteractor, string rayName)
        {
            if (rayInteractor == null || !rayInteractor.enabled)
            {
                LogGrabDebug($"{rayName} ray check skipped: interactor is null or disabled.");
                return false;
            }

            if (!rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                LogGrabDebug($"{rayName} ray hit: none.");
                return false;
            }

            Transform hitTransform = hit.collider != null ? hit.collider.transform : null;
            bool isTarget = IsTargetObject(hitTransform);
            LogGrabDebug($"{rayName} ray hit: {GetTransformPath(hitTransform)}, MatchTargetObject={isTarget}, TargetObject={GetObjectName(targetObject)}.");
            return isTarget;
        }

        private bool IsTargetObject(Transform hitTransform)
        {
            if (hitTransform == null)
            {
                return false;
            }

            Transform targetTransform = targetObject != null ? targetObject.transform : null;
            if (targetTransform == null)
            {
                return false;
            }

            return hitTransform == targetTransform ||
                   hitTransform.IsChildOf(targetTransform) ||
                   targetTransform.IsChildOf(hitTransform);
        }

        private IEnumerator CompletePlacementByRayGrab()
        {
            ReleaseFromCurrentInteractors();
            yield return null;

            PlaceObjectToTargetSocket();
            CompleteStep(OnCorrectPlacement);
            yield return null;
            LogSocketSelectionState("NextFrameAfterSelectEnter");
            completePlacementCoroutine = null;
        }

        private void ReleaseFromCurrentInteractors()
        {
            if (grabInteractable == null)
            {
                return;
            }

            IXRSelectInteractable interactable = grabInteractable;
            var selectingInteractors = new System.Collections.Generic.List<IXRSelectInteractor>(grabInteractable.interactorsSelecting);
            foreach (IXRSelectInteractor interactor in selectingInteractors)
            {
                if (interactor is XRBaseInteractor baseInteractor && baseInteractor.interactionManager != null)
                {
                    baseInteractor.interactionManager.SelectExit(interactor, interactable);
                }
            }
        }

        private void PlaceObjectToTargetSocket()
        {
            Transform attachTransform = targetSocket.attachTransform != null ? targetSocket.attachTransform : targetSocket.transform;
            transform.SetPositionAndRotation(attachTransform.position, attachTransform.rotation);
            LogGrabDebug($"Placed object to socket. Object={name}, Socket={GetObjectName(targetSocket)}, AttachTransform={GetTransformPath(attachTransform)}");

            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            TrySelectIntoTargetSocket();
            DetachToSceneRoot(attachTransform);
            LogSocketSelectionState("AfterPlaceObjectToTargetSocket");
        }

        private void TrySelectIntoTargetSocket()
        {
            if (grabInteractable == null || targetSocket == null || targetSocket.hasSelection)
            {
                return;
            }

            XRInteractionManager interactionManager = targetSocket.interactionManager;
            if (interactionManager != null)
            {
                interactionManager.SelectEnter((IXRSelectInteractor)targetSocket, (IXRSelectInteractable)grabInteractable);
            }
        }

        private void DetachToSceneRoot(Transform attachTransform)
        {
            Transform previousParent = transform.parent;
            transform.SetParent(null, true);

            if (attachTransform != null)
            {
                transform.SetPositionAndRotation(attachTransform.position, attachTransform.rotation);
            }

            LogGrabDebug($"Detached object transform to scene root. Object={name}, PreviousParent={GetObjectName(previousParent)}, NewParent={GetObjectName(transform.parent)}");
        }

        private void StopCompletePlacementCoroutine()
        {
            if (completePlacementCoroutine == null)
            {
                return;
            }

            StopCoroutine(completePlacementCoroutine);
            completePlacementCoroutine = null;
        }

        private void LogRayHitChange()
        {
            if (!enableGrabDebugLogs)
            {
                return;
            }

            VR4_ExperimentManager manager = VR4_ExperimentManager.Instance;
            if (manager == null)
            {
                return;
            }

            LogRayHitChange(manager.rightRayInteractor, "RightRay", ref lastRightRayHit);
            LogRayHitChange(manager.leftRayInteractor, "LeftRay", ref lastLeftRayHit);
        }

        private void LogRayHitChange(XRRayInteractor rayInteractor, string rayName, ref Transform lastHit)
        {
            Transform currentHit = GetCurrentRayHit(rayInteractor);
            if (currentHit == lastHit)
            {
                return;
            }

            lastHit = currentHit;
            LogGrabDebug($"{rayName} current hit changed: {GetTransformPath(currentHit)}, MatchTargetObject={IsTargetObject(currentHit)}, TargetObject={GetObjectName(targetObject)}.");
        }

        private Transform GetCurrentRayHit(XRRayInteractor rayInteractor)
        {
            if (rayInteractor == null || !rayInteractor.enabled)
            {
                return null;
            }

            if (!rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit) || hit.collider == null)
            {
                return null;
            }

            return hit.collider.transform;
        }

        private void LogGrabDebug(string message)
        {
            if (!enableGrabDebugLogs)
            {
                return;
            }

            Debug.Log($"[VR4_GrabObject] {message}", this);
        }

        private void LogSocketSelectionState(string context)
        {
            if (!enableGrabDebugLogs)
            {
                return;
            }

            if (targetSocket == null || grabInteractable == null)
            {
                LogGrabDebug($"{context}: Cannot check socket selection. Socket={GetObjectName(targetSocket)}, GrabInteractable={GetObjectName(grabInteractable)}.");
                return;
            }

            bool socketSelectingGrab = targetSocket.IsSelecting((IXRSelectInteractable)grabInteractable);
            bool grabSelectedBySocket = grabInteractable.interactorsSelecting.Contains((IXRSelectInteractor)targetSocket);
            LogGrabDebug(
                $"{context}: Socket={GetObjectName(targetSocket)}, " +
                $"SocketGameObject={GetObjectName(targetSocket.gameObject)}, " +
                $"SocketHasSelection={targetSocket.hasSelection}, " +
                $"SocketSelectingGrab={socketSelectingGrab}, " +
                $"Grab={GetObjectName(grabInteractable)}, " +
                $"GrabIsSelected={grabInteractable.isSelected}, " +
                $"GrabSelectedBySocket={grabSelectedBySocket}, " +
                $"GrabSelectingInteractors=[{GetSelectingInteractorNames()}]");
        }

        private string GetSelectingInteractorNames()
        {
            if (grabInteractable == null || grabInteractable.interactorsSelecting.Count == 0)
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < grabInteractable.interactorsSelecting.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                IXRSelectInteractor interactor = grabInteractable.interactorsSelecting[i];
                builder.Append(interactor is UnityEngine.Object unityObject ? unityObject.name : interactor?.ToString());
            }

            return builder.ToString();
        }

        private static string GetObjectName(UnityEngine.Object unityObject)
        {
            return unityObject != null ? unityObject.name : "null";
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null)
            {
                return "null";
            }

            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
        #endregion

        #region ==========API==========
        public void Configure(
            XRSocketInteractor socket,
            GameObject target,
            Action onCorrectPlacement)
        {
            targetSocket = socket;
            targetObject = target;
            OnCorrectPlacement = onCorrectPlacement;
            ResetStepCompletion();
            StopCompletePlacementCoroutine();
        }

        public void Clear()
        {
            StopCompletePlacementCoroutine();
            OnCorrectPlacement = null;
            targetSocket = null;
            targetObject = null;
            ClearStepInteractionPermission();
            ResetStepCompletion();
            enabled = false;
        }
        #endregion
    }
}
