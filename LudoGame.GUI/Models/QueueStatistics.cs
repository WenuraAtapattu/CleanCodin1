namespace LudoGame.GUI.Models;

/// <summary>
/// Statistics about the server request queue performance.
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