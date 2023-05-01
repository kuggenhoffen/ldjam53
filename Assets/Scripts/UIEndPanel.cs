using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class UIEndPanel : MonoBehaviour
{
    public enum ViewType {
        STAGE_START,
        GAME_OVER,
        STAGE_COMPLETE,
        GAME_COMPLETE
    };
    const float hugeScore = 2000f;

    [SerializeField]
    TMPro.TMP_Text scoreText;
    [SerializeField]
    TMPro.TMP_Text topText;
    [SerializeField]
    TMPro.TMP_Text bottomText;
    [SerializeField]
    TMPro.TMP_Text buttonText;
    int scoreToShow;
    Coroutine scoreCoroutine;
    ViewType viewType;
    Animator animator;
    GameManager manager;
    bool scoreFinished = false;
    AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        manager = FindObjectOfType<GameManager>();
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Show(int score, int stage, ViewType vt)
    {
        viewType = vt;
        scoreToShow = score;
        SetScoreText(0);
        SetTexts(stage);
        gameObject.SetActive(true);
        animator.SetInteger("State", (int)viewType);
        animator.SetTrigger("Trigger");
        Button button = FindObjectOfType<Button>();
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (button && eventSystem) {
            eventSystem.SetSelectedGameObject(button.gameObject);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnAnimationStartScore()
    {
        scoreFinished = false;
        scoreCoroutine = StartCoroutine(AnimateScore(scoreToShow));
    }

    public void OnAnimationStartFadeFinished()
    {
        manager.OnStartAnimationEnd();
    }

    void SetScoreText(int score)
    {
        if (viewType == ViewType.GAME_COMPLETE || viewType == ViewType.GAME_OVER) {
            scoreText.text = $"Final Score: {score}";
            return;
        }
        scoreText.text = $"Score: {score}";
    }

    void SetTexts(int stage)
    {
        switch (viewType) {
            case ViewType.STAGE_START:
                topText.text = $"STAGE {stage}";
                bottomText.text = "";
                buttonText.text = "";
                break;
            case ViewType.GAME_OVER:
                topText.text = "GAME";
                bottomText.text = "OVER";
                buttonText.text = "MAIN MENU";
                break;
            case ViewType.STAGE_COMPLETE:
                topText.text = $"STAGE {stage}";
                bottomText.text = "COMPLETED";
                buttonText.text = "CONTINUE";
                break;
            case ViewType.GAME_COMPLETE:
                topText.text = $"GAME";
                bottomText.text = "COMPLETED";
                buttonText.text = "MAIN MENU";
                break;
        }
    }

    IEnumerator AnimateScore(int score)
    {
        int currentScore = 0;
        while (currentScore < score)
        {
            SetScoreText(currentScore);
            if (currentScore < 200 || (score - currentScore) < 200) {
                currentScore++;
            }
            else {
                currentScore += 11;
            }

            if (true || !audioSource.isPlaying) {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.pitch = Mathf.Lerp(0.9f, 1.2f, currentScore / hugeScore);
                audioSource.Play();
            }
            
            yield return null;
        }
        SetScoreText(score);
        scoreFinished = true;
    }

    public void OnButtonClick()
    {
        if (!scoreFinished) {
            StopCoroutine(scoreCoroutine);
            scoreFinished = true;
            SetScoreText(scoreToShow);
        }
        else {
            manager.OnEndButtonClick();
        }
    }

}
