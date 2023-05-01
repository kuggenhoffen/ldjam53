using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    PersistentData persistentData;

    // Start is called before the first frame update
    void Start()
    {
        persistentData = FindObjectOfType<PersistentData>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStartBtnClick()
    {
        persistentData.score = 0;
        persistentData.health = 100f;
        SceneManager.LoadScene("Stage1");
    }

    public void OnExitBtnClick()
    {
        Application.Quit();
    }
}
