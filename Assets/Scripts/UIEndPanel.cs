using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIEndPanel : MonoBehaviour
{

    [SerializeField]
    TMPro.TMP_Text scoreText;
    int scoreToShow;
    Coroutine scoreCoroutine;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Show(int score)
    {
        scoreToShow = score;
        SetScoreText(0);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnAnimationStartScore()
    {
        scoreCoroutine = StartCoroutine(AnimateScore(scoreToShow));
    }

    void SetScoreText(int score)
    {
        scoreText.text = $"Score: {score}";
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
            yield return null;
        }
        SetScoreText(score);
    }
}
