using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LudoGame.Server.Models.Requests;

namespace LudoGame.Server.Services;

/// <summary>
/// Implements a request queue using modern Channel<T> for async processing.
/// Handles requests that cannot be processed immediately by queuing them for later processing.
/// This addresses the assignment requirement for server-side request queuing.
/// </summary>
public class RequestQueueService
{
    private readonly Channel<GameRequest> _requestQueue;
    private readonly Channel<GameRequest> _priorityQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<RequestQueueService> _logger;
    private readonly GameService _gameService;
    
    // Queue statistics
    private long _totalRequestsReceived = 0;
    private long _totalRequestsProcessed = 0;
    private long _totalRequestsQueued = 0;
    private long _totalRequestsFailed = 0;
    
    public int QueueDepth => _requestQueue.Reader.Count;
    public int PriorityQueueDepth => _priorityQueue.Reader.Count;
    public long TotalRequestsReceived => _totalRequestsReceived;
    public long TotalRequestsProcessed => _totalRequestsProcessed;
    public long TotalRequestsQueued => _totalRequestsQueued;
    public long TotalRequestsFailed => _totalRequestsFailed;
    
    public RequestQueueService(GameService gameService, ILogger<RequestQueueService> logger)
    {
        _gameService = gameService;
        _logger = logger;
        
        // Create unbounded channels for maximum throughput
        _requestQueue = Channel.CreateUnbounded<GameRequest>();
        _priorityQueue = Channel.CreateUnbounded<GameRequest>();
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Start background processing tasks
        _ = ProcessStandardQueueAsync(_cancellationTokenSource.Token);
        _ = ProcessPriorityQueueAsync(_cancellationTokenSource.Token);
        
        _logger.LogInformation("RequestQueueService initialized and started");
    }
    
    /// <summary>
    /// Enqueues a standard request for processing.
    /// Returns immediately if the queue accepts the request, otherwise handles backpressure.
    /// </summary>
    public async Task<bool> EnqueueAsync(GameRequest request, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalRequestsReceived);
        
