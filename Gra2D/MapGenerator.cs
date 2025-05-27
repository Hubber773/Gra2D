using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace Gra2D
{
    public class MapGenerator
    {
        private readonly GameConfig config;
        private StringBuilder generatedMapSB = new StringBuilder();
        public const int LAS = 1;
        public const int LAKA = 2;
        public const int SKALA = 3;
        public const int WODA = 6;
        public const int LASLECZNICZY = 7;

        public MapGenerator(GameConfig config)
        {
            this.config = config;
        }

        public StringBuilder? GenerateMap(int rows, int columns)
        {
            Random rnd = new Random();
            rows = rows > 0 && rows <= config.MaxRows ? rows : config.MaxRows;
            columns = columns > 0 && columns <= config.MaxColumns ? columns : config.MaxColumns;

            if (rows > config.MaxRows || columns > config.MaxColumns)
            {
                MessageBox.Show($"Za duża liczba wierszy/kolumn (max {config.MaxRows} wierszy, {config.MaxColumns} kolumn)");
                return null;
            }

            int[,] tempMapa = new int[rows, columns];
            int totalChance = config.ForestChance + config.MeadowChance + config.RockChance;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (totalChance == 0)
                    {
                        tempMapa[i, j] = config.DefaultTileType;
                    }
                    else
                    {
                        int roll = rnd.Next(0, totalChance);
                        if (roll < config.ForestChance)
                            tempMapa[i, j] = LAS;
                        else if (roll < config.ForestChance + config.MeadowChance)
                            tempMapa[i, j] = LAKA;
                        else
                            tempMapa[i, j] = SKALA;
                    }
                }
            }

            PuddleGenerator puddleGenerator = new PuddleGenerator(config);
            puddleGenerator.GeneratePuddles(tempMapa, rows, columns);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (rnd.Next(0, 100) < config.HealingForestChance)
                    {
                        if (CanPlaceTile(LASLECZNICZY, tempMapa[i, j]))
                        {
                            tempMapa[i, j] = LASLECZNICZY;
                        }
                    }
                }
            }

            int centerY = rows / 2;
            int centerX = columns / 2;
            for (int i = centerY - config.PlayerSpawnRadius; i <= centerY + config.PlayerSpawnRadius; i++)
            {
                for (int j = centerX - config.PlayerSpawnRadius; j <= centerX + config.PlayerSpawnRadius; j++)
                {
                    if (i >= 0 && i < rows && j >= 0 && j < columns)
                    {
                        tempMapa[i, j] = LAKA;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    sb.Append(tempMapa[i, j] + " ");
                }
                sb.AppendLine();
            }
            generatedMapSB = sb;
            return sb;
        }

        private bool IsValidTile(int tileType)
        {
            return tileType == LAS || tileType == LAKA || tileType == SKALA ||
                   tileType == WODA || tileType == LASLECZNICZY;
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

        public void SaveToClipboard()
        {
            Clipboard.SetText(generatedMapSB.ToString());
            MessageBox.Show("Skopiowano do schowka");
        }
    }
}