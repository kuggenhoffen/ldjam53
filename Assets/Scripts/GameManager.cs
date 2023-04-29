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
        public bool pickup;
    };

    const float maxVelocity = 5f;
    const float velocityChange = 0.1f;
    const float maxAngle = 20f;
    const float acceleration = 0.5f;
    const int lanes = 4;
    const float laneOffset = 2f;
    const float freeLaneCheckLength = 2.5f;
    const float avoidCheckLength = 2f;
    float velocity = 0f;
    float targetVelocity = 0f;

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
    bool droneHasPackage = false;
    
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
                        SetTargetLane(1);
                        targetVelocity = 2.5f;
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
                if (velocity < targetVelocity && velocity < maxVelocity) {
                    velocity += acceleration * Time.deltaTime;
                }
                if (velocity > maxVelocity) {
                    velocity = maxVelocity;
                }
                float verticalVelocityAngleLimit = Mathf.Lerp(maxAngle, 0f, Mathf.Abs(drone.GetComponent<Rigidbody2D>().velocity.y) / 2f);
                float targetAngle = Mathf.Clamp(Mathf.LerpAngle(0, maxAngle, velocity / maxVelocity), 0f, verticalVelocityAngleLimit);
                
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
                targetVelocity += velocityChange;
                break;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Target target = other.GetComponent<Target>();
        if (target)
        {
            target.enabled = true;   
        }
    }

    void FixedUpdate()
    {
        bool needSwitchLane = false;
        int avoidLane = -1;
        int pickupLane = -1;
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
                Debug.Log("Hit: " + hit.collider.gameObject.name + " " + LayerMask.LayerToName(hit.collider.gameObject.layer));
                laneState.colliding = ((target != null) && (target.type == Target.TargetType.OBSTACLE)) || (LayerMask.LayerToName(hit.collider.gameObject.layer) == "Obstacle");
                laneState.pickup = (target != null) && (target.type != Target.TargetType.OBSTACLE) && target.IsMatched();
                if (laneState.colliding)
                {
                    isAvoidable = hit.collider.gameObject.layer == LayerMask.NameToLayer("Obstacle") || (target && target.IsMatched());
                }
            }

            RaycastHit2D freeLaneHit = Physics2D.Raycast(GetLanePosition(i), Vector2.right, freeLaneCheckLength, ~LayerMask.GetMask("Ignore Raycast"));
            if (freeLaneHit.collider)
            {
                laneState.free = false;
            }
            else if (avoidLane < 0 || Mathf.Abs(currentLane - i) < Mathf.Abs(currentLane - avoidLane))
            {
                avoidLane = i;
            }

            laneStates[i] = laneState;

            if (laneState.pickup)
            {
                pickupLane = i;
                needSwitchLane = true;
            }
            else if (i == currentLane && laneState.colliding && isAvoidable)
            {
                needSwitchLane = true;
            }
        }

        if (needSwitchLane)
        {
            Debug.Log($"Need switch, {pickupLane}, {avoidLane}");
            SetTargetLane(pickupLane >= 0 ? pickupLane : avoidLane);
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
            else if (laneState.pickup) {
                Gizmos.color = Color.yellow;
            }
            Gizmos.DrawRay(GetLanePosition(i) + Vector3.up * 0.05f, Vector2.right * avoidCheckLength);
            Gizmos.color = Color.red;
            if (laneState.free) {
                Gizmos.color = Color.green;
            }
            Gizmos.DrawRay(GetLanePosition(i) - Vector3.up * 0.05f, Vector2.right * freeLaneCheckLength);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(GetLanePosition(i), Vector2.right * 1000f);
        }
    }

    Vector3 GetLanePosition(int lane)
    {
        return laneBasePosition.position + Vector3.up * lane * laneOffset;
    }

    void SetTargetLane(int lane)
    {
        Debug.Log($"Set target lane {lane}");
        currentLane = lane;
        droneLane.position = laneBasePosition.position + Vector3.up * lane * laneOffset;
    }

    public bool Pickup(PickupController pickupController, Target.TargetType type)
    {
        bool retval = false;
        Debug.Log($"Pickup {droneHasPackage} {type} {pickupController.hasPackage}");
        if (droneHasPackage && type == Target.TargetType.DROPOFF && !pickupController.hasPackage)
        {
            droneHasPackage = false;
            retval = true;
        }
        else if (!droneHasPackage && type == Target.TargetType.PICKUP && pickupController.hasPackage)
        {
            droneHasPackage = true;
            retval = true;
        }
        drone.hasPackage = droneHasPackage;
        return retval;
    }
}
