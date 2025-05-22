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
    [SerializeField] private GameObject _loadScreen;

    [SerializeField] private Toggle[] _stars;
    [SerializeField] private TMP_Text _pointsText;

    [SerializeField] private TMP_Text _pointsTextWin;
    [SerializeField] private TMP_Text _pointsTextOver;
    [SerializeField] private TMP_Text _stepsText;

    public GameManager gameManager;

    public event Action OnBossDeath;
    public event Action OnStepEnd;

    private bool _settingsIsOpen;
    private bool _sfxIsOpen;
    private bool _musicIsOpen;

    private int _levelNumber;

    private string _mapOrLevel;



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
            _stepsText.text = _stepSlider.value.ToString();
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
        _panelWin.GetComponent<Animator>().SetTrigger("Appear");
        _pointsTextWin.text = _pointsText.text;

        gameManager.ActivateStars();
    }
    public void ActivateStars(int starsCount)
    {
        for (int i = 0; i < starsCount; i++)
        {
            _stars[i].isOn = true;
        }
    }

    public void GameOver()
    {
        _panelOver.SetActive(true);
        _panelOver.GetComponent<Animator>().SetTrigger("Appear");
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

    public void SpendBossHP(int damage, bool isCritical = false)
    {
        if (_bossHPSlider.value - damage > 0)
        {
            _bossHPSlider.value -= damage;
        }
        else
        {
            _bossHPSlider.value = 0;
            OnBossDeath?.Invoke();
        }

        if (isCritical)
        {
            _bossImage.GetComponentInParent<FlashEffect>().StartFlash(Color.red);
        }
        else
        {
            _bossImage.GetComponentInParent<FlashEffect>().StartFlash(Color.gray);
        }
    }

    public void ResetStepSlider()
    {
        _stepsText.text = "50";
        _stepSlider.value = 50;

    }

    public void SetLevel(int levelNumber, string mapOrLevel = "level")
    {
        GetComponent<Animator>().SetTrigger("StartLoading");
        _levelNumber = levelNumber;
        _mapOrLevel = mapOrLevel;
    }

    public void ChangeScene()
    {
        switch (_mapOrLevel)
        {
            case "map":
                OpenMap();
                break;
            case "level":
                gameManager.SetLevel(_levelNumber);
                break;
            default:
                throw new NotImplementedException($"Неизвестный вариант ответа {_mapOrLevel} при вызове ChangeScene()");
        }

        
    }
}
