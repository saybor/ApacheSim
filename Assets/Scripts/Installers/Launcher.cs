using Zenject;
using UnityEngine;

public class Launcher : IInitializable
{
    private readonly VehicleSpawner _spawner;
    private readonly VehicleConfig _helicopterPrefab;

    public Launcher(VehicleSpawner spawner, VehicleConfig helicopterPrefab)
    {
        _spawner = spawner;
        _helicopterPrefab = helicopterPrefab;
    }

    public void Initialize()
    {
        _spawner.Spawn(_helicopterPrefab);
    }
}