using System;

namespace Gra2D
{
    public class GameConfig
    {
        public int PlayerHealth { get; set; } = 100;
        public int PlayerMaxHealth { get; set; } = 100;
        public int PlayerSpawnRadius { get; set; } = 1;
        public float MovementCooldown { get; set; } = 0.15f;
        public int BombCount { get; set; } = 3;
        public int BombDamage { get; set; } = 50;
        public int BombExplosionDelay { get; set; } = 2000;
        public int BombBlastRadius { get; set; } = 1;
        public int BombExplosionTile { get; set; } = 5;
        public int MaxRows { get; set; } = 20;
        public int MaxColumns { get; set; } = 40;
        public int SegmentSize { get; set; } = 32;
        public int ForestChance { get; set; } = 30;
        public int MeadowChance { get; set; } = 40;
        public int RockChance { get; set; } = 2;
        public int HealingForestChance { get; set; } = 5;
        public int HealingForestHealthGain { get; set; } = 25;
        public int WaterChance { get; set; } = 60;
        public int WaterPuddleCount { get; set; } = 3;
        public bool DisableWaterPuddles { get; set; } = false;

        // Default tile type for base tiles and player spawn area
        // Valid values: 1 (Forest, las.png), 2 (Meadow, laka.png), 3 (Rock, skala.png),
        //               6 (Water, woda.png), 7 (Healing Forest, lasleczniczy.png)
        public int DefaultTileType { get; set; } = 2;

        // Tiles that override others in priority order (left = highest, right = lowest)
        // Higher-priority tiles overwrite lower-priority ones during generation
        // Valid values: 1 (Forest, las.png), 2 (Meadow, laka.png), 3 (Rock, skala.png),
        //               6 (Water, woda.png), 7 (Healing Forest, lasleczniczy.png)
        public int[] TileOnTopPriority { get; set; } = new int[] { 6 };

        // Honestly this explains itself do i even need to explain this reeaaally?
        // Valid values: 1 (Forest, las.png), 2 (Meadow, laka.png), 3 (Rock, skala.png),
        //               5 (Destroyed, zniszczone.png), 6 (Water, woda.png),
        //               7 (Healing Forest, lasleczniczy.png)
        public float TreeChopTime { get; set; } = 0.65f;
        public int ProgressBarHeight { get; set; } = 5;
        public string ProgressBarColor { get; set; } = "Yellow";
        public string MenuMusic { get; set; } = "preparation.mp3";
        public string[] GameMusicFiles { get; set; } = new string[]
        {
            "background.mp3",
            "background2.mp3",
            "background3.mp3",
            "background4.mp3",
            "background5.mp3",
            "background6.mp3",
            "background7.mp3"
        };
    }
}