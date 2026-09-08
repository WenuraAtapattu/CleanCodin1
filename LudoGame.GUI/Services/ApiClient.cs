using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LudoGame.Server.Models.DTOs;
using LudoGame.Server.Models.Requests;

namespace LudoGame.GUI.Services;

/// <summary>
/// HTTP client for communicating with the LUDO-T game server.
/// Replaces direct game logic dependencies with server communication.
/// Implements async operations for client-side concurrency.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _serverBaseUrl;
    
    public ApiClient(string serverBaseUrl = "https://localhost:5001/api/")
    {
        _serverBaseUrl = serverBaseUrl;
        
        // Configure HttpClient to ignore SSL errors for development
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(serverBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
    
    /// <summary>
    /// Starts a new game on the server.
    /// </summary>
    public async Task<GameResultDTO> StartGameAsync(bool interactiveSections = false)
    {
        try
        {
            var request = new StartGameRequest { InteractiveSections = interactiveSections };
            var response = await _httpClient.PostAsJsonAsync("game/start", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameResultDTO>() 
                   ?? new GameResultDTO { Success = false, Message = "Failed to parse response" };
        }
        catch (Exception ex)
        {
            return new GameResultDTO { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
    
    /// <summary>
    /// Pauses the current game on the server.
    /// </summary>
    public async Task<GameResultDTO> PauseGameAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("game/pause", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameResultDTO>() 
                   ?? new GameResultDTO { Success = false, Message = "Failed to parse response" };
        }
        catch (Exception ex)
        {
            return new GameResultDTO { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
    
    /// <summary>
    /// Resumes a paused game on the server.
    /// </summary>
    public async Task<GameResultDTO> ResumeGameAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("game/resume", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameResultDTO>() 
                   ?? new GameResultDTO { Success = false, Message = "Failed to parse response" };
        }
        catch (Exception ex)
        {
            return new GameResultDTO { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
    
    /// <summary>
    /// Resets the game on the server.
    /// </summary>
    public async Task<GameResultDTO> ResetGameAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("game/reset", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameResultDTO>() 
                   ?? new GameResultDTO { Success = false, Message = "Failed to parse response" };
        }
        catch (Exception ex)
        {
            return new GameResultDTO { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
    
    /// <summary>
    /// Executes a single game step on the server.
    /// </summary>
    public async Task<GameResultDTO> ExecuteStepAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("game/step", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameResultDTO>() 
                   ?? new GameResultDTO { Success = false, Message = "Failed to parse response" };
        }
        catch (Exception ex)
        {
            return new GameResultDTO { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
    
    /// <summary>
    /// Gets the current game state from the server.
    /// </summary>
    public async Task<GameStateDTO> GetGameStateAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("game/state");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameStateDTO>() 
                   ?? new GameStateDTO();
        }
        catch (Exception ex)
        {
            return new GameStateDTO { GameState = "Error" };
        }
    }
    
    /// <summary>
    /// Processes a move request on the server.
    /// </summary>
    public async Task<GameResultDTO> ProcessMoveAsync(string playerColor, string pieceName, int diceValue)
    {
        try
        {
            var request = new MoveRequest 
            { 
                PlayerColor = playerColor, 
                PieceName = pieceName, 
                DiceValue = diceValue 
            };
            var response = await _httpClient.PostAsJsonAsync("game/move", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameResultDTO>() 
                   ?? new GameResultDTO { Success = false, Message = "Failed to parse response" };
        }
        catch (Exception ex)
        {
            return new GameResultDTO { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
    
    /// <summary>
    /// Checks server health.
    /// </summary>
    public async Task<bool> CheckServerHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("game/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets queue statistics from the server.
    /// </summary>
    public async Task<QueueStatistics> GetQueueStatisticsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("game/queue/stats");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<QueueStatistics>() 
                   ?? new QueueStatistics();
        }
        catch
        {
            return new QueueStatistics();
        }
    }
}