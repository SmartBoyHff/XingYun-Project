using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

// ============================================================
// 文件名：VR4_TirePressureGauge
// 模块：模块4 - 维护保养
// 功能：胎压表交互脚本，负责随机胎压、释压/补气修正、答案记录和步骤完成。
// 创建日期：2026-05-5
// 最后更新：2026-05-5
// ============================================================

namespace VRHelmet.VRTeam.Maintenance
{
    /// <summary>
    /// ̥ѹ��ű�����ʱûע��
    /// </summary>
    /// <summary>
    /// VR4_TirePressureGauge 类型说明
    /// 
    /// 【功能说明】
    /// 1. 属于模块4 - 维护保养中的 VR4_TirePressureGauge 类型。
    /// 2. 负责处理随机胎压、释压补气、答案记录和步骤完成。
    /// 3. 与维护保养流程中的步骤数据、交互逻辑或 UI 显示协同工作。
    /// 
    /// 【依赖组件】
    /// - Unity 组件体系。
    /// - VRHelmet.VRTeam.Maintenance 维护保养流程脚本。
    /// </summary>
    public class VR4_TirePressureGauge : VR4_BaseObject
    {
        #region ==========Field==========
        [Header("Display")]
        public TextMeshProUGUI pressureText;

        [Header("Pressure Range")]
        public float minPressure = 2.7f;
        public float maxPressure = 3.1f;
        public float fixedNormalPressure = 2.9f;

        [Header("XR Interaction")]
        public Button openPressurePoke;
        public XRSimpleInteractable releasePressurePoke;
        public InputActionReference grabInputAction;

        [Header("Pipe Mouth")]
        public Transform pipeMouthTransform;

        [Header("Animation")]
        public Animator grabInputAnimator;
        public AnimationClip grabInputAnimationClip;

        [Header("Answer")]
        public int normalAnswerValue = 1;
        public int abnormalAnswerValue = 2;
        public int[] answerValues = new int[4];

        [Header("Topic Data Write Back")]
        public VR4_TopicManager topicManager;
        public int targetTopicTableIndex = 0;
        public int targetTopicDataStartIndex = 0;

        [SerializeField] private bool enableRuntimeLogs = false;
        [SerializeField] private float pressureDisplayStep = 0.1f;
        [SerializeField] private float pressureDisplayStepInterval = 0.03f;
        [SerializeField] private float pressureCompleteDelay = 2.0f;
        public bool logicEnabled = false;

        private const int AnswerCapacity = 4;
        private const float RandomMinPressure = 2.5f;
        private const float RandomMaxPressure = 3.5f;

        private float currentPressure;
        private bool hasPressure = false;
        private bool currentPressureResolved = false;
        private bool grabInputActionEnabledByThis = false;
        private PlayableGraph grabInputAnimationGraph;
        private Coroutine pressureDisplayRoutine;
        private Coroutine pressureCompleteDelayRoutine;
        private int answerIndex = 0;
        private Vector3 pipeMouthInitialLocalPosition;
        private Quaternion pipeMouthInitialLocalRotation;
        private bool hasPipeMouthInitialPose = false;
        private Vector3 gaugeInitialPosition;
        private Quaternion gaugeInitialRotation;
        private bool hasGaugeInitialPose = false;
        #endregion

        #region ==========Unity Method==========
        private void Reset()
        {
            grabInputAnimator = GetComponent<Animator>();
        }

        private void OnValidate()
        {
            if (maxPressure < minPressure)
            {
                maxPressure = minPressure;
            }

            EnsureAnswerArray();
        }

        private void Awake()
        {
            EnsureAnswerArray();
            CacheGaugeInitialPose();
            CachePipeMouthInitialPose();

            if (grabInputAnimator == null)
            {
                grabInputAnimator = GetComponent<Animator>();
            }
        }

        private void Start()
        {
            if (openPressurePoke != null)
            {
                openPressurePoke.onClick.RemoveListener(OpenPressurePoked);
                openPressurePoke.onClick.AddListener(OpenPressurePoked);
            }
        }

        private void OnEnable()
        {
            if (releasePressurePoke != null)
            {
                releasePressurePoke.selectEntered.RemoveListener(OnReleasePressurePoked);
                releasePressurePoke.selectEntered.AddListener(OnReleasePressurePoked);
            }

            if (grabInputAction != null && grabInputAction.action != null)
            {
                InputAction action = grabInputAction.action;
                action.performed -= OnGrabInputPerformed;
                action.performed += OnGrabInputPerformed;

                if (!action.enabled)
                {
                    action.Enable();
                    grabInputActionEnabledByThis = true;
                }
            }
        }

