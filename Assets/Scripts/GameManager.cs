using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
public class GameManager : MonoBehaviour
{
    
    enum GameState {
        START,
        FYING,
        FINISH,
        GAMEOVER,
        SCORE
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
    const float endAcceleration = 2f;
    const int lanes = 4;
    const float laneOffset = 2f;
    const float freeLaneCheckLength = 2.5f;
    const float avoidCheckLength = 2f;
    const float maxEndVelocity = 10f;
    const float droneLaneSpeed = 10f;
    float velocity = 0f;
    float targetVelocity = 0f;
    float endVelocity = 0f;

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
    [SerializeField]
    UIEndPanel uiEndPanel;
    [SerializeField]
    GameObject uiGame;
    [SerializeField]
    Slider healthSlider;

    string currentInput;
    
    Camera cam;

    Target[] targets;
    const int inputTextSize = 36;
    const int inputTextMaxSize = 48;
    const float inputIndicateTime = 1f;
    float nextInputIndicateClear = 0f;

    GameState state = GameState.START;
    
    BoxCollider2D screenCollider;
    int currentLane = 0;
    bool droneHasPackage = false;
    
    int maxLane {
        get {
            return lanes - 1;
        }
    }

    int score;
    float health;

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
        SetState(GameState.START);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            RemoveInput();
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            switch(state)
            {
                case GameState.START:
                    if (currentInput == "TAKEOFF") {
                        SetState(GameState.FYING);
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

        healthSlider.value = health;

        switch (state)
        {
            case GameState.FYING:
                stage.transform.Translate(Vector3.left * velocity * Time.deltaTime);
                if (velocity < targetVelocity && velocity < maxVelocity) {
                    velocity += acceleration * Time.deltaTime;
                }
                if (velocity > maxVelocity) {
                    velocity = maxVelocity;
                }
                float verticalVelocityAngleLimit = Mathf.Lerp(maxAngle, 0f, Mathf.Abs(drone.GetComponent<Rigidbody2D>().velocity.y) / 2f);
                float targetAngle = Mathf.Clamp(Mathf.LerpAngle(0, maxAngle, velocity / maxVelocity), 0f, verticalVelocityAngleLimit);
                
                drone.transform.eulerAngles = new Vector3(0f, 0f, Mathf.SmoothDampAngle(drone.transform.eulerAngles.z, -targetAngle, ref rotationVelocity, 0.1f));

                Vector3 droneLaneTarget = laneBasePosition.position + Vector3.up * currentLane * laneOffset;
                if (droneLane.position != droneLaneTarget) {
                    droneLane.position = Vector3.MoveTowards(droneLane.position, droneLaneTarget, droneLaneSpeed * Time.deltaTime);
                }
                break;
            case GameState.FINISH:
                stage.transform.Translate(Vector3.left * velocity * Time.deltaTime);
                if (health > 0f) {
                    endVelocity += endAcceleration * Time.deltaTime;
                    if (endVelocity > maxEndVelocity) {
                        endVelocity = maxEndVelocity;
                    }
                    droneLane.transform.Translate(Vector3.right * endVelocity * Time.deltaTime);
                }
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
                score += target.word.Length * 6;
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
        if (state != GameState.FYING)
        {
            return;
        }

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
    }

    public bool Pickup(PickupController pickupController, Target.TargetType type)
    {
        bool retval = false;
        Debug.Log($"Pickup {droneHasPackage} {type} {pickupController.hasPackage}");
        if (droneHasPackage && type == Target.TargetType.DROPOFF && !pickupController.hasPackage)
        {
            droneHasPackage = false;
            retval = true;
            score += 100;
        }
        else if (!droneHasPackage && type == Target.TargetType.PICKUP && pickupController.hasPackage)
        {
            droneHasPackage = true;
            retval = true;
        }
        drone.hasPackage = droneHasPackage;
        return retval;
    }

    public float GetPosition()
    {
        return stage.transform.position.x;
    }

    public void EndStage()
    {
        SetState(GameState.FINISH);
    }

    void SetState(GameState newState)
    {
        switch (newState)
        {
            case GameState.START:
                health = 100f;
                SetTargetLane(0);
                for (int i = 0; i < lanes; i++)
                {
                    laneStates.Add(new LaneState());
                }
                score = 0;
                drone.SetKinematic(true);
                uiEndPanel.Hide();
                uiGame.SetActive(true);
                droneLane.position = drone.transform.position;
                break;
            case GameState.FYING:
                SetTargetLane(1);
                targetVelocity = 2.5f;
                drone.SetKinematic(false);
                break;
            case GameState.FINISH:
                if (health <= 0f) {
                    // Explode
                    drone.gameObject.SetActive(false);
                }
                StartCoroutine(EndStageCoroutine());
                break;
            case GameState.GAMEOVER:
                uiEndPanel.Show(score);
                uiGame.SetActive(false);
                break;
            case GameState.SCORE:
                uiEndPanel.Show(score);
                uiGame.SetActive(false);
                break;
        }
        state = newState;
    }

    IEnumerator EndStageCoroutine()
    {
        float elapsed = 0;
        const float endDuration = 5f;
        while (elapsed < endDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetState(GameState.SCORE);
    }

    public void Collision()
    {
        if (state != GameState.FYING)
        {
            return;
        }

        health -= 5f;
        if (health <= 0f)
        {
            SetState(GameState.FINISH);
        }
        drone.Nudge();
    }
}
