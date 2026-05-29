using UnityEngine;
using System;

public class PidController
{
    private readonly float _pGain;
    private readonly float _iGain;
    private readonly float _dGain;
    private readonly float _minOut; 
    private readonly float _maxOut; 

    private float _integrationStored;
    private float _lastError;
    private bool _isFirstUpdate = true;

    public PidController(in HelicopterConfig.PidSettings settings, float minOut = -1f, float maxOut = 1f)
    {
        _pGain = settings._pGain;
        _iGain = settings._iGain;
        _dGain = settings._dGain;
        _minOut = minOut;
        _maxOut = maxOut;
    }

    public float Update(float error, float dt)
    {
        if (dt <= 0f) return 0f;

        float pOut = error * _pGain;

        _integrationStored += error * dt;
        _integrationStored = Mathf.Clamp(_integrationStored, _minOut, _maxOut);
        float iOut = _integrationStored * _iGain;

        float dOut = 0f;
        if (!_isFirstUpdate)
        {
            float errorRateOfChange = (error - _lastError) / dt;
            dOut = errorRateOfChange * _dGain;
        }
        else { _isFirstUpdate = false; }

        _lastError = error;

        return Mathf.Clamp(pOut + iOut + dOut, _minOut, _maxOut);
    }

    public void Reset()
    {
        _integrationStored = 0f;
        _lastError = 0f;
        _isFirstUpdate = true;
    }
}