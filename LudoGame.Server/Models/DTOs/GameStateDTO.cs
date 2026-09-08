namespace LudoGame.Server.Models.DTOs;

public class GameStateDTO
{
    public string GameState { get; set; } = "NotStarted";
    public int CurrentRound { get; set; }
    public string? CurrentPlayer { get; set; }
    public Dictionary<string, PlayerStatusDTO> PlayerStatuses { get; set; } = new();
    public BoardStateDTO BoardState { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class PlayerStatusDTO
{
    public string Color { get; set; } = "";
    public int PiecesOnBoard { get; set; }
    public int PiecesAtBase { get; set; }
    public int PiecesAtHome { get; set; }
    public int FinishPlace { get; set; }
    public bool HasWon { get; set; }
}

public class BoardStateDTO
{
    public int? MysteryCellPosition { get; set; }
    public int MysteryCellRoundsRemaining { get; set; }
    public Dictionary<int, List<string>> OccupiedCells { get; set; } = new();
}

public class GameResultDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public GameStateDTO? GameState { get; set; }
}