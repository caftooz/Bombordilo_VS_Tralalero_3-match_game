using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("References")]
    public Button settingsButton;
    public Animator settingsPanelAnimator;
    public Animator soundSliderAnimator;
    public Animator musicSliderAnimator;
    public Animator settingsButtonAnimator; // Аниматор кнопки настроек

    [Header("Animation Timing")]
    public float sliderCloseDelay = 0.2f; // Задержка перед закрытием панели

    private bool isSettingsOpen = false;
    private bool isSoundSliderOpen = false;
    private bool isMusicSliderOpen = false;

    private void Start()
    {
        settingsButton.onClick.AddListener(ToggleSettings);
    }

    public void ToggleSettings()
    {
        if (isSettingsOpen)
        {
            // Если есть открытые слайдеры, закрываем их перед закрытием панели
            if (isSoundSliderOpen || isMusicSliderOpen)
            {
                CloseAllSliders();
                Invoke("CloseSettingsPanel", sliderCloseDelay);
            }
            else
            {
                CloseSettingsPanel();
            }
        }
        else
        {
            OpenSettingsPanel();
        }
    }

    private void OpenSettingsPanel()
    {
        isSettingsOpen = true;
        settingsPanelAnimator.SetTrigger("Open");
        settingsButtonAnimator.SetBool("IsOpen", true); // Запускаем анимацию кнопки
        
    }

    private void CloseSettingsPanel()
    {
        isSettingsOpen = false;
        settingsPanelAnimator.SetTrigger("Close");
        settingsButtonAnimator.SetBool("IsOpen", false); // Возвращаем кнопку в исходное состояние
    }


    public void Disable()
    {
        if (isMusicSliderOpen)
        {
            musicSliderAnimator.gameObject.SetActive(false);
        }
        else
        {
            soundSliderAnimator.gameObject.SetActive(false);
        }
    }

    private void CloseAllSliders()
    {
        if (isSoundSliderOpen)
        {
            soundSliderAnimator.SetTrigger("Close");
            isSoundSliderOpen = false;
        }

        if (isMusicSliderOpen)
        {
            musicSliderAnimator.SetTrigger("Close");
            isMusicSliderOpen = false;
        }
    }

    // Вызывается при нажатии на кнопку звука
    public void ToggleSoundSlider()
    {
        isSoundSliderOpen = !isSoundSliderOpen;
        soundSliderAnimator.SetTrigger(isSoundSliderOpen ? "Open" : "Close");

        // Закрываем другой слайдер, если он открыт
        if (isSoundSliderOpen && isMusicSliderOpen)
        {
            musicSliderAnimator.SetTrigger("Close");
            isMusicSliderOpen = false;
        }
    }

    // Вызывается при нажатии на кнопку музыки
    public void ToggleMusicSlider()
    {
        isMusicSliderOpen = !isMusicSliderOpen;
        musicSliderAnimator.SetTrigger(isMusicSliderOpen ? "Open" : "Close");

        // Закрываем другой слайдер, если он открыт
        if (isMusicSliderOpen && isSoundSliderOpen)
        {
            soundSliderAnimator.SetTrigger("Close");
            isSoundSliderOpen = false;
        }
    }
}