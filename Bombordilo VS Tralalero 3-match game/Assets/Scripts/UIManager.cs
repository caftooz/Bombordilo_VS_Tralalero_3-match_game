using System;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Slider _bossHPSlider;
    [SerializeField] private Image _bossImage;

    [SerializeField] private SpriteRenderer _background;

    [SerializeField] private Slider _stepSlider;

    [SerializeField] private GameObject _panelWin;
    [SerializeField] private GameObject _panelOver;
    [SerializeField] private GameObject _panelMap;

    public event Action OnBossDeth;
    public event Action OnStepEnd;

    public void SetBoss(int bossHP, int bossMaxHP, Sprite bossSprite)
    {
        _bossHPSlider.maxValue = bossMaxHP;
        _bossHPSlider.value = bossHP;

        _bossImage.sprite = bossSprite;
    }
    public void SetBackground(Sprite background)
    {
        _background.sprite = background;
    }
    public void SpendStep()
    {
        if (_stepSlider.value > 2)
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
        _panelWin.SetActive(true);
    }
    public void GameOver()
    {
        _panelOver.SetActive(true);
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
