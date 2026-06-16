using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// Depth gauge interaction script for module 4 maintenance flow.
    /// </summary>
    public class VR4_DepthGauge : VR4_BaseObject
    {
        #region ==========Field==========
        [Header("Display")]
        public TextMeshProUGUI ValueText;

        [Header("XR Interaction")]
        public XRSimpleInteractable powerButton;

        [Header("Depth Range")]
        public float randomMinDepth = 2.0f;
        public float randomMaxDepth = 5.5f;
        public float normalMinDepth = 2.5f;
        public float normalMaxDepth = 5.5f;

        [Header("Display Animation")]
        [SerializeField] private float valueDisplayStep = 0.1f;
        [SerializeField] private float valueDisplayStepInterval = 0.03f;
        [SerializeField] private float completeDelay = 2.0f;

        [Header("Answer")]
        public int normalAnswerValue = 1;
        public int abnormalAnswerValue = 2;
        public int[] answerValues = new int[AnswerCapacity];

        [Header("Topic Data Write Back")]
        public VR4_TopicManager topicManager;
        public int targetTopicTableIndex = 0;
        public int targetTopicDataStartIndex = 0;

        [SerializeField] private bool enableRuntimeLogs = false;
        public bool logicEnabled = false;

        private const int AnswerCapacity = 4;

        private float currentDepth;
        private bool hasDepth = false;
        private bool currentDepthResolved = false;
        private Coroutine valueDisplayRoutine;
        private int answerIndex = 0;
        private Vector3 gaugeInitialPosition;
        private Quaternion gaugeInitialRotation;
        private bool hasGaugeInitialPose = false;
        #endregion

        #region ==========Unity Method==========
        private void OnValidate()
        {
            if (randomMaxDepth < randomMinDepth)
            {
                randomMaxDepth = randomMinDepth;
            }

            if (normalMaxDepth < normalMinDepth)
            {
                normalMaxDepth = normalMinDepth;
            }

            EnsureAnswerArray();
        }

        private void Awake()
        {
            EnsureAnswerArray();
            CacheGaugeInitialPose();
        }

        private void Start()
        {
            BindButtonEvents();
            ResetDisplayValue();
        }

        private void OnEnable()
        {
            BindButtonEvents();
        }

        private void OnDisable()
        {
            UnbindButtonEvents();
            ResetDepthCheckRuntime();
        }
        #endregion

        #region ==========Logic==========
        private void BindButtonEvents()
        {
            if (powerButton != null)
            {
                powerButton.selectEntered.RemoveListener(OnPowerButtonSelected);
                powerButton.selectEntered.AddListener(OnPowerButtonSelected);
            }

        }

        private void UnbindButtonEvents()
        {
            if (powerButton != null)
            {
                powerButton.selectEntered.RemoveListener(OnPowerButtonSelected);
            }

        }

        private void OnPowerButtonSelected(SelectEnterEventArgs args)
        {
            ShowValueText();

            if (powerButton != null)
            {
                powerButton.enabled = false;
            }
        }

        private void GenerateRandomDepth()
        {
            currentDepth = UnityEngine.Random.Range(randomMinDepth, randomMaxDepth);
            hasDepth = true;
            currentDepthResolved = false;

            ShowValueText();
            StoreAnswerValue(IsDepthReasonable() ? normalAnswerValue : abnormalAnswerValue);
            StartValueDisplayTransition(0f, currentDepth, CompleteCurrentDepthStepAfterDelay);
        }

        private bool IsDepthReasonable()
        {
            return currentDepth >= normalMinDepth && currentDepth <= normalMaxDepth;
        }

        private void StartValueDisplayTransition(float startValue, float targetValue, Action onCompleted = null)
        {
            StopValueTextCountUp();

            if (ValueText == null)
            {
                onCompleted?.Invoke();
                return;
            }

            valueDisplayRoutine = StartCoroutine(ChangeValueText(startValue, targetValue, onCompleted));
        }

        private IEnumerator ChangeValueText(float startValue, float targetValue, Action onCompleted)
        {
            float displayValue = startValue;
            float step = Mathf.Max(0.01f, valueDisplayStep);
            float interval = Mathf.Max(0f, valueDisplayStepInterval);

            while (!Mathf.Approximately(displayValue, targetValue))
            {
                SetValueText(displayValue);
                displayValue = Mathf.Min(displayValue + step, targetValue);

                if (interval > 0f)
                {
                    yield return new WaitForSeconds(interval);
                }
                else
                {
                    yield return null;
                }
            }

            SetValueText(targetValue);
            valueDisplayRoutine = null;
            onCompleted?.Invoke();
        }

        private void CompleteCurrentDepthStepAfterDelay()
        {
            valueDisplayRoutine = StartCoroutine(CompleteCurrentDepthStepDelayed());
        }

        private IEnumerator CompleteCurrentDepthStepDelayed()
        {
            if (completeDelay > 0f)
            {
                yield return new WaitForSeconds(completeDelay);
            }

            valueDisplayRoutine = null;
            CompleteCurrentDepthStep();
        }

        private void CompleteCurrentDepthStep()
        {
            if (currentDepthResolved)
            {
                return;
            }

            currentDepthResolved = true;
            FinishDepthCheckRuntime();
            LogRuntime($"[VR4Depth] Frame={Time.frameCount} Completed Gauge={name}, Depth={currentDepth:0.00}");
            CompleteStep();
            ResetDisplayValue();

            if (HasCollectedAllAnswers())
            {
                RestoreGaugeInitialPose();
                gameObject.SetActive(false);
            }
        }

        private void CacheGaugeInitialPose()
        {
            gaugeInitialPosition = transform.position;
            gaugeInitialRotation = transform.rotation;
            hasGaugeInitialPose = true;
        }

        private void RestoreGaugeInitialPose()
        {
            if (!hasGaugeInitialPose)
            {
                return;
            }

            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.position = gaugeInitialPosition;
                rigidbody.rotation = gaugeInitialRotation;
            }

            transform.SetPositionAndRotation(gaugeInitialPosition, gaugeInitialRotation);
        }

        private void SetValueText(float displayValue)
        {
            if (ValueText != null)
            {
                ValueText.text = $"{displayValue:0.00}mm";
            }
        }

        private void StopValueTextCountUp()
        {
            if (valueDisplayRoutine != null)
            {
                StopCoroutine(valueDisplayRoutine);
                valueDisplayRoutine = null;
            }
        }

        private void StoreAnswerValue(int value)
        {
            EnsureAnswerArray();

            if (answerIndex >= answerValues.Length)
            {
                return;
            }

            answerValues[answerIndex] = value;
            answerIndex++;

            if (answerIndex >= answerValues.Length)
            {
                WriteAnswersToTopicData();
            }
        }

        private bool HasCollectedAllAnswers()
        {
            EnsureAnswerArray();
            return answerIndex >= answerValues.Length;
        }

        private void WriteAnswersToTopicData()
        {
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

        private void ResetDisplayValue()
        {
            StopValueTextCountUp();
            currentDepth = 0f;
            //SetValueText(0f);
        }

        private void ShowValueText()
        {
            if (ValueText != null)
            {
                ValueText.gameObject.SetActive(true);
                ValueText.enabled = true;
            }
        }

        private void ResetDepthCheckRuntime()
        {
            StopValueTextCountUp();
            logicEnabled = false;
            hasDepth = false;
            currentDepthResolved = false;
            currentDepth = 0f;
        }

        private void FinishDepthCheckRuntime()
        {
            logicEnabled = false;
            hasDepth = false;
        }

        private void LogRuntime(string message)
        {
            if (enableRuntimeLogs)
            {
                Debug.Log(message);
            }
        }
        #endregion

        #region ==========API==========
        public void SetRandomValue()
        {
            if (valueDisplayRoutine != null)
            {
                return;
            }

            SetValueText(0f);
            ResetStepCompletion();
            logicEnabled = true;
            GenerateRandomDepth();
        }

        public void BeginDepthCheck()
        {
            ResetStepCompletion();
            logicEnabled = true;
            LogRuntime($"[VR4Depth] Frame={Time.frameCount} BeginDepthCheck Gauge={name}");
        }

        public override void ResetStepCompletion()
        {
            base.ResetStepCompletion();
            ResetDepthCheckRuntime();
        }

        public void ResetGauge()
        {
            ResetDepthCheckRuntime();
            base.ResetStepCompletion();
            answerIndex = 0;

            EnsureAnswerArray();
            Array.Clear(answerValues, 0, answerValues.Length);

            ResetDisplayValue();

            if (powerButton != null)
            {
                powerButton.enabled = true;
            }
        }
        #endregion
    }
}
