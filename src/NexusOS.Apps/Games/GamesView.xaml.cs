using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NexusOS.Apps.Games;

public partial class GamesView : UserControl
{
    public GamesView()
    {
        InitializeComponent();
        InitializeMinesweeper();
        InitializeSnake();
        InitializeRunner();
        InitializeSudoku();
    }

    private void SwitchGame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string game)
        {
            MinesweeperContainer.Visibility = game == "Minesweeper" ? Visibility.Visible : Visibility.Collapsed;
            SnakeContainer.Visibility = game == "Snake" ? Visibility.Visible : Visibility.Collapsed;
            RunnerContainer.Visibility = game == "Runner" ? Visibility.Visible : Visibility.Collapsed;
            SudokuContainer.Visibility = game == "Sudoku" ? Visibility.Visible : Visibility.Collapsed;

            BtnMinesweeper.Style = TryFindResource(game == "Minesweeper" ? "NexusButtonPrimary" : "NexusButtonGhost") as Style;
            BtnSnake.Style = TryFindResource(game == "Snake" ? "NexusButtonPrimary" : "NexusButtonGhost") as Style;
            BtnRunner.Style = TryFindResource(game == "Runner" ? "NexusButtonPrimary" : "NexusButtonGhost") as Style;
            BtnSudoku.Style = TryFindResource(game == "Sudoku" ? "NexusButtonPrimary" : "NexusButtonGhost") as Style;

            if (game == "Snake") SnakeContainer.Focus();
            if (game == "Runner") RunnerContainer.Focus();
        }
    }

    #region 1. Minesweeper Game

    private const int GridRows = 9;
    private const int GridCols = 9;
    private const int TotalMines = 10;

    private readonly Button[,] _msButtons = new Button[GridRows, GridCols];
    private readonly bool[,] _msMines = new bool[GridRows, GridCols];
    private readonly bool[,] _msRevealed = new bool[GridRows, GridCols];
    private readonly bool[,] _msFlagged = new bool[GridRows, GridCols];
    private readonly int[,] _msNeighborCounts = new int[GridRows, GridCols];

    private DispatcherTimer? _msTimer;
    private int _msElapsedSeconds = 0;
    private bool _msGameOver = false;

    private void InitializeMinesweeper()
    {
        MinesweeperGrid.Children.Clear();
        _msTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _msTimer.Tick += (s, e) =>
        {
            _msElapsedSeconds++;
            MinesweeperTimerText.Text = _msElapsedSeconds.ToString("D3");
        };

        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                var row = r;
                var col = c;
                var btn = new Button
                {
                    Background = TryFindResource("NexusSurfaceHoverBrush") as Brush ?? Brushes.SlateGray,
                    BorderBrush = TryFindResource("NexusBorderBrush") as Brush ?? Brushes.DimGray,
                    BorderThickness = new Thickness(1),
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Padding = new Thickness(0),
                    Margin = new Thickness(1)
                };

                btn.Click += (s, e) => OnMinesweeperCellClick(row, col);
                btn.MouseRightButtonUp += (s, e) => OnMinesweeperCellRightClick(row, col);

                _msButtons[r, c] = btn;
                MinesweeperGrid.Children.Add(btn);
            }
        }

        ResetMinesweeper();
    }

    private void ResetMinesweeper()
    {
        _msTimer?.Stop();
        _msElapsedSeconds = 0;
        _msGameOver = false;
        MinesweeperTimerText.Text = "000";
        MinesRemainingText.Text = TotalMines.ToString("D3");
        MinesweeperResetButton.Content = "😊";

        // Clear boards
        Array.Clear(_msMines, 0, _msMines.Length);
        Array.Clear(_msRevealed, 0, _msRevealed.Length);
        Array.Clear(_msFlagged, 0, _msFlagged.Length);
        Array.Clear(_msNeighborCounts, 0, _msNeighborCounts.Length);

        // Place random mines
        var rand = new Random();
        int placed = 0;
        while (placed < TotalMines)
        {
            int r = rand.Next(GridRows);
            int c = rand.Next(GridCols);
            if (!_msMines[r, c])
            {
                _msMines[r, c] = true;
                placed++;
            }
        }

        // Calculate neighbor counts
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                if (_msMines[r, c]) continue;
                int count = 0;
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int nr = r + dr;
                        int nc = c + dc;
                        if (nr >= 0 && nr < GridRows && nc >= 0 && nc < GridCols && _msMines[nr, nc])
                        {
                            count++;
                        }
                    }
                }
                _msNeighborCounts[r, c] = count;
            }
        }

        // Reset UI buttons
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                var btn = _msButtons[r, c];
                btn.Content = "";
                btn.IsEnabled = true;
                btn.Background = TryFindResource("NexusSurfaceHoverBrush") as Brush ?? Brushes.SlateGray;
            }
        }
    }

    private void OnMinesweeperCellClick(int r, int c)
    {
        if (_msGameOver || _msFlagged[r, c] || _msRevealed[r, c]) return;

        if (_msElapsedSeconds == 0)
        {
            _msTimer?.Start();
        }

        if (_msMines[r, c])
        {
            // Hit a mine!
            _msGameOver = true;
            _msTimer?.Stop();
            MinesweeperResetButton.Content = "😵";
            RevealAllMines();
            return;
        }

        RevealCell(r, c);
        CheckMinesweeperWin();
    }

    private void RevealCell(int r, int c)
    {
        if (r < 0 || r >= GridRows || c < 0 || c >= GridCols || _msRevealed[r, c] || _msFlagged[r, c])
            return;

        _msRevealed[r, c] = true;
        var btn = _msButtons[r, c];
        btn.Background = TryFindResource("NexusSurfaceBrush") as Brush ?? Brushes.DarkSlateGray;

        int count = _msNeighborCounts[r, c];
        if (count > 0)
        {
            btn.Content = count.ToString();
            btn.Foreground = GetMinesweeperNumberColor(count);
        }
        else
        {
            btn.Content = "";
            // Cascade reveal
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr != 0 || dc != 0)
                    {
                        RevealCell(r + dr, c + dc);
                    }
                }
            }
        }
    }

    private void OnMinesweeperCellRightClick(int r, int c)
    {
        if (_msGameOver || _msRevealed[r, c]) return;

        _msFlagged[r, c] = !_msFlagged[r, c];
        _msButtons[r, c].Content = _msFlagged[r, c] ? "🚩" : "";
    }

    private void RevealAllMines()
    {
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                if (_msMines[r, c])
                {
                    _msButtons[r, c].Content = "💣";
                    _msButtons[r, c].Background = TryFindResource("NexusDangerBrush") as Brush ?? Brushes.Red;
                }
            }
        }
    }

    private void CheckMinesweeperWin()
    {
        int revealedCount = 0;
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                if (_msRevealed[r, c]) revealedCount++;
            }
        }

        if (revealedCount == (GridRows * GridCols - TotalMines))
        {
            _msGameOver = true;
            _msTimer?.Stop();
            MinesweeperResetButton.Content = "😎";
        }
    }

    private Brush GetMinesweeperNumberColor(int count)
    {
        return count switch
        {
            1 => Brushes.DodgerBlue,
            2 => Brushes.LimeGreen,
            3 => Brushes.Crimson,
            4 => Brushes.MediumPurple,
            5 => Brushes.Orange,
            _ => Brushes.Cyan
        };
    }

    private void MinesweeperReset_Click(object sender, RoutedEventArgs e)
    {
        ResetMinesweeper();
    }

    #endregion

    #region 2. Cyber Snake Game

    private const int SnakeTileSize = 20;
    private readonly DispatcherTimer _snakeTimer = new();
    private readonly List<Point> _snake = new();
    private Point _food;
    private Point _direction = new(1, 0);
    private int _snakeScore = 0;
    private bool _isSnakeRunning = false;

    private void InitializeSnake()
    {
        _snakeTimer.Interval = TimeSpan.FromMilliseconds(110);
        _snakeTimer.Tick += SnakeTimer_Tick;
    }

    private void SnakeStart_Click(object sender, RoutedEventArgs e)
    {
        StartSnakeGame();
    }

    private void StartSnakeGame()
    {
        _snake.Clear();
        _snakeScore = 0;
        SnakeScoreText.Text = "Score: 0";
        _direction = new Point(1, 0);

        _snake.Add(new Point(6, 5));
        _snake.Add(new Point(5, 5));
        _snake.Add(new Point(4, 5));

        SpawnFood();
        _isSnakeRunning = true;
        _snakeTimer.Start();
        SnakeContainer.Focus();
        DrawSnake();
    }

    private void SpawnFood()
    {
        var rand = new Random();
        int maxCols = (int)(SnakeCanvas.Width / SnakeTileSize);
        int maxRows = (int)(SnakeCanvas.Height / SnakeTileSize);

        Point p;
        do
        {
            p = new Point(rand.Next(maxCols), rand.Next(maxRows));
        } while (_snake.Contains(p));

        _food = p;
    }

    private void SnakeTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isSnakeRunning) return;

        var head = _snake[0];
        var newHead = new Point(head.X + _direction.X, head.Y + _direction.Y);

        int maxCols = (int)(SnakeCanvas.Width / SnakeTileSize);
        int maxRows = (int)(SnakeCanvas.Height / SnakeTileSize);

        // Wall collisions
        if (newHead.X < 0 || newHead.X >= maxCols || newHead.Y < 0 || newHead.Y >= maxRows || _snake.Contains(newHead))
        {
            _isSnakeRunning = false;
            _snakeTimer.Stop();
            MessageBox.Show($"Game Over! Your Score: {_snakeScore}", "Cyber Snake", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _snake.Insert(0, newHead);

        if (newHead == _food)
        {
            _snakeScore += 10;
            SnakeScoreText.Text = $"Score: {_snakeScore}";
            SpawnFood();
        }
        else
        {
            _snake.RemoveAt(_snake.Count - 1);
        }

        DrawSnake();
    }

    private void DrawSnake()
    {
        SnakeCanvas.Children.Clear();

        // Draw Food
        var foodRect = new Rectangle
        {
            Width = SnakeTileSize - 2,
            Height = SnakeTileSize - 2,
            Fill = TryFindResource("NexusDangerBrush") as Brush ?? Brushes.Red,
            RadiusX = 4,
            RadiusY = 4
        };
        Canvas.SetLeft(foodRect, _food.X * SnakeTileSize);
        Canvas.SetTop(foodRect, _food.Y * SnakeTileSize);
        SnakeCanvas.Children.Add(foodRect);

        // Draw Snake
        for (int i = 0; i < _snake.Count; i++)
        {
            var p = _snake[i];
            var segment = new Rectangle
            {
                Width = SnakeTileSize - 2,
                Height = SnakeTileSize - 2,
                Fill = i == 0 ? (TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue) : Brushes.MediumSeaGreen,
                RadiusX = 3,
                RadiusY = 3
            };
            Canvas.SetLeft(segment, p.X * SnakeTileSize);
            Canvas.SetTop(segment, p.Y * SnakeTileSize);
            SnakeCanvas.Children.Add(segment);
        }
    }

    private void Snake_KeyDown(object sender, KeyEventArgs e)
    {
        if (!_isSnakeRunning) return;

        if ((e.Key == Key.Up || e.Key == Key.W) && _direction.Y != 1) _direction = new Point(0, -1);
        else if ((e.Key == Key.Down || e.Key == Key.S) && _direction.Y != -1) _direction = new Point(0, 1);
        else if ((e.Key == Key.Left || e.Key == Key.A) && _direction.X != 1) _direction = new Point(-1, 0);
        else if ((e.Key == Key.Right || e.Key == Key.D) && _direction.X != -1) _direction = new Point(1, 0);
    }

    #endregion

    #region 3. 2D Cyber Runner Game

    private readonly DispatcherTimer _runnerTimer = new();
    private double _playerY = 0;
    private double _playerVelocityY = 0;
    private const double Gravity = 1.2;
    private const double JumpVelocity = -15;
    private const double GroundY = 240;
    private double _distance = 0;
    private readonly List<Rect> _obstacles = new();
    private readonly Random _runnerRand = new();

    private void InitializeRunner()
    {
        _runnerTimer.Interval = TimeSpan.FromMilliseconds(30);
        _runnerTimer.Tick += RunnerTimer_Tick;
        ResetRunner();
    }

    private void ResetRunner()
    {
        _playerY = GroundY;
        _playerVelocityY = 0;
        _distance = 0;
        _obstacles.Clear();
        _obstacles.Add(new Rect(500, GroundY, 20, 30));
        _runnerTimer.Start();
    }

    private void RunnerTimer_Tick(object? sender, EventArgs e)
    {
        _distance += 1;
        RunnerScoreText.Text = $"Distance: {_distance:0}m";

        // Physics
        _playerVelocityY += Gravity;
        _playerY += _playerVelocityY;
        if (_playerY >= GroundY)
        {
            _playerY = GroundY;
            _playerVelocityY = 0;
        }

        // Move obstacles
        for (int i = 0; i < _obstacles.Count; i++)
        {
            var obs = _obstacles[i];
            obs.X -= 6;
            _obstacles[i] = obs;
        }

        if (_obstacles.Count > 0 && _obstacles[0].X < -30)
        {
            _obstacles.RemoveAt(0);
        }

        if (_obstacles.Count == 0 || _obstacles.Last().X < 350)
        {
            _obstacles.Add(new Rect(620 + _runnerRand.Next(50, 150), GroundY, 20, 30));
        }

        // Collision check
        var playerBox = new Rect(80, _playerY, 24, 30);
        foreach (var obs in _obstacles)
        {
            if (playerBox.IntersectsWith(obs))
            {
                _runnerTimer.Stop();
                MessageBox.Show($"Crash! Final Distance: {_distance:0}m", "Cyber Runner 2D", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetRunner();
                return;
            }
        }

        DrawRunner();
    }

    private void DrawRunner()
    {
        RunnerCanvas.Children.Clear();

        // Ground line
        var ground = new Line
        {
            X1 = 0,
            Y1 = GroundY + 30,
            X2 = RunnerCanvas.Width,
            Y2 = GroundY + 30,
            Stroke = TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue,
            StrokeThickness = 3
        };
        RunnerCanvas.Children.Add(ground);

        // Player
        var player = new Rectangle
        {
            Width = 24,
            Height = 30,
            Fill = TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue,
            RadiusX = 4,
            RadiusY = 4
        };
        Canvas.SetLeft(player, 80);
        Canvas.SetTop(player, _playerY);
        RunnerCanvas.Children.Add(player);

        // Obstacles
        foreach (var obs in _obstacles)
        {
            var obstacleRect = new Rectangle
            {
                Width = obs.Width,
                Height = obs.Height,
                Fill = TryFindResource("NexusDangerBrush") as Brush ?? Brushes.Red,
                RadiusX = 3,
                RadiusY = 3
            };
            Canvas.SetLeft(obstacleRect, obs.X);
            Canvas.SetTop(obstacleRect, obs.Y);
            RunnerCanvas.Children.Add(obstacleRect);
        }
    }

    private void RunnerJump_Click(object sender, RoutedEventArgs e)
    {
        JumpRunner();
    }

    private void Runner_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space || e.Key == Key.Up)
        {
            JumpRunner();
        }
    }

    private void JumpRunner()
    {
        if (_playerY >= GroundY - 1)
        {
            _playerVelocityY = JumpVelocity;
        }
    }

    #endregion

    #region 4. Sudoku Game

    private readonly Button[,] _sudokuButtons = new Button[9, 9];
    private readonly int[,] _sudokuBoard = new int[9, 9];
    private (int Row, int Col)? _selectedSudokuCell;

    private void InitializeSudoku()
    {
        SudokuGrid.Children.Clear();

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                var row = r;
                var col = c;
                var btn = new Button
                {
                    Background = TryFindResource("NexusSurfaceBrush") as Brush ?? Brushes.DarkSlateGray,
                    BorderBrush = TryFindResource("NexusBorderSubtleBrush") as Brush ?? Brushes.DimGray,
                    BorderThickness = new Thickness(1),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(0)
                };

                btn.Click += (s, e) =>
                {
                    _selectedSudokuCell = (row, col);
                    HighlightSudokuSelection();
                };

                _sudokuButtons[r, c] = btn;
                SudokuGrid.Children.Add(btn);
            }
        }

        GenerateSudokuPuzzle();
    }

    private void GenerateSudokuPuzzle()
    {
        // Simple valid playable puzzle pattern
        int[,] defaultPuzzle = {
            { 5, 3, 0, 0, 7, 0, 0, 0, 0 },
            { 6, 0, 0, 1, 9, 5, 0, 0, 0 },
            { 0, 9, 8, 0, 0, 0, 0, 6, 0 },
            { 8, 0, 0, 0, 6, 0, 0, 0, 3 },
            { 4, 0, 0, 8, 0, 3, 0, 0, 1 },
            { 7, 0, 0, 0, 2, 0, 0, 0, 6 },
            { 0, 6, 0, 0, 0, 0, 2, 8, 0 },
            { 0, 0, 0, 4, 1, 9, 0, 0, 5 },
            { 0, 0, 0, 0, 8, 0, 0, 7, 9 }
        };

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                _sudokuBoard[r, c] = defaultPuzzle[r, c];
                var btn = _sudokuButtons[r, c];
                int val = defaultPuzzle[r, c];
                btn.Content = val > 0 ? val.ToString() : "";
                btn.Foreground = val > 0
                    ? (TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue)
                    : (TryFindResource("NexusTextPrimaryBrush") as Brush ?? Brushes.White);
            }
        }
    }

    private void HighlightSudokuSelection()
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                var isSelected = _selectedSudokuCell.HasValue && _selectedSudokuCell.Value.Row == r && _selectedSudokuCell.Value.Col == c;
                _sudokuButtons[r, c].Background = isSelected
                    ? (TryFindResource("NexusAccentPressedBrush") as Brush ?? Brushes.SteelBlue)
                    : (TryFindResource("NexusSurfaceBrush") as Brush ?? Brushes.DarkSlateGray);
            }
        }
    }

    private void SudokuDigit_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSudokuCell.HasValue && sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int digit))
        {
            var (r, c) = _selectedSudokuCell.Value;
            _sudokuBoard[r, c] = digit;
            _sudokuButtons[r, c].Content = digit.ToString();
            _sudokuButtons[r, c].Foreground = TryFindResource("NexusSuccessBrush") as Brush ?? Brushes.LightGreen;
        }
    }

    private void SudokuClearCell_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSudokuCell.HasValue)
        {
            var (r, c) = _selectedSudokuCell.Value;
            _sudokuBoard[r, c] = 0;
            _sudokuButtons[r, c].Content = "";
        }
    }

    private void SudokuNewGame_Click(object sender, RoutedEventArgs e)
    {
        GenerateSudokuPuzzle();
    }

    #endregion
}
