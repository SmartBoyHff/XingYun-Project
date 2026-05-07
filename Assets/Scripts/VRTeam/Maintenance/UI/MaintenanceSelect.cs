using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaintenanceSelect : MonoBehaviour
{
    public Dropdown dropdown;
    public Text scoreGameObject;
    public GameObject arrow; //下滑箭头
    public string option1;//默认选项
    public string option2;//选项1
    public string option3;//选项2
    // 设置正确选项的文字
    public string rightAnswer = "正常";
    public string wrongAnswer = "有刮痕";
    private int score = 0;

    void Start()
    {
        // 清空默认选项，设置提示项
        SetupDropdown();
        // 默认分数不显示
        scoreGameObject.gameObject.SetActive(false);
        // 默认开启下滑选项
        dropdown.enabled = true;
    }

    void SetupDropdown()
    {
        // 清空原有的选项列表
        dropdown.options.Clear();
        // 添加三个选项（顺序：提示项、正确项、错误项）
        dropdown.options.Add(new Dropdown.OptionData(""));
        dropdown.options.Add(new Dropdown.OptionData(""));
        dropdown.options.Add(new Dropdown.OptionData(""));
        //给添加的三个选项赋值
        dropdown.options[0].text = option1;
        dropdown.options[1].text = option2;
        dropdown.options[2].text = option3;

        // 默认显示 "请选择"
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    // 提交答案时调用（比如点击按钮后判断）
    public void SubmitAnswer()
    {
        Debug.Log("SubmitAnswer 被调用了！");  // 加上这一行测试

        int selectedIndex = dropdown.value;
        string selectedText = dropdown.options[selectedIndex].text;

        // 如果选中的是提示项（请选择）
        if (selectedIndex == 0)
        {
            Debug.Log("⚠️ 请先选择一个选项！");
            // 可以在这里提示用户，比如弹窗或者改变文字颜色
            return;
        }

        // 判断答案正确与否
        if (selectedText == rightAnswer)
        {
            Debug.Log("✅ 回答正确！");
            // 这里可以加正确反馈：加分、显示对勾、跳转下一题等
            score = 1;
            scoreGameObject.text = score.ToString();
            scoreGameObject.gameObject.SetActive(true);
            arrow.gameObject.SetActive(false);
            dropdown.enabled = false;
        }
        else if (selectedText == wrongAnswer)
        {
            Debug.Log("❌ 回答错误！");
            // 这里可以加错误反馈：扣分、显示红叉、提示重试等
            score = 0;
            scoreGameObject.text = score.ToString();
            scoreGameObject.gameObject.SetActive(true);
            arrow.gameObject.SetActive(false);
            dropdown.enabled = false;
        }
    }

    // 可选：当用户点击下拉框时自动移除"请选择"选项（更好的体验）
    // 需要在 Dropdown 的 On Value Changed 事件中绑定这个方法
    public void OnDropdownValueChanged(int index)
    {
        // 如果用户第一次选择了有效选项（不是"请选择"），并且"请选择"还在列表中
        if (index != 0 && dropdown.options[0].text == "请选择")
        {
            // 移除"请选择"选项
            dropdown.options.RemoveAt(0);
            // 调整当前选中的索引（因为删除了第一项，原索引需要减1）
            dropdown.value = index - 1;
            dropdown.RefreshShownValue();

            Debug.Log($"用户选择了：{dropdown.options[dropdown.value].text}");
        }
        // 如果用户选中的是"请选择"，什么都不做，保持提示
    }
}
