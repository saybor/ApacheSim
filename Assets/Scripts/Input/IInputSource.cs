using UnityEngine;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InputData
{
    public Vector2 _cyclicInput;          // 8b WASD: X - Cyclic Roll (A/D), Y - Cyclic Pitch (W/S) [-1, -1;1, 1]
    public float _collectiveInput;        // 4b collective lever (main rotor)  [-1;1]
    public float _pedalsInput;            // 4b Q/E tail rotor pedals [-1;1]
}


public interface IInputSource
{
    InputData InputData { get; }
}