        private void OnDisable()
        {
            if (openPressurePoke != null)
            {
                openPressurePoke.onClick.RemoveListener(OpenPressurePoked);
            }

            if (releasePressurePoke != null)
            {
                releasePressurePoke.selectEntered.RemoveListener(OnReleasePressurePoked);
            }

            if (grabInputAction != null && grabInputAction.action != null)
            {
                grabInputAction.action.performed -= OnGrabInputPerformed;

                if (grabInputActionEnabledByThis)
                {
                    grabInputAction.action.Disable();
                    grabInputActionEnabledByThis = false;
                }
            }

            ResetPressureCheckRuntime();
            StopGrabInputAnimation();
        }
        #endregion

        #region ==========Logic==========
        private void OpenPressurePoked()
        {
            ShowPressureText();

            openPressurePoke.gameObject.SetActive(false);
        }

        private void GenerateRandomPressure()
        {
            currentPressure = UnityEngine.Random.Range(RandomMinPressure, RandomMaxPressure);
            hasPressure = true;
            currentPressureResolved = false;
            ShowPressureText();

            EvaluateAnswerAfterRandomPressure();
            StartPressureDisplayTransition(0f, currentPressure, () =>
            {
                if (IsPressureReasonable())
                {
                    CompleteCurrentPressureStepAfterDelay();
                }
            });
        }

        private void OnReleasePressurePoked(SelectEnterEventArgs args)
        {
            if (!logicEnabled || !hasPressure || currentPressureResolved)
            {
                return;
            }

            if (currentPressure <= maxPressure)
            {
                return;
            }

            SetPressureToNormal(CompleteCurrentPressureStepAfterDelay);
        }

        private void OnGrabInputPerformed(InputAction.CallbackContext context)
        {
            if (!logicEnabled || !hasPressure || currentPressureResolved)
            {
                return;
            }

            if (currentPressure >= minPressure)
            {
                return;
            }

            PlayGrabInputAnimation();
            SetPressureToNormal(CompleteCurrentPressureStepAfterDelay);
        }

        private void PlayGrabInputAnimation()
        {
            if (grabInputAnimator == null || grabInputAnimationClip == null)
            {
                return;
            }

            StopGrabInputAnimation();
            AnimationPlayableUtilities.PlayClip(grabInputAnimator, grabInputAnimationClip, out grabInputAnimationGraph);
        }

        private void StopGrabInputAnimation()
        {
            if (grabInputAnimationGraph.IsValid())
            {
                grabInputAnimationGraph.Destroy();
            }
        }

        private bool IsPressureReasonable()
        {
            return currentPressure >= minPressure && currentPressure <= maxPressure;
        }

        private void EvaluateAnswerAfterRandomPressure()
        {
            StoreAnswerValue(IsPressureReasonable() ? normalAnswerValue : abnormalAnswerValue);
        }

        private void SetPressureToNormal(Action onCompleted = null)
        {
            float startPressure = currentPressure;
            currentPressure = fixedNormalPressure;
            StartPressureDisplayTransition(startPressure, currentPressure, onCompleted);
        }

        private void SetPressureText(float displayPressure)
        {
            if (pressureText != null)
            {
                pressureText.text = $"{displayPressure:0.00}bar\n({GetPressureStateText()})";
            }
        }

        private void StartPressureDisplayTransition(float startPressure, float targetPressure, Action onCompleted = null)
        {
            StopPressureTextCountUp();

            if (pressureText == null)
            {
                onCompleted?.Invoke();
                return;
            }

            pressureDisplayRoutine = StartCoroutine(ChangePressureText(startPressure, targetPressure, onCompleted));
        }

        private IEnumerator ChangePressureText(float startPressure, float targetPressure, Action onCompleted)
        {
            float displayPressure = startPressure;
            float step = Mathf.Max(0.01f, pressureDisplayStep);
            float interval = Mathf.Max(0f, pressureDisplayStepInterval);
            int direction = targetPressure >= startPressure ? 1 : -1;

            while (!Mathf.Approximately(displayPressure, targetPressure))
            {
                SetPressureText(displayPressure);
                displayPressure = direction > 0
                    ? Mathf.Min(displayPressure + step, targetPressure)
                    : Mathf.Max(displayPressure - step, targetPressure);

                if (interval > 0f)
                {
                    yield return new WaitForSeconds(interval);
                }
                else
                {
                    yield return null;
                }
            }

            SetPressureText(targetPressure);
            pressureDisplayRoutine = null;
            onCompleted?.Invoke();
        }

