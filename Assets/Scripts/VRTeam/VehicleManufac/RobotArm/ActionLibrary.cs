using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionLibrary : MonoBehaviour
{
    public static ActionLibrary Instance;

    public List<ActionRecord> records = new List<ActionRecord>();
    private string savePath;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        savePath = Application.persistentDataPath + "/actions.json";
       LoadAll();
    }

    // 获取指定机械臂的所有动作
    public List<ActionRecord> GetRecordsByArm(int armIndex)
    {
        List<ActionRecord> result = new List<ActionRecord>();
        foreach (var r in records)
            if (r.armIndex == armIndex) result.Add(r);
        return result;
    }

    // 添加动作（自动生成带机械臂前缀的名称）
    public void AddRecord(ActionClip clip, int armIndex)
    {
        string name = GenerateDefaultName(armIndex);
        AddRecord(clip, name, "用户", armIndex);
    }

    public void AddRecord(ActionClip clip, string name, string author, int armIndex)
    {
        ActionRecord rec = new ActionRecord
        {
            actionName = name,
            clip = clip,
            recordTime = System.DateTime.Now,
            author = author,
            armIndex = armIndex
        };
        records.Add(rec);
        SaveAll();
    }

    // 删除指定 ActionRecord 对象
    public void DeleteRecord(ActionRecord record)
    {
        records.Remove(record);
        SaveAll();
    }

    // 删除索引（旧接口保留）
    public void DeleteRecord(int index)
    {
        if (index >= 0 && index < records.Count)
            records.RemoveAt(index);
        SaveAll();
    }

    private string GenerateDefaultName(int armIndex)
    {
        int maxNum = 0;
        string prefix = "机械臂" + (armIndex + 1) + "_动作";
        foreach (var r in records)
        {
            if (r.armIndex != armIndex) continue;
            // 提取动作名称最后的数字
            string numStr = System.Text.RegularExpressions.Regex.Match(r.actionName, @"\d+$").Value;
            if (int.TryParse(numStr, out int n) && n > maxNum) maxNum = n;
        }
        return prefix + (maxNum + 1);
    }

    void SaveAll()
    {
        string json = JsonUtility.ToJson(new ActionListWrapper { actions = records }, true);
        System.IO.File.WriteAllText(savePath, json);
    }

    void LoadAll()
    {
        if (System.IO.File.Exists(savePath))
        {
            string json = System.IO.File.ReadAllText(savePath);
            var wrapper = JsonUtility.FromJson<ActionListWrapper>(json);
            if (wrapper != null) records = wrapper.actions;
         
        }
    }

    [System.Serializable]
    private class ActionListWrapper { public List<ActionRecord> actions; }
}
