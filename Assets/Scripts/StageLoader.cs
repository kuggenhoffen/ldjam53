using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Camera.main.gameObject.SetActive(false);
        GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 1);
        SceneManager.LoadScene("PlayScene", LoadSceneMode.Additive);
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
