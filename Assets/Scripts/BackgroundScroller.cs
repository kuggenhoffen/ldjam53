using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField]
    float parallaxMultiplier;
    [SerializeField]
    bool automatic = false;
    List<GameObject> objects = new List<GameObject>();

    GameManager manager;
    float lastPos;
    float textureUnitsX;
    Vector3 lastTransformPos;

    // Start is called before the first frame update
    void Start()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        textureUnitsX = renderer.sprite.texture.width / renderer.sprite.pixelsPerUnit;
        lastTransformPos = transform.position;
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
        scene.GetRootGameObjects(objects);
        foreach (GameObject obj in objects)
        {
            manager = obj.GetComponent<GameManager>();
            if (manager != null) {
                break;
            }
        }

        if (manager != null) {
            lastPos = manager.GetPosition();
        }
    }

    // Update is called once per frame
    void Update()
    {
        float posDiff = -1f;
        if (!automatic) {
            if (manager == null) {
                return;
            }
            posDiff = manager.GetPosition() - lastPos;
            lastPos = manager.GetPosition();
        }

        transform.position += Vector3.right * posDiff * parallaxMultiplier;        

        if (lastTransformPos.x - transform.position.x >= textureUnitsX)
        {
            transform.position += Vector3.right * textureUnitsX;
            lastTransformPos = transform.position;
        }
    }
}
