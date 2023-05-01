using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Target))]
[RequireComponent(typeof(Animator))]
public class PickupController : MonoBehaviour
{
    GameManager manager;
    Target target;
    public bool hasPackage = false;
    // Start is called before the first frame update
    void Start()
    {
        target = GetComponent<Target>();

        GetComponent<Animator>().SetBool("pickup", target.type == Target.TargetType.PICKUP);
        hasPackage = target.type == Target.TargetType.PICKUP;
        GetComponent<Animator>().SetBool("package", hasPackage);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoeaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoeaded;
    }

    void OnSceneLoeaded(Scene scene, LoadSceneMode mode)
    {
        manager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            hasPackage = manager.Pickup(this, target.type) ? !hasPackage : hasPackage;
            GetComponent<Animator>().SetBool("package", hasPackage);
        }
    }
}
