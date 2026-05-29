using UnityEngine;
using Zenject;

public abstract class VehicleConfig : MonoInstaller
{
    public abstract bool IsConfigValid();
}