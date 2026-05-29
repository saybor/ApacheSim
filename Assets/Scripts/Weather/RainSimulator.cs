using UnityEngine;
using Zenject;

public class RainSimulator : MonoBehaviour
{
    [Inject] private EnvironmentConfig _config;
    [Inject] private VehicleSpawner _spawner;

    [Header("Max Data-Driven Values")]
    [SerializeField] private int _maxEmissionRate = 5000;
    [SerializeField] private float _maxVerticalSpeed = 25f;

    private ParticleSystem _ps;
    private ParticleSystem.EmissionModule _emission;
    private ParticleSystem.VelocityOverLifetimeModule _velocity;
    private ParticleSystem.MainModule _main;

    /// smoothing //
    private float _currentIntensity;

    private void Awake()
    {
        _ps = gameObject.AddComponent<ParticleSystem>();

        var renderer = GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 25f;
        renderer.velocityScale = 0f;

        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = new Color(0.7f, 0.7f, 0.8f, 0.05f);

        _main = _ps.main;
        _emission = _ps.emission;
        _velocity = _ps.velocityOverLifetime;

        _main.loop = true;
        _main.playOnAwake = true;
        _main.maxParticles = 10000;
        _main.prewarm = true;
        _main.startLifetime = 1.0f;

        _main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);

        _main.simulationSpace = ParticleSystemSimulationSpace.World;

        var shape = _ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(60, 10, 60);
        shape.position = new Vector3(0, 15, 0);

        _velocity.enabled = true;
        _ps.Play();
    }

    private void Update()
    {
        if (_config == null) return;

        _currentIntensity = Mathf.MoveTowards(_currentIntensity, _config._rainIntensity, 0.5f * Time.deltaTime);
        _emission.rateOverTime = _currentIntensity * _maxEmissionRate;

        float currentFallSpeed = Mathf.Lerp(10f, _maxVerticalSpeed, _currentIntensity);
        Vector3 totalVelocity = _config._windVelocity + new Vector3(0, -currentFallSpeed, 0);

        _velocity.x = totalVelocity.x;
        _velocity.y = totalVelocity.y;
        _velocity.z = totalVelocity.z;

        UpdateEnvironmentAtmosphere(_currentIntensity);

        /// synchronize with our current position //
        IVehicle vehicle = _spawner.ActiveVehicle;
        if (vehicle != null)
        {
            Vector3 targetPos = vehicle.CameraTarget.Position;
            transform.position = new Vector3(targetPos.x, 0.0f, targetPos.z);
        }
    }

    private void UpdateEnvironmentAtmosphere(float intensity)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = Mathf.Lerp(0.005f, 0.045f, intensity);
        float currentLightStyle = Mathf.Lerp(1.0f, 0.1f, intensity);
        RenderSettings.ambientLight = new Color(currentLightStyle, currentLightStyle, currentLightStyle, 1f);
    }
}