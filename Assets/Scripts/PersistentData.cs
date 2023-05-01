using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PersistentData : MonoBehaviour
{

    public float health;
    public int score;

    // Start is called before the first frame update
    void Start()
    {
        score = 0;
        health = 100f;
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}