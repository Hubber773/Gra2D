using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Gra2D
{
    public class PlayerMovement
    {
        private readonly GameConfig config;
        private readonly Image playerImage;
        private readonly int[,] map;
        private readonly int mapWidth;
        private readonly int mapHeight;
        private int playerX;
        private int playerY;
        private DateTime lastMoveTime;
        private bool[] keyStates = new bool[4]; // W, A, S, D
        private DispatcherTimer movementTimer;

        public PlayerMovement(GameConfig config, Image playerImage, int[,] map, int mapWidth, int mapHeight)
        {
            this.config = config;
            this.playerImage = playerImage;
            this.map = map;
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
            playerX = mapWidth / 2;
            playerY = mapHeight / 2;
            lastMoveTime = DateTime.MinValue;
            UpdatePlayerPosition();

            movementTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            movementTimer.Tick += (s, e) => TryMove();
            movementTimer.Start();
        }

        public (int X, int Y) GetPlayerPosition() => (playerX, playerY);

        public void HandleKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.W) keyStates[0] = true;
            else if (e.Key == Key.A) keyStates[1] = true;
            else if (e.Key == Key.S) keyStates[2] = true;
            else if (e.Key == Key.D) keyStates[3] = true;
        }

        public void HandleKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.W) keyStates[0] = false;
            else if (e.Key == Key.A) keyStates[1] = false;
            else if (e.Key == Key.S) keyStates[2] = false;
            else if (e.Key == Key.D) keyStates[3] = false;
        }

        public bool TryMove()
        {
            if ((DateTime.Now - lastMoveTime).TotalSeconds < config.MovementCooldown)
                return false;

            int newX = playerX;
            int newY = playerY;

            if (keyStates[0]) newY--;
            else if (keyStates[2]) newY++;
            if (keyStates[1]) newX--;
            else if (keyStates[3]) newX++;

            if (newX == playerX && newY == playerY)
                return false;

            if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight)
            {
                if (map[newY, newX] != MapGenerator.WODA)
                {
                    playerX = newX;
                    playerY = newY;
                    lastMoveTime = DateTime.Now;
                    UpdatePlayerPosition();
                    return true;
                }
            }
            return false;
        }

        private void UpdatePlayerPosition()
        {
            Grid.SetRow(playerImage, playerY);
            Grid.SetColumn(playerImage, playerX);
        }
    }
}