using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime;

public class CurveDrawer : MonoBehaviour
{
    [SerializeField] private LineChart chart;


    // 用 Dictionary 存储关节名 → Serie 对象的映射，方便后续操作
    private Dictionary<string, Serie> jointSeriesDict = new Dictionary<string, Serie>();
    private string currentJoint = "shoulderPitch";  // 默认显示大臂

    private void Start()
    {
        if (chart == null)
            chart = GetComponent<LineChart>();

        chart.RemoveData();  // 清空图表所有数据

        string[] jointNames = { "shoulderYaw", "shoulderPitch", "elbow", "wristPitch", "wristRoll" };
        foreach (var name in jointNames)
        {
            // AddSerie 返回 Serie 对象，保存到字典
            Serie serie = chart.AddSerie<Line>(name);
            // 设置系列样式（可选）
            serie.show = false;   // 初始全部隐藏
            jointSeriesDict[name] = serie;
        }

        // 显示默认关节
        ShowJoint(currentJoint);
    }

    /// <summary>
    /// 根据动作记录绘制当前关节的曲线
    /// </summary>
    public void DrawCurve(ActionClip clip)
    {
        if (clip == null || clip.frames.Count < 2) return;

        // 获取当前显示的系列并清空数据
        Serie serie = jointSeriesDict[currentJoint];
        serie.ClearData();

        // 移除之前的质量标注散点系列（如果存在）
        Serie oldScatter = chart.GetSerie("qualityMarks");
        if (oldScatter != null)
            chart.RemoveSerie(oldScatter);

        // 填充折线数据
        for (int i = 0; i < clip.frames.Count; i++)
        {
            float time = clip.frames[i].timestamp;
            float angle = GetAngleByJointName(clip.frames[i].angles, currentJoint);
            chart.AddData(serie.index, time, angle);
        }

        // 添加质量标注点（用散点系列）
        Serie scatter = chart.AddSerie<Scatter>("qualityMarks");
        scatter.symbol.type = SymbolType.Circle;
        scatter.symbol.size = 10f;
        scatter.itemStyle.color = Color.red;

        foreach (var frame in clip.frames)
        {
            if (frame.qualityMark > 0)
            {
                float time = frame.timestamp;
                float angle = GetAngleByJointName(frame.angles, currentJoint);
                chart.AddData(scatter.index, time, angle);
            }
        }

        // 刷新图表
        chart.RefreshAllComponent();
    }

    /// <summary>
    /// 切换查看的关节
    /// </summary>
    public void ShowJoint(string jointName)
    {
        if (!jointSeriesDict.ContainsKey(jointName)) return;

        // 隐藏上一个系列
        if (jointSeriesDict.ContainsKey(currentJoint))
            jointSeriesDict[currentJoint].show = false;

        // 显示新系列
        jointSeriesDict[jointName].show = true;
        currentJoint = jointName;

        // 如果已有记录数据，则重绘曲线（通常由外部调用 DrawCurve）
    }

    private float GetAngleByJointName(JointAngles a, string jointName)
    {
        return jointName switch
        {
            "shoulderYaw" => a.shoulderYaw,
            "shoulderPitch" => a.shoulderPitch,
            "elbow" => a.elbow,
            "wristPitch" => a.wristPitch,
            "wristRoll" => a.wristRoll,
            _ => 0f
        };
    }
    public void DrawCurveForRecord(ActionRecord record)
    {
        if (record == null || record.clip == null) return;
        DrawCurve(record.clip);
        // 可选：修改标题
        var title = chart.GetChartComponent<Title>();
        if (title != null) title.text = record.actionName;
    }
}
