using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioClipPlayer))]
[RequireComponent(typeof(BoxCollider2D))]
public class GameManager : MonoBehaviour
{
    
    enum GameState {
        WAITING,
        START,
        FLYING,
        TUTORIAL,
        FINISH,
        GAMEOVER,
        SCORE
    };

    struct LaneState {
        public bool free;
        public bool colliding;
        public bool pickup;
        public bool forced;
    };

    const float maxVelocity = 5f;
    const float velocityChange = 0.1f;
    const float maxAngle = 20f;
    const float acceleration = 0.5f;
    const float endAcceleration = 2f;
    const int lanes = 4;
    const float laneOffset = 2f;
    const float forcedLaneCheckLength = 4f;
    const float freeLaneCheckLength = 2.5f;
    const float avoidCheckLength = 2f;
    const float maxEndVelocity = 10f;
    const float droneLaneSpeed = 10f;
    const float maxDroneVolume = 0.3f;
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
    [SerializeField]
    GameObject explosionPrefab;

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
    AudioClipPlayer audioClipPlayer;
    AudioSource droneAudioSource;
    PersistentData persistentData;

    // Start is called before the first frame update
    void Start()
    {
        droneAudioSource = GetComponent<AudioSource>();
        audioClipPlayer = GetComponent<AudioClipPlayer>();
        currentInputTextMesh.fontSize = inputTextSize;
        cam = Camera.main;
        targets = FindObjectsOfType<Target>();
        
        List<string> usedWords = new List<string>();
        foreach (Target target in targets)
        {
            target.enabled = false;
            if (target.forcedType != Target.ForcedTargetType.NONE) {
                continue;
            }
            string newWord = null;
            do {
                newWord = GetNewWord();
            } while (usedWords.Contains(newWord));
            target.word = newWord;
        }

        screenCollider = GetComponent<BoxCollider2D>();
        SetState(GameState.WAITING);
        stage = GameObject.Find("Stage");
        persistentData = FindObjectOfType<PersistentData>();
        droneAudioSource.volume = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (state == GameState.WAITING) {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && state != GameState.GAMEOVER)
        {
            SetState(GameState.GAMEOVER);
            return;
        }
        
        // Resize screenCollider to fit camera view
        cam = Camera.main;
        screenCollider.size = new Vector2(cam.orthographicSize * cam.aspect * 2, cam.orthographicSize * 2);

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            RemoveInput();
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            switch(state)
            {
                case GameState.START:
                case GameState.FLYING:
                case GameState.TUTORIAL:
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
            case GameState.FLYING:
                foreach (LaneState laneState in laneStates) {
                    if (laneState.forced) {
                        SetState(GameState.TUTORIAL);
                        break;
                    }
                }
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
            case GameState.TUTORIAL:
                bool forced = false;
                foreach (LaneState laneState in laneStates) {
                    if (laneState.forced) {
                        forced = true;
                        break;
                    }
                }
                if (!forced) {
                    SetState(GameState.FLYING);
                }
                break;

        }

        float horPitch = Mathf.Lerp(0.8f, 1.1f, Mathf.InverseLerp(0f, maxVelocity, velocity));
        float verPitch = Mathf.Lerp(0.8f, 1.2f, Mathf.InverseLerp(-2f, 2f, drone.GetComponent<Rigidbody2D>().velocity.y));
        droneAudioSource.pitch = horPitch * verPitch;
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
        audioClipPlayer.PlayAudioClip();
    }

