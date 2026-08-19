using Foundation.Console;
using Pico.Platform;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class Displayobject
{
    public string name;
    public GameObject game;
}


public class CustomCommands : MonoBehaviour
{
    public static CustomCommands Instance { get; private set; }

    private Coroutine activePingCoroutine;
    private bool isPinging = false;
    private bool isFirst = true, isFirst1 = true, isFirst2 = true;
    public Displayobject[] displayobject;


    // 备份存储：保存文本和颜色
    private List<(string text, TerminalType type)> backupItems = new List<(string, TerminalType)>();
    // 存储 cam 与 topic 的映射关系
    public Dictionary<int, string> camToTopic = new Dictionary<int, string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 开始 Ping 模拟（输出到终端）
    /// </summary>
    public void PingCommand()
    {
        if (isPinging)
        {
            //Terminal.LogWarning("Ping 已经在运行中，请先停止。");
            return;
        }

        //Terminal.LogSuccess("开始 Ping 192.168.13.1 ...");
        //Terminal.Log("输入 'ping stop' 或点击停止按钮终止。");
        activePingCoroutine = StartCoroutine(PingCoroutine());
    }

    /// <summary>
    /// 停止 Ping 模拟
    /// </summary>
    public void StopPing()
    {
        if (!isPinging)
        {
            //Terminal.LogWarning("当前没有正在运行的 Ping 任务。");
            return;
        }

        if (activePingCoroutine != null)
            StopCoroutine(activePingCoroutine);
        activePingCoroutine = null;
        isPinging = false;
        //Terminal.LogSuccess("Ping 已停止。");
    }
    public void Stop()
    {
        if (activePingCoroutine != null)
            StopCoroutine(activePingCoroutine);
        activePingCoroutine = null;
        Terminal.Clear();
    }

    public void Start()
    {
        Terminal.Clear();
        Terminal.Log($"inwinic@Anna :~ $");
    }
    private IEnumerator PingCoroutine()
    {
        isPinging = true;
        int seq = 1;
        const string target = "192.168.13.1";

        while (true)
        {
            int timeMs = UnityEngine.Random.Range(10, 201); // 随机 10~200 ms
            Terminal.Log($"64 bytes from {target}: icmp_seq={seq} ttl=64 time={timeMs}ms");
            seq++;
            yield return new WaitForSeconds(1f);
        }
    }
    private IEnumerator DelayedScroll()
    {
        yield return null;          // 等待一帧，让所有 AddItemAsync 协程至少执行到 yield return null
        yield return null;          // 再等一帧，确保 Canvas 布局完全更新
        Sensor_TerminalViewTMP view = FindObjectOfType<Sensor_TerminalViewTMP>();
        if (view != null)
            view.ScrollToBottom();
    }

