using UnityEngine;
using System.Collections.Generic;
using Zenject;

public class MainInstaller : MonoInstaller
{
    [SerializeField] private VehicleConfig _helicopterPrefab;
    [SerializeField] private EnvironmentConfig _weatherConfig;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private FlightDataUI _flightDataUI;
    [SerializeField] private WeatherSettingsUI _settingsUI;

    public override void InstallBindings()
    {
        /// camera //
        Container.Bind<Camera>().FromInstance(_mainCamera).AsSingle();
        Container.BindInterfacesAndSelfTo<ChaseCameraController>().AsSingle();

        /// weather config //
        EnvironmentConfig dynamicConfig = Object.Instantiate(_weatherConfig);
        dynamicConfig.name = $"DynamicConfig_{_weatherConfig.name}";
        Container.Bind<EnvironmentConfig>().FromInstance(dynamicConfig).AsSingle();

        /// bind selected config for helicopter (demo, for full version will bind table of vehicles)
        Container.Bind<VehicleConfig>().FromInstance(_helicopterPrefab).AsSingle();

        /// input and proxy setup //
        Container.BindInterfacesAndSelfTo<InputSourceKeyboard>().AsSingle();
        Container.BindInterfacesAndSelfTo<InputSourceNetwork>().AsSingle();
        Container.BindInterfacesAndSelfTo<InputSourceJoystick>().AsSingle();

        Container.Bind<Dictionary<InputMode, IInputSource>>().FromMethod(ctx =>
            new Dictionary<InputMode, IInputSource>
            {
                { InputMode.Keyboard, ctx.Container.Resolve<InputSourceKeyboard>() },
                { InputMode.Network, ctx.Container.Resolve<InputSourceNetwork>() },
                { InputMode.Joystick, ctx.Container.Resolve<InputSourceJoystick>() }
            }).AsSingle();

        Container.Bind<IVehicle.IFactory>().To<VehicleFactory>().AsSingle();

        Container.BindInterfacesAndSelfTo<VehicleSpawner>().AsSingle();

        Container.Bind<FlightDataUI>().FromInstance(_flightDataUI).AsSingle();
        Container.Bind<WeatherSettingsUI>().FromInstance(_settingsUI).AsSingle();

        /// weather //
        Container.BindInterfacesAndSelfTo<WindSimulator>().AsSingle().NonLazy();

        /// bootstrapper //
        Container.BindInterfacesTo<Launcher>().AsSingle();
    }
}