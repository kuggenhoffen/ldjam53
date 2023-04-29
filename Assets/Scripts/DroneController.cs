using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DroneController : MonoBehaviour
{

    public bool hasPackage;
    Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        hasPackage = false;
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetBool("package", hasPackage);
    }
}
