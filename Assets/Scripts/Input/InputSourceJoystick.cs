using System;
using UnityEngine;
using Zenject;

public class InputSourceJoystick : IInputSource, ITickable
{
    private InputData _currentData;
    public InputData InputData => _currentData;

    private const float _deadZone = 0.1f;
    private const float _sqrDeadZone = _deadZone * _deadZone;

    private const float PedalsResetSpeed = 5f;
    private const float PedalsTargetSpeed = 2f;
    private const float CollectiveSpeed = 2f;

    public void Tick()
    {
        float dt = Time.deltaTime;

        /// 1. Cyclic step (roll/pitch) - left stick //
        float rollInput = -Input.GetAxis("Horizontal");
        float pitchInput = Input.GetAxis("Vertical");

        Vector2 cyclicStick = new Vector2(rollInput, pitchInput);
        if (cyclicStick.sqrMagnitude > _sqrDeadZone)
        {
            _currentData._cyclicInput.x = rollInput;
            _currentData._cyclicInput.y = pitchInput;
        }
        else
        {
            _currentData._cyclicInput = Vector2.zero;
        }

        /// 2. Collective step (up/down) - A/B or Cross/Circle + Smooth //
        float liftUp = Input.GetKey(KeyCode.JoystickButton0) ? 1f : 0f;
        float liftDown = Input.GetKey(KeyCode.JoystickButton1) ? 1f : 0f;
        float targetCollective = liftUp - liftDown;

        _currentData._collectiveInput = Mathf.MoveTowards(_currentData._collectiveInput, targetCollective, CollectiveSpeed * dt);

        /// 3. Pedals (yaw) + Smooth //
        float yawInput = Input.GetAxis("JoystickYaw");
        float targetPedals = 0f;

        if (Mathf.Abs(yawInput) > _deadZone)
        {
            targetPedals = yawInput;
        }

        float currentSpeed = (targetPedals == 0f) ? PedalsResetSpeed : PedalsTargetSpeed;
        _currentData._pedalsInput = Mathf.MoveTowards(_currentData._pedalsInput, targetPedals, currentSpeed * dt);
    }
}