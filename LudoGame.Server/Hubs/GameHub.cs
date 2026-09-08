using Microsoft.AspNetCore.SignalR;

namespace LudoGame.Server.Hubs;

/// <summary>
/// SignalR hub for real-time game state updates.
/// Enables server-to-client communication for live game updates.
/// Supports client-side concurrency by broadcasting changes to all connected clients.
/// </summary>
public class GameHub : Hub
{
    private readonly ILogger<GameHub> _logger;
    private static int _connectedClients = 0;
    
    public GameHub(ILogger<GameHub> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _connectedClients);
        var clientId = Context.ConnectionId;
        
        _logger.LogInformation("Client {ClientId} connected. Total clients: {TotalClients}", 
            clientId, _connectedClients);
        
        // Send welcome message to the connected client
        await Clients.Caller.SendAsync("Connected", new 
        { 
            Message = "Connected to LUDO-T Game Server",
            ClientId = clientId,
            Timestamp = DateTime.UtcNow
        });
        
        // Broadcast updated client count to all clients
        await Clients.All.SendAsync("ClientCountUpdated", _connectedClients);
        
        await base.OnConnectedAsync();
    }
    
    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _connectedClients);
        var clientId = Context.ConnectionId;
        
        _logger.LogInformation("Client {ClientId} disconnected. Total clients: {TotalClients}", 
            clientId, _connectedClients);
        
        // Broadcast updated client count to all clients
        await Clients.All.SendAsync("ClientCountUpdated", _connectedClients);
        
        await base.OnDisconnectedAsync(exception);
    }
    
    /// <summary>
    /// Client joins a specific game session.
    /// </summary>
    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Game_{gameId}");
        _logger.LogInformation("Client {ClientId} joined game {GameId}", Context.ConnectionId, gameId);
        
        await Clients.Group($"Game_{gameId}").SendAsync("PlayerJoined", new
        {
            ClientId = Context.ConnectionId,
            GameId = gameId,
            Timestamp = DateTime.UtcNow
        });
    }
    
    /// <summary>
    /// Client leaves a specific game session.
    /// </summary>
    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Game_{gameId}");
        _logger.LogInformation("Client {ClientId} left game {GameId}", Context.ConnectionId, gameId);
        
        await Clients.Group($"Game_{gameId}").SendAsync("PlayerLeft", new
        {
            ClientId = Context.ConnectionId,
            GameId = gameId,
            Timestamp = DateTime.UtcNow
        });
    }
    
    /// <summary>
    /// Server method to broadcast game state updates to all clients.
    /// </summary>
    public async Task BroadcastGameStateUpdate(object gameState)
    {
        _logger.LogDebug("Broadcasting game state update to all clients");
        await Clients.All.SendAsync("GameStateUpdated", gameState);
    }
    
    /// <summary>
    /// Server method to broadcast game state updates to a specific game group.
    /// </summary>
    public async Task BroadcastGameStateToGame(string gameId, object gameState)
    {
        _logger.LogDebug("Broadcasting game state update to game {GameId}", gameId);
        await Clients.Group($"Game_{gameId}").SendAsync("GameStateUpdated", gameState);
    }
    
    /// <summary>
    /// Server method to broadcast player move events.
    /// </summary>
    public async Task BroadcastPlayerMove(object moveData)
    {
        _logger.LogDebug("Broadcasting player move to all clients");
        await Clients.All.SendAsync("PlayerMoved", moveData);
    }
    
    /// <summary>
    /// Server method to broadcast piece capture events.
    /// </summary>
    public async Task BroadcastPieceCapture(object captureData)
    {
        _logger.LogDebug("Broadcasting piece capture to all clients");
        await Clients.All.SendAsync("PieceCaptured", captureData);
    }
    
    /// <summary>
    /// Server method to broadcast game completion events.
    /// </summary>
    public async Task BroadcastGameEnded(object gameResult)
    {
        _logger.LogInformation("Broadcasting game ended to all clients");
        await Clients.All.SendAsync("GameEnded", gameResult);
    }
    
    /// <summary>
    /// Client sends a heartbeat to maintain connection.
    /// </summary>
    public async Task SendHeartbeat()
    {
        await Clients.Caller.SendAsync("HeartbeatResponse", DateTime.UtcNow);
    }
    
    /// <summary>
    /// Gets the current number of connected clients.
    /// </summary>
    public static int GetConnectedClientCount()
    {
        return _connectedClients;
    }
}