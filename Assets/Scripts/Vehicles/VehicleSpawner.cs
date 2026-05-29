using System;
using System.Collections.Generic;
using UnityEngine;

public class VehicleSpawner
{
    const int _predictedVehiclesAmount = 20;

    public event Action<IVehicle> OnVehicleSpawned;

    private readonly IVehicle.IFactory _factory;

    private List<IVehicle> _vehiclesSpawned;
    public IVehicle ActiveVehicle { get; private set; }
    public List<IVehicle> VehiclesList => _vehiclesSpawned;


    public VehicleSpawner(IVehicle.IFactory factory)
    {
        _factory = factory;
        _vehiclesSpawned = new List<IVehicle>(_predictedVehiclesAmount);
    }

    public void Spawn(VehicleConfig prefab)
    {
        IVehicle vehicle = _factory.Create(prefab);
        if (vehicle != null)
        {
            _vehiclesSpawned.Add(vehicle);
            ActiveVehicle = vehicle;
            OnVehicleSpawned?.Invoke(ActiveVehicle);
        }
    }
}