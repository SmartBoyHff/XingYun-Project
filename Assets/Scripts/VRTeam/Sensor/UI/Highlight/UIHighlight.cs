using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UI;

public class UIHighlight : MonoBehaviour
{
    [Header("全局配置（可选）")]
    [Tooltip("如果不为空，颜色和速度将从该配置获取，忽略下方的局部设置")]
    public UIHighlightManager globalConfig;

    [Header("局部设置")]
    public bool isLocal=false;
    public List<Color> localColors = new List<Color>
    {
        Color.white,
        Color.red,
        Color.blue,
        Color.green
    };

    [Range(0.1f, 10f)]
    public float localSpeed = 1f;


    private Color[] CurrentColors => globalConfig != null ? globalConfig.colors : localColors.ToArray();
    private float CurrentSpeed => globalConfig != null ? globalConfig.speed : localSpeed;

    [Header("其他设置")]
    public bool playOnStart = true;
    public bool loop = true;

    private Graphic graphic;
    private int currentIndex;
    private int nextIndex;
    private float t;
    private bool isPlaying;

    // 当前实际使用的颜色序列（运行时从全局或局部获取）
    private Color[] currentColors;
    private float currentSpeed;

    private void Awake()
    {
        if (!isLocal)
        {
            globalConfig = Resources.Load<UIHighlightManager>("Material/UIHighlightManager");
        }
        graphic = GetComponent<Graphic>();
        RefreshConfig();
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    /// <summary>
    /// 刷新颜色和速度设置（在全局配置变更后调用）
    /// </summary>
    public void RefreshConfig()
    {
        if (globalConfig != null)
        {
            currentColors = globalConfig.colors;
            currentSpeed = globalConfig.speed;
        }
        else
        {
            currentColors = localColors.ToArray();
            currentSpeed = localSpeed;
        }

        // 确保至少有2个颜色
        if (currentColors == null || currentColors.Length < 2)
        {
            Debug.LogWarning("UIColorCycle: 颜色序列至少需要2个颜色！", this);
            isPlaying = false;
        }
    }

    private void Update()
    {
        if (!isPlaying) return;
        var colors = CurrentColors;
        if (colors == null || colors.Length < 2) return;

        t += Time.deltaTime * CurrentSpeed;
        graphic.color = Color.Lerp(colors[currentIndex], colors[nextIndex], t);

        if (t >= 1f)
        {
            t = 0f;
            currentIndex = nextIndex;
            nextIndex = GetNextIndex(currentIndex, colors.Length);
            if (!loop && currentIndex == colors.Length - 1)
                isPlaying = false;
        }
    }

    int GetNextIndex(int index, int length)
    {
        int next = index + 1;
        if (next >= length) next = loop ? 0 : next;
        return next;
    }

    public void Play()
    {
        RefreshConfig(); // 确保使用最新配置
        if (currentColors == null || currentColors.Length < 2)
            return;

        currentIndex = 0;
        nextIndex = GetNextIndex(currentIndex, CurrentColors.Length);
        t = 0f;
        isPlaying = true;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void Stop()
    {
        isPlaying = false;
        if (currentColors != null && currentColors.Length > 0)
            graphic.color = currentColors[0];
    }

    public void Resume()
    {
        if (currentColors != null && currentColors.Length >= 2)
            isPlaying = true;
    }

    /// <summary>
    /// 可选：强制设置局部颜色（会断开全局配置引用）
    /// </summary>
    public void SetLocalColors(List<Color> newColors)
    {
        globalConfig = null;
        localColors = newColors;
        RefreshConfig();
        Play();
    }

    public void SetSpeed(float speed)
    {
        // 单独修改,为了灵活性保留
        localSpeed = speed;
        if (globalConfig == null)
            currentSpeed = speed;
    }
}
