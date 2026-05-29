using UnityEngine;
using TMPro;
using Zenject;

public class FlightDataUI : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private TextMeshProUGUI _txtAltHolding;
    [SerializeField] private TextMeshProUGUI _txtIsLanding;

    [Header("Flight Data")]
    [SerializeField] private TextMeshProUGUI _txtUpperRotorPitch;
    [SerializeField] private TextMeshProUGUI _txtLowerRotorPitch;
    [SerializeField] private TextMeshProUGUI _txtPitch;
    [SerializeField] private TextMeshProUGUI _txtRoll;

    [Header("Settings")]
    [SerializeField] private Color _stableColor = Color.black;
    [SerializeField] private Color _activeColor = Color.green;

    [Inject] private readonly VehicleSpawner _spawner;

    private void Start()
    {
        UpdateIndicatorStatus(_txtAltHolding, false);
        UpdateIndicatorStatus(_txtIsLanding, false);

        SetFieldsDefaultColor();
    }

    private void Update()
    {
        IVehicleHelicopterTelemetry vehicle = _spawner.ActiveVehicle as IVehicleHelicopterTelemetry;
        if (vehicle == null) return;

        UpdateIndicatorStatus(_txtAltHolding, vehicle.IsHoldingAltitude);
        UpdateIndicatorStatus(_txtIsLanding, vehicle.IsLanding);

        _txtPitch.text = $"{vehicle.Pitch:F1}°";
        _txtRoll.text = $"{-vehicle.Roll:F1}°";
        _txtUpperRotorPitch.text = $"{vehicle.GetRotorDynamicData(0)._currentBladesPitch:F1}°";
        _txtLowerRotorPitch.text = $"{vehicle.GetRotorDynamicData(1)._currentBladesPitch:F1}°";
    }

    private void UpdateIndicatorStatus(TextMeshProUGUI textTarget, bool isActive)
    {
        if (textTarget == null) return;
        textTarget.color = isActive ? _activeColor : _stableColor;
    }

    private void SetFieldsDefaultColor()
    {
        if (_txtPitch != null) _txtPitch.color = _stableColor;
        if (_txtRoll != null) _txtRoll.color = _stableColor;
        if (_txtUpperRotorPitch != null) _txtUpperRotorPitch.color = _stableColor;
        if (_txtLowerRotorPitch != null) _txtLowerRotorPitch.color = _stableColor;
    }
}