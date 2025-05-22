using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("References")]
    public Button settingsButton;
    public Animator settingsPanelAnimator;
    public Animator soundSliderAnimator;
    public Animator musicSliderAnimator;
    public Animator settingsButtonAnimator; // �������� ������ ��������

    [Header("Animation Timing")]
    public float sliderCloseDelay = 0.2f; // �������� ����� ��������� ������

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
            // ���� ���� �������� ��������, ��������� �� ����� ��������� ������
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
        settingsButtonAnimator.SetBool("IsOpen", true); // ��������� �������� ������
        
    }

    private void CloseSettingsPanel()
    {
        isSettingsOpen = false;
        settingsPanelAnimator.SetTrigger("Close");
        settingsButtonAnimator.SetBool("IsOpen", false); // ���������� ������ � �������� ���������
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

    // ���������� ��� ������� �� ������ �����
    public void ToggleSoundSlider()
    {
        isSoundSliderOpen = !isSoundSliderOpen;
        soundSliderAnimator.SetTrigger(isSoundSliderOpen ? "Open" : "Close");

        // ��������� ������ �������, ���� �� ������
        if (isSoundSliderOpen && isMusicSliderOpen)
        {
            musicSliderAnimator.SetTrigger("Close");
            isMusicSliderOpen = false;
        }
    }

    // ���������� ��� ������� �� ������ ������
    public void ToggleMusicSlider()
    {
        isMusicSliderOpen = !isMusicSliderOpen;
        musicSliderAnimator.SetTrigger(isMusicSliderOpen ? "Open" : "Close");

        // ��������� ������ �������, ���� �� ������
        if (isMusicSliderOpen && isSoundSliderOpen)
        {
            soundSliderAnimator.SetTrigger("Close");
            isSoundSliderOpen = false;
        }
    }
}