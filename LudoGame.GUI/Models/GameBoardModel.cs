using System;
using System.Collections.ObjectModel;
using System.Linq;
using LudoGame;

namespace LudoGame.GUI.Models
{
    public class GameBoardModel
    {
        public ObservableCollection<CellModel> Cells { get; }
        public ObservableCollection<PieceModel> Pieces { get; }
        public ObservableCollection<HomeBaseModel> HomeBases { get; }
        
        public GameBoardModel()
        {
            Cells = new ObservableCollection<CellModel>();
            Pieces = new ObservableCollection<PieceModel>();
            HomeBases = new ObservableCollection<HomeBaseModel>();
            
            InitializeBoard();
        }
        
        private void InitializeBoard()
        {
            // Initialize the 52 cells of the board
            for (int i = 0; i < 52; i++)
            {
                Cells.Add(new CellModel
                {
                    Index = i,
                    Color = GetCellColor(i),
                    IsMysteryCell = false
                });
            }
            
            // Initialize home bases
            HomeBases.Add(new HomeBaseModel { Color = Color.Red, Name = "Red Base" });
            HomeBases.Add(new HomeBaseModel { Color = Color.Green, Name = "Green Base" });
            HomeBases.Add(new HomeBaseModel { Color = Color.Yellow, Name = "Yellow Base" });
            HomeBases.Add(new HomeBaseModel { Color = Color.Blue, Name = "Blue Base" });
        }
        
        private string GetCellColor(int cellIndex)
        {
            return cellIndex switch
            {
                >= 0 and <= 12 => "#FFC8C8", // Red section
                >= 13 and <= 25 => "#C8FFC8", // Green section
                >= 26 and <= 38 => "#FFFFC8", // Yellow section
                >= 39 and <= 51 => "#C8C8FF", // Blue section
                _ => "#FFFFFF"
            };
        }
        
        public void UpdateMysteryCell(int position, bool isActive)
        {
            if (position >= 0 && position < Cells.Count)
            {
                Cells[position].IsMysteryCell = isActive;
            }
        }
        
        public void UpdatePiecePosition(string pieceName, int cellIndex, Color color)
        {
            // Remove existing piece position
            var existingPiece = Pieces.FirstOrDefault(p => p.Name == pieceName);
            if (existingPiece != null)
            {
                Pieces.Remove(existingPiece);
            }
            
            // Add updated piece position
            Pieces.Add(new PieceModel
            {
                Name = pieceName,
                CellIndex = cellIndex,
                Color = color
            });
        }
    }
    
    public class CellModel
    {
        public int Index { get; set; }
        public string Color { get; set; } = "#FFFFFF";
        public bool IsMysteryCell { get; set; }
        public bool IsOccupied { get; set; }
    }
    
    public class PieceModel
    {
        public string Name { get; set; } = "";
        public int CellIndex { get; set; }
        public Color Color { get; set; }
        public PieceState State { get; set; }
    }
    
    public class HomeBaseModel
    {
        public Color Color { get; set; }
        public string Name { get; set; } = "";
        public int PiecesInBase { get; set; } = 4;
    }
}
