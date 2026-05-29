using System;
using System.Collections.Generic;
using Zenject;
using UnityEngine;

public class HelicopterConfig : VehicleConfig
{
    [Serializable]
    public class PidSettings
    {
        public float _pGain;
        public float _iGain;
        public float _dGain;

        public PidSettings(float p, float i, float d)
        {
            _pGain = p;
            _iGain = i;
            _dGain = d;
        }
    }

    [Serializable]
    public class RotorConfig
    {
        public Transform _rotorGraphics;
        public Transform _rotorThrust;
        public int       _bladesCount = 4;
        public float _bladesChord = 0.53f;
        public float _RPM = 284;
        public float _RPMVisualRatio = 0.18f;
        public float _radius = 7.3f;
        public float _maxPitchAngle = 12f;
        public float _pitchChangeSpeed = 20f;
        public int   _direction = -1;
        public float _maxSwashplateTilt = 35f;
        public float _torqueFactor = 0.0833f;
    }

    [Serializable]
    public class FirmwareLimitsConfig
    {
        [Header("Autopilot / Limits Settings")]
        public float _altitudeVelocityDamping = 0.5f;
        public float _maxFlightPitchAngle = 25f;
        public float _maxFlightRollAngle = 25f;
        public float _maxYawAngularVelocity = 1.0f;

        [Header("PID Stabilization Settings (Data Only)")]
        public PidSettings _pitchPid = new PidSettings(0.8f, 0.0f, 0.02f);
        public PidSettings _rollPid = new PidSettings(0.8f, 0.0f, 0.02f);
        public PidSettings _yawPid = new PidSettings(1.0f, 1.0f, 0.01f);
        public PidSettings _altitudePid = new PidSettings(0.15f, 0.0f, 0.0f);
    }

    [Header("Base Settings")]
    public float _mass = 5000f;
    public Vector3 _inertiaTensor = new Vector3(8000f, 10000f, 5000f);
    public Transform _centerOfMass;
    public float _drag = 0.6f;
    public float _angularDrag = 4.5f;

    [Header("Aerodynamics")]
    public float _frontArea = 4.5f;
    public float _sideArea = 18.5f;
    public float _shapeDragCoeff = 0.45f;

    [Header("Rotors")]
    public RotorConfig _mainRotor;
    public RotorConfig _tailRotor;

    [Header("Firmware Limits")]
    public FirmwareLimitsConfig _firmwareLimits;

    [Header("Camera settings")]
    public ICameraTarget.ChaseCameraSettings _cameraSettings;


#if UNITY_EDITOR
    [EditorButton("ResetToDefault")]
    public bool _ResetToDefault;

    public void ResetToDefault()
    {
        _mass = 5000f;
        _inertiaTensor = new Vector3(8000f, 10000f, 5000f);
        _drag = 0.6f;
        _angularDrag = 4.5f;

        _frontArea = 4.5f;
        _sideArea = 18.5f;
        _shapeDragCoeff = 0.45f;

        _mainRotor._RPM = 284;
        _mainRotor._radius = 7.3f;
        _mainRotor._bladesCount = 4;
        _mainRotor._bladesChord = 0.53f;
        _mainRotor._maxPitchAngle = 12f;
        _mainRotor._pitchChangeSpeed = 20f;
        _mainRotor._RPMVisualRatio = 0.18f;
        _mainRotor._direction = -1;
        _mainRotor._maxSwashplateTilt = 35f;
        _mainRotor._torqueFactor = 0.0833f;

        _tailRotor._RPM = 1341;
        _tailRotor._radius = 1.4f;
        _tailRotor._bladesCount = 4;
        _tailRotor._bladesChord = 0.25f;
        _tailRotor._maxPitchAngle = 15f;
        _tailRotor._pitchChangeSpeed = 40f;
        _tailRotor._RPMVisualRatio = 0.18f;
        _tailRotor._direction = -1;
        _tailRotor._maxSwashplateTilt = 0f;
        _tailRotor._torqueFactor = 0f;


        _firmwareLimits._altitudeVelocityDamping = 0.5f;
        _firmwareLimits._maxFlightPitchAngle = 25f;
        _firmwareLimits._maxFlightRollAngle = 25f;
        _firmwareLimits._maxYawAngularVelocity = 1.0f;

        _firmwareLimits._pitchPid = new PidSettings(0.8f, 0.0f, 0.02f);
        _firmwareLimits._rollPid = new PidSettings(0.8f, 0.0f, 0.02f);
        _firmwareLimits._yawPid = new PidSettings(1.0f, 1.0f, 0.01f);
        _firmwareLimits._altitudePid = new PidSettings(0.15f, 0.0f, 0.0f);

        _cameraSettings._distance = 20.0f;
        _cameraSettings._height = 5.0f;
        _cameraSettings._lookAtOffsetEye = 1.0f;
    }

    private void OnValidate()
    {

    }
#endif

    public override bool IsConfigValid()
    {
        if (_mainRotor._rotorGraphics == null || _mainRotor._rotorThrust == null) return false;
        if (_tailRotor._rotorGraphics == null || _tailRotor._rotorThrust == null) return false;
        if (_centerOfMass == null) return false;
        return true;
    }

    public override void InstallBindings()
    {
        Container.Bind<HelicopterConfig>().FromInstance(this).AsSingle();

        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        Container.Bind<Rigidbody>().FromInstance(rb).AsSingle();

        var tailRotor = BuildRotor(_tailRotor);
        var mainRotor = BuildRotor(_mainRotor);

        Container.Bind<IVehicleFlyingRotor>().WithId("MainRotor").FromInstance(mainRotor).AsTransient();
        Container.Bind<IVehicleFlyingRotor>().WithId("TailRotor").FromInstance(tailRotor).AsTransient();

        Container.BindInterfacesAndSelfTo<InputSourceProvider>().AsSingle();
        Container.BindInterfacesAndSelfTo<HelicopterChassisSim>().AsSingle();
        Container.BindInterfacesAndSelfTo<HelicopterFirmware>().AsSingle().NonLazy();

        Container.BindInterfacesAndSelfTo<VehicleHelicopter>().AsSingle();
    }

    private HelicopterRotorSim BuildRotor(in RotorConfig rotor)
    {
        var physics = Container.InstantiateComponent<HelicopterRotorSim>(rotor._rotorThrust.gameObject);
        physics.Initialize(rotor);

        var visual = Container.InstantiateComponent<FlyingRotorVisual>(rotor._rotorGraphics.gameObject);
        visual.Initialize(physics);

        return physics;
    }
}