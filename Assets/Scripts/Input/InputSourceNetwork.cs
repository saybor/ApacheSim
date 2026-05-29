using System;
using System.Net.Sockets;
using System.Threading;
using Zenject;

public class InputSourceNetwork : IInputSource, IDisposable, IInitializable
{
    private readonly object _syncRoot = new object();
    private InputData _currentData;
    public InputData InputData
    {
        get
        {
            lock (_syncRoot) return _currentData;
        }
    }

    private UdpClient _udpClient;
    private Thread _receiveThread;
    volatile private bool _isRunning;

    public void Initialize()
    {
        _udpClient = new UdpClient(5555);
        _isRunning = true;
        _receiveThread = new Thread(ReceiveData) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
        _receiveThread.Start();
    }

    private unsafe void ReceiveData()
    {
        int bufferSize = sizeof(InputData) * 4;
        byte* stackBuffer = stackalloc byte[bufferSize];
        var bufferSpan = new Span<byte>(stackBuffer, bufferSize);

        var socket = _udpClient.Client;
        socket.ReceiveTimeout = 500;
        
        while (_isRunning)
        {
            try
            {
                int receivedBytes = socket.Receive(bufferSpan);

                if (!_isRunning) break;

                if (receivedBytes == sizeof(InputData))
                {
                    InputData received = *(InputData*)stackBuffer;
                    lock (_syncRoot)
                    {
                        _currentData = received;
                    }
                }
            }
            catch(SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut) continue;
                break;
            }
            catch 
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _isRunning = false;
        _udpClient?.Close();
        _receiveThread?.Join();
    }
}