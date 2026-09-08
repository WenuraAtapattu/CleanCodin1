# Client-Server Architecture Design for LUDO-T Game

## Overview

This document describes the client-server architecture refactoring for the LUDO-T game to meet Assignment 2 requirements for multi-user, multi-tier, client-server application with concurrency.

## Architecture Components

### Three-Tier Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Client Tier                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ WPF GUI App  │  │ Test Client 1│  │ Test Client 2│      │
│  │              │  │              │  │              │      │
│  │ - Views      │  │ - Auto Test  │  │ - Auto Test  │      │
│  │ - ViewModels │  │ - Async Req  │  │ - Async Req  │      │
│  │ - HTTP Client│  │ - Load Test  │  │ - Load Test  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                              ↕ HTTP/SignalR
┌─────────────────────────────────────────────────────────────┐
│                        Server Tier                            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           ASP.NET Core Web API Server                  │  │
│  │  ┌────────────────────────────────────────────────┐  │  │
│  │  │  Controllers                                    │  │  │
│  │  │  - GameController                              │  │  │
│  │  │  - PlayerController                            │  │  │
│  │  │  - BoardController                             │  │  │
│  │  └────────────────────────────────────────────────┘  │  │
│  │  ┌────────────────────────────────────────────────┐  │  │
│  │  │  Services                                       │  │  │
│  │  │  - GameService (thread-safe)                    │  │  │
│  │  │  - RequestQueueService                          │  │  │
│  │  │  - PlayerConnectionService                      │  │  │
│  │  └────────────────────────────────────────────────┘  │  │
│  │  ┌────────────────────────────────────────────────┐  │  │
│  │  │  SignalR Hub                                     │  │  │
│  │  │  - GameHub (real-time updates)                  │  │  │
│  │  └────────────────────────────────────────────────┘  │  │
│  │  ┌────────────────────────────────────────────────┐  │  │
│  │  │  Core Game Logic (from existing project)       │  │  │
│  │  │  - GameManager                                  │  │  │
│  │  │  - Board, Players, Rules                        │  │  │
│  │  └────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              ↕ ADO.NET/Entity Framework
┌─────────────────────────────────────────────────────────────┐
│                       Data Tier                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   SQLite DB  │  │  Game State  │  │  Player Data │      │
│  │  (Optional)  │  │  Storage     │  │  Storage     │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

## Technology Stack

### Server Tier
- **ASP.NET Core 8.0 Web API**: HTTP server with built-in concurrency
- **SignalR**: Real-time bidirectional communication
- **ThreadPool**: Built-in thread pool for request processing
- **Channel<T>:** Modern async queue for request handling
- **MemoryCache**: Game state caching (optional database)

### Client Tier
- **WPF (Existing)**: Refactored to use HTTP client instead of direct game logic
- **HttpClient**: Async HTTP communication with server
- **SignalR Client**: Real-time updates from server
- **Task/Async-Await**: Asynchronous client operations

### Communication Protocol
- **REST API**: Command requests (start game, make move, etc.)
- **SignalR WebSocket**: Real-time game state updates
- **JSON**: Data serialization format

## Clean Architecture Compliance

### Dependency Inversion
- Server controllers depend on service interfaces
- Services depend on game logic interfaces
- No direct dependencies on concrete implementations

### Separation of Concerns
- **Presentation Layer**: WPF GUI (client)
- **Application Layer**: Web API controllers and services
- **Domain Layer**: Core game logic (existing GameManager)
- **Infrastructure Layer**: Data access, SignalR, HTTP

### Single Responsibility
- Each controller handles specific domain (Game, Player, Board)
- Each service handles specific functionality (Game management, queuing, connections)
- Game logic remains unchanged from original implementation

## Concurrency Implementation

### Server-Side Concurrency

#### 1. Request Processing with ThreadPool
```csharp
// ASP.NET Core automatically uses ThreadPool for requests
[HttpPost("start")]
public async Task<IActionResult> StartGame()
{
    // Automatically processed on thread pool thread
    var result = await _gameService.StartGameAsync();
    return Ok(result);
}
```

#### 2. Request Queue with Channel<T>
```csharp
public class RequestQueueService
{
    private readonly Channel<GameRequest> _queue;
    private readonly CancellationTokenSource _cts;
    
    public RequestQueueService()
    {
        _queue = Channel.CreateUnbounded<GameRequest>();
        _cts = new CancellationTokenSource();
        
        // Start background processor
        _ = ProcessQueueAsync(_cts.Token);
    }
    
    public async Task EnqueueAsync(GameRequest request)
    {
        await _queue.Writer.WriteAsync(request);
    }
    
    private async Task ProcessQueueAsync(CancellationToken token)
    {
        await foreach (var request in _queue.Reader.ReadAllAsync(token))
        {
            await ProcessRequest(request);
        }
    }
}
```

#### 3. Thread-Safe Game State
```csharp
public class GameService
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly GameManager _gameManager;
    
    public async Task<GameResult> MakeMoveAsync(MoveRequest request)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Thread-safe game state access
            return _gameManager.MakeMove(request);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### Client-Side Concurrency

#### 1. Asynchronous HTTP Requests
```csharp
public class GameClient
{
    private readonly HttpClient _httpClient;
    
    public async Task<GameResult> StartGameAsync()
    {
        var response = await _httpClient.PostAsync("api/game/start", null);
        return await response.Content.ReadFromJsonAsync<GameResult>();
    }
}
```

#### 2. Real-Time Updates with SignalR
```csharp
public class GameClient
{
    private readonly HubConnection _hubConnection;
    
    public async Task ConnectAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/gamehub")
            .Build();
            
