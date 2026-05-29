using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class WeatherSettingsUI : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] private Slider _rainIntensity;
    [SerializeField] private Slider _windAngle;
    [SerializeField] private Slider _windMagnitude;

    [Header("Input Mode Buttons")]
    [SerializeField] private Button _btnKeyboard;
    [SerializeField] private Button _btnJoystick;
    [SerializeField] private Button _btnUdp;

    [Inject] private readonly EnvironmentConfig _weather;
    [Inject] private readonly VehicleSpawner _spawner;

    private void Start()
    {
        _rainIntensity.minValue = 0f;
        _rainIntensity.maxValue = 1f;
        _rainIntensity.value = _weather._rainIntensity;

        _windAngle.minValue = 0f;
        _windAngle.maxValue = 360f;

        Vector3 currentWind = _weather._windVelocity;
        _windMagnitude.minValue = 0f;
        _windMagnitude.maxValue = 25f;
        _windMagnitude.value = new Vector2(currentWind.x, currentWind.z).magnitude;

        float angle = Mathf.Atan2(currentWind.z, currentWind.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        _windAngle.value = angle;

        BindUIEvents();
    }

    private void BindUIEvents()
    {
        _rainIntensity.onValueChanged.AddListener(OnRainChanged);
        _windAngle.onValueChanged.AddListener(OnWindChanged);
        _windMagnitude.onValueChanged.AddListener(OnWindChanged);

        if (_btnKeyboard != null) _btnKeyboard.onClick.AddListener(() => SetInputMode(InputMode.Keyboard));
        if (_btnJoystick != null) _btnJoystick.onClick.AddListener(() => SetInputMode(InputMode.Joystick));
        if (_btnUdp != null) _btnUdp.onClick.AddListener(() => SetInputMode(InputMode.Network));

        UpdateButtonInteractivity(InputMode.Keyboard);
    }

    private void SetInputMode(InputMode mode)
    {
        var vehicle = _spawner.ActiveVehicle;
        if (vehicle == null) return;

        vehicle.ChangeInputMode(mode);
        UpdateButtonInteractivity(mode);
        RemoveFocus();
    }

    private void UpdateButtonInteractivity(InputMode activeMode)
    {
        if (_btnKeyboard != null) _btnKeyboard.interactable = (activeMode != InputMode.Keyboard);
        if (_btnJoystick != null) _btnJoystick.interactable = (activeMode != InputMode.Joystick);
        if (_btnUdp != null) _btnUdp.interactable = (activeMode != InputMode.Network);
    }

    private void OnRainChanged(float value)
    {
        _weather._rainIntensity = value;
        RemoveFocus();
    }

    private void OnWindChanged(float value)
    {
        float angleRad = _windAngle.value * Mathf.Deg2Rad;
        float magnitude = _windMagnitude.value;

        float windX = Mathf.Cos(angleRad) * magnitude;
        float windZ = Mathf.Sin(angleRad) * magnitude;

        _weather._windVelocity = new Vector3(windX, 0f, windZ);

        RemoveFocus();
    }

    private void RemoveFocus()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }
}