        private void CompleteCurrentPressureStepAfterDelay()
        {
            StopPressureCompleteDelay();
            pressureCompleteDelayRoutine = StartCoroutine(CompleteCurrentPressureStepDelayed());
        }

        private IEnumerator CompleteCurrentPressureStepDelayed()
        {
            if (pressureCompleteDelay > 0f)
            {
                yield return new WaitForSeconds(pressureCompleteDelay);
            }

            pressureCompleteDelayRoutine = null;
            CompleteCurrentPressureStep();
        }

        private void StopPressureTextCountUp()
        {
            if (pressureDisplayRoutine != null)
            {
                StopCoroutine(pressureDisplayRoutine);
                pressureDisplayRoutine = null;
            }
        }

        private void StopPressureCompleteDelay()
        {
            if (pressureCompleteDelayRoutine != null)
            {
                StopCoroutine(pressureCompleteDelayRoutine);
                pressureCompleteDelayRoutine = null;
            }
        }

        private string GetPressureStateText()
        {
            if (currentPressure > maxPressure)
            {
                return "需要释压";
            }

            if (currentPressure < minPressure)
            {
                return "需要充气";
            }

            return "正常";
        }

        private void CompleteCurrentPressureStep()
        {
            if (currentPressureResolved)
            {
                return;
            }

            currentPressureResolved = true;
            FinishPressureCheckRuntime();
            LogRuntime($"[VR4Tire] Frame={Time.frameCount} Completed Gauge={name}, Pressure={currentPressure:0.00}");
            CompleteStep();
            bool hasCollectedAllAnswers = HasCollectedAllAnswers();

            if (hasCollectedAllAnswers)
            {
                RestoreGaugeInitialPose();
            }

            RestorePipeMouthInitialPose();
            ClearPressureText();

            if (hasCollectedAllAnswers)
            {
                HidePressureText();
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
                transform.SetPositionAndRotation(gaugeInitialPosition, gaugeInitialRotation);
            }
            else
            {
                transform.SetPositionAndRotation(gaugeInitialPosition, gaugeInitialRotation);
            }
        }

        private void CachePipeMouthInitialPose()
        {
            if (pipeMouthTransform == null)
            {
                hasPipeMouthInitialPose = false;
                return;
            }

            pipeMouthInitialLocalPosition = pipeMouthTransform.localPosition;
            pipeMouthInitialLocalRotation = pipeMouthTransform.localRotation;
            hasPipeMouthInitialPose = true;
        }

        private void RestorePipeMouthInitialPose()
        {
            if (!hasPipeMouthInitialPose || pipeMouthTransform == null)
            {
                return;
            }

            pipeMouthTransform.localPosition = pipeMouthInitialLocalPosition;
            pipeMouthTransform.localRotation = pipeMouthInitialLocalRotation;
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

        private void ClearPressureText()
        {
            StopPressureTextCountUp();
            currentPressure = 0f;

            if (pressureText != null)
            {
                pressureText.enabled = true;
                pressureText.text = "0.00bar";
            }
        }

        private void ShowPressureText()
        {
            if (pressureText != null)
            {
                pressureText.enabled = true;
            }
        }

        private void HidePressureText()
        {
            if (pressureText != null)
            {
                pressureText.enabled = false;
            }
        }

        private void ResetPressureCheckRuntime()
        {
            StopPressureTextCountUp();
            StopPressureCompleteDelay();
            logicEnabled = false;
            hasPressure = false;
            currentPressureResolved = false;
            currentPressure = 0f;
        }

        private void FinishPressureCheckRuntime()
        {
            logicEnabled = false;
            hasPressure = false;
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
            Debug.Log("OK");
            GenerateRandomPressure();
        }

        public void BeginPressureCheck()
        {
            ResetStepCompletion();
            logicEnabled = true;
            LogRuntime($"[VR4Tire] Frame={Time.frameCount} BeginPressureCheck Gauge={name}");
        }

        public override void ResetStepCompletion()
        {
            base.ResetStepCompletion();
            ResetPressureCheckRuntime();
        }

        public void ResetGauge()
        {
            ResetPressureCheckRuntime();
            base.ResetStepCompletion();
            answerIndex = 0;

            EnsureAnswerArray();
            Array.Clear(answerValues, 0, answerValues.Length);

            ClearPressureText();
        }
        #endregion
    }
}
