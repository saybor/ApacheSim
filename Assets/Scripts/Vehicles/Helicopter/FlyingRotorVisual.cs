using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingRotorVisual : MonoBehaviour
{
    private IVehicleFlyingRotor _rotor;

    public void Initialize(IVehicleFlyingRotor rotor)
    {
        _rotor = rotor;
    }

    private void Update()
    {
        if (_rotor == null) return;

        /// RPM to degree/seconds //
        float degreesPerSecond = _rotor.VisualRPM * 6f;

        transform.Rotate(_rotor.MainAxis, degreesPerSecond * Time.deltaTime, Space.World);
    }
}