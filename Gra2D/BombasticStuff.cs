using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public class BombasticStuff
    {
        private readonly GameConfig config;
        private readonly int[,] mapa;
        private readonly Image[,] tablicaTerenu;
        private readonly BitmapImage[] obrazyTerenu;
        private readonly PlayerMovement playerMovement;
        private readonly Grid gameGrid;
        private readonly Action<int> applyDamage;
        private readonly Dictionary<(int X, int Y), CancellationTokenSource> bombTokens = new Dictionary<(int X, int Y), CancellationTokenSource>();
        private readonly List<Task> activeBombTasks = new List<Task>();

        public BombasticStuff(GameConfig config, int[,] mapa, Image[,] tablicaTerenu, BitmapImage[] obrazyTerenu,
            PlayerMovement playerMovement, Grid gameGrid, Action<int> applyDamage)
        {
            this.config = config;
            this.mapa = mapa;
            this.tablicaTerenu = tablicaTerenu;
            this.obrazyTerenu = obrazyTerenu;
            this.playerMovement = playerMovement;
            this.gameGrid = gameGrid;
            this.applyDamage = applyDamage;
        }

        public void PlaceBomb(int bombaX, int bombaY)
        {
            mapa[bombaY, bombaX] = 4;
            tablicaTerenu[bombaY, bombaX].Source = obrazyTerenu[4];

            var cts = new CancellationTokenSource();
            bombTokens[(bombaX, bombaY)] = cts;
            var bombTask = Task.Delay(config.BombExplosionDelay, cts.Token).ContinueWith(async task =>
            {
                if (cts.Token.IsCancellationRequested)
                    return;

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    bombTokens.Remove((bombaX, bombaY));
                    HashSet<(int X, int Y)> damagedByBombs = new HashSet<(int X, int Y)>();
                    await ExplodeBombAsync(bombaX, bombaY, damagedByBombs);
                });
            }, cts.Token);
            activeBombTasks.Add(bombTask);
        }

        public void ClearBombs()
        {
            foreach (var token in bombTokens.Values)
            {
                token.Cancel();
            }
            try
            {
                Task.WhenAll(activeBombTasks).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
            }
            bombTokens.Clear();
            activeBombTasks.Clear();
        }

        private async Task ExplodeBombAsync(int bombaX, int bombaY, HashSet<(int X, int Y)> damagedByBombs)
        {
            List<(int X, int Y)> bombsToExplode = new List<(int X, int Y)>();
            HashSet<(int X, int Y)> processedBombs = new HashSet<(int X, int Y)>();
            processedBombs.Add((bombaX, bombaY));

            for (int y = -config.BombBlastRadius; y <= config.BombBlastRadius; y++)
            {
                for (int x = -config.BombBlastRadius; x <= config.BombBlastRadius; x++)
                {
                    int nowyX = bombaX + x;
                    int nowyY = bombaY + y;

                    if (nowyX >= 0 && nowyX < mapa.GetLength(1) && nowyY >= 0 && nowyY < mapa.GetLength(0))
                    {
                        if (mapa[nowyY, nowyX] == 4 && !processedBombs.Contains((nowyX, nowyY)))
                        {
                            bombsToExplode.Add((nowyX, nowyY));
                            processedBombs.Add((nowyX, nowyY));
                            if (bombTokens.ContainsKey((nowyX, nowyY)))
                            {
                                bombTokens[(nowyX, nowyY)].Cancel();
                                bombTokens.Remove((nowyX, nowyY));
                            }
                        }
                        else if (mapa[nowyY, nowyX] != MapGenerator.WODA)
                        {
                            mapa[nowyY, nowyX] = config.BombExplosionTile;
                            tablicaTerenu[nowyY, nowyX].Source = obrazyTerenu[config.BombExplosionTile];
                        }

                        var (playerX, playerY) = playerMovement.GetPlayerPosition();
                        if (playerX == nowyX && playerY == nowyY && !damagedByBombs.Contains((bombaX, bombaY)))
                        {
                            damagedByBombs.Add((bombaX, bombaY));
                            applyDamage(config.BombDamage);
                        }
                    }
                }
            }

            foreach (var (x, y) in bombsToExplode)
            {
                mapa[y, x] = config.BombExplosionTile;
                tablicaTerenu[y, x].Source = obrazyTerenu[config.BombExplosionTile];
                await ExplodeBombAsync(x, y, damagedByBombs);
            }
        }
    }
}