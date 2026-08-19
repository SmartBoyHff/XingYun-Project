using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using TMPro;
using UnityEngine.SceneManagement;

public class VideoManager : MonoBehaviour
{
    public static VideoManager Instance { get; private set; }

    [Header("全局视频资源库")]
    public List<VideoClip> videoLibrary;     // 所有视频，索引 0 为无视频/默认

    [Header("播放器")]
    public VideoPlayer videoPlayer;
    public TextMeshProUGUI VideoName;
    public AudioSource videoAudioSource; 

    [Header("事件（可选）")]
    public UnityEvent<int> onVideoChange;    // 参数：当前播放索引
    public UnityEvent<bool> onPlayPause;    // true=播放中, false=暂停

    private int currentIndex = -1;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(transform.root.gameObject);  
       
    }
   

    void Start()
    {
        // 确保有一个 AudioSource 用来播放声音
        if (videoAudioSource == null)
        {
            videoAudioSource = gameObject.AddComponent<AudioSource>();
        }
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        videoPlayer.controlledAudioTrackCount = 1;   // 控制第一个音轨
        videoPlayer.EnableAudioTrack(0, true);       // 默认启用音轨
        videoPlayer.playOnAwake = false;             // 完全手动控制
    }

    public void PlayByIndex(int index)
    {
        if (index < 0 || index >= videoLibrary.Count) return;

        // 相同视频且正在播放则忽略
        if (currentIndex == index && videoPlayer.isPlaying) return;

        // 停止当前播放，并清除 clip（强制重置状态）
        videoPlayer.Stop();
        videoPlayer.clip = null;
        VideoName.text = videoLibrary[index].name;
        // 【关键】重新配置音频轨道（针对新视频，即使它没音轨也要重置一次）
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);          // 先假设有音轨
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);

        // 设置新视频
        videoPlayer.clip = videoLibrary[index];

        // 准备并播放
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnPrepareCompleted;
    }

    private void OnPrepareCompleted(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnPrepareCompleted;

        // 如果当前视频没有音轨，手动关闭音频输出（可选，但不影响下次切换）
        if (vp.audioTrackCount == 0)
        {
            vp.EnableAudioTrack(0, false);
        }
        else
        {
            vp.EnableAudioTrack(0, true);
        }

        vp.Play();
        currentIndex = GetCurrentClipIndex(); // 需要自己实现一个获取当前 clip 索引的方法
        onVideoChange?.Invoke(currentIndex);
    }
    private int GetCurrentClipIndex()
    {
        for (int i = 0; i < videoLibrary.Count; i++)
        {
            if (videoLibrary[i] == videoPlayer.clip)
                return i;
        }
        return -1;
    }
    public void Pause()
    {
        if (videoPlayer && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            onPlayPause?.Invoke(false);
        }
    }

    public void Resume()
    {
        if (videoPlayer && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            onPlayPause?.Invoke(true);
        }
    }

    public void Stop()
    {
        if (videoPlayer)
        {
            videoPlayer.Stop();
            currentIndex = -1;
        }
    }
    public void PlayPrevious()
    {
        int newIndex = currentIndex - 1;
        if (newIndex < 0) newIndex = videoLibrary.Count - 1;
        PlayByIndex(newIndex);
    }

    public void PlayNext()
    {
        int newIndex = currentIndex + 1;
        if (newIndex >= videoLibrary.Count) newIndex = 0;
        PlayByIndex(newIndex);
    }
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
