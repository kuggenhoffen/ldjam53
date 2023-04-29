using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GameManager : MonoBehaviour
{
    
    enum GameState {
        LANDED,
        FYING
    };

    struct LaneState {
        public bool free;
        public bool colliding;
    };

    const float maxVelocity = 2f;
    const float maxAngle = 20f;
    const float acceleration = 0.5f;
    const int lanes = 4;
    const float laneOffset = 2f;
    const float freeLaneCheckLength = 5f;
    const float avoidCheckLength = 2f;
    float velocity = 0f;

    [SerializeField]
    GameObject targetPrefab;
    [SerializeField]
    TMPro.TMP_Text currentInputTextMesh;
    [SerializeField]
    GameObject stage;
    [SerializeField]
    DroneController drone;
    [SerializeField]
    Transform laneBasePosition;
    [SerializeField]
    Transform droneLane;
    string currentInput;
    
    Camera cam;

    Target[] targets;
    const int inputTextSize = 36;
    const int inputTextMaxSize = 48;
    const float inputIndicateTime = 1f;
    float nextInputIndicateClear = 0f;

    GameState state = GameState.LANDED;
    
    BoxCollider2D screenCollider;
    int currentLane = 0;
    
    int maxLane {
        get {
            return lanes - 1;
        }
    }

    List<LaneState> laneStates = new List<LaneState>(lanes);
    float rotationVelocity = 0f;

    // Start is called before the first frame update
    void Start()
    {
        currentInputTextMesh.fontSize = inputTextSize;
        cam = Camera.main;
        targets = FindObjectsOfType<Target>();
        
        List<string> usedWords = new List<string>();
        foreach (Target target in targets)
        {
            string newWord = null;
            do {
                newWord = GetNewWord();
            } while (usedWords.Contains(newWord));
            target.word = newWord;
            target.enabled = false;
        }

        // Resize screenCollider to fit camera view
        screenCollider = GetComponent<BoxCollider2D>();
        screenCollider.size = new Vector2(cam.orthographicSize * cam.aspect * 2, cam.orthographicSize * 2);
        SetTargetLane(0);
        for (int i = 0; i < lanes; i++)
        {
            laneStates.Add(new LaneState());
        }
    }

    // Update is called once per frame
    void Update()
    {
        stage.transform.Translate(Vector3.left * velocity * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            RemoveInput();
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            switch(state)
            {
                case GameState.LANDED:
                    if (currentInput == "TAKEOFF") {
                        state = GameState.FYING;
                        SetTargetLane(maxLane);
                    }
                    break;
                case GameState.FYING:
                    CheckTargets();
                    break;
            }
            ClearInput();
        }

        if (Input.anyKeyDown && Input.inputString.Length > 0)
        {
            char key = Input.inputString[0];
            AddInput(key);
        }


        if (nextInputIndicateClear < Time.time)
        {
            currentInputTextMesh.text = currentInput;
        }

        switch (state)
        {
            case GameState.FYING:
                if (velocity < maxVelocity) {
                    velocity += acceleration * Time.deltaTime;
                }
                float verticalVelocityAngleLimit = Mathf.Lerp(maxAngle, 0f, Mathf.Abs(drone.GetComponent<Rigidbody2D>().velocity.y) / 2f);
                float targetAngle = Mathf.Clamp(Mathf.LerpAngle(0, maxAngle, velocity / maxVelocity), 0f, verticalVelocityAngleLimit);
                Debug.Log($"verticalLimit {verticalVelocityAngleLimit}, vel.y {drone.GetComponent<Rigidbody2D>().velocity.y}, targetAngle {targetAngle}");
                
                drone.transform.eulerAngles = new Vector3(0f, 0f, Mathf.SmoothDampAngle(drone.transform.eulerAngles.z, -targetAngle, ref rotationVelocity, 0.1f));
                break;
        }
    }

    string GetNewWord()
    {
        string newWord = WordList.getWord();
        foreach (Target target in targets)
        {
            if (target.GetComponent<Target>().word == newWord)
            {
                return GetNewWord();
            }
        }
        return newWord;
    }

    void AddInput(char key)
    {
        if (char.IsControl(key) || char.IsWhiteSpace(key))
        {
            return;
        }
        currentInput += char.ToUpper(key);
        string formattedText = currentInput.Substring(0, currentInput.Length - 1) + $"<size={inputTextMaxSize}>{currentInput[currentInput.Length - 1]}</size>";
        currentInputTextMesh.text = formattedText;
        nextInputIndicateClear = Time.time + inputIndicateTime;
    }

    void RemoveInput()
    {
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            currentInputTextMesh.text = currentInput;
        }
    }

    void ClearInput()
    {
        currentInput = "";
        nextInputIndicateClear = 0f;
    }

    void CheckTargets()
    {
        Debug.Log("Check input: " + currentInput);
        foreach (Target target in targets)
        {
            Debug.Log($"Comparing {currentInput} to {target.word}");
            if (target.enabled && !target.IsMatched() && target.word == currentInput)
            {
                target.SetMatched();
                break;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger enter: " + other.gameObject.name);
        Target target = other.GetComponent<Target>();
        if (target)
        {
            target.enabled = true;   
        }
    }

    void FixedUpdate()
    {
        bool needAvoid = false;
        int avoidLane = 0;
        for (int i = 0; i < laneStates.Count; i++)
        {
            LaneState laneState = laneStates[i];
            laneState.colliding = false;
            laneState.free = true;
            bool isAvoidable = false;

            RaycastHit2D hit = Physics2D.Raycast(GetLanePosition(i), Vector2.right, avoidCheckLength, ~LayerMask.GetMask("Ignore Raycast"));
            if (hit.collider)
            {
                Target target = hit.collider.GetComponent<Target>();
                if (target)
                {
                    laneState.colliding = true;
                    isAvoidable = target.IsMatched();
                }
            }

            RaycastHit2D freeLaneHit = Physics2D.Raycast(GetLanePosition(i), Vector2.right, freeLaneCheckLength, ~LayerMask.GetMask("Ignore Raycast"));
            if (freeLaneHit.collider)
            {
                laneState.free = false;
            }
            else
            {
                avoidLane = i;
            }

            laneStates[i] = laneState;

            if (i == currentLane && laneState.colliding && isAvoidable)
            {
                needAvoid = true;
            }
        }

        if (needAvoid)
        {
            SetTargetLane(avoidLane);
        }
    }

    void OnDrawGizmos()
    {
        LaneState defaultLane = new LaneState();
        LaneState laneState;
        for (int i = 0; i < lanes; i++)
        {
            laneState = defaultLane;
            if (i < laneStates.Count)
            {
                laneState = laneStates[i];
            }
            Gizmos.color = Color.green;
            if (laneState.colliding) {
                Gizmos.color = Color.red;
            }
            Gizmos.DrawRay(GetLanePosition(i) + Vector3.up * 0.05f, Vector2.right * avoidCheckLength);
            Gizmos.color = Color.red;
            if (laneState.free) {
                Gizmos.color = Color.green;
            }
            Gizmos.DrawRay(GetLanePosition(i) - Vector3.up * 0.05f, Vector2.right * freeLaneCheckLength);
        }
    }

    Vector3 GetLanePosition(int lane)
    {
        return laneBasePosition.position + Vector3.up * lane * laneOffset;
    }

    void SetTargetLane(int lane)
    {
        currentLane = lane;
        droneLane.position = laneBasePosition.position + Vector3.up * lane * laneOffset;
    }
}
