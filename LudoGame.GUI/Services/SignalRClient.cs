using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace LudoGame.GUI.Services;

/// <summary>
/// SignalR client for real-time game state updates from the server.
/// Enables client-side concurrency by receiving updates when other clients make changes.
/// </summary>
public class SignalRClient
{
    private HubConnection? _hubConnection;
    private readonly string _serverUrl;
    private bool _isConnected = false;
    
    public event Action<string>? OnConnected;
    public event Action<string>? OnDisconnected;
    public event Action<object>? OnGameStateUpdated;
    public event Action<object>? OnPlayerMoved;
    public event Action<object>? OnPieceCaptured;
    public event Action<object>? OnGameEnded;
    public event Action<int>? OnClientCountUpdated;
    
    public bool IsConnected => _isConnected && _hubConnection?.State == HubConnectionState.Connected;
    
    public SignalRClient(string serverUrl = "https://localhost:5001/gamehub")
    {
        _serverUrl = serverUrl;
    }
    
    /// <summary>
    /// Connects to the SignalR hub for real-time updates.
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_serverUrl, options =>
                {
                    // Ignore SSL errors for development
                    options.HttpMessageHandlerFactory = (message) =>
                    {
                        var handler = new HttpClientHandler();
                        handler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                        return handler;
                    };
                })
                .WithAutomaticReconnect()
                .Build();
            
            // Register event handlers
            _hubConnection.On<string>("Connected", (message) =>
            {
                _isConnected = true;
                OnConnected?.Invoke(message);
            });
            
            _hubConnection.On<object>("GameStateUpdated", (state) =>
            {
                OnGameStateUpdated?.Invoke(state);
            });
            
            _hubConnection.On<object>("PlayerMoved", (moveData) =>
            {
                OnPlayerMoved?.Invoke(moveData);
            });
            
            _hubConnection.On<object>("PieceCaptured", (captureData) =>
            {
                OnPieceCaptured?.Invoke(captureData);
            });
            
            _hubConnection.On<object>("GameEnded", (gameResult) =>
            {
                OnGameEnded?.Invoke(gameResult);
            });
            
            _hubConnection.On<int>("ClientCountUpdated", (count) =>
            {
                OnClientCountUpdated?.Invoke(count);
            });
            
            // Start the connection
            await _hubConnection.StartAsync();
            Console.WriteLine("SignalR client connected successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to SignalR: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Disconnects from the SignalR hub.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _isConnected = false;
            OnDisconnected?.Invoke("Disconnected from server");
        }
    }
    
    /// <summary>
    /// Joins a specific game session.
    /// </summary>
    public async Task JoinGameAsync(string gameId)
    {
        if (_hubConnection != null && IsConnected)
        {
            await _hubConnection.InvokeAsync("JoinGame", gameId);
        }
    }
    
    /// <summary>
    /// Leaves a specific game session.
    /// </summary>
    public async Task LeaveGameAsync(string gameId)
    {
        if (_hubConnection != null && IsConnected)
        {
            await _hubConnection.InvokeAsync("LeaveGame", gameId);
        }
    }
    
    /// <summary>
    /// Sends a heartbeat to maintain the connection.
    /// </summary>
    public async Task SendHeartbeatAsync()
    {
        if (_hubConnection != null && IsConnected)
        {
            await _hubConnection.InvokeAsync("SendHeartbeat");
        }
    }
}