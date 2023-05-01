using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class DroneController : MonoBehaviour
{
    const float nudgeForce = 10f;

    public bool hasPackage;
    Animator animator;
    AudioSource audioSource;
    [SerializeField]
    AudioClip[] audioClipsHurt;

    // Start is called before the first frame update
    void Start()
    {
        hasPackage = false;
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetBool("package", hasPackage);
    }

    public void SetKinematic(bool kinematic)
    {
        GetComponent<Rigidbody2D>().isKinematic = kinematic;
        transform.parent.GetComponent<Rigidbody2D>().isKinematic = kinematic;
        Debug.Log($"Parent: {transform.parent.name}");
    }

    public void Nudge()
    {
        // Add random force to random direction on the left side of the screen
        Vector3 direction = new Vector3(-1, Random.Range(-1f, 1f), 0);
        GetComponent<Rigidbody2D>().AddForce(direction.normalized * nudgeForce, ForceMode2D.Impulse);
        animator.SetTrigger("damage");
        if (audioClipsHurt.Length > 0) {
            audioSource.PlayOneShot(audioClipsHurt[Random.Range(0, audioClipsHurt.Length)]);
        }
    }
}
