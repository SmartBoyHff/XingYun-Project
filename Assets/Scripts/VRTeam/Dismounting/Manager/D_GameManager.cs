using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class D_GameManager : MonoBehaviour
{
   public static D_GameManager Instance { get; private set; }

    [SerializeField] private bool isTeachingMode = true;
    public bool IsTeachingMode => isTeachingMode;

    [SerializeField] private HighlightController highlightController; // 教学模式下的高亮管理器
    [SerializeField] private D_StepUIManager stepUIManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetMode(bool teaching)
    {
        isTeachingMode = teaching;
        if (highlightController != null)
            highlightController.gameObject.SetActive(teaching);
    }
}
