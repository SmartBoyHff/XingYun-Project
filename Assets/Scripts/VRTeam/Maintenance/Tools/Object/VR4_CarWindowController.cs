using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// Selects one of four window buttons with Grab, then moves the
    /// corresponding window up or down while the A or B button is held.
    /// </summary>
    public class VR4_CarWindowController : VR4_BaseObject
    {
        #region ==========Field==========

        [Header("Window Buttons")]
        [SerializeField] private GameObject frontLeftButton;
        [SerializeField] private GameObject frontRightButton;
        [SerializeField] private GameObject rearLeftButton;
        [SerializeField] private GameObject rearRightButton;

        [Header("Windows")]
        [SerializeField] private GameObject frontLeftWindow;
        [SerializeField] private GameObject frontRightWindow;
        [SerializeField] private GameObject rearLeftWindow;
        [SerializeField] private GameObject rearRightWindow;

        [Header("Window Movement")]
        [SerializeField] private float minWindowY;
        [SerializeField] private float maxWindowY = 0.5f;
        [SerializeField, Min(0f)] private float moveSpeed = 0.2f;

        [Header("Input")]
        [SerializeField] private InputActionReference grabAction;
        [SerializeField] private InputActionReference aButtonAction;
        [SerializeField] private InputActionReference bButtonAction;

        [Header("Answer")]
        public int normalAnswerValue = 1;
        public int abnormalAnswerValue = 2;
        public int[] answerValues = new int[4];

        [Header("Topic Data Write Back")]
        [SerializeField] private VR4_TopicManager topicManager;
        [SerializeField] private int targetTopicTableIndex;
        [SerializeField] private int targetTopicDataStartIndex;

        [Header("Step Completion")]
        [SerializeField, Min(0f)] private float completeDelay = 1f;
        [SerializeField] private VR4_SeatController seatController;
        [SerializeField] private bool frontLeftWindowChecked;
        [SerializeField] private bool frontRightWindowChecked;
        [SerializeField] private bool rearLeftWindowChecked;
        [SerializeField] private bool rearRightWindowChecked;

        private GameObject selectedWindow;
        private int selectedButtonIndex = -1;
        private bool grabEnabledByThis;
        private bool aButtonEnabledByThis;
        private bool bButtonEnabledByThis;
        private Coroutine completeDelayCoroutine;
        private bool isAButtonHeld;
        private bool isBButtonHeld;

        private const int AnswerCapacity = 4;

        #endregion

        #region ==========Unity Method==========

        private void OnValidate()
        {
            EnsureAnswerArray();
        }

        private void Awake()
        {
            EnsureAnswerArray();
        }

        private void OnEnable()
        {
            EnableActionIfNeeded(grabAction, ref grabEnabledByThis);
            EnableActionIfNeeded(aButtonAction, ref aButtonEnabledByThis);
            EnableActionIfNeeded(bButtonAction, ref bButtonEnabledByThis);
            BindWindowButtonActions();
        }

        private void OnDisable()
        {
            UnbindWindowButtonActions();
            DisableActionIfNeeded(grabAction, ref grabEnabledByThis);
            DisableActionIfNeeded(aButtonAction, ref aButtonEnabledByThis);
            DisableActionIfNeeded(bButtonAction, ref bButtonEnabledByThis);

            StopCompleteDelay();
            selectedWindow = null;
            selectedButtonIndex = -1;
            isAButtonHeld = false;
            isBButtonHeld = false;
        }

        private void Update()
        {
            if (!IsAllowedByCurrentStep())
            {
                return;
            }

            if (WasPressedThisFrame(grabAction))
            {
                selectedWindow = null;
                selectedButtonIndex = -1;
            }

            if (IsPressed(grabAction) && selectedButtonIndex < 0)
            {
                SelectTargetWindow();
            }

            MoveSelectedWindow();
        }

        #endregion

        #region ==========Logic==========

        private void SelectTargetWindow()
        {
            TrySelectWindowByRay();
        }

        private bool TrySelectWindowByRay()
        {
            VR4_ExperimentManager manager = VR4_ExperimentManager.Instance;
            if (manager == null)
            {
                return false;
            }

            return TrySelectWindowByRay(manager.rightRayInteractor) ||
                   TrySelectWindowByRay(manager.leftRayInteractor);
        }

        private bool TrySelectWindowByRay(XRRayInteractor rayInteractor)
        {
            if (rayInteractor == null || !rayInteractor.enabled)
            {
                return false;
            }

            if (TrySelectWindowFromSelectedInteractable(rayInteractor))
            {
                return true;
            }

            if (!rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit) || hit.collider == null)
            {
                return false;
            }

            int buttonIndex = GetButtonIndex(hit.collider);
            if (buttonIndex < 0)
            {
                return false;
            }

            SelectWindow(buttonIndex);
            return true;
        }

        private bool TrySelectWindowFromSelectedInteractable(XRRayInteractor rayInteractor)
        {
            for (int i = 0; i < rayInteractor.interactablesSelected.Count; i++)
            {
                IXRSelectInteractable selectedInteractable = rayInteractor.interactablesSelected[i];
                if (!(selectedInteractable is Component selectedComponent))
                {
                    continue;
                }

                int buttonIndex = GetButtonIndex(selectedComponent.transform);
                if (buttonIndex < 0)
                {
                    continue;
                }

                SelectWindow(buttonIndex);
                return true;
            }

            return false;
        }

        private void SelectWindow(int buttonIndex)
        {
            selectedButtonIndex = buttonIndex;
            MarkWindowChecked(buttonIndex);
            selectedWindow = IsWindowNormal(buttonIndex) ? GetWindow(buttonIndex) : null;
        }

        private bool IsWindowNormal(int windowIndex)
        {
            EnsureAnswerArray();
            return windowIndex >= 0 &&
                   windowIndex < answerValues.Length &&
                   answerValues[windowIndex] == normalAnswerValue;
        }

        private void GenerateRandomAnswers()
        {
            EnsureAnswerArray();

            for (int i = 0; i < answerValues.Length; i++)
            {
                answerValues[i] = UnityEngine.Random.value < 0.5f
                    ? normalAnswerValue
                    : abnormalAnswerValue;
            }
        }

        private void MarkWindowChecked(int buttonIndex)
        {
            switch (buttonIndex)
            {
                case 0:
                    frontLeftWindowChecked = true;
                    break;
                case 1:
                    frontRightWindowChecked = true;
                    break;
                case 2:
                    rearLeftWindowChecked = true;
                    break;
                case 3:
                    rearRightWindowChecked = true;
                    break;
            }

            if (AreAllWindowsChecked() && completeDelayCoroutine == null)
            {
                WriteAnswersToTopicData();
                completeDelayCoroutine = StartCoroutine(CompleteStepDelayed());
            }
        }

        private bool AreAllWindowsChecked()
        {
            return frontLeftWindowChecked &&
                   frontRightWindowChecked &&
                   rearLeftWindowChecked &&
                   rearRightWindowChecked;
        }

        private IEnumerator CompleteStepDelayed()
        {
            if (completeDelay > 0f)
            {
                yield return new WaitForSeconds(completeDelay);
            }

            completeDelayCoroutine = null;

            if (AreAllWindowsChecked())
            {
                CompleteStep();

                if (seatController != null)
                {
                    seatController.RestoreBeforeSitState();
                }
            }
        }

        private void StopCompleteDelay()
        {
            if (completeDelayCoroutine == null)
            {
                return;
            }

            StopCoroutine(completeDelayCoroutine);
            completeDelayCoroutine = null;
        }

        private void ResetWindowChecks()
        {
            StopCompleteDelay();
            frontLeftWindowChecked = false;
            frontRightWindowChecked = false;
            rearLeftWindowChecked = false;
            rearRightWindowChecked = false;
            selectedWindow = null;
            selectedButtonIndex = -1;
        }

        private void WriteAnswersToTopicData()
        {
            EnsureAnswerArray();

            if (topicManager == null || topicManager.topicTables == null)
            {
                return;
            }

            if (targetTopicTableIndex < 0 || targetTopicTableIndex >= topicManager.topicTables.Count)
            {
                return;
            }

            TopicTable table = topicManager.topicTables[targetTopicTableIndex];
            if (table == null || table.topicDatas == null)
            {
                return;
            }

            for (int i = 0; i < answerValues.Length; i++)
            {
                int topicDataIndex = targetTopicDataStartIndex + i;
                if (topicDataIndex < 0 || topicDataIndex >= table.topicDatas.Count)
                {
                    return;
                }

                table.topicDatas[topicDataIndex].choiceAnswer = answerValues[i];
            }
        }

        private void EnsureAnswerArray()
        {
            if (answerValues == null)
            {
                answerValues = new int[AnswerCapacity];
            }

            if (answerValues.Length != AnswerCapacity)
            {
                Array.Resize(ref answerValues, AnswerCapacity);
            }
        }

        private void BindWindowButtonActions()
        {
            if (aButtonAction != null && aButtonAction.action != null)
            {
                aButtonAction.action.performed -= OnAButtonPerformed;
                aButtonAction.action.canceled -= OnAButtonCanceled;
                aButtonAction.action.performed += OnAButtonPerformed;
                aButtonAction.action.canceled += OnAButtonCanceled;
            }

            if (bButtonAction != null && bButtonAction.action != null)
            {
                bButtonAction.action.performed -= OnBButtonPerformed;
                bButtonAction.action.canceled -= OnBButtonCanceled;
                bButtonAction.action.performed += OnBButtonPerformed;
                bButtonAction.action.canceled += OnBButtonCanceled;
            }
        }

        private void UnbindWindowButtonActions()
        {
            if (aButtonAction != null && aButtonAction.action != null)
            {
                aButtonAction.action.performed -= OnAButtonPerformed;
                aButtonAction.action.canceled -= OnAButtonCanceled;
            }

            if (bButtonAction != null && bButtonAction.action != null)
            {
                bButtonAction.action.performed -= OnBButtonPerformed;
                bButtonAction.action.canceled -= OnBButtonCanceled;
            }
        }

        private void OnAButtonPerformed(InputAction.CallbackContext context)
        {
            isAButtonHeld = true;
        }

        private void OnAButtonCanceled(InputAction.CallbackContext context)
        {
            isAButtonHeld = false;
        }

        private void OnBButtonPerformed(InputAction.CallbackContext context)
        {
            isBButtonHeld = true;
        }

        private void OnBButtonCanceled(InputAction.CallbackContext context)
        {
            isBButtonHeld = false;
        }

        private void MoveSelectedWindow()
        {
            if (selectedWindow == null || isAButtonHeld == isBButtonHeld)
            {
                return;
            }

            float minY = Mathf.Min(minWindowY, maxWindowY);
            float maxY = Mathf.Max(minWindowY, maxWindowY);
            float targetY = isAButtonHeld ? maxY : minY;

            Transform windowTransform = selectedWindow.transform;
            Vector3 localPosition = windowTransform.localPosition;
            localPosition.y = Mathf.Clamp(localPosition.y, minY, maxY);
            localPosition.y = Mathf.MoveTowards(
                localPosition.y,
                targetY,
                moveSpeed * Time.deltaTime);
            windowTransform.localPosition = localPosition;
        }

        private int GetButtonIndex(Collider other)
        {
            return other != null ? GetButtonIndex(other.transform) : -1;
        }

        private int GetButtonIndex(Transform targetTransform)
        {
            if (targetTransform == null)
            {
                return -1;
            }

            if (IsButtonTransform(targetTransform, frontLeftButton))
            {
                return 0;
            }

            if (IsButtonTransform(targetTransform, frontRightButton))
            {
                return 1;
            }

            if (IsButtonTransform(targetTransform, rearLeftButton))
            {
                return 2;
            }

            if (IsButtonTransform(targetTransform, rearRightButton))
            {
                return 3;
            }

            return -1;
        }

        private static bool IsButtonTransform(Transform targetTransform, GameObject button)
        {
            return button != null &&
                   (targetTransform == button.transform ||
                    targetTransform.IsChildOf(button.transform));
        }

        private GameObject GetWindow(int buttonIndex)
        {
            switch (buttonIndex)
            {
                case 0:
                    return frontLeftWindow;
                case 1:
                    return frontRightWindow;
                case 2:
                    return rearLeftWindow;
                case 3:
                    return rearRightWindow;
                default:
                    return null;
            }
        }

        private static bool WasPressedThisFrame(InputActionReference actionReference)
        {
            return actionReference != null &&
                   actionReference.action != null &&
                   actionReference.action.WasPressedThisFrame();
        }

        private static bool IsPressed(InputActionReference actionReference)
        {
            return actionReference != null &&
                   actionReference.action != null &&
                   actionReference.action.IsPressed();
        }

        private static void EnableActionIfNeeded(
            InputActionReference actionReference,
            ref bool enabledByThis)
        {
            enabledByThis = false;

            if (actionReference == null || actionReference.action == null || actionReference.action.enabled)
            {
                return;
            }

            actionReference.action.Enable();
            enabledByThis = true;
        }

        private static void DisableActionIfNeeded(
            InputActionReference actionReference,
            ref bool enabledByThis)
        {
            if (enabledByThis && actionReference != null && actionReference.action != null)
            {
                actionReference.action.Disable();
            }

            enabledByThis = false;
        }

        #endregion

        #region ==========API==========

        public override void ResetStepCompletion()
        {
            base.ResetStepCompletion();
            ResetWindowChecks();
            GenerateRandomAnswers();
        }

        #endregion
    }
}
