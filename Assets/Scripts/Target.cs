using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class Target : MonoBehaviour
{
    TMPro.TMP_Text textMesh;
    string text;
    SpriteRenderer targetRect;
    PolygonCollider2D targetCollider;
    [SerializeField]
    GameObject targetPrefab;
    GameObject targetVisual;
    bool matched;

    // Start is called before the first frame update
    void Start()
    {
        targetVisual = Instantiate(targetPrefab, transform);
        textMesh = targetVisual.GetComponentInChildren<TMPro.TMP_Text>();
        textMesh.color = Color.red;
        targetRect = targetVisual.GetComponent<SpriteRenderer>();
        targetRect.color = Color.red;
        targetCollider = GetComponent<PolygonCollider2D>();
        
        // Get width and height of targetSprite
        targetRect.size = targetCollider.bounds.size;
        targetRect.size = new Vector2(targetRect.size.x, targetRect.size.y);
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
    }

    public bool IsMatched()
    {
        return matched;
    }
}
