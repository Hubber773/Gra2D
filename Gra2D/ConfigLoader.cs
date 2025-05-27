using System;
using System.IO;
using System.Text;

namespace Gra2D
{
    public class ConfigLoader
    {
        public GameConfig LoadConfig(string configPath)
        {
            GameConfig config = new GameConfig();

            if (File.Exists(configPath))
            {
                try
                {
                    var lines = File.ReadAllLines(configPath);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                            continue;

                        var parts = line.Split('=');
                        if (parts.Length != 2) continue;

                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        switch (key)
                        {
                            case "PlayerHealth":
                                if (int.TryParse(value, out int health)) config.PlayerHealth = health;
                                break;
                            case "PlayerMaxHealth":
                                if (int.TryParse(value, out int maxHealth)) config.PlayerMaxHealth = maxHealth;
                                break;
                            case "BombCount":
                                if (int.TryParse(value, out int bombs)) config.BombCount = bombs;
                                break;
                            case "BombDamage":
                                if (int.TryParse(value, out int bombDamage)) config.BombDamage = bombDamage;
                                break;
                            case "BombExplosionDelay":
                                if (int.TryParse(value, out int delay)) config.BombExplosionDelay = delay;
                                break;
                            case "BombBlastRadius":
                                if (int.TryParse(value, out int radius)) config.BombBlastRadius = radius;
                                break;
                            case "MaxRows":
                                if (int.TryParse(value, out int rows)) config.MaxRows = rows;
                                break;
                            case "MaxColumns":
                                if (int.TryParse(value, out int cols)) config.MaxColumns = cols;
                                break;
                            case "SegmentSize":
                                if (int.TryParse(value, out int size)) config.SegmentSize = size;
                                break;
                            case "ForestChance":
                                if (int.TryParse(value, out int forestChance)) config.ForestChance = forestChance;
                                break;
                            case "MeadowChance":
                                if (int.TryParse(value, out int meadowChance)) config.MeadowChance = meadowChance;
                                break;
                            case "RockChance":
                                if (int.TryParse(value, out int rockChance)) config.RockChance = rockChance;
                                break;
                            case "HealingForestChance":
                                if (int.TryParse(value, out int healChance)) config.HealingForestChance = healChance;
                                break;
                            case "HealingForestHealthGain":
                                if (int.TryParse(value, out int heal)) config.HealingForestHealthGain = heal;
                                break;
                            case "WaterChance":
                                if (int.TryParse(value, out int waterChance)) config.WaterChance = waterChance;
                                break;
                            case "WaterPuddleCount":
                                if (int.TryParse(value, out int puddleCount)) config.WaterPuddleCount = puddleCount;
                                break;
                            case "DisableWaterPuddles":
                                if (bool.TryParse(value, out bool disablePuddles)) config.DisableWaterPuddles = disablePuddles;
                                break;
                            case "DefaultTileType":
                                if (int.TryParse(value, out int defaultTile)) config.DefaultTileType = defaultTile;
                                break;
                            case "TileOnTopPriority":
                                config.TileOnTopPriority = value.Split(',')
                                    .Select(s => s.Trim())
                                    .Where(s => int.TryParse(s, out _))
                                    .Select(int.Parse)
                                    .ToArray();
                                break;
                            case "BombExplosionTile":
                                if (int.TryParse(value, out int explosionTile)) config.BombExplosionTile = explosionTile;
                                break;
                            case "PlayerSpawnRadius":
                                if (int.TryParse(value, out int spawnRadius)) config.PlayerSpawnRadius = spawnRadius;
                                break;
                            case "MovementCooldown":
                                if (float.TryParse(value, out float cooldown)) config.MovementCooldown = cooldown;
                                break;
                            case "TreeChopTime":
                                if (float.TryParse(value, out float chopTime)) config.TreeChopTime = chopTime;
                                break;
                            case "ProgressBarHeight":
                                if (int.TryParse(value, out int height)) config.ProgressBarHeight = height;
                                break;
                            case "ProgressBarColor":
                                config.ProgressBarColor = value;
                                break;
                            case "MenuMusic":
                                config.MenuMusic = value;
                                break;
                            case "GameMusicFiles":
                                config.GameMusicFiles = value.Split(',').Select(s => s.Trim()).ToArray();
                                break;
                        }
                    }
                }
                catch
                {
                }
            }
            else
            {
                SaveDefaultConfig(configPath);
            }

            return config;
        }

        private void SaveDefaultConfig(string configPath)
        {
            try
            {
                StringBuilder defaultConfig = new StringBuilder();
                defaultConfig.AppendLine("PlayerHealth=100");
                defaultConfig.AppendLine("PlayerMaxHealth=100");
                defaultConfig.AppendLine("BombCount=3");
                defaultConfig.AppendLine("BombDamage=50");
                defaultConfig.AppendLine("BombExplosionDelay=2000");
                defaultConfig.AppendLine("BombBlastRadius=1");
                defaultConfig.AppendLine("MaxRows=20");
                defaultConfig.AppendLine("MaxColumns=40");
                defaultConfig.AppendLine("SegmentSize=32");
                defaultConfig.AppendLine("ForestChance=30");
                defaultConfig.AppendLine("MeadowChance=40");
                defaultConfig.AppendLine("RockChance=6");
                defaultConfig.AppendLine("HealingForestChance=5");
                defaultConfig.AppendLine("HealingForestHealthGain=25");
                defaultConfig.AppendLine("WaterChance=50");
                defaultConfig.AppendLine("WaterPuddleCount=3");
                defaultConfig.AppendLine("DisableWaterPuddles=false");
                defaultConfig.AppendLine("DefaultTileType=2");
                defaultConfig.AppendLine("TileOnTopPriority=6");
                defaultConfig.AppendLine("BombExplosionTile=5");
                defaultConfig.AppendLine("PlayerSpawnRadius=1");
                defaultConfig.AppendLine("MovementCooldown=0.15");
                defaultConfig.AppendLine("TreeChopTime=0.65");
                defaultConfig.AppendLine("ProgressBarHeight=5");
                defaultConfig.AppendLine("ProgressBarColor=Yellow");
                defaultConfig.AppendLine("MenuMusic=preparation.mp3");
                defaultConfig.Append("GameMusicFiles=background.mp3,background2.mp3,background3.mp3,");
                defaultConfig.Append("background4.mp3,background5.mp3,background6.mp3,background7.mp3");

                File.WriteAllText(configPath, defaultConfig.ToString());
            }
            catch
            {
            }
        }
    }
}