using System;
using UnityEngine;
using Zenject;

public class InputSourceKeyboard : IInputSource, ITickable
{
    private InputData _currentData;
    public InputData InputData => _currentData;

    /// smoothing //
    private const float PedalsResetSpeed = 5f;
    private const float PedalsTargetSpeed = 2f;
    private const float CollectiveSpeed = 2f;

    public void Tick()
    {
        float dt = Time.deltaTime;

        /// 1. Cyclic step (roll/pitch) //
        _currentData._cyclicInput.x = -Input.GetAxis("Horizontal");
        _currentData._cyclicInput.y = Input.GetAxis("Vertical");

        /// 2. Collective step (up/down) //
        float targetCollective = 0f;
        if (Input.GetKey(KeyCode.Space)) targetCollective += 1f;
        if (Input.GetKey(KeyCode.LeftControl)) targetCollective -= 1f;

        _currentData._collectiveInput = Mathf.MoveTowards(_currentData._collectiveInput, targetCollective, CollectiveSpeed * dt);

        /// 3. Pedals (yaw) + smooth //
        float targetPedals = 0f;
        if (Input.GetKey(KeyCode.E)) targetPedals += 1f;
        if (Input.GetKey(KeyCode.Q)) targetPedals -= 1f;

        /// increase pedals fallback //
        float currentSpeed = (targetPedals == 0f) ? PedalsResetSpeed : PedalsTargetSpeed;

        _currentData._pedalsInput = Mathf.MoveTowards(_currentData._pedalsInput, targetPedals, currentSpeed * dt);
    }
}