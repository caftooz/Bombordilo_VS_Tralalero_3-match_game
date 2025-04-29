using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Slider _bossHPSlider;
    [SerializeField] private SpriteRenderer _bossImage;

    [SerializeField] private SpriteRenderer _background;

    [SerializeField] private Slider _stepSlider;

    [SerializeField] private GameObject _panelWin;
    [SerializeField] private GameObject _panelOver;
    [SerializeField] private GameObject _panelMap;

    [SerializeField] private Toggle[] _stars;
    [SerializeField] private TMP_Text _pointsText;

    [SerializeField] private TMP_Text _pointsTextWin;
    [SerializeField] private TMP_Text _pointsTextOver;

    public event Action OnBossDeth;
    public event Action OnStepEnd;

    public void SetBoss(int bossHP, int bossMaxHP, Sprite bossSprite)
    {
        _bossHPSlider.maxValue = bossMaxHP;
        _bossHPSlider.value = bossHP;

        _bossImage.sprite = bossSprite;
    }
    public void SetPoints(int points)
    {
        _pointsText.text = points.ToString();
    }
    public void SetBackground(Sprite background)
    {
        _background.sprite = background;
    }
    public void SpendStep()
    {
        if (_stepSlider.value > 1)
        {
            _stepSlider.value--;
        }
        else
        {
            OnStepEnd?.Invoke();
        }
    }

    public void GameWin()
    {
        foreach (var star in _stars)
        {
            star.isOn = false;
        }
        _panelWin.SetActive(true);
        _pointsTextWin.text = _pointsText.text;
    }
    public void AvtivateStars(int starsCount)
    {
        for (int i = 0; i < starsCount; i++)
        {
            _stars[i].isOn = true;
        }
    }

    public void GameOver()
    {
        _panelOver.SetActive(true);
        _pointsTextOver.text = _pointsText.text;
    }

    public void DeactivatePanels()
    {
        _panelOver.SetActive(false);
        _panelWin.SetActive(false);
        _panelMap.SetActive(false);
    }
    public void OpenMap()
    {
        DeactivatePanels();
        _panelMap.SetActive(true);
    }

    public void SpendBossHP(int damage)
    {
        if (_bossHPSlider.value - damage > 0)
        {
            _bossHPSlider.value -= damage;
        }
        else
        {
            _bossHPSlider.value = 0;
            OnBossDeth?.Invoke();
        }
    }

    public void ResetStepSlider()
    {
        _stepSlider.value = 32;
    }
}
