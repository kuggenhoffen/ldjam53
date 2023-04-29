using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{

    TMPro.TMP_Text textMesh;

    // Start is called before the first frame update
    void Start()
    {
        textMesh = GetComponentInChildren<TMPro.TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetWord(string word)
    {
        textMesh.text = word;
    }
}
