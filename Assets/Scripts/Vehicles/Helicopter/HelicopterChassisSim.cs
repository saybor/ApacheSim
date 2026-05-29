//#define DEBUG_LOGS
using UnityEngine;
using Zenject;
using System.Runtime.CompilerServices;

public interface IVehicleChassis : ICameraTarget
{
    void ApplyMovement(float pitchCmd, float rollCmd, float yawCmd, float collectiveCmd);
    float GetHoverCollective();

    Vector3 NormalizedLocalEulerAngles { get; }
    Vector3 LocalAngularVelocity { get; }

    Vector3 GlobalVelocity { get; }
    Vector3 LocalVelocity { get; }

    public float GetEffectiveArea(Vector3 worldDynamicFlowDirection);
    public void AddExternalForce(Vector3 force);

    bool IsGrounded { get; }
    public RotorDynamicData GetRotorDynamicData(int rotorID);
}

public class HelicopterChassisSim : IVehicleChassis, IFixedTickable
{
    private const float _groundCheckDistance = 2.5f;

    private readonly Rigidbody _rb;
    private readonly HelicopterConfig _config;
    private readonly IVehicleFlyingRotor _mainRotor;
    private readonly IVehicleFlyingRotor _tailRotor;

    private float _rollCmd;
    private float _pitchCmd;
    private float _yawCmd;
    private float _collectiveCmd;

    public HelicopterChassisSim(
            HelicopterConfig config,
            Rigidbody rb,
            [Inject(Id = "MainRotor")] IVehicleFlyingRotor mainRotor,
            [Inject(Id = "TailRotor")] IVehicleFlyingRotor tailRotor)
    {
        _config = config;
        _rb = rb;
        _mainRotor = mainRotor;
        _tailRotor = tailRotor;

        _rb.drag = _config._drag;
        _rb.angularDrag = _config._angularDrag;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        _rb.automaticCenterOfMass = false;

        _rb.mass = _config._mass;
        _rb.centerOfMass = _config._centerOfMass.localPosition;
        _rb.automaticInertiaTensor = false;
        _rb.inertiaTensor = _config._inertiaTensor;
        _rb.inertiaTensorRotation = Quaternion.identity;
    }

    public void ApplyMovement(float pitchCmd, float rollCmd, float yawCmd, float collectiveCmd)
    {
        _pitchCmd = pitchCmd;
        _rollCmd = rollCmd;
        _yawCmd = yawCmd;

        _collectiveCmd = collectiveCmd;
    }

    public float GetHoverCollective()
    {
        float neededPitchDegree = _mainRotor.GetCollectiveForVerticalThrust(_rb.mass * Mathf.Abs(Physics.gravity.y));
        float hoverCollectiveCmd = neededPitchDegree / _config._mainRotor._maxPitchAngle;
        return Mathf.Clamp01(hoverCollectiveCmd);
    }

    public void FixedTick()
    {
        Vector2 mainCyclicAngle = new Vector2(_rollCmd, _pitchCmd);

        float mainCollectivePitch = _collectiveCmd * _config._mainRotor._maxPitchAngle;
        _mainRotor.ApplyMovement(mainCollectivePitch, mainCyclicAngle);

        float tailCollectivePitch = _yawCmd * _config._tailRotor._maxPitchAngle;
        _tailRotor.ApplyMovement(tailCollectivePitch, Vector2.zero);

        /// main rotor //
        IVehicleFlyingRotor.RotorForceResult rotorResultMain = _mainRotor.CalculateCombinedForces();
        _rb.AddForceAtPosition(rotorResultMain._force, _mainRotor.Position, ForceMode.Force);
        _rb.AddTorque(rotorResultMain._torque, ForceMode.Force);

        /// tail rotor //
        IVehicleFlyingRotor.RotorForceResult rotorResultTail = _tailRotor.CalculateCombinedForces();
        _rb.AddForceAtPosition(rotorResultTail._force, _tailRotor.Position, ForceMode.Force);
        _rb.AddTorque(rotorResultTail._torque, ForceMode.Force);

#if DEBUG_LOGS
        Vector3 localForceMain = _rb.transform.InverseTransformDirection(rotorResultMain._force);
        Vector3 localTorqueMain = _rb.transform.InverseTransformDirection(rotorResultMain._torque);
        Vector3 localForceTail = _rb.transform.InverseTransformDirection(rotorResultTail._force);
        Vector3 localTorqueTail = _rb.transform.InverseTransformDirection(rotorResultTail._torque);

        Debug.Log($"<b>[CMD]</b>: RollCmd: {_rollCmd:F2}, PitchCmd: {_pitchCmd:F2}, YawCmd: {_yawCmd:F2}, CollCmd: {_collectiveCmd:F2}\n" +
            $"<b>[MAIN]</b>: Pitch: {mainCollectivePitch:F1}°, Cyclic: {mainCyclicAngle} | FORCE: {localForceMain} (Mag: {rotorResultMain._force.magnitude:F1} N) | TORQUE: {localTorqueMain} (Mag: {rotorResultMain._torque.magnitude:F1} Nm)\n" +
            $"<b>[TAIL]</b>: Pitch: {tailCollectivePitch:F1}° | FORCE: {localForceTail} (Mag: {rotorResultTail._force.magnitude:F1} N) | TORQUE: {localTorqueTail} (Mag: {rotorResultTail._torque.magnitude:F1} Nm)");
#endif
    }

    public Vector3 LocalAngularVelocity => _rb.transform.InverseTransformDirection(_rb.angularVelocity);
    public Vector3 GlobalVelocity => _rb.velocity;
    public Vector3 LocalVelocity => _rb.transform.InverseTransformDirection(GlobalVelocity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float NormalizeAngle(float angle) => (angle %= 360f) > 180f ? angle - 360f : (angle < -180f ? angle + 360f : angle);

    public Vector3 NormalizedLocalEulerAngles
    {
        get
        {
            Vector3 rawAngles = _rb.transform.localEulerAngles;
            return new Vector3(
                NormalizeAngle(rawAngles.x),
                NormalizeAngle(rawAngles.y),
                NormalizeAngle(rawAngles.z)
            );
        }
    }

    public bool IsGrounded
    {
        get
        {
            Vector3 direction = Vector3.down;
            return Physics.Raycast(_rb.transform.position, direction, _groundCheckDistance);
        }
    }

    public Vector3 Position => _rb.transform.position;
    public Vector3 Forward => _rb.transform.forward;

    public ICameraTarget.ChaseCameraSettings CameraSettings => _config._cameraSettings;

    public RotorDynamicData GetRotorDynamicData(int rotorID)
    {
        if (rotorID == 0) return _mainRotor.DynamicData;
        if (rotorID == 1) return _tailRotor.DynamicData;
        return null;
    }

    public void AddExternalForce(Vector3 force)
    {
        if(!IsGrounded)
        {
            _rb.AddForce(force, ForceMode.Force);
        }
    }

    public float GetEffectiveArea(Vector3 worldDynamicFlowDirection)
    {
        if (worldDynamicFlowDirection == Vector3.zero) return _config._frontArea;
        Vector3 flowDir = worldDynamicFlowDirection.normalized;
        float sideWeight = Mathf.Abs(Vector3.Dot(_rb.transform.right, flowDir));
        float blendedArea = Mathf.Lerp(_config._frontArea, _config._sideArea, sideWeight);
        return blendedArea * _config._shapeDragCoeff;
    }
}