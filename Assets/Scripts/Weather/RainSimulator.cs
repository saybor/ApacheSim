using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Zenject;

public class RainSimulator : MonoBehaviour
{
    [Inject] private EnvironmentConfig _config;
    [Inject] private VehicleSpawner _spawner;

    [Header("Max Data-Driven Values")]
    [SerializeField] private int _maxEmissionRate = 5000;
    [SerializeField] private float _maxVerticalSpeed = 25f;

    [Header("URP Resources")]
    [SerializeField] private Material _rainMaterialPrefab;

    private ParticleSystem _ps;
    private ParticleSystem.EmissionModule _emission;
    private ParticleSystem.VelocityOverLifetimeModule _velocity;
    private ParticleSystem.MainModule _main;

    private Volume _rainVolume;
    private ColorAdjustments _colorAdjustments;

    /// smoothing //
    private float _currentIntensity;

    private void Awake()
    {
        _ps = gameObject.AddComponent<ParticleSystem>();

        var renderer = GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 25f;
        renderer.velocityScale = 0f;

        if (_rainMaterialPrefab != null)
        {
            renderer.material = new Material(_rainMaterialPrefab);
            renderer.material.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.8f, 0.05f));
        }
        else
        {
            Debug.LogError($"<b>[RainSimulator]</b>: _rainMaterialPrefab is missing");
            renderer.material = new Material(Shader.Find("Hidden/Universal Render Pipeline/FallbackError"));
            renderer.material.color = new Color(0.7f, 0.7f, 0.8f, 0.05f);
        }

        /// add eff volume to gradient lighting //
        _rainVolume = gameObject.AddComponent<Volume>();
        _rainVolume.isGlobal = true;
        _rainVolume.priority = 100;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _colorAdjustments = profile.Add<ColorAdjustments>(true);
        _colorAdjustments.active = true;
        _colorAdjustments.postExposure.overrideState = true;
        _rainVolume.profile = profile;

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
        RenderSettings.fogDensity = Mathf.Lerp(0.005f, 0.04f, intensity);

        if (_colorAdjustments != null)
        {
            float targetExposure = Mathf.Lerp(0f, -0.8f, intensity);
            _colorAdjustments.postExposure.value = targetExposure;
        }
    }

    private void OnDestroy()
    {
        if (_rainVolume != null && _rainVolume.profile != null)
        {
            Destroy(_rainVolume.profile);
        }
    }
}