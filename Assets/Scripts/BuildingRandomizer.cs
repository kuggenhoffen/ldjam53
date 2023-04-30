using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingRandomizer : MonoBehaviour
{
    [SerializeField]
    List<GameObject> prefabs;

    List<GameObject> objects = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        UpdateSize();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void UpdateSize()
    {
        foreach (GameObject obj in objects)
        {
            Destroy(obj);
        }
        objects.Clear();

        int newSize = Random.Range(1, 4);
        for (int i = 0; i < newSize; i++)
        {
            GameObject obj = Instantiate(prefabs[Random.Range(0, prefabs.Count)], transform);
            obj.transform.localPosition = Vector3.up * i * prefabs[0].GetComponent<SpriteRenderer>().bounds.size.y;
            objects.Add(obj);
        }
    }
}
