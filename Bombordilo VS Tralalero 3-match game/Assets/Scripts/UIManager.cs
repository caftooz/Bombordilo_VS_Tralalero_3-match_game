using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Slider _bossHPSlider;
    [SerializeField] private Image _bossImage;

    [SerializeField] private SpriteRenderer _background;

    [SerializeField] private Slider _stepSlider;

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
        if (_stepSlider.value > 1)
        {
            _stepSlider.value--;
        }
        else
        {
            OnStepEnd?.Invoke();
        }
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