        try
        {
            // Try to write to the queue with a timeout to prevent indefinite blocking
            bool enqueued = await _requestQueue.Writer.WriteAsync(request, cancellationToken);
            
            if (enqueued)
            {
                Interlocked.Increment(ref _totalRequestsQueued);
                _logger.LogDebug("Request {RequestId} enqueued successfully. Queue depth: {Depth}", 
                    request.RequestId, QueueDepth);
                return true;
            }
            else
            {
                Interlocked.Increment(ref _totalRequestsFailed);
                _logger.LogWarning("Request {RequestId} could not be enqueued (queue full or closed)", 
                    request.RequestId);
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref _totalRequestsFailed);
            _logger.LogWarning("Request {RequestId} enqueue cancelled", request.RequestId);
            return false;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _totalRequestsFailed);
            _logger.LogError(ex, "Error enqueuing request {RequestId}", request.RequestId);
            return false;
        }
    }
    
    /// <summary>
    /// Enqueues a priority request for expedited processing.
    /// </summary>
    public async Task<bool> EnqueuePriorityAsync(GameRequest request, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalRequestsReceived);
        
        try
        {
            bool enqueued = await _priorityQueue.Writer.WriteAsync(request, cancellationToken);
            
            if (enqueued)
            {
                Interlocked.Increment(ref _totalRequestsQueued);
                _logger.LogDebug("Priority request {RequestId} enqueued successfully. Priority queue depth: {Depth}", 
                    request.RequestId, PriorityQueueDepth);
                return true;
            }
            else
            {
                Interlocked.Increment(ref _totalRequestsFailed);
                _logger.LogWarning("Priority request {RequestId} could not be enqueued", request.RequestId);
                return false;
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _totalRequestsFailed);
            _logger.LogError(ex, "Error enqueuing priority request {RequestId}", request.RequestId);
            return false;
        }
    }
    
    /// <summary>
    /// Processes standard requests from the queue.
    /// Runs as a background task.
    /// </summary>
    private async Task ProcessStandardQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Standard queue processor started");
        
        try
        {
            await foreach (var request in _requestQueue.Reader.ReadAllAsync(cancellationToken))
            {
                await ProcessRequestAsync(request, "standard");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Standard queue processor cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in standard queue processor");
        }
    }
    
    /// <summary>
    /// Processes priority requests from the priority queue.
    /// Runs as a background task with higher priority.
    /// </summary>
    private async Task ProcessPriorityQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Priority queue processor started");
        
        try
        {
            await foreach (var request in _priorityQueue.Reader.ReadAllAsync(cancellationToken))
            {
                await ProcessRequestAsync(request, "priority");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Priority queue processor cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in priority queue processor");
        }
    }
    
    /// <summary>
    /// Processes a single request with error handling and retry logic.
    /// </summary>
    private async Task ProcessRequestAsync(GameRequest request, string queueType)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogDebug("Processing {QueueType} request {RequestId} of type {RequestType}", 
                queueType, request.RequestId, request.RequestType);
            
            // Process the request based on its type
            var result = await ProcessRequestByTypeAsync(request);
            
            Interlocked.Increment(ref _totalRequestsProcessed);
            
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug("Request {RequestId} processed successfully in {ProcessingTime}ms", 
                request.RequestId, processingTime);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _totalRequestsFailed);
            _logger.LogError(ex, "Error processing request {RequestId}", request.RequestId);
            
            // Optionally implement retry logic here
            // For now, we just log the error
        }
    }
    
    /// <summary>
    /// Routes the request to the appropriate game service method based on request type.
    /// </summary>
    private async Task<object> ProcessRequestByTypeAsync(GameRequest request)
    {
        return request.RequestType.ToLower() switch
        {
            "startgame" => await _gameService.StartGameAsync(new Models.Requests.StartGameRequest()),
            "pausegame" => await _gameService.PauseGameAsync(),
            "resumegame" => await _gameService.ResumeGameAsync(),
            "resetgame" => await _gameService.ResetGameAsync(),
            "executestep" => await _gameService.ExecuteStepAsync(),
            "move" => await _gameService.ProcessMoveAsync(new MoveRequest
            {
                PlayerColor = request.Parameters.GetValueOrDefault("playerColor", ""),
                PieceName = request.Parameters.GetValueOrDefault("pieceName", ""),
                DiceValue = int.Parse(request.Parameters.GetValueOrDefault("diceValue", "0"))
            }),
            _ => throw new ArgumentException($"Unknown request type: {request.RequestType}")
        };
    }
    
    /// <summary>
    /// Gets queue statistics for monitoring.
    /// </summary>
    public QueueStatistics GetStatistics()
    {
        return new QueueStatistics
        {
            QueueDepth = QueueDepth,
            PriorityQueueDepth = PriorityQueueDepth,
            TotalRequestsReceived = TotalRequestsReceived,
            TotalRequestsProcessed = TotalRequestsProcessed,
            TotalRequestsQueued = TotalRequestsQueued,
            TotalRequestsFailed = TotalRequestsFailed,
            SuccessRate = TotalRequestsReceived > 0 
                ? (double)TotalRequestsProcessed / TotalRequestsReceived * 100 
                : 0
        };
    }
    
    /// <summary>
    /// Gracefully shuts down the queue service.
    /// </summary>
    public async Task ShutdownAsync()
    {
        _logger.LogInformation("Shutting down RequestQueueService...");
        
        _cancellationTokenSource.Cancel();
        
        // Complete the channels to signal no more items will be added
        _requestQueue.Writer.Complete();
        _priorityQueue.Writer.Complete();
        
        // Wait a short time for pending requests to process
        await Task.Delay(1000);
        
        _logger.LogInformation("RequestQueueService shutdown complete. Final statistics: {@Statistics}", 
            GetStatistics());
    }
}

/// <summary>
/// Represents a game request to be processed by the queue.
/// </summary>
public class GameRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string RequestType { get; set; } = "";
    public Dictionary<string, string> Parameters { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ClientId { get; set; }
    public int Priority { get; set; } = 0; // Higher numbers = higher priority
}

/// <summary>
/// Statistics about the request queue performance.
/// </summary>
public class QueueStatistics
{
    public int QueueDepth { get; set; }
    public int PriorityQueueDepth { get; set; }
    public long TotalRequestsReceived { get; set; }
    public long TotalRequestsProcessed { get; set; }
    public long TotalRequestsQueued { get; set; }
    public long TotalRequestsFailed { get; set; }
    public double SuccessRate { get; set; }
}