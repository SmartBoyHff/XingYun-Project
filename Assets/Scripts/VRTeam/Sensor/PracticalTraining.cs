using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class PTScene
{
    public string PTName;
    public string sceneName;
    public Sprite PTSprite;
}
public class PracticalTraining : MonoBehaviour
{
    public GameObject trainingButton;
    public RectTransform Panel;
    public List<PTScene> pTScene;
    
    private void Awake()
    {
        for(int i=0;i<pTScene.Count;i++)
        {
            PTScene sceneData = pTScene[i];
            GameObject btnObj = Instantiate(trainingButton, Panel);
            Button btn = btnObj.GetComponentInChildren<Button>();
            Image[] btnImage = btnObj.GetComponentsInChildren<Image>();
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            foreach (Image ima in btnImage)  
                ima.sprite = sceneData.PTSprite;
            if (btnText != null) 
                btnText.text = sceneData.PTName;
            string sceneToLoad = sceneData.sceneName;   
            btn.onClick.AddListener(() => OnTrainingButtonClicked(sceneToLoad));
            Debug.Log(1);
        }
    }
    private void OnTrainingButtonClicked(string sceneName)
    {
        Debug.Log(sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
