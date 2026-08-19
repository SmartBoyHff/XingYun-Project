using Foundation.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 外部依赖
[Serializable]
public struct ExternalDependency
{
    public ExamUIManager externalScript;
    public int targetStepIndex;
}
[Serializable]
public class ExamStep
{
   
    [Tooltip("是否需要默认文本")]
    public bool isDefaultText = true;
    [Tooltip("期望输入的命令（不区分大小写）")]
    public string startCommand;

    [Tooltip("命令正确时显示的输出文本")]
    public string endCommand;
    public string endName;
    [Tooltip("可选：该步骤分值，默认为1")]
    public int scoreValue = 1;

    [Tooltip("开始文本")]//不勾选默认文本才有
    public string[] startText;
    [Tooltip("开始输入显示的输出文本")]
    public string[] startOutput;
    public TerminalType startColor;
    [Tooltip("跳过显示的输出文本")]
    public string[] skipOutput;
    public TerminalType skipColor;
    [Tooltip("结束时显示的输出文本")]
    public string[] endOutput;
    public TerminalType endColor;

}
[Serializable]
public class TutorialStepSO 
{
    public string name;
    [Header("显示/隐藏物体")]
    public GameObject[] objectsToShow;
    public GameObject[] objectsToHide;

    [Header("高亮（UIHighlight / Outlinable）")]
    public GameObject[] objectsToHighlight;

    [Header("语音")]
    public AudioClip voiceClip;

    [Header("文本")]
    [Tooltip("多语言 key，若留空则使用下方 stepText")]
    public string textKey;
    [TextArea(3, 5)] public string stepText;           // 无多语言时使用  
    [TextArea(3, 5)] public string Text; //提示
    [TextArea(3, 5)] public string Text1; //提示

    [Header("完成条件")]
    public ExternalDependency[] dependencies;  //外部脚本
    public bool autoProceed = false;           // 等待外部脚本驱动
    public bool waitForVoice = false;          // 等语音播完
    public float waitTime;                    //等待时间

    [Header("专属按钮（点击即完成本步，并可加分）")]
    public Button next;          // 如果赋值，点击此按钮即推进且加分
    public Button[] nexts;
    public UnityEvent onSkip;



}
[Serializable]
public class Content
{
    public string title;//标题
    public Sprite illustration;//配图
    public Subtitle[] subtitles;

}
[Serializable]
public class Subtitle
{
    public string title;//小标题
    [TextArea(3, 5)] public string text;//小标题内容
}