    public void ShowSystemInfo()
    {
        // 获取当前 UTC 时间，格式如 "Sun Dec 21 04:08:24 UTC 2025"
        string currentTime = DateTime.UtcNow.ToString("ddd MMM dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC " + DateTime.UtcNow.Year;
        string[] infoLines = new string[]
        {
        "Welcome to 5.10.0-1.h1.A0S3.3.aarch64",
        " ",
        $"System information as of time:{currentTime}",
        " ",
        "System load:    14.21",
        "Processes:      198",
        "Memory used:    7.7%",
        "Swap used:      0.0%",
        "Usage On:       88%",
        "IP address:     192.168.13.1",
        "Users online:   1"
        };
        string[] infoLines2 = new string[]
        {
        "Welcome to 5.10.0-1.h1.A0S3.3.aarch64",
        " ",
        $"System information as of time:{currentTime}",
        " ",
        "System load:    14.21",
        "Processes:      198",
        "Memory used:    7.7%",
        "Swap used:      0.0%",
        "Usage On:       88%",
        "IP address:     192.168.13.11",
        "Users online:   2"
        };
        if (isFirst)
        {
            foreach (string line in infoLines)
            {
                Terminal.Log(line);
            }

            isFirst = false;
        }
        else
        {
            foreach (string line in infoLines2)
                Terminal.Log(line);
            isFirst = true;
        }


        // 延迟滚动，确保所有项都已被 UI 处理完毕
        StartCoroutine(DelayedScroll());
    }


    // 备份当前所有终端内容
    public void BackupCurrentContent()
    {
        backupItems.Clear();
        foreach (TerminalItem item in Terminal.Instance.Items)
        {
            backupItems.Add((item.Text, item.Type));
        }
       // Terminal.LogSuccess($"已备份 {backupItems.Count} 条终端内容（含颜色信息）。");
    }
    // 恢复备份的内容
    public void RestoreBackup()
    {
        if (backupItems.Count == 0)
        {
            Terminal.LogWarning("没有可恢复的备份内容。");
            return;
        }

        Terminal.Clear();
        foreach (var (text, type) in backupItems)
        {
            // 使用 Terminal.Add 直接添加 TerminalItem，保持原类型和颜色
            Terminal.Add(new TerminalItem(type, text));
        }
        //Terminal.LogSuccess($"已恢复 {backupItems.Count} 条终端内容。");
        backupItems.Clear();
        // 延迟滚动，确保所有项都已被 UI 处理完毕
        StartCoroutine(DelayedScroll());
    }
    public void VimCommand()
    {
        BackupCurrentContent();

        Terminal.Clear();
        string[] addressLines = new string[]
        {
            "127.0.0.1 7000",
            "192.168.13.109 7000",
            "192.168.13.121 7000",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "\"viz _address. conf\" 3L， 55C          2，1    All"
        };
        string[] addressLines2 = new string[]
        {
            "127.0.0.1 7000",
            "192.168.13.109 7000",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "\"viz _address. conf\" 3L， 55C          2，1    All"
        };
        if (isFirst1)
        {
            foreach (string line in addressLines)
                Terminal.Log(line);
            isFirst1 = false;
        }
        else
        {
            foreach (string line in addressLines2)
                Terminal.Log(line);
            isFirst1 = true;
        }


    }
    public void IDCommand()
    {
        Terminal.Clear();
        string[] addressLines = new string[]
       {
            "127.0.0.1 7000",
            "192.168.13.109 7000",
            "192.168.13.121 7000",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "~",
            "\"viz _address. conf\" 3L， 55C          2，1    All"
       };
        foreach (string line in addressLines)
            Terminal.Log(line);
    }

    public void CameraCommand()
    {
        camToTopic.Clear();
        string[] addressLines = new string[]
       {
            "mdc::visual::Connect Yes!",
            "============== Camera Mapping (Slot->Cam Id->FrameId-> VencCh)==============",
            "SlotID=C1 cameraId=8 frameId=mdc_camera_instance_ 79 vencCh=33 1920x1280",
            "fps=30 streamDepth=2",
            "SlotID=C2 cameraId=9 frameId=mdc_ camera_instance_ 80 vencCh=34 1920x1280",
            "fps=30 streamDepth=10",
            "SlotID=C3 cameraId=10 frameId=mdc_ camera _instance_81 vencCh=35 1920x1280",
            "fps=30 streamDepth=10",
            "SlotID=C4 cameraId=11 frameId=mdc _ camera _instance _82 vencCh=36 1920x1280",
            "fps=30 streamDepth=10",
            "[mviz] Advertise topic=mdc_ camera_instance_82 for cam=11",
            "[mviz] Advertise topic=mdc_ camera _instance_81 for cam=10",
            "[mviz] Advertise topic=mdc_ camera _instance _80 for cam=9",
            "[mviz] Advertise topic=mdc_ camera _instance _ 79 for cam=8"
       };
        string[] addressLines2 = new string[]
      {
            "mdc::visual::Connect Yes!",
            "============== Camera Mapping (Slot->Cam Id->FrameId-> VencCh)==============",
            "SlotID=B1 cameraId=4 frameId=mdc_ camera _instance_ 75 vencCh=29 1920x1280",
            "fps=30 streamDepth=10",
            "[mviz] Advertise topic=md_camera_instance_ 75 for cam=4",
      };
        if (isFirst2)
        {
            foreach (string line in addressLines)
            {
                if (line.Contains("[mviz] Advertise topic="))
                {
                    // 提取 topic 和 cam
                    int topicStart = line.IndexOf("topic=") + 6;
                    int topicEnd = line.IndexOf(" for cam=");
                    if (topicStart > 6 && topicEnd > topicStart)
                    {
                        string topic = line.Substring(topicStart, topicEnd - topicStart);
                        string camPart = line.Substring(topicEnd + 9);
                        if (int.TryParse(camPart, out int camId))
                        {
                            camToTopic[camId] = topic;
                        }
                    }
                }
                Terminal.Log(line);
            }
            isFirst2 = false;
        }
        else
        {
            foreach (string line in addressLines2)
            {
                if (line.Contains("[mviz] Advertise topic="))
                {
                    // 提取 topic 和 cam
                    int topicStart = line.IndexOf("topic=") + 6;
                    int topicEnd = line.IndexOf(" for cam=");
                    if (topicStart > 6 && topicEnd > topicStart)
                    {
                        string topic = line.Substring(topicStart, topicEnd - topicStart);
                        string camPart = line.Substring(topicEnd + 9);
                        if (int.TryParse(camPart, out int camId))
                        {
                            camToTopic[camId] = topic;
                        }
                    }
                }
                Terminal.Log(line);
            }
            isFirst2 = true;
        }
        activePingCoroutine = StartCoroutine(CameraFailCoroutine());
    }

    public void OpenCommand()
    {
        string[] addressLines = new string[]
       {"[ INFO] The current version of the operating system is Ubuntu 20.04.”",
        " ”",
"[INFO] The current Ros version is noetic .”",
" ”",
"[INF0] librviz_plugin. so Version 3. 0. 003-0000000”",
"[INFO] MDc_Application_ Visualizer version 3. 0. 003-0000000”",
"[INFO] Running MViz and listening on 192. 168. 13. 121 : 7000”",
"[INFO] Attempting to start MViz ...”",
" ”",
"2026-04-11 15:17:07. 126 [Info] [DataValidation] Whether to enable”",
"data range verification. [true]”",
"2026-04-11 15:17:07.165 [Info] [Comm] TCP listening port [7000]”",
"2026-04-11 15:17:07. 494 [Info] [Comm] Add a visual application no”",
"de socket [io]. The number of connected visual application nodesis [1].”",
"[INFO] MViz started successfully.”",
"[INFO] Attempting to start rviz.”",
"Got keys from plugin meta data （”vnc“ )”",
"QFactoryLoader: : QFactoryLoader() looking at ” /usr/lib/ x86_64-lin",
"ux-gnu/qt5/plugins/platforms/libqxcb. so”",
"Found metadata in lib /usr/lib/x86_64-linux-gnu/qt5/plugins/platforms/libqxcb.so , metadata=”",
"{",
@"   ""IID"" : ""org . qt-project .Qt . QPA . QPlatformIntegrationFactoryInterface.5.3""",
@"  ""MetaData"": {",
@"       ""Keys"" : [",
@"           ""xcb""",
@"       ]”",
@"  },”",
@"  ""archreq"" : 0",
@"  ""className"" : ""QXcbIntegrationPlugin""",
@"  ""debug"" : false ,”",
@"  ""version"": 330752",
"}",
" ",
" ",
@"Got keys from plugin meta data (""xcb"")",
@"QFactoryLoader: : QFactoryLoader O) checking directory path/opt/r",
@"os/noetic/bin/platforms""",
"loaded library /usr/lib/x86_64-linux-gnu/qt5/plugins/platforms/",
@"libqxcb. so""",
@"loaded library ""Xcursor""",
@"Got keys from plugin meta data (""ibus"")",
@"QFactoryLoader: : QFactoryLoader() checking directory path  ""/opt/r",
@"os/noetic/bin/platforminputcontexts""",
@"loaded library  /usr/lib/ x86_64-linux-gnu/ qt5/plugins/platformin""",
@"putcontexts/libcomposeplatforminputcontextplugin. so""",
@"QFactoryLoader: : QFactoryLoaderO) checking directory path  ""/usr/1",
@"ib/x86_64-linux-gnu/ qt5/plugins/ styles""",
@"QFactoryLoader: : QFactoryLoaderO) checking directory path ""/opt/r",
@"os/noetic/bin/styles""",
@"[INFO] [1775891832. 860348700] : rviz version 1.14. 26",
"[INFO] [1775891832. 860669600] : compiled against Qt version 5.12.8",
"[INFO] [1775891832.860852200] : compiled against 0GRE version 1.90(Ghadamon)",
"[INFO] [1775891832. 887025700]: Forcing 0penGl version 0.",
"ined symbol: _ZN10QXcbWindow17startSystemResizeERK6QPointN2Qt6Co",
@"rnerE,version Qt_5_ PRIVATE_API)""",
"Cannot load library /usr/lib/x86_64-linux-gnu/qt5/plugins/xcbgli",
"ntegrations/libqxcb-egl-integration. so: (/usr/lib/x86_64-linux-g",
"nu/qt5/plugins/xcbg lintegrations/libqxcb-egl-integration. so: und",
"efined symbol: _ZN1oQXcbWindowl7startSystemResizeERK6QPointN2Qt6",
"CornerE, version Qt_5 _ PRIVATE_API)",
@"QLibraryPrivate: : loadPlugin failed on ""/usr/lib/ x86_64-linux-gnu",
@"/qt5/plugins/xcbglintegrations/libqxcb-egl-integration. so"" : ""Ca",
"nnot load library /usr/lib/ x86_64-linux-gnu/ qt5/plugins/ xcbglint",
"egrations/libqxcb-egl-integration . so: (/usr/lib/ x86_64-linux-gnu",
"/qt5/plugins/xcbglintegrations/libqxcb-egl-integration. so: undef",
"ined symbol: _ ZN1oQXcbWindow17startSystemResizeERK6QPointN2Qt6Co",
@"rnerE, version Qt_5_ PRIVATE_API)""",
"QCssParser: :parseColorValue: Specified color with alpha value bu",
"t no alpha given: 'rgba 152,153, 158 '",
"QCssParser: :parseColorValue: Specified color with alpha value bu",
"t no alpha given: 'rgba 151, 151, 151'",
"QCssParser: :parseColorValue: Specified color with alpha value bu",
"t no alpha given: 'rgba 1, 223, 255'",
"QCssParser: : parseColorValue: Specified color with alpha value bu",
"t no alpha given: 'rgba 152, 153, 158'",
"QCssParser: :parseColorValue: Specified color with alpha va lue bu",
"t no alpha given: 'rgba 22, 23, 26'",
"QCssParser: :parseColorValue: Specified color with alpha value bu",
"t no alpha given: 'rgba 120, 122, 128'",
"QCssParser: :parseColorValue: Specified co lor with alpha va lue bu",
"t no alpha given: 'rgba 24, 24, 28'",
       };
        activePingCoroutine = StartCoroutine(Variablespeed(addressLines, 14, true));

    }
    public void RoscoreCommand()
    {
        string[] addressLines = new string[]
        {
            "... logging to /home/inwinic/ .ros/log/5aebb79a-3576-11f1-9f8 ",
            "0-40c2ba8d2c00/roslaunch-Anna-154 . log",
            "Checking log directory for disk usage. This may take a while.",
             "Press Ctrl-C to interrupt",
            "Done checking log file disk usage. Usage is <1GB.",
            "started roslaunch server http: / /Anna:57404/",
            "ros_comm version 1.17.4",
            " ",
            "SUMMARY",
            "=========",
            " ",
            "PARAMETERS",
            "*/rosdistro: noetic",
            "*/rosversion: 1.17. 4",
            " ",
            "NODES",
            "auto-starting new master",
            "process [master]: started with pid [162]",
            "ROS _ MASTER_URI=http : / /Anna : 11311/",
            " ",
            "setting /run_id to 5aebb79a-3576-11f1-9f80-40c2ba8d2c00",
            "process [rosout-1]: started with pid [174]",
            "started core service [/rosout]",
        };
        activePingCoroutine = StartCoroutine(Variablespeed(addressLines, 5, false));
    }
    /// <summary>
    /// 变速输出文本
    /// </summary>
    /// <param name="strings">文本内容</param>
    /// <param name="i">第几行变速</param>
    /// <param name="needsObject">是否需要物品显示</param>
    private IEnumerator Variablespeed(string[] strings, int i, bool needsObject)
    {
        displayobject[2].game.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        int count = 0;
        while (count < strings.Length)
        {

            if (count > i && count <strings.Length)
            {
                Terminal.Log(strings[count]);
                if (needsObject)
                    displayobject[0].game.SetActive(true);
                yield return new WaitForSeconds(0.001f);
            }
            else
            {
                Terminal.Log(strings[count]);
                yield return new WaitForSeconds(0.5f);
            }
            if (count >= strings.Length - 1 && needsObject)
            {
                displayobject[0].game.SetActive(false);
                displayobject[1].game.SetActive(true);
            }
            count++;

        }
        displayobject[2].game.SetActive(false);
    }
    private IEnumerator CameraFailCoroutine()
    {
        yield return new WaitForSeconds(1f);  // 快速连续输出
        // 快速输出：每 0.2 秒输出一条，连续输出 20 条后自动停止（也可无限，由停止命令控制）
        int count = 0;
        //int maxCount = 20;  // 可根据需要调整，或无限循环
        while (true)
        {
            // 随机选择一个 cam
            if (camToTopic.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, camToTopic.Count);
                int cam = new List<int>(camToTopic.Keys)[index];
                string topic = camToTopic[cam];
                int bytes = UnityEngine.Random.Range(10000, 100000);  // 随机字节数
                Terminal.Log($"cam={cam} topic={topic} publish FAIL bytes={bytes}");
            }
            else
            {
                Terminal.LogWarning("未找到 cam 映射，无法输出失败消息。");
            }
            count++;
            yield return new WaitForSeconds(0.2f);  // 快速连续输出
        }
    }
}
