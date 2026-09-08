using System;
using System.Collections.Generic;
using LudoGame;

namespace LudoGame.GUI.Services
{
    public class GameLoggerService : IGameLogger
    {
        private Action<string>? _logCallback;
        
        public GameLoggerService(Action<string>? logCallback = null)
        {
            _logCallback = logCallback;
        }
        
        public void SetLogCallback(Action<string> callback)
        {
            _logCallback = callback;
        }
        
        private void Log(string message)
        {
            _logCallback?.Invoke(message);
        }
        
        public void LogInitialPlayer(Color color, string n1, string n2, string n3, string n4)
        {
            Log($"Player {color}: {n1}, {n2}, {n3}, {n4}");
        }
        
        public void LogOpeningRoll(Color color, int value)
        {
            Log($"{color} rolls {value}");
        }
        
        public void LogFirstPlayer(Color color)
        {
            Log($"{color} goes first");
        }
        
        public void LogRoundOrder(string order)
        {
            Log($"Turn order: {order}");
        }
        
        public void LogDiceRoll(Color color, int value)
        {
            Log($"{color} rolls {value}");
        }
        
        public void LogStartingPointMove(Color color, string pieceName)
        {
            Log($"{color} {pieceName} moves to starting point");
        }
        
        public void LogPieceStatus(Color color, int boardCount, int baseCount, int homeCount = 0)
        {
            Log($"{color} - Board: {boardCount}, Base: {baseCount}, Home: {homeCount}");
        }
        
        public void LogMove(Color color, string pieceName, string fromLocation, string toLocation, int units, Direction direction)
        {
            Log($"{color} {pieceName}: {fromLocation} -> {toLocation} ({units} units, {direction})");
        }
        
        public void LogMove(Color color, string pieceName, int startPos, int endPos, int units, Direction direction)
        {
            Log($"{color} {pieceName}: {startPos} -> {endPos} ({units} units, {direction})");
        }
        
        public void LogMoveDetailed(Color color, string pieceName, string fromLocation, string toLocation, int diceValue, int movedUnits, Direction direction)
        {
            Log($"{color} {pieceName}: {fromLocation} -> {toLocation} (rolled {diceValue}, moved {movedUnits} units, {direction})");
        }
        
        public void LogMoveDetailed(Color color, string pieceName, int startPos, int endPos, int diceValue, int movedUnits, Direction direction)
        {
            Log($"{color} {pieceName}: {startPos} -> {endPos} (rolled {diceValue}, moved {movedUnits} units, {direction})");
        }
        
        public void LogMoveEnterHomeStraight(Color color, string pieceName, int ringPosition, int homePathPosition, int totalUnits, int stepsInHome, Direction direction)
        {
            Log($"{color} {pieceName} enters home straight: {ringPosition} -> home[{homePathPosition}] ({totalUnits} units, {stepsInHome} in home, {direction})");
        }
        
        public void LogMoveEnterHomeStraightDetailed(Color color, string pieceName, int ringPosition, int homePathPosition, int diceValue, int movedUnits, int stepsInHome, Direction direction)
        {
            Log($"{color} {pieceName} enters home straight: {ringPosition} -> home[{homePathPosition}] (rolled {diceValue}, moved {movedUnits} units, {stepsInHome} in home, {direction})");
        }
        
        public void LogMoveOnHomeStraight(Color color, string pieceName, int fromStep, int toStep, int units, Direction direction)
        {
            Log($"{color} {pieceName} on home straight: home[{fromStep}] -> home[{toStep}] ({units} units, {direction})");
        }
        
        public void LogMoveOnHomeStraightDetailed(Color color, string pieceName, int fromStep, int toStep, int diceValue, int movedUnits, Direction direction)
        {
            Log($"{color} {pieceName} on home straight: home[{fromStep}] -> home[{toStep}] (rolled {diceValue}, moved {movedUnits} units, {direction})");
        }
        
        public void LogBlockedMove(Color color, string pieceName, int startPos, int endPos, Color blockingColor, string blockingPieceName)
        {
            Log($"{color} {pieceName} blocked at {endPos} by {blockingColor} {blockingPieceName}");
        }
        
        public void LogNoAlternativeMove(Color color)
        {
            Log($"{color} has no alternative move - turn ignored");
        }
        
        public void LogPartialMove(Color color, int partialPosition)
        {
            Log($"{color} partial move to {partialPosition}");
        }
        
        public void LogCapture(Color color, string pieceName, int position, Color capturedColor, string capturedPieceName)
        {
            Log($"{color} {pieceName} captures {capturedColor} {capturedPieceName} at {position}");
        }
        
        public void LogMysterySpawn(int location, int rounds)
        {
            Log($"Mystery cell spawned at {location} for {rounds} rounds");
        }
        
        public void LogPlacement(Color color, int place)
        {
            Log($"{color} finishes in {place} place");
        }
        
        public void LogWinner(Color color, int place)
        {
            Log($"{color} is the winner ({place} place)");
        }
        
        public void LogFinalStandings(IReadOnlyList<(Color color, int place)> standings)
        {
            var standingsText = string.Join(", ", standings.Select(s => $"{s.color} ({s.place})"));
            Log($"Final standings: {standingsText}");
        }
        
        public void LogRoundStatusHeader(Color color)
        {
            Log($"--- {color} Status ---");
        }
        
        public void LogPieceLocation(string pieceName, string location)
        {
            Log($"{pieceName}: {location}");
        }
        
        public void LogMysteryCellStatus(int location, int roundsRemaining)
        {
            Log($"Mystery cell at {location}, {roundsRemaining} rounds remaining");
        }
        
        public void LogMysteryCellLanding(Color color, string locationName)
        {
            Log($"{color} lands on mystery cell at {locationName}");
        }
        
        public void LogMysteryCellTeleport(Color color, string pieceName, TeleportLocation location)
        {
            Log($"{color} {pieceName} teleported to {location}");
        }
        
        public void LogEnergizedEffect(Color color, string pieceName)
        {
            Log($"{color} {pieceName} is energized");
        }
        
        public void LogSickEffect(Color color, string pieceName)
        {
            Log($"{color} {pieceName} is sick");
        }
        
        public void LogBriefingEffect(Color color, string pieceName)
        {
            Log($"{color} {pieceName} is being briefed");
        }
        
        public void LogBriefingEscape(Color color, string pieceName)
        {
            Log($"{color} {pieceName} escapes briefing");
        }
        
        public void LogDirectionChange(Color color, string pieceName)
        {
            Log($"{color} {pieceName} changes direction");
        }
        
        public void LogGammaToBeta(Color color, string pieceName)
        {
            Log($"{color} {pieceName} moves from Gamma to Beta");
        }
        
        public void LogConsecutiveSixesIgnored(Color color)
        {
            Log($"{color} ignores turn due to consecutive sixes");
        }
    }
}
