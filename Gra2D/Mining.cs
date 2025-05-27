using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;

namespace Gra2D
{
    public class Mining
    {
        private readonly GameConfig config;
        private readonly Image playerImage;
        private readonly int[,] map;
        private readonly Image[,] terrainImages;
        private readonly BitmapImage[] terrainBitmaps;
        private readonly Action<int> updateWoodCount;
        private readonly Action<int> updateHealth;
        private readonly Grid gameGrid;
        private readonly Func<(int X, int Y)> getPlayerPosition;
        private Rectangle? progressBar;
        private bool isChopping = false;
        private CancellationTokenSource? chopCancellationTokenSource;

        public Mining(GameConfig config, Image playerImage, int[,] map, Image[,] terrainImages,
            BitmapImage[] terrainBitmaps, Action<int> updateWoodCount, Action<int> updateHealth,
            Grid gameGrid, Func<(int X, int Y)> getPlayerPosition)
        {
            this.config = config;
            this.playerImage = playerImage;
            this.map = map;
            this.terrainImages = terrainImages;
            this.terrainBitmaps = terrainBitmaps;
            this.updateWoodCount = updateWoodCount;
            this.updateHealth = updateHealth;
            this.gameGrid = gameGrid;
            this.getPlayerPosition = getPlayerPosition;
        }

        public async Task ChopTree(int playerX, int playerY)
        {
            if (isChopping || (map[playerY, playerX] != MapGenerator.LAS && map[playerY, playerX] != MapGenerator.LASLECZNICZY))
                return;

            isChopping = true;
            chopCancellationTokenSource = new CancellationTokenSource();
            ShowProgressBar(playerX, playerY);

            double elapsed = 0;
            bool isHealingForest = map[playerY, playerX] == MapGenerator.LASLECZNICZY;

            try
            {
                while (elapsed < config.TreeChopTime)
                {
                    await Task.Delay(50, chopCancellationTokenSource.Token);
                    var (currentX, currentY) = getPlayerPosition();
                    if (currentX != playerX || currentY != playerY)
                    {
                        RemoveProgressBar();
                        isChopping = false;
                        return;
                    }

                    elapsed += 0.05;
                    UpdateProgressBar(elapsed / config.TreeChopTime);
                }

                if (map[playerY, playerX] == MapGenerator.LAS || map[playerY, playerX] == MapGenerator.LASLECZNICZY)
                {
                    map[playerY, playerX] = MapGenerator.LAKA;
                    terrainImages[playerY, playerX].Source = terrainBitmaps[MapGenerator.LAKA];
                    updateWoodCount(1);
                    if (isHealingForest)
                    {
                        updateHealth(config.HealingForestHealthGain);
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                RemoveProgressBar();
                isChopping = false;
                chopCancellationTokenSource?.Dispose();
                chopCancellationTokenSource = null;
            }
        }

        public void CancelChopping()
        {
            if (isChopping && chopCancellationTokenSource != null)
            {
                chopCancellationTokenSource.Cancel();
            }
        }

        private void ShowProgressBar(int playerX, int playerY)
        {
            progressBar = new Rectangle
            {
                Width = config.SegmentSize,
                Height = config.ProgressBarHeight,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(config.ProgressBarColor)
            };
            Grid.SetRow(progressBar, playerY);
            Grid.SetColumn(progressBar, playerX);
            Grid.SetZIndex(progressBar, 2);
            Panel.SetZIndex(playerImage, 1);
            gameGrid.Children.Add(progressBar);
        }

        private void UpdateProgressBar(double progress)
        {
            if (progressBar != null)
            {
                progressBar.Width = config.SegmentSize * progress;
            }
        }

        private void RemoveProgressBar()
        {
            if (progressBar != null)
            {
                gameGrid.Children.Remove(progressBar);
                progressBar = null;
            }
        }
    }
}