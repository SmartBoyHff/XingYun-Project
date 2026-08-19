using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExamSequenceLibrary : MonoBehaviour
{
    public static ExamSequenceLibrary Instance;
    public List<ExamSequence> sequences = new List<ExamSequence>();
    private string savePath;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        savePath = Application.persistentDataPath + "/examSequences.json";
        LoadAll();
    }

    public void AddSequence(ExamSequence seq)
    {
        sequences.Add(seq);
        SaveAll();
    }

    public void DeleteSequence(int index)
    {
        if (index >= 0 && index < sequences.Count)
            sequences.RemoveAt(index);
        SaveAll();
    }

    public string GenerateDefaultSequenceName()
    {
        int maxNum = 0;
        foreach (var s in sequences)
        {
            string numStr = System.Text.RegularExpressions.Regex.Match(s.sequenceName, @"\d+").Value;
            if (int.TryParse(numStr, out int n) && n > maxNum) maxNum = n;
        }
        return "øº∫À–Ú¡–" + (maxNum + 1);
    }

    void SaveAll()
    {
        string json = JsonUtility.ToJson(new SeqListWrapper { sequences = sequences }, true);
        System.IO.File.WriteAllText(savePath, json);
    }

    void LoadAll()
    {
        if (System.IO.File.Exists(savePath))
        {
            string json = System.IO.File.ReadAllText(savePath);
            var wrapper = JsonUtility.FromJson<SeqListWrapper>(json);
            if (wrapper != null) sequences = wrapper.sequences;
        }
    }
    public void SaveSequencesToFile()
    {
        SaveAll();
    }

    [System.Serializable]
    private class SeqListWrapper { public List<ExamSequence> sequences; }
}
