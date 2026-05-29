using UnityEngine;
using Zenject;
using System;

public interface ICameraTarget
{
    [Serializable]
    public class ChaseCameraSettings
    {
        public float _distance = 20.0f;
        public float _height = 5.0f;
        public float _lookAtOffsetEye = 1.0f;
    }

    Vector3 Position { get; }
    Vector3 Forward { get; }
    ChaseCameraSettings CameraSettings { get; }
}


public class ChaseCameraController : ILateTickable, IDisposable
{
    private readonly Camera _camera;
    private readonly VehicleSpawner _spawner;
    private ICameraTarget _target;

    private ICameraTarget.ChaseCameraSettings _cameraSettings;

    /// smooth follow params //
    private float _positionSmooth = 5.0f;
    private float _rotationSmooth = 1.5f;

    private Vector3 _lookDirection;

    public ChaseCameraController(Camera camera, VehicleSpawner spawner)
    {
        _camera = camera;
        _spawner = spawner;
        _spawner.OnVehicleSpawned += OnVehicleSpawned;
        _cameraSettings = null;
    }

    public void Dispose()
    {
        if (_spawner != null)  _spawner.OnVehicleSpawned -= OnVehicleSpawned;
    }

    public void OnVehicleSpawned(IVehicle _vehicle)
    {
        _target = _vehicle.CameraTarget;
        if (_target != null)
        {
            Vector3 flatForward = _target.Forward;
            flatForward.y = 0;
            _lookDirection = flatForward.normalized;
            _cameraSettings = _target.CameraSettings;
        }
    }

    public void LateTick()
    {
        if (_target == null) return;

        Vector3 targetDirection = _target.Forward;
        targetDirection.y = 0;

        if (targetDirection.sqrMagnitude > 0.001f)
        {
            targetDirection.Normalize();
        }
        else
        {
            targetDirection = _lookDirection;
        }

        if (Vector3.Dot(_lookDirection, targetDirection) < -0.999f)
        {
            _lookDirection += Vector3.right * 0.01f;
        }

        float rotT = 1.0f - Mathf.Exp(-_rotationSmooth * Time.deltaTime);
        _lookDirection = Vector3.Slerp(_lookDirection, targetDirection, rotT);
        _lookDirection.y = 0;
        _lookDirection.Normalize();

        Vector3 targetPosition = _target.Position - (_lookDirection * _cameraSettings._distance) + (Vector3.up * _cameraSettings._height);

        float posT = 1.0f - Mathf.Exp(-_positionSmooth * Time.deltaTime);
        _camera.transform.position = Vector3.Lerp(_camera.transform.position, targetPosition, posT);

        Vector3 lookAtTarget = _target.Position + (Vector3.up * _cameraSettings._lookAtOffsetEye);
        _camera.transform.LookAt(lookAtTarget);
    }
}