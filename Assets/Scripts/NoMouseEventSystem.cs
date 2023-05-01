using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventSystem))]
public class NoMouseEventSystem : MonoBehaviour
{
    EventSystem eventSystem;
    GameObject selectedObj;

    // Start is called before the first frame update
    void Start()
    {
        eventSystem = GetComponent<EventSystem>();
        selectedObj = eventSystem.currentSelectedGameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (eventSystem.currentSelectedGameObject == null)
             eventSystem.SetSelectedGameObject(selectedObj);
     
         selectedObj = eventSystem.currentSelectedGameObject;
    }
}
