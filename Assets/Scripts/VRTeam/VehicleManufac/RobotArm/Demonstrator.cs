using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;

public class Demonstrator : MonoBehaviour
{
    [Header("Step Settings")]
    [Tooltip("每次加减的基础步长（0~1 范围）")]
    public float stepSize = 0.01f;

    [Header("Speed Control")]
    [Range(0.1f, 1.0f)]
    public float speedPercent = 1.0f;    // 1.0 = 100%
    public float initialDelay = 0.4f;
    public float repeatRate = 0.1f;

    [Header("UI References")]
    public Button speedUpButton;         // “加速”按钮（+10%）
    public Button speedDownButton;       // “减速”按钮（-10%）
    public TextMeshProUGUI speedDisplayText;        // 速度百分比显示
                                                   

    private bool isAddPressed = false;
    private bool isSubtractPressed = false;
    private Coroutine addCoroutine = null;
    private Coroutine subtractCoroutine = null;
    private void Start()
    {
       
        if (speedUpButton != null)
            speedUpButton.onClick.AddListener(OnSpeedUp);
        if (speedDownButton != null)
            speedDownButton.onClick.AddListener(OnSpeedDown);

        UpdateSpeedDisplay();
    }
    public void OnAddPointerDown(GameObject game)
    {
        if (addCoroutine == null)
        {
            isAddPressed = true;
            addCoroutine = StartCoroutine(ContinuousAdd(game));
        }
    }

    public void OnAddPointerUp()
    {
        isAddPressed = false;
        if (addCoroutine != null)
        {
            StopCoroutine(addCoroutine);
            addCoroutine = null;
        }
    }

    // ----- 供外部绑定的公开方法（减）-----
    public void OnSubtractPointerDown(GameObject game)
    {
        if (subtractCoroutine == null)
        {
            isSubtractPressed = true;
            subtractCoroutine = StartCoroutine(ContinuousSubtract(game));
        }
    }

    public void OnSubtractPointerUp()
    {
        isSubtractPressed = false;
        if (subtractCoroutine != null)
        {
            StopCoroutine(subtractCoroutine);
            subtractCoroutine = null;
        }
    }

    // ----- 协程 -----
    private IEnumerator ContinuousAdd(GameObject game)
    {
        yield return new WaitForSeconds(initialDelay);
        while (isAddPressed)
        {
            AddValue(game);
            yield return new WaitForSeconds(repeatRate);
        }
    }

    private IEnumerator ContinuousSubtract(GameObject game)
    {
        yield return new WaitForSeconds(initialDelay);
        while (isSubtractPressed)
        {
            SubtractValue(game);
            yield return new WaitForSeconds(repeatRate);
        }
    }
    // ----- 加减数值 -----
    public void AddValue( GameObject game)
    {
        if (game == null) return;
        XRKnob k = game.GetComponent<XRKnob>();
        float delta = stepSize * speedPercent;
        k.value = Mathf.Clamp01(k.value + delta);
    }

    public void SubtractValue(GameObject game)
    {
        if (game == null) return;
        XRKnob k = game.GetComponent<XRKnob>();
        float delta = stepSize * speedPercent;
        k.value = Mathf.Clamp01(k.value - delta);
    }

    // ----- 速度调节（每次 ±10%）-----
    public void OnSpeedUp()
    {
        speedPercent = Mathf.Min(speedPercent + 0.1f, 2.0f); // 上限 200%
        UpdateSpeedDisplay();
    }

    public void OnSpeedDown()
    {
        speedPercent = Mathf.Max(speedPercent - 0.1f, 0.1f); // 下限 10%
        UpdateSpeedDisplay();
    }

    // ----- 更新显示 -----
    private void UpdateSpeedDisplay()
    {
        if (speedDisplayText != null)
        {
            int percent = Mathf.RoundToInt(speedPercent * 100);
            speedDisplayText.text = percent+"";
        }
    }

}
