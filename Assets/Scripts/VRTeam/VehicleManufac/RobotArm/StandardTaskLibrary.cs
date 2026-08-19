using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandardTaskLibrary : MonoBehaviour
{
    public static StandardTaskLibrary Instance;

    public List<StandardTask> tasks = new List<StandardTask>();
    private string savePath;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        savePath = Application.persistentDataPath + "/standardTasks.json";
        LoadAll();
    }

    public void AddTask(StandardTask task)
    {
        tasks.Add(task);
        SaveAll();
    }

    public void DeleteTask(int index)
    {
        if (index >= 0 && index < tasks.Count)
            tasks.RemoveAt(index);
        SaveAll();
    }

    void SaveAll()
    {
        string json = JsonUtility.ToJson(new TaskListWrapper { tasks = tasks }, true);
        System.IO.File.WriteAllText(savePath, json);
    }

    void LoadAll()
    {
        if (System.IO.File.Exists(savePath))
        {
            string json = System.IO.File.ReadAllText(savePath);
            var wrapper = JsonUtility.FromJson<TaskListWrapper>(json);
            if (wrapper != null) tasks = wrapper.tasks;
        }
    }
    public string GenerateDefaultName()
    {
        int maxNum = 0;
        foreach (var t in tasks)
        {
            string numStr = System.Text.RegularExpressions.Regex.Match(t.taskName, @"\d+").Value;
            if (int.TryParse(numStr, out int n) && n > maxNum) maxNum = n;
        }
        return "хннЯ" + (maxNum + 1);
    }

    [System.Serializable]
    private class TaskListWrapper { public List<StandardTask> tasks; }
}
