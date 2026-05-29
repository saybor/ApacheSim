//#define DEBUG_LOGS
using UnityEngine;
using Zenject;

public interface IVehicleFirmware
{

}

public interface IVehicleHelicopterTelemetry
{
    public bool IsLanding { get; }
    public bool IsHoldingAltitude { get; }
    public float Pitch { get; }
    public float Roll { get; }
    public float Yaw { get; }
    public Vector2 Rotation { get; }
    public RotorDynamicData GetRotorDynamicData(int rotorID);
}

public interface IHelicopterFirmware : IVehicleHelicopterTelemetry
{

}

public class HelicopterFirmware : IVehicleFirmware, IHelicopterFirmware, IFixedTickable
{
    private const float _altitudeMin = 0.35f;
    private const float _altitudeMax = 0.95f;

    private readonly IInputSourceProvider _input;
    private readonly IVehicleChassis _chassis;
    private readonly HelicopterConfig _config;

    private readonly PidController _pitchPid;
    private readonly PidController _rollPid;
    private readonly PidController _yawPid;
    private readonly PidController _altitudePid;

    private float _targetCollective = 0f;
    private bool _isHoldingAltitude = false;
    private bool _isLanding = false;
    private float _targetAltitude = 0f;
    private float _altitudeVelocityDamping = 0.5f;

    public HelicopterFirmware(
        IInputSourceProvider input,
        IVehicleChassis chassis,
        HelicopterConfig config)
    {
        _input = input;
        _chassis = chassis;
        _config = config;

        _altitudeVelocityDamping = _config._firmwareLimits._altitudeVelocityDamping;

        _pitchPid = new PidController(_config._firmwareLimits._pitchPid);
        _rollPid = new PidController(_config._firmwareLimits._rollPid);
        _yawPid = new PidController(_config._firmwareLimits._yawPid);
        _altitudePid = new PidController(_config._firmwareLimits._altitudePid);
    }

    public void FixedTick()
    {
        InputData input = _input.InputData;

        float dt = Time.fixedDeltaTime;

        /// PID controls //
        Vector3 currentEuler = _chassis.NormalizedLocalEulerAngles;
        Vector3 currentAngularVel = _chassis.LocalAngularVelocity;

        float targetPitch = input._cyclicInput.y * _config._firmwareLimits._maxFlightPitchAngle;
        float targetRoll = input._cyclicInput.x * _config._firmwareLimits._maxFlightRollAngle;
        float targetYawVelocity = input._pedalsInput * _config._firmwareLimits._maxYawAngularVelocity;

        /// calc and normalize angle errors //
        float pitchError = Mathf.DeltaAngle(currentEuler.x, targetPitch) / _config._firmwareLimits._maxFlightPitchAngle;
        float rollError = -Mathf.DeltaAngle(currentEuler.z, targetRoll) / _config._firmwareLimits._maxFlightRollAngle;
        float yawSpeedError = -(targetYawVelocity - currentAngularVel.y) / _config._firmwareLimits._maxYawAngularVelocity;

        float pitchOutput = _pitchPid.Update(pitchError, dt);
        float rollOutput = _rollPid.Update(rollError, dt);
        float yawOutput = _yawPid.Update(yawSpeedError, dt);

        /// Feed-forward torque compensation when changing collective //
        float yawFeedForward = _targetCollective * 0.5f;
        yawOutput += yawFeedForward;

        /// Alt hold //
        float currentAltitude = _chassis.Position.y;

        if (input._collectiveInput > 0.1f)
        {
            _isHoldingAltitude = false;
            _targetCollective += _altitudeVelocityDamping * dt;
        }
        else if (input._collectiveInput < -0.1f)
        {
            _isHoldingAltitude = false;
            _targetCollective -= _altitudeVelocityDamping * dt;
        }
        else
        {
            if (!_isHoldingAltitude)
            {
                _isHoldingAltitude = true;
                _targetAltitude = currentAltitude;
                _altitudePid.Reset();
            }

            float altitudeError = _targetAltitude - currentAltitude;
            float altitudeOutput = _altitudePid.Update(altitudeError, dt);
            float hoverCollective = _chassis.GetHoverCollective();

            float verticalVelocity = _chassis.GlobalVelocity.y;
            _targetCollective = hoverCollective + altitudeOutput - (verticalVelocity * 0.5f);
        }

        /// landing //
        float minCollective = _altitudeMin;
        if (_chassis.IsGrounded && !(input._collectiveInput > 0.1f))
        {
            _isLanding = true;
            minCollective = 0f;
            _targetCollective = 0f;
        }
        else
        {
            _isLanding = false;
        }
        _targetCollective = Mathf.Clamp(_targetCollective, minCollective, _altitudeMax);

#if DEBUG_LOGS
        if (Mathf.Abs(input._cyclicInput.y) > 0.1f)
            Debug.Log($"<b>[PITCH DEBUG]</b>: In:{input._cyclicInput.y:F2} | AngleErr:{pitchError:F2} | Out:{pitchOutput:F2}");

        if (Mathf.Abs(input._cyclicInput.x) > 0.1f)
            Debug.Log($"<b>[ROLL DEBUG]</b>: In:{input._cyclicInput.x:F2} | AngleErr:{rollError:F2} | Out:{rollOutput:F2}");
#endif

        pitchOutput = Mathf.Clamp(pitchOutput, -1f, 1f);
        rollOutput = Mathf.Clamp(rollOutput, -1f, 1f);
        yawOutput = Mathf.Clamp(yawOutput, -1f, 1f);

        _chassis.ApplyMovement(pitchOutput, rollOutput, yawOutput, _targetCollective);
    }

    public bool IsHoldingAltitude => _isHoldingAltitude && !_isLanding;
    public bool IsLanding => _isLanding;
    public float Pitch => _chassis.NormalizedLocalEulerAngles.x;
    public float Roll => -_chassis.NormalizedLocalEulerAngles.z;
    public float Yaw => -_chassis.NormalizedLocalEulerAngles.y;
    public Vector2 Rotation => _chassis.NormalizedLocalEulerAngles;
    public RotorDynamicData GetRotorDynamicData(int rotorID) => _chassis.GetRotorDynamicData(rotorID);

}