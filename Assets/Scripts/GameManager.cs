using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    const float velocity = 1f;
    [SerializeField]
    GameObject targetPrefab;
    [SerializeField]
    TMPro.TMP_Text currentInputTextMesh;
    [SerializeField]
    GameObject stage;
    string currentInput;
    
    Camera cam;

    Target[] targets;
    const int inputTextSize = 36;
    const int inputTextMaxSize = 48;
    const float inputIndicateTime = 1f;
    float nextInputIndicateClear = 0f;

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
            CheckTargets();
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
            if (!target.IsMatched() && target.word == currentInput)
            {
                target.SetMatched();
                break;
            }
        }
    }
}
