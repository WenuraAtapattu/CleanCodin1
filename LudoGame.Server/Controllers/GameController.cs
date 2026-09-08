using Microsoft.AspNetCore.Mvc;
using LudoGame.Server.Models.DTOs;
using LudoGame.Server.Models.Requests;
using LudoGame.Server.Services;

namespace LudoGame.Server.Controllers;

/// <summary>
/// REST API controller for game management operations.
/// Handles HTTP requests from clients and forwards them to the game service.
/// Supports concurrent client access through async operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly GameService _gameService;
    private readonly RequestQueueService _requestQueueService;
    private readonly ILogger<GameController> _logger;
    
    public GameController(GameService gameService, RequestQueueService requestQueueService, ILogger<GameController> logger)
    {
        _gameService = gameService;
        _requestQueueService = requestQueueService;
        _logger = logger;
    }
    
    /// <summary>
    /// Starts a new game instance.
    /// POST: api/game/start
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<GameResultDTO>> StartGame([FromBody] StartGameRequest request)
    {
        try
        {
            _logger.LogInformation("StartGame request received");
            
            var result = await _gameService.StartGameAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StartGame");
            return StatusCode(500, new GameResultDTO 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }
    
    /// <summary>
    /// Pauses the current game.
    /// POST: api/game/pause
    /// </summary>
    [HttpPost("pause")]
    public async Task<ActionResult<GameResultDTO>> PauseGame()
    {
        try
        {
            _logger.LogInformation("PauseGame request received");
            
            var result = await _gameService.PauseGameAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PauseGame");
            return StatusCode(500, new GameResultDTO 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }
    
    /// <summary>
    /// Resumes a paused game.
    /// POST: api/game/resume
    /// </summary>
    [HttpPost("resume")]
    public async Task<ActionResult<GameResultDTO>> ResumeGame()
    {
        try
        {
            _logger.LogInformation("ResumeGame request received");
            
            var result = await _gameService.ResumeGameAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResumeGame");
            return StatusCode(500, new GameResultDTO 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }
    
    /// <summary>
    /// Resets the game to initial state.
    /// POST: api/game/reset
    /// </summary>
    [HttpPost("reset")]
    public async Task<ActionResult<GameResultDTO>> ResetGame()
    {
        try
        {
            _logger.LogInformation("ResetGame request received");
            
            var result = await _gameService.ResetGameAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResetGame");
            return StatusCode(500, new GameResultDTO 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }
    
    /// <summary>
    /// Executes a single game step.
    /// POST: api/game/step
    /// </summary>
    [HttpPost("step")]
    public async Task<ActionResult<GameResultDTO>> ExecuteStep()
    {
        try
        {
            _logger.LogInformation("ExecuteStep request received");
            
            var result = await _gameService.ExecuteStepAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecuteStep");
            return StatusCode(500, new GameResultDTO 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }
    
    /// <summary>
    /// Gets the current game state.
    /// GET: api/game/state
    /// </summary>
    [HttpGet("state")]
    public ActionResult<GameStateDTO> GetGameState()
    {
        try
        {
            _logger.LogDebug("GetGameState request received");
            
            var state = _gameService.GetGameState();
            return Ok(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetGameState");
            return StatusCode(500, new GameStateDTO 
            { 
                GameState = "Error",
                LastUpdated = DateTime.UtcNow
            });
        }
    }
    
    /// <summary>
    /// Processes a move request.
    /// POST: api/game/move
    /// </summary>
    [HttpPost("move")]
    public async Task<ActionResult<GameResultDTO>> ProcessMove([FromBody] MoveRequest request)
    {
        try
        {
            _logger.LogInformation("ProcessMove request received for {PlayerColor} {PieceName}", 
                request.PlayerColor, request.PieceName);
            
            var result = await _gameService.ProcessMoveAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessMove");
            return StatusCode(500, new GameResultDTO 
            { 
                Success = false, 
                Message = $"Internal server error: {ex.Message}" 
            });
        }
    }
    
    /// <summary>
    /// Gets queue statistics for monitoring.
    /// GET: api/game/queue/stats
    /// </summary>
    [HttpGet("queue/stats")]
    public ActionResult<QueueStatistics> GetQueueStatistics()
    {
        try
        {
            var stats = _requestQueueService.GetStatistics();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue statistics");
            return StatusCode(500, new QueueStatistics());
        }
    }
    
    /// <summary>
    /// Health check endpoint.
    /// GET: api/game/health
    /// </summary>
    [HttpGet("health")]
    public ActionResult HealthCheck()
    {
        return Ok(new { 
            Status = "Healthy", 
            Timestamp = DateTime.UtcNow,
            QueueDepth = _requestQueueService.QueueDepth,
            TotalRequestsProcessed = _requestQueueService.TotalRequestsProcessed
        });
    }
}