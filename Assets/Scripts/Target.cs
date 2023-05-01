using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioClipPlayer))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class Target : MonoBehaviour
{
    const float COLLISION_DELAY = 0.5f;

    public enum TargetType {
        NONE,
        OBSTACLE,
        PICKUP,
        DROPOFF
    };

    public enum ForcedTargetType {
        NONE,
        TAKEOFF,
        TUTORIAL1,
        TUTORIAL2,
        TUTORIAL3
    };
    TMPro.TMP_Text textMesh;
    [SerializeField]
    string text;
    SpriteRenderer targetRect;
    PolygonCollider2D targetCollider;
    [SerializeField]
    GameObject targetPrefab;
    GameObject targetVisual;
    bool matched;
    public TargetType type;
    public ForcedTargetType forcedType;

    GameManager gameManager;
    float lastCollisionTime = 0f;
    AudioClipPlayer audioClipPlayer;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        audioClipPlayer = GetComponent<AudioClipPlayer>();
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoeaded;
        if (targetVisual == null)
        {
            targetVisual = Instantiate(targetPrefab, transform);
        }
        textMesh = targetVisual.GetComponentInChildren<TMPro.TMP_Text>();
        textMesh.color = Color.red;
        targetRect = targetVisual.GetComponent<SpriteRenderer>();
        targetRect.color = Color.red;
        targetCollider = GetComponent<PolygonCollider2D>();
        
        // Get width and height of targetSprite
        targetRect.size = targetCollider.bounds.size;
        targetRect.size = new Vector2(targetRect.size.x, targetRect.size.y);
        targetRect.transform.position = targetCollider.bounds.center + Vector3.back * 0.1f;
        // If the position is above center of camera view, move text below
        Vector3 screenPos = Camera.main.WorldToViewportPoint(transform.position);
        if (screenPos.y > 0.5f)
        {
            textMesh.rectTransform.localPosition = new Vector3(-targetRect.size.x / 2, -targetRect.size.y / 2, 0);
        }
        else {
            textMesh.rectTransform.localPosition = new Vector3(-targetRect.size.x / 2, targetRect.size.y / 2, 0);
        }
        matched = false;

    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoeaded;
    }

    void OnSceneLoeaded(Scene scene, LoadSceneMode mode)
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (textMesh.text != text)
        {
            textMesh.text = text;
        }
    }

    public string word {
        set {
            text = value;
        }
        get {
            return text;
        }
    }

    public void SetMatched()
    {
        matched = true;
        textMesh.color = Color.green;
        targetRect.color = Color.green;
        audioClipPlayer.PlayAudioClip();

    }

    public bool IsMatched()
    {
        return matched;
    }

    void CheckCollision(Collider2D collider)
    {
        if (Time.time > lastCollisionTime + COLLISION_DELAY)
        {
            if (collider.gameObject.tag == "Player" && type == TargetType.OBSTACLE && !matched)
            {
                lastCollisionTime = Time.time;
                gameManager.Collision();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        CheckCollision(collider);
    }

    void OnTriggerStay2D(Collider2D collider)
    {
        CheckCollision(collider);
    }


}
