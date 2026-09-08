using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LudoGame.Server.Models.DTOs;
using LudoGame.Server.Models.Requests;

namespace LudoGame.Server.Services;

/// <summary>
/// Thread-safe service that manages game state for server-side operations.
/// Implements proper concurrency control for multi-client access.
/// Note: This is a simplified version. Full implementation would integrate with the existing GameManager.
/// </summary>
public class GameService
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private GameStateDTO _currentState = new();
    private readonly object _stateLock = new();
    
    public GameService()
    {
        InitializeGameState();
    }
    
    private void InitializeGameState()
    {
        lock (_stateLock)
        {
            _currentState = new GameStateDTO
            {
                GameState = "Ready",
                CurrentRound = 0,
                CurrentPlayer = null,
                LastUpdated = DateTime.UtcNow
            };
        }
    }
    
    /// <summary>
    /// Starts a new game instance with thread-safe initialization.
    /// </summary>
    public async Task<GameResultDTO> StartGameAsync(StartGameRequest request)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Initialize game logic
            // Note: This would call a modified GameManager.InitializeGameOnly() if available
            // For now, we'll simulate the initialization
            
            lock (_stateLock)
            {
                _currentState.GameState = "Running";
                _currentState.CurrentRound = 1;
                _currentState.LastUpdated = DateTime.UtcNow;
            }
            
            return new GameResultDTO
            {
                Success = true,
                Message = "Game started successfully",
                GameState = GetGameState()
            };
        }
        catch (Exception ex)
        {
            return new GameResultDTO
            {
                Success = false,
                Message = $"Error starting game: {ex.Message}"
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Pauses the current game instance.
    /// </summary>
    public async Task<GameResultDTO> PauseGameAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            lock (_stateLock)
            {
                _currentState.GameState = "Paused";
                _currentState.LastUpdated = DateTime.UtcNow;
            }
            
            return new GameResultDTO
            {
                Success = true,
                Message = "Game paused",
                GameState = GetGameState()
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Resumes a paused game instance.
    /// </summary>
    public async Task<GameResultDTO> ResumeGameAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            lock (_stateLock)
            {
                _currentState.GameState = "Running";
                _currentState.LastUpdated = DateTime.UtcNow;
            }
            
            return new GameResultDTO
            {
                Success = true,
                Message = "Game resumed",
                GameState = GetGameState()
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Resets the game to initial state.
    /// </summary>
    public async Task<GameResultDTO> ResetGameAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            InitializeGameState();
            
            return new GameResultDTO
            {
                Success = true,
                Message = "Game reset successfully",
                GameState = GetGameState()
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Executes a single game step with thread-safe access.
    /// </summary>
    public async Task<GameResultDTO> ExecuteStepAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            // Execute game step logic
            // Note: This would call a modified GameManager.ExecuteSingleStep() if available
            
            lock (_stateLock)
            {
                _currentState.CurrentRound++;
                _currentState.LastUpdated = DateTime.UtcNow;
                
                // Simulate player rotation
                var players = new[] { "Red", "Green", "Yellow", "Blue" };
                var currentIndex = _currentState.CurrentRound % 4;
                _currentState.CurrentPlayer = players[currentIndex];
            }
            
            return new GameResultDTO
            {
                Success = true,
                Message = "Step executed successfully",
                GameState = GetGameState()
            };
        }
        catch (Exception ex)
        {
            return new GameResultDTO
            {
                Success = false,
                Message = $"Error executing step: {ex.Message}"
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Gets the current game state (thread-safe read).
    /// </summary>
    public GameStateDTO GetGameState()
    {
        lock (_stateLock)
        {
            // Return a copy to prevent external modification
            return new GameStateDTO
            {
                GameState = _currentState.GameState,
                CurrentRound = _currentState.CurrentRound,
                CurrentPlayer = _currentState.CurrentPlayer,
                PlayerStatuses = new Dictionary<string, PlayerStatusDTO>(_currentState.PlayerStatuses),
                BoardState = new BoardStateDTO
                {
                    MysteryCellPosition = _currentState.BoardState.MysteryCellPosition,
                    MysteryCellRoundsRemaining = _currentState.BoardState.MysteryCellRoundsRemaining,
                    OccupiedCells = new Dictionary<int, List<string>>(_currentState.BoardState.OccupiedCells)
                },
                LastUpdated = _currentState.LastUpdated
            };
        }
    }
    
    /// <summary>
    /// Processes a move request from a client.
    /// </summary>
    public async Task<GameResultDTO> ProcessMoveAsync(MoveRequest request)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Process the move using game logic
            // Note: This would integrate with the actual GameManager move processing
            
            lock (_stateLock)
            {
                _currentState.LastUpdated = DateTime.UtcNow;
            }
            
            return new GameResultDTO
            {
                Success = true,
                Message = $"Move processed for {request.PlayerColor} {request.PieceName}",
                GameState = GetGameState()
            };
        }
        catch (Exception ex)
        {
            return new GameResultDTO
            {
                Success = false,
                Message = $"Error processing move: {ex.Message}"
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Gets player-specific status information.
    /// </summary>
    public PlayerStatusDTO? GetPlayerStatus(string color)
    {
        lock (_stateLock)
        {
            _currentState.PlayerStatuses.TryGetValue(color, out var status);
            return status;
        }
    }
    
    /// <summary>
    /// Gets board state information.
    /// </summary>
    public BoardStateDTO GetBoardState()
    {
        lock (_stateLock)
        {
            return new BoardStateDTO
            {
                MysteryCellPosition = _currentState.BoardState.MysteryCellPosition,
                MysteryCellRoundsRemaining = _currentState.BoardState.MysteryCellRoundsRemaining,
                OccupiedCells = new Dictionary<int, List<string>>(_currentState.BoardState.OccupiedCells)
            };
        }
    }
}