using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RocketSmoke : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private float smokeDensity = 30f; // Плотность дыма
    [SerializeField] private float fadeSpeed = 2f;     // Скорость рассеивания

    private ParticleSystem ps;
    private ParticleSystem.EmissionModule emissionModule;
    private float currentEmissionRate;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        emissionModule = ps.emission;
        currentEmissionRate = smokeDensity;

        ConfigureParticles();

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
    }

    void Update()
    {
        ControlSmoke();
    }

    private void ConfigureParticles()
    {
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 1f;
        main.startSpeed = 0.2f;
        main.startSize = 0.3f;
        main.gravityModifier = -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.1f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0.1f;
        velocity.y = 0.2f;
    }

    private void ControlSmoke()
    {
        bool isThrusting = Input.GetKey(KeyCode.Space); // Замените на ваш ввод

        float targetRate = isThrusting ? smokeDensity : 0f;
        currentEmissionRate = Mathf.Lerp(currentEmissionRate, targetRate, fadeSpeed * Time.deltaTime);
        emissionModule.rateOverTime = currentEmissionRate;
    }
}