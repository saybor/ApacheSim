using UnityEngine;
using Zenject;

public interface IWeatherElementSimulator
{

}

public class WindSimulator : IWeatherElementSimulator, IFixedTickable
{
    private readonly EnvironmentConfig _weather;
    private readonly VehicleSpawner _spawner;

    public WindSimulator(EnvironmentConfig weather, VehicleSpawner spawner)
    {
        _weather = weather;
        _spawner = spawner;
    }

    public void FixedTick()
    {
        Vector3 worldWindVector = _weather._windVelocity;

        float windSqrSpeed = worldWindVector.sqrMagnitude;
        if (windSqrSpeed < Mathf.Epsilon) return;

        foreach (var vehicle in _spawner.VehiclesList)
        {
            Vector3 relativeWindWorld = worldWindVector - vehicle.GlobalVelocity;
            float effectiveAreaCoeff = vehicle.GetEffectiveArea(relativeWindWorld);

            /// important - GetEffectiveArea already includes _sideDrag and also coz of PhysX drag we NOT include our vehicle speed //
            float forceMagnitude = 0.5f * _weather.CurrentAirDensity * windSqrSpeed * effectiveAreaCoeff;
            Vector3 finalWorldForce = worldWindVector.normalized * forceMagnitude;

            vehicle.AddExternalForce(finalWorldForce);
        }
    }
}