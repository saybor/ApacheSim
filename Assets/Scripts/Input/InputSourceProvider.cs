using System;
using System.Collections.Generic;
using Zenject;
using UnityEngine;

public enum InputMode { Keyboard, Joystick, Network }

public interface IInputSourceProvider
{
    InputData InputData { get; }
    IInputSource ActiveInput { get; }
    void SetMode(InputMode mode);
}

public class InputSourceProvider : IInputSourceProvider, IInitializable
{
    private readonly Dictionary<InputMode, IInputSource> _inputSources;
    public IInputSource ActiveInput { get; private set; }

    public InputSourceProvider(Dictionary<InputMode, IInputSource> inputSources)
    {
        _inputSources = inputSources;
    }

    public void Initialize()
    {
        SetMode(InputMode.Keyboard);
    }

    public void SetMode(InputMode mode)
    {
        if (_inputSources.TryGetValue(mode, out var newSource))
        {
            ActiveInput = newSource;
        }
        else
        {
            Debug.LogError($"[InputProxy] No implementation found for {mode}!");
        }
    }

    public InputData InputData
    {
        get
        {
            if (ActiveInput != null) return ActiveInput.InputData;
            return default; 
        }
    }
}