using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public interface IVehicle
{
    public ICameraTarget CameraTarget {get;}
    public void ChangeInputMode(InputMode mode);

    public float GetEffectiveArea(Vector3 worldDynamicFlowDirection);
    public void AddExternalForce(Vector3 force);

    public Vector3 GlobalVelocity { get; }

    public interface IFactory
    {
        IVehicle Create(VehicleConfig prefab);
    }
}

public class VehicleHelicopter : IVehicle, IVehicleHelicopterTelemetry
{
    private readonly ICameraTarget _cameraTarget;
    private readonly IInputSourceProvider _inputProvider;
    private readonly IHelicopterFirmware _firmware;
    private readonly IVehicleChassis _chassis;

    public VehicleHelicopter(ICameraTarget cameraTarget,
        IHelicopterFirmware firmware,
        IInputSourceProvider inputProvider,
        IVehicleChassis chassis)
    {
        _cameraTarget = cameraTarget;
        _inputProvider = inputProvider;
        _firmware = firmware;
        _chassis = chassis;
    }

    public ICameraTarget CameraTarget => _cameraTarget;
    public bool          IsLanding => _firmware.IsLanding;
    public bool          IsHoldingAltitude => _firmware.IsHoldingAltitude;
    public float Pitch => _firmware.Pitch;
    public float Roll => -_firmware.Roll;
    public float Yaw => -_firmware.Yaw;
    public Vector2 Rotation => _firmware.Rotation;

    public Vector3 GlobalVelocity => _chassis.GlobalVelocity;
    public RotorDynamicData GetRotorDynamicData(int rotorID) => _chassis.GetRotorDynamicData(rotorID);

    public float GetEffectiveArea(Vector3 worldDynamicFlowDirection) => _chassis.GetEffectiveArea(worldDynamicFlowDirection);
    public void AddExternalForce(Vector3 force) =>  _chassis.AddExternalForce(force);


    public void ChangeInputMode(InputMode mode) => _inputProvider.SetMode(mode);
}
