using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public interface IVehicleFlyingRotor
{
    public struct RotorForceResult
    {
        public Vector3 _force;
        public Vector3 _torque;
    }

    public RotorDynamicData DynamicData { get; }
    void ApplyMovement(float bladesPitchDegree, Vector2 angle);
    public float GetCollectiveForVerticalThrust(float targetVerticalThrust);
    RotorForceResult CalculateCombinedForces();

    Vector3 Position { get; }

    float VisualRPM { get; }
    Vector3 MainAxis { get; }

}

public class RotorDynamicData
{
    public float _currentBladesPitch;
    public Vector2 _cyclicInput;
    
    public RotorDynamicData(float initialPitch, Vector2 initialCyclic)
    {
        _currentBladesPitch = initialPitch;
        _cyclicInput = initialCyclic;
    }
    public RotorDynamicData() { }
}

public class HelicopterRotorSim : MonoBehaviour, IVehicleFlyingRotor
{
    private const float _thrustReduction = 4.0f;
    private const float _degreeToLiftCoef = 0.11f;

    private HelicopterConfig.RotorConfig _config;
    [Inject] private EnvironmentConfig _weather;

    private float _rotorKineticalFactor;
    private RotorDynamicData _rotorDynamicData = new RotorDynamicData();

    public void Initialize(HelicopterConfig.RotorConfig config)
    {
        _config = config;
        RecalculateKineticFactor();
    }

    public RotorDynamicData DynamicData => _rotorDynamicData;

    private void RecalculateKineticFactor()
    {
        float RPS = _config._RPM / 60f;
        float effectiveSpeed = 2f * Mathf.PI * RPS * (_config._radius * 0.75f); // V = 2 * Pi * RPS * R
        float _totalBladesArea = _config._radius * _config._bladesChord * _config._bladesCount;
        _rotorKineticalFactor = 0.5f * _totalBladesArea * effectiveSpeed * effectiveSpeed / _thrustReduction;
    }

    public void ApplyMovement(float _bladesPitchDegree, Vector2 _angle)
    {
        _rotorDynamicData._currentBladesPitch = _bladesPitchDegree;
        _rotorDynamicData._cyclicInput = _angle; /// x = roll, y = pitch //
    }

    public IVehicleFlyingRotor.RotorForceResult CalculateCombinedForces()
    {
        IVehicleFlyingRotor.RotorForceResult result = new IVehicleFlyingRotor.RotorForceResult();
        if (_config == null) return result;

        /// FORCE //
        float thrustMagnitude = _rotorKineticalFactor * _weather.CurrentAirDensity * (_rotorDynamicData._currentBladesPitch * _degreeToLiftCoef);
        /// Swashplate sim //
        Vector3 thrustDirection = MainAxis;
        if (_rotorDynamicData._cyclicInput != Vector2.zero && _config._maxSwashplateTilt != 0.0f)
        {
            float targetPitchAngle = _rotorDynamicData._cyclicInput.y * _config._maxSwashplateTilt;
            float targetRollAngle = _rotorDynamicData._cyclicInput.x * _config._maxSwashplateTilt;
            Quaternion swashplateRotation = Quaternion.Euler(targetPitchAngle, 0f, -targetRollAngle);
            thrustDirection = _config._rotorThrust.rotation * swashplateRotation * Vector3.up;
        }
        result._force = thrustDirection * thrustMagnitude; /// our direction is already normalized //

        /// TORQUE //
        float torqueMagnitude = thrustMagnitude * _config._torqueFactor * (-_config._radius * 0.75f);
        result._torque = MainAxis * (torqueMagnitude * _config._direction);

        return result;
    }

    public float GetCollectiveForVerticalThrust(float targetVerticalThrust)
    {
        if (_config == null) return 0f;

        float cosAlpha = Vector3.Dot(MainAxis, Vector3.up);
        cosAlpha = Mathf.Max(cosAlpha, 0.1f); /// null prot, if we are lying on the side //
        float thrustMagnitude = targetVerticalThrust / cosAlpha;

        float constantPart = _rotorKineticalFactor * _weather.CurrentAirDensity;
        if (constantPart <= 0.001f) return 0f;
        return thrustMagnitude / (constantPart * _degreeToLiftCoef);
    }

    public Vector3 Position => transform.position;
    public float VisualRPM => _config._RPM * _config._RPMVisualRatio * _config._direction;
    public Vector3 MainAxis => _config._rotorThrust.up;
}