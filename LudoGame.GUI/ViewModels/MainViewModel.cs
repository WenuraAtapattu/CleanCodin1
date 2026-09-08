using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using LudoGame.GUI.Services;
using LudoGame.GUI.Models;

namespace LudoGame.GUI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _apiClient;
        private readonly SignalRClient _signalRClient;
        private string _currentPlayer = "Not Started";
        private int _gameRound = 0;
        private string _gameState = "Ready";
        private string _gameLog = "";
        private bool _isGameRunning = false;
        private bool _isPaused = false;
        private bool _isServerConnected = false;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public string CurrentPlayer
        {
            get => _currentPlayer;
            set
            {
                _currentPlayer = value;
                OnPropertyChanged();
            }
        }
        
        public int GameRound
        {
            get => _gameRound;
            set
            {
                _gameRound = value;
                OnPropertyChanged();
            }
        }
        
        public string GameState
        {
            get => _gameState;
            set
            {
                _gameState = value;
                OnPropertyChanged();
            }
        }
        
        public string GameLog
        {
            get => _gameLog;
            set
            {
                _gameLog = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsGameRunning
        {
            get => _isGameRunning;
            set
            {
                _isGameRunning = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsServerConnected
        {
            get => _isServerConnected;
            set
            {
                _isServerConnected = value;
                OnPropertyChanged();
            }
        }
        
        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand StepCommand { get; }
        public ICommand ConnectCommand { get; }
        
        public MainViewModel()
        {
            _apiClient = new ApiClient();
            _signalRClient = new SignalRClient();
            
            StartCommand = new RelayCommand(StartGame, CanStartGame);
            PauseCommand = new RelayCommand(TogglePause, CanTogglePause);
            ResetCommand = new RelayCommand(ResetGame, CanResetGame);
            StepCommand = new RelayCommand(StepGame, CanStepGame);
            ConnectCommand = new RelayCommand(ConnectToServer, CanConnectToServer);
            
            // Set up SignalR event handlers
            _signalRClient.OnConnected += (message) =>
            {
                IsServerConnected = true;
                LogMessage($"Connected to server: {message}");
            };
            
            _signalRClient.OnGameStateUpdated += (state) =>
            {
                LogMessage("Game state updated from server");
            };
            
            _signalRClient.OnPlayerMoved += (moveData) =>
            {
                LogMessage("Player move update received from server");
            };
            
            LogMessage("LUDO-T Game Client Ready");
            LogMessage("Click 'Connect to Server' to connect to the game server");
        }
        
        private bool CanStartGame(object? parameter) => !IsGameRunning && IsServerConnected;
        
        private async void StartGame(object? parameter)
        {
            try
            {
                LogMessage("Starting LUDO-T Game Simulation...");
                
                var result = await _apiClient.StartGameAsync();
                
                if (result.Success)
                {
                    IsGameRunning = true;
                    IsPaused = false;
                    GameState = "Running";
                    LogMessage("Game started successfully");
                    
                    // Start periodic game state updates
                    _ = StartGameStateUpdatesAsync();
                }
                else
                {
                    LogMessage($"Failed to start game: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error starting game: {ex.Message}");
            }
        }
        
        private bool CanTogglePause(object? parameter) => IsGameRunning && IsServerConnected;
        
        private async void TogglePause(object? parameter)
        {
            try
            {
                var result = IsPaused 
                    ? await _apiClient.ResumeGameAsync() 
                    : await _apiClient.PauseGameAsync();
                
                if (result.Success)
                {
                    IsPaused = !IsPaused;
                    GameState = IsPaused ? "Paused" : "Running";
                    LogMessage(IsPaused ? "Game paused" : "Game resumed");
                }
                else
                {
                    LogMessage($"Failed to toggle pause: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error toggling pause: {ex.Message}");
            }
        }
        
        private bool CanResetGame(object? parameter) => IsServerConnected;
        
        private async void ResetGame(object? parameter)
        {
            try
            {
                var result = await _apiClient.ResetGameAsync();
                
                if (result.Success)
                {
                    IsGameRunning = false;
                    IsPaused = false;
                    GameRound = 0;
                    CurrentPlayer = "Not Started";
                    GameState = "Ready";
                    GameLog = "";
                    
                    LogMessage("Game reset. Ready to start new game.");
                }
                else
                {
                    LogMessage($"Failed to reset game: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error resetting game: {ex.Message}");
            }
        }
        
        private bool CanStepGame(object? parameter) => IsGameRunning && IsServerConnected;
        
        private async void StepGame(object? parameter)
        {
            try
            {
                var result = await _apiClient.ExecuteStepAsync();
                
                if (result.Success)
                {
                    LogMessage("Game step executed successfully");
                    
                    // Update game state from server response
                    if (result.GameState != null)
                    {
                        CurrentPlayer = result.GameState.CurrentPlayer ?? "Unknown";
                        GameRound = result.GameState.CurrentRound;
                    }
                }
                else
                {
                    LogMessage($"Failed to execute step: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error executing step: {ex.Message}");
            }
        }
        
        private bool CanConnectToServer(object? parameter) => !IsServerConnected;
        
        private async void ConnectToServer(object? parameter)
        {
            try
            {
                LogMessage("Connecting to game server...");
                
                // Check server health
                bool isHealthy = await _apiClient.CheckServerHealthAsync();
                
                if (isHealthy)
                {
                    // Connect to SignalR for real-time updates
                    await _signalRClient.ConnectAsync();
                    
                    LogMessage("Successfully connected to game server");
                }
                else
                {
                    LogMessage("Server health check failed");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error connecting to server: {ex.Message}");
            }
        }
        
        private async Task StartGameStateUpdatesAsync()
        {
            while (IsGameRunning && IsServerConnected)
            {
                try
                {
                    var state = await _apiClient.GetGameStateAsync();
                    
                    // Update UI with current game state
                    CurrentPlayer = state.CurrentPlayer ?? "Unknown";
                    GameRound = state.CurrentRound;
                    GameState = state.GameState;
                    
                    await Task.Delay(500); // Update every 500ms
                }
                catch (Exception ex)
                {
                    LogMessage($"Error getting game state: {ex.Message}");
                    await Task.Delay(2000); // Wait longer on error
                }
            }
        }
        
        public void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            GameLog += $"[{timestamp}] {message}\n";
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool> _canExecute;
        
        public event EventHandler? CanExecuteChanged;
        
        public RelayCommand(Action<object?> execute, Func<object?, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter)
        {
            return _canExecute(parameter);
        }
        
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
        
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
