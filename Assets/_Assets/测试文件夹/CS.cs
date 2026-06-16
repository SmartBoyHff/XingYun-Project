using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CS : MonoBehaviour
{
    [Header("场景名称")]
    public string oldSceneName = "Scene_Old";
    public string newSceneName = "Scene_New";

    [Header("摄像机控制")]
    public Camera oldSceneCamera;
    public Camera newSceneCamera;

    private bool isNewSceneLoaded = false;

    void Start()
    {
        // 开始时，确保旧场景摄像机启用，新场景摄像机禁用（如果尚未加载则不管）
        if (oldSceneCamera) oldSceneCamera.enabled = true;
        if (newSceneCamera) newSceneCamera.enabled = false;

        // 标记此物体跨场景不销毁
        DontDestroyOnLoad(gameObject);
    }

    // 按钮调用：切换到新场景
    public void SwitchToNewScene()
    {
        if (!isNewSceneLoaded)
        {
            // 异步加载新场景（附加模式）
            var asyncOp = SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
            asyncOp.completed += (op) =>
            {
                isNewSceneLoaded = true;
                // 加载完成后，找到新场景的主摄像机（如果上面未拖拽赋值）
                if (newSceneCamera == null)
                    newSceneCamera = FindCameraInScene(newSceneName);
                // 显示新场景，隐藏旧场景
                ShowNewScene();
            };
        }
        else
        {
            // 已经加载过，直接显示新场景
            ShowNewScene();
        }
    }

    // 按钮调用：回到老场景
    public void SwitchToOldScene()
    {
        if (isNewSceneLoaded)
        {
            ShowOldScene();
        }
        // 如果新场景未加载，那就已经在老场景了，不需要做任何事
    }

    private void ShowNewScene()
    {
        if (oldSceneCamera) oldSceneCamera.enabled = false;
        if (newSceneCamera) newSceneCamera.enabled = true;

        // 可选：将活动场景设为新场景，以便新场景的 UI 弹窗等正常工作
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(newSceneName));
    }

    private void ShowOldScene()
    {
        if (oldSceneCamera) oldSceneCamera.enabled = true;
        if (newSceneCamera) newSceneCamera.enabled = false;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(oldSceneName));
    }

    private Camera FindCameraInScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera cam = root.GetComponentInChildren<Camera>();
            if (cam != null) return cam;
        }
        return null;
    }
}
