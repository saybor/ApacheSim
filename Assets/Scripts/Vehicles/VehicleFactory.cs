using Zenject;
using UnityEngine;

public class VehicleFactory : IVehicle.IFactory
{
    private readonly DiContainer _container;

    public VehicleFactory(DiContainer container)
    {
        _container = container;
    }

    public IVehicle Create(VehicleConfig vehicleConfig)
    {
        if (vehicleConfig == null || !vehicleConfig.IsConfigValid())
        {
            Debug.LogError($"<b>[VehicleFactory]</b>: VehicleConfig missing or not valid on <b>{vehicleConfig?.name}</b>");
            return null;
        }

        GameObject vehicleInstance = _container.InstantiatePrefab(vehicleConfig.gameObject);
        return vehicleInstance.GetComponent<GameObjectContext>().Container.Resolve<IVehicle>();
    }
}