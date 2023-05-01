using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStartBtnClick()
    {
        PlayerPrefs.SetInt("Score", 0);
        PlayerPrefs.SetFloat("Health", 100f);
        SceneManager.LoadScene("Stage1");
    }

    public void OnExitBtnClick()
    {
        Application.Quit();
    }
}
