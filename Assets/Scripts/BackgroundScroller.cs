using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField]
    float parallaxMultiplier;
    List<GameObject> objects = new List<GameObject>();

    GameManager manager;
    float lastPos;
    float textureUnitsX;
    Vector3 lastTransformPos;

    // Start is called before the first frame update
    void Start()
    {
        manager = FindObjectOfType<GameManager>();
        lastPos = manager.GetPosition();
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        textureUnitsX = renderer.sprite.texture.width / renderer.sprite.pixelsPerUnit;
        lastTransformPos = transform.position;
        Debug.Log("Texture units: " + textureUnitsX);
    }

    // Update is called once per frame
    void Update()
    {
        float posDiff = manager.GetPosition() - lastPos;
        transform.position += Vector3.right * posDiff * parallaxMultiplier;
        lastPos = manager.GetPosition();

        if (lastTransformPos.x - transform.position.x >= textureUnitsX)
        {
            transform.position += Vector3.right * textureUnitsX;
            lastTransformPos = transform.position;
        }
    }
}
