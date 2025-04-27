using System;
using System.Collections.Generic;

namespace Gra2D
{
    public class PuddleGenerator
    {
        private readonly GameConfig config;
        private readonly Random rnd;

        public PuddleGenerator(GameConfig config)
        {
            this.config = config;
            this.rnd = new Random();
        }

        public void GeneratePuddles(int[,] map, int rows, int columns)
        {
            if (config.DisableWaterPuddles || config.WaterPuddleCount <= 0 || config.WaterChance <= 0)
                return;

            int puddleCount = rnd.Next(1, config.WaterPuddleCount + 1);
            for (int p = 0; p < puddleCount; p++)
            {
                int puddleX = rnd.Next(0, columns);
                int puddleY = rnd.Next(0, rows);
                int maxPuddleSize = rnd.Next(5, 9);
                int puddleSize = (int)Math.Round(config.WaterChance / 100.0 * maxPuddleSize);
                puddleSize = Math.Max(1, Math.Min(maxPuddleSize, puddleSize));

                int tileType = MapGenerator.WODA;

                Queue<(int, int)> puddleQueue = new Queue<(int, int)>();
                HashSet<(int, int)> visited = new HashSet<(int, int)>();
                if (CanPlaceTile(tileType, map[puddleY, puddleX]))
                {
                    puddleQueue.Enqueue((puddleY, puddleX));
                    visited.Add((puddleY, puddleX));
                    map[puddleY, puddleX] = tileType;
                }
                else
                {
                    continue;
                }

                (int dy, int dx)[] directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
                int spreadChance = 75;

                while (puddleQueue.Count > 0 && puddleSize > 0)
                {
                    var (y, x) = puddleQueue.Dequeue();
                    List<(int, int)> neighbors = new List<(int, int)>();
                    foreach (var (dy, dx) in directions)
                    {
                        int newX = x + dx;
                        int newY = y + dy;
                        if (newX >= 0 && newX < columns && newY >= 0 && newY < rows &&
                            !visited.Contains((newY, newX)) && map[newY, newX] != tileType)
                        {
                            neighbors.Add((newY, newX));
                        }
                    }

                    for (int i = neighbors.Count - 1; i > 0; i--)
                    {
                        int j = rnd.Next(0, i + 1);
                        var temp = neighbors[i];
                        neighbors[i] = neighbors[j];
                        neighbors[j] = temp;
                    }

                    foreach (var (newY, newX) in neighbors)
                    {
                        if (rnd.Next(0, 100) < spreadChance && puddleSize > 0)
                        {
                            if (CanPlaceTile(tileType, map[newY, newX]))
                            {
                                map[newY, newX] = tileType;
                                puddleQueue.Enqueue((newY, newX));
                                visited.Add((newY, newX));
                                puddleSize--;
                            }
                        }
                    }
                }
            }
        }

        private bool CanPlaceTile(int newTile, int currentTile)
        {
            if (config.TileOnTopPriority.Length == 0)
                return true;

            int newTilePriority = Array.IndexOf(config.TileOnTopPriority, newTile);
            int currentTilePriority = Array.IndexOf(config.TileOnTopPriority, currentTile);

            if (newTilePriority == -1)
                return false;

            if (currentTilePriority == -1)
                return true;

            return newTilePriority < currentTilePriority;
        }
    }
}