using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "weatherConfig", menuName = "Configs/Weather")]
public class EnvironmentConfig : ScriptableObject
{
    private const float _baseDryAirDensity = 1.225f;

    public Vector3 _windVelocity = new Vector3(0, 0, 0);
    [Range(0f, 1f)] public float _rainIntensity;


    public float CurrentAirDensity
    {
        get
        {
            float rainDropFactor = _rainIntensity * 0.02f;
            return _baseDryAirDensity * (1f - rainDropFactor);
        }
    }
}