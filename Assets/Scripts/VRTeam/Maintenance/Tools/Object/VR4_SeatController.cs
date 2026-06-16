using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// Moves the XR player to a seat when the seat is selected.
    /// Completing the step restores the player's original pose.
    /// </summary>
    public class VR4_SeatController : VR4_BaseObject
    {
        #region ==========Field==========

        [Header("Seat")]
        [SerializeField] private Transform seatPoint;

        [Header("XR Player")]
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private Transform headCamera;

        [Header("Objects Disabled While Seated")]
        [SerializeField] private GameObject objectToDisable1;
        [SerializeField] private GameObject objectToDisable2;

        [Header("Car Door")]
        [SerializeField] private VR4_SwitchObject carDoorSwitch;

        [Header("Interaction")]
        [SerializeField] private XRBaseInteractable seatInteractable;

        [Header("Screen Fade")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField, Min(0f)] private float fadeToBlackDuration = 0.35f;
        [SerializeField, Min(0f)] private float blackScreenDuration = 0.1f;
        [SerializeField, Min(0f)] private float fadeFromBlackDuration = 0.35f;
        [SerializeField] private bool useUnscaledTime = true;

        private Vector3 originalOriginPosition;
        private Quaternion originalOriginRotation;
        private bool hasSavedOriginalPose;
        private bool isSeated;
        private bool isChangingSeat;
        private Coroutine seatTransitionCoroutine;
        private bool seatInteractableDisabledByThis;

        public bool IsSeated => isSeated;

        #endregion

        #region ==========Unity Method==========

        private void Awake()
        {
            CacheSeatInteractable();
            SetFadeAlpha(0f);
            SetFadeInputBlocked(false);
        }

        private void OnEnable()
        {
            CacheSeatInteractable();

            if (seatInteractable != null)
            {
                seatInteractable.selectEntered.RemoveListener(OnSeatSelected);
                seatInteractable.selectEntered.AddListener(OnSeatSelected);
            }

        }

        private void OnDisable()
        {
            if (seatInteractable != null)
            {
                seatInteractable.selectEntered.RemoveListener(OnSeatSelected);
            }

            StopSeatTransition();
        }

        protected override void OnDestroy()
        {
            if (isSeated)
            {
                RestorePlayerPose();
            }

            SetControlledObjectsActive(true);
            base.OnDestroy();
        }

        #endregion

        #region ==========Logic==========

        private void CacheSeatInteractable()
        {
            if (seatInteractable == null)
            {
                seatInteractable = GetComponent<XRBaseInteractable>();
            }
        }

        private void OnSeatSelected(SelectEnterEventArgs args)
        {
            if (!CanInteract(args.interactorObject as XRBaseInteractor, "SitDown"))
            {
                return;
            }

            bool wasSeatedOrChanging = isSeated || isChangingSeat;
            SitDown();

            if (!wasSeatedOrChanging && (isSeated || isChangingSeat))
            {
                DisableSeatInteraction();
            }
        }

        private void DisableSeatInteraction()
        {
            if (seatInteractable == null || !seatInteractable.enabled)
            {
                return;
            }

            seatInteractable.enabled = false;
            seatInteractableDisabledByThis = true;
        }

        private void RestoreSeatInteraction()
        {
            if (!seatInteractableDisabledByThis || seatInteractable == null)
            {
                return;
            }

            seatInteractable.enabled = true;
            seatInteractableDisabledByThis = false;
        }

        private void SaveOriginalPlayerPose()
        {
            originalOriginPosition = xrOrigin.position;
            originalOriginRotation = xrOrigin.rotation;
            hasSavedOriginalPose = true;
        }

        private void AlignPlayerWithSeat()
        {
            Vector3 headForward = Vector3.ProjectOnPlane(headCamera.forward, Vector3.up);
            Vector3 seatForward = Vector3.ProjectOnPlane(seatPoint.forward, Vector3.up);

            if (headForward.sqrMagnitude > 0.001f && seatForward.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.SignedAngle(headForward, seatForward, Vector3.up);
                xrOrigin.RotateAround(headCamera.position, Vector3.up, angle);
            }

            xrOrigin.position += seatPoint.position - headCamera.position;
        }

        private void CompleteSitDown()
        {
            AlignPlayerWithSeat();

            if (carDoorSwitch != null)
            {
                carDoorSwitch.value = false;
            }

            SetControlledObjectsActive(false);
            isSeated = true;
            CompleteStep();
        }

        private IEnumerator PlaySeatTransition()
        {
            SetFadeInputBlocked(true);

            yield return Fade(0f, 1f, fadeToBlackDuration);

            CompleteSitDown();

            if (blackScreenDuration > 0f)
            {
                if (useUnscaledTime)
                {
                    yield return new WaitForSecondsRealtime(blackScreenDuration);
                }
                else
                {
                    yield return new WaitForSeconds(blackScreenDuration);
                }
            }

            yield return Fade(1f, 0f, fadeFromBlackDuration);

            SetFadeInputBlocked(false);
            isChangingSeat = false;
            seatTransitionCoroutine = null;
        }

        private IEnumerator Fade(float startAlpha, float targetAlpha, float duration)
        {
            if (duration <= 0f)
            {
                SetFadeAlpha(targetAlpha);
                yield break;
            }

            float elapsed = 0f;
            SetFadeAlpha(startAlpha);

            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
                yield return null;
            }

            SetFadeAlpha(targetAlpha);
        }

        private void StopSeatTransition()
        {
            if (seatTransitionCoroutine != null)
            {
                StopCoroutine(seatTransitionCoroutine);
                seatTransitionCoroutine = null;
            }

            isChangingSeat = false;
            SetFadeAlpha(0f);
            SetFadeInputBlocked(false);
        }

        private void SetFadeAlpha(float alpha)
        {
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = alpha;
            }
        }

        private void SetFadeInputBlocked(bool blocked)
        {
            if (fadeCanvasGroup == null)
            {
                return;
            }

            fadeCanvasGroup.interactable = blocked;
            fadeCanvasGroup.blocksRaycasts = blocked;
        }

        private void RestorePlayerPose()
        {
            if (!hasSavedOriginalPose || xrOrigin == null)
            {
                return;
            }

            xrOrigin.SetPositionAndRotation(originalOriginPosition, originalOriginRotation);
            hasSavedOriginalPose = false;
            isSeated = false;
        }

        private void SetControlledObjectsActive(bool active)
        {
            if (objectToDisable1 != null)
            {
                objectToDisable1.SetActive(active);
            }

            if (objectToDisable2 != null)
            {
                objectToDisable2.SetActive(active);
            }
        }

        #endregion

        #region ==========API==========

        /// <summary>
        /// Saves the current XR Origin pose and immediately moves the headset
        /// to the configured seat point.
        /// </summary>
        public void SitDown()
        {
            if (!IsAllowedByCurrentStep() ||
                isSeated ||
                isChangingSeat ||
                seatPoint == null ||
                xrOrigin == null ||
                headCamera == null)
            {
                return;
            }

            SaveOriginalPlayerPose();

            if (fadeCanvasGroup == null)
            {
                CompleteSitDown();
                return;
            }

            isChangingSeat = true;
            seatTransitionCoroutine = StartCoroutine(PlaySeatTransition());
        }

        /// <summary>
        /// Completes the BaseObject seat step while keeping the player seated.
        /// </summary>
        public void CompleteSeatStep()
        {
            if (!isSeated)
            {
                return;
            }

            CompleteStep();
        }

        /// <summary>
        /// Restores the player without completing the current step.
        /// </summary>
        public void LeaveSeat()
        {
            RestoreBeforeSitState();
        }

        /// <summary>
        /// Restores the XR player pose saved before sitting and re-enables
        /// the objects that were hidden while seated.
        /// </summary>
        public void RestoreBeforeSitState()
        {
            StopSeatTransition();
            RestorePlayerPose();
            SetControlledObjectsActive(true);
            RestoreSeatInteraction();
        }

        public override void ResetStepCompletion()
        {
            RestoreBeforeSitState();
            base.ResetStepCompletion();
        }

        #endregion
    }
}
