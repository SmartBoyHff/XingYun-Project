using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Sensor_ExamStep
{
    [Tooltip("期望用户输入的命令（不区分大小写）")]
    public string expectedCommand;

    [Tooltip("命令正确时显示的输出文本")]
    public string successOutput;

    [Tooltip("可选：该步骤分值，默认为1")]
    public int scoreValue = 1;
}
