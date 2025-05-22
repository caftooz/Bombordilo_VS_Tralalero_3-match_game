using UnityEngine;

public class FPSCap : MonoBehaviour
{
    [SerializeField] private int targetFPS = 60; // �������� FPS

    private void Start()
    {
        Application.targetFrameRate = targetFPS;
    }
}