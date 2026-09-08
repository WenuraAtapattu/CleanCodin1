using LudoGame;

namespace LudoGame.Server.Models.Requests;

public class StartGameRequest
{
    public bool InteractiveSections { get; set; } = false;
}

public class MoveRequest
{
    public string PlayerColor { get; set; } = "";
    public string PieceName { get; set; } = "";
    public int DiceValue { get; set; }
}

public class GameStateRequest
{
    public string? ClientId { get; set; }
}

public class JoinGameRequest
{
    public string ClientId { get; set; } = Guid.NewGuid().ToString();
    public string ClientName { get; set; } = "Anonymous Client";
}