    void RemoveInput()
    {
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            currentInputTextMesh.text = currentInput;
            audioClipPlayer.PlayAudioClip();
        }
    }

    void ClearInput()
    {
        currentInput = "";
        nextInputIndicateClear = 0f;
    }

    void CheckTargets()
    {
        foreach (Target target in targets)
        {
            if (target.enabled) {
                Debug.Log($"Comparing {currentInput} to {target.word}");
                if (!target.IsMatched() && target.word == currentInput)
                {
                    score += target.word.Length * 6;
                    target.SetMatched();
                    targetVelocity += velocityChange;
                    if (target.forcedType == Target.ForcedTargetType.TAKEOFF) {
                        SetState(GameState.FLYING);
                    }
                    else if (target.forcedType != Target.ForcedTargetType.NONE && state == GameState.TUTORIAL) {
                        SetState(GameState.FLYING);
                    }
                    break;
                }
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
        if (state != GameState.FLYING)
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
            laneState.forced = false;
            bool isAvoidable = false;

            RaycastHit2D hit = Physics2D.Raycast(GetLanePosition(i), Vector2.right, avoidCheckLength, ~LayerMask.GetMask("Ignore Raycast"));
            if (hit.collider)
            {
                Target target = hit.collider.GetComponent<Target>();
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

            RaycastHit2D forcedHit = Physics2D.Raycast(GetLanePosition(i), Vector2.right, forcedLaneCheckLength, ~LayerMask.GetMask("Ignore Raycast"));
            if (forcedHit.collider)
            {
                Target target = forcedHit.collider.GetComponent<Target>();
                laneState.forced = (target != null) && !target.IsMatched() && (target.forcedType != Target.ForcedTargetType.NONE);
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
        if (lane >= 0 && lane < lanes) {
            Debug.Log($"Set target lane {lane}");
            currentLane = lane;
        }
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
        if (stage == null) {
            stage = GameObject.Find("Stage");
        }
        return stage.transform.position.x;
    }

    public void EndStage()
    {
        SetState(GameState.FINISH);
    }

    int GetStageNumber()
    {        
        string currentName = SceneManager.GetActiveScene().name;
        Debug.Log("current scene name: " + currentName);
        int currentStage = 999;
        try {
            currentStage = int.Parse(currentName.Substring(5));
        }
        catch (Exception e) { };
        return currentStage;
    }

    int GetNextStageNumber()
    {
        return GetStageNumber() + 1;
    }

    static string BuildStagePath(int stageNumber)
    {
        return $"Assets/Scenes/Stage{stageNumber}.unity";
    }

    bool IsLastStage()
    {
        return SceneUtility.GetBuildIndexByScenePath(BuildStagePath(GetNextStageNumber())) == -1;
    }

    void SetState(GameState newState)
    {
        int showScore;
        switch (newState)
        {
            case GameState.WAITING:
                uiGame.SetActive(false);
                uiEndPanel.Show(0, GetStageNumber(), UIEndPanel.ViewType.STAGE_START); 
                drone.SetKinematic(true);           
                break;
            case GameState.START:
                StartCoroutine(StartStageCoroutine(1f));
                health = persistentData.health;
                SetTargetLane(0);
                for (int i = 0; i < lanes; i++)
                {
                    laneStates.Add(new LaneState());
                }
                score = 0;
                uiEndPanel.Hide();
                uiGame.SetActive(true);
                droneLane.position = drone.transform.position;
                break;
            case GameState.FLYING:
                if (state != GameState.FLYING && state != GameState.TUTORIAL) {
                    SetTargetLane(1);
                    targetVelocity = 2.5f;
                    drone.SetKinematic(false);
                }
                else if (state == GameState.TUTORIAL) {
                    for (int i = 0; i < lanes; i++)
                    {
                        if (laneStates[i].forced) {
                            LaneState laneState = laneStates[i];
                            laneState.forced = false;
                            laneStates[i] = laneState;
                        }
                    }
                }
                break;
            case GameState.FINISH:
                GameState nextState = GameState.SCORE;
                float duration = 5f;
                if (health <= 0f) {
                    // Explode
                    drone.gameObject.SetActive(false);
                    Instantiate(explosionPrefab, drone.transform.position, Quaternion.identity);
                    nextState = GameState.GAMEOVER;
                    duration = 2f;
                    droneAudioSource.Stop();
                }
                StartCoroutine(EndStageCoroutine(duration, nextState));
                break;
            case GameState.GAMEOVER:
                persistentData.score += score;
                uiEndPanel.Show(persistentData.score, GetStageNumber(), UIEndPanel.ViewType.GAME_OVER);
                uiGame.SetActive(false);
                droneAudioSource.Stop();
                break;
            case GameState.SCORE:
                persistentData.health = health;
                showScore = score;
                persistentData.score += score;
                if (IsLastStage()) {
                    showScore = persistentData.score;
                }
                uiEndPanel.Show(showScore, GetStageNumber(), IsLastStage() ? UIEndPanel.ViewType.GAME_COMPLETE : UIEndPanel.ViewType.STAGE_COMPLETE);
                uiGame.SetActive(false);
                break;
        }
        Debug.Log($"Change state {state} -> {newState}");
        state = newState;
    }

    IEnumerator EndStageCoroutine(float duration, GameState nextState)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            droneAudioSource.volume = maxDroneVolume - elapsed / duration * maxDroneVolume;
            yield return null;
        }
        SetState(nextState);
    }

    IEnumerator StartStageCoroutine(float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            droneAudioSource.volume = elapsed / duration * maxDroneVolume;
            yield return null;
        }
    }

    public void Collision()
    {
        if (state != GameState.FLYING)
        {
            return;
        }

        health -= 5f;
        if (health <= 0f)
        {
            SetState(GameState.FINISH);
        }
        else {
            drone.Nudge();
        }
    }

    public void OnStartAnimationEnd()
    {
        SetState(GameState.START);
    }

    public void OnEndButtonClick()
    {
        switch (state) {
            case GameState.GAMEOVER:
                SceneManager.LoadScene("TitleScene");
                break;
            case GameState.SCORE:
                if (IsLastStage())
                {
                    SceneManager.LoadScene("TitleScene");
                }
                else
                {
                    SceneManager.LoadScene($"Stage{GetNextStageNumber()}");
                }
                break;
        }
    }
}