        _hubConnection.On<GameState>("GameStateUpdated", (state) =>
        {
            // Real-time update callback
            OnGameStateUpdated(state);
        });
        
        await _hubConnection.StartAsync();
    }
}
```

#### 3. Background UI Updates
```csharp
public class MainViewModel
{
    private async Task UpdateGameStateAsync()
    {
        // Run on background thread
        var state = await _gameClient.GetGameStateAsync();
        
        // Update UI on main thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentPlayer = state.CurrentPlayer;
            GameRound = state.Round;
        });
    }
}
```

## Test Client Implementation

### Test Client 1: Rapid Request Sender
```csharp
public class TestClient1
{
    private readonly HttpClient _httpClient;
    
    public async Task RunRapidRequestsTest()
    {
        var tasks = new List<Task>();
        
        // Send 100 rapid requests
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(SendRequestAsync(i));
        }
        
        await Task.WhenAll(tasks);
    }
    
    private async Task SendRequestAsync(int requestId)
    {
        var request = new GameRequest { Id = requestId };
        await _httpClient.PostAsJsonAsync("api/game/move", request);
    }
}
```

### Test Client 2: Concurrent Load Tester
```csharp
public class TestClient2
{
    public async Task RunConcurrentLoadTest()
    {
        // Run multiple test clients simultaneously
        var clientTasks = new List<Task>();
        
        for (int i = 0; i < 5; i++)
        {
            clientTasks.Add(RunClientSessionAsync(i));
        }
        
        await Task.WhenAll(clientTasks);
    }
    
    private async Task RunClientSessionAsync(int clientId)
    {
        // Each client sends requests independently
        for (int i = 0; i < 50; i++)
        {
            await SendGameRequestAsync(clientId, i);
            await Task.Delay(10); // 10ms between requests
        }
    }
}
```

## API Endpoints

### Game Management
- `POST /api/game/start` - Start a new game
- `POST /api/game/pause` - Pause the game
- `POST /api/game/resume` - Resume the game
- `POST /api/game/reset` - Reset the game
- `GET /api/game/state` - Get current game state

### Player Actions
- `POST /api/player/move` - Make a move
- `GET /api/player/{color}/status` - Get player status
- `GET /api/player/{color}/pieces` - Get player pieces

### Board Information
- `GET /api/board/state` - Get board state
- `GET /api/board/mystery` - Get mystery cell info

## SignalR Events

### Server → Client
- `GameStateUpdated` - Game state changed
- `PlayerMoved` - Player made a move
- `PieceCaptured` - Piece was captured
- `GameEnded` - Game completed

### Client → Server
- `JoinGame` - Client joins game
- `LeaveGame` - Client leaves game

## Implementation Phases

### Phase 1: Server Foundation
1. Create ASP.NET Core Web API project
2. Set up basic project structure
3. Implement game service with GameManager integration
4. Add basic API endpoints

### Phase 2: Concurrency
1. Implement request queue service
2. Add thread-safe game state management
3. Configure async request processing
4. Add proper error handling

### Phase 3: Real-Time Communication
1. Add SignalR to server
2. Implement GameHub for real-time updates
3. Add game state broadcasting
4. Handle client connections/disconnections

### Phase 4: Client Refactoring
1. Refactor WPF GUI to use HTTP client
2. Add SignalR client for real-time updates
3. Update ViewModels to use async operations
4. Remove direct game logic dependencies

### Phase 5: Test Clients
1. Create TestClient1 project
2. Create TestClient2 project
3. Implement rapid request sending
4. Implement concurrent load testing
5. Add performance monitoring

### Phase 6: Testing & Validation
1. Test concurrent client connections
2. Test request queuing under load
3. Test real-time updates between clients
4. Validate all assignment requirements

## Assignment Rubric Coverage

### Server-Side Concurrency (25%)
✅ Handles requests from multiple clients simultaneously  
✅ Implements request queuing for rapid requests  
✅ Thread-safe game state management  
✅ Async request processing  

### Client-Side Concurrency (25%)
✅ Multiple clients can interact with server  
✅ Real-time updates when other clients make changes  
✅ Test clients send simultaneous asynchronous requests  
✅ Rapid succession request handling  

### Architecture (20%)
✅ Clean Architecture model preserved  
✅ Components allocated to appropriate tiers  
✅ Client runs as separate application  
✅ Client, server, and data tiers separated  

## File Structure

```
CleanCodin2/
├── LudoGame.csproj                    # Core game logic (unchanged)
├── LudoGame.GUI/                      # WPF Client (refactored)
├── LudoGame.Server/                   # NEW: ASP.NET Core Server
│   ├── LudoGame.Server.csproj
│   ├── Controllers/
│   │   ├── GameController.cs
│   │   ├── PlayerController.cs
│   │   └── BoardController.cs
│   ├── Services/
│   │   ├── GameService.cs
│   │   ├── RequestQueueService.cs
│   │   └── PlayerConnectionService.cs
│   ├── Hubs/
│   │   └── GameHub.cs
│   └── Models/
│       ├── DTOs/
│       └── Requests/
├── LudoGame.TestClient1/              # NEW: Test Client 1
│   ├── LudoGame.TestClient1.csproj
│   └── Program.cs
├── LudoGame.TestClient2/              # NEW: Test Client 2
│   ├── LudoGame.TestClient2.csproj
│   └── Program.cs
└── LudoGameSolution.sln
```

## Next Steps

1. Create LudoGame.Server project structure
2. Implement basic API endpoints
3. Add concurrency and queuing
4. Refactor WPF client
5. Create test clients
6. Test and validate requirements
