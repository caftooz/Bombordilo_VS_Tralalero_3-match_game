using UnityEngine;

public class FPSCap : MonoBehaviour
{
    [SerializeField] private int targetFPS = 60; // Желаемый FPS

    private void Start()
    {
        Application.targetFrameRate = targetFPS;
    }
}