using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using LudoGame.GUI.Services;

namespace LudoGame.GUI.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private DispatcherTimer? _uiUpdateTimer;
        
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            InitializeGameBoard();
            StartUIUpdateTimer();
        }
        
        private void InitializeGameBoard()
        {
            DrawGameBoard();
            _viewModel.LogMessage("LUDO-T Game Client Ready");
            _viewModel.LogMessage("Click 'Connect to Server' to connect to the game server");
        }
        
        private void StartUIUpdateTimer()
        {
            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Update every 100ms
            };
            _uiUpdateTimer.Tick += (sender, e) => UpdateUIFromViewModel();
            _uiUpdateTimer.Start();
        }
        
        private void UpdateUIFromViewModel()
        {
            if (!IsLoaded) return;
            
            try
            {
                // Update log display
                if (GameLogTextBox.Text != _viewModel.GameLog)
                {
                    GameLogTextBox.Text = _viewModel.GameLog;
                    GameLogTextBox.ScrollToEnd();
                }
                
                // Update other UI elements
                if (CurrentPlayerText.Text != _viewModel.CurrentPlayer)
                    CurrentPlayerText.Text = _viewModel.CurrentPlayer;
                    
                if (GameRoundText.Text != _viewModel.GameRound.ToString())
                    GameRoundText.Text = _viewModel.GameRound.ToString();
                    
                if (GameStateText.Text != _viewModel.GameState)
                    GameStateText.Text = _viewModel.GameState;
                
                // Update server status
                bool isConnected = _viewModel.IsServerConnected;
                string statusText = isConnected ? "Connected" : "Disconnected";
                if (ServerStatusText.Text != statusText)
                {
                    ServerStatusText.Text = statusText;
                    ServerStatusText.Foreground = isConnected 
                        ? new SolidColorBrush(Colors.Green) 
                        : new SolidColorBrush(Colors.Red);
                }
            }
            catch
            {
                // Ignore UI update errors
            }
        }
        
        private void DrawGameBoard()
        {
            GameBoardCanvas.Children.Clear();
            
            double centerX = 300;
            double centerY = 300;
            double radius = 200;
            
            for (int i = 0; i < 52; i++)
            {
                double angle = (i * 360.0 / 52.0) - 90;
                double angleRad = angle * Math.PI / 180.0;
                
                double x = centerX + radius * Math.Cos(angleRad);
                double y = centerY + radius * Math.Sin(angleRad);
                
                Ellipse cell = new Ellipse
                {
                    Width = 30,
                    Height = 30,
                    Fill = GetCellColor(i),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                
                Canvas.SetLeft(cell, x - 15);
                Canvas.SetTop(cell, y - 15);
                GameBoardCanvas.Children.Add(cell);
                
                TextBlock cellNumber = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black,
                    TextAlignment = TextAlignment.Center
                };
                
                Canvas.SetLeft(cellNumber, x - 7);
                Canvas.SetTop(cellNumber, y - 7);
                GameBoardCanvas.Children.Add(cellNumber);
            }
            
            DrawHomeBase(centerX - 150, centerY - 150, Colors.Red, "Red");
            DrawHomeBase(centerX + 150, centerY - 150, Colors.Green, "Green");
            DrawHomeBase(centerX + 150, centerY + 150, Colors.Yellow, "Yellow");
            DrawHomeBase(centerX - 150, centerY + 150, Colors.Blue, "Blue");
            
            Ellipse centerHome = new Ellipse
            {
                Width = 80,
                Height = 80,
                Fill = Brushes.LightGray,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            
            Canvas.SetLeft(centerHome, centerX - 40);
            Canvas.SetTop(centerHome, centerY - 40);
            GameBoardCanvas.Children.Add(centerHome);
            
            TextBlock centerLabel = new TextBlock
            {
                Text = "HOME",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            
            Canvas.SetLeft(centerLabel, centerX - 20);
            Canvas.SetTop(centerLabel, centerY - 10);
            GameBoardCanvas.Children.Add(centerLabel);
        }
        
        private void DrawHomeBase(double x, double y, Color color, string colorName)
        {
            Rectangle baseRect = new Rectangle
            {
                Width = 80,
                Height = 80,
                Fill = new SolidColorBrush(Color.FromArgb(200, color.R, color.G, color.B)),
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                RadiusX = 10,
                RadiusY = 10
            };
            
            Canvas.SetLeft(baseRect, x - 40);
            Canvas.SetTop(baseRect, y - 40);
            GameBoardCanvas.Children.Add(baseRect);
            
            TextBlock label = new TextBlock
            {
                Text = colorName,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            
            Canvas.SetLeft(label, x - 20);
            Canvas.SetTop(label, y - 10);
            GameBoardCanvas.Children.Add(label);
        }
        
        private Brush GetCellColor(int cellIndex)
        {
            return cellIndex switch
            {
                >= 0 and <= 12 => new SolidColorBrush(Color.FromArgb(255, 255, 200, 200)),
                >= 13 and <= 25 => new SolidColorBrush(Color.FromArgb(255, 200, 255, 200)),
                >= 26 and <= 38 => new SolidColorBrush(Color.FromArgb(255, 255, 255, 200)),
                >= 39 and <= 51 => new SolidColorBrush(Color.FromArgb(255, 200, 200, 255)),
                _ => Brushes.White
            };
        }
        
        protected override void OnClosed(EventArgs e)
        {
            _uiUpdateTimer?.Stop();
            base.OnClosed(e);
        }
    }
}