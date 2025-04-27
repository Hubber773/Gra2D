using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        private GameConfig config;
        private MapGenerator mapGenerator;
        private PlayerMovement playerMovement;
        private Mining mining;
        private int[,] mapa;
        private int szerokoscMapy;
        private int wysokoscMapy;
        private Image[,] tablicaTerenu;
        private BitmapImage[] obrazyTerenu = new BitmapImage[MapGenerator.LASLECZNICZY + 1];
        private Image obrazGracza;
        private int iloscDrewna = 0;
        private int iloscBomb;
        private int zycie;
        private bool isLoadingMap = false;
        private bool isPlayerDead = false;
        private Dictionary<(int X, int Y), CancellationTokenSource> bombTokens = new Dictionary<(int X, int Y), CancellationTokenSource>();
        private List<Task> activeBombTasks = new List<Task>();
        private string currentMapText;

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            mapGenerator = new MapGenerator(config);
            WczytajObrazyTerenu();

            obrazGracza = new Image
            {
                Width = config.SegmentSize,
                Height = config.SegmentSize,
                Source = new BitmapImage(new Uri("gracz.png", UriKind.Relative))
            };

            iloscBomb = config.BombCount;
            zycie = config.PlayerHealth;
            L_bomba.Content = "Ilość bomb: " + iloscBomb;
            L_zycie.Content = "Życia: " + zycie;

            PlayBackgroundMusic(config.MenuMusic);
            KeyDown += OknoGlowne_KeyDown;
            KeyUp += OknoGlowne_KeyUp;
        }

        private void LoadConfig()
        {
            config = new GameConfig();
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_config.txt");

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
                        int radius;

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
                                if (int.TryParse(value, out radius)) config.BombBlastRadius = radius;
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
                                if (int.TryParse(value, out radius)) config.PlayerSpawnRadius = radius;
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

        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[MapGenerator.LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[MapGenerator.LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[MapGenerator.SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
            obrazyTerenu[4] = new BitmapImage(new Uri("bomb.png", UriKind.Relative));
            obrazyTerenu[5] = new BitmapImage(new Uri("zniszczone.png", UriKind.Relative));
            obrazyTerenu[MapGenerator.WODA] = new BitmapImage(new Uri("woda.png", UriKind.Relative));
            obrazyTerenu[MapGenerator.LASLECZNICZY] = new BitmapImage(new Uri("lasleczniczy.png", UriKind.Relative));
        }

        private async Task WczytajMapeAsync(string sciezkaPliku)
        {
            isLoadingMap = true;
            isPlayerDead = false;
            foreach (var token in bombTokens.Values)
            {
                token.Cancel();
            }
            try
            {
                await Task.WhenAll(activeBombTasks);
            }
            catch (TaskCanceledException)
            {
            }
            bombTokens.Clear();
            activeBombTasks.Clear();
            mining?.CancelChopping();

            try
            {
                var linie = await Task.Run(() => File.ReadAllLines(sciezkaPliku));
                currentMapText = File.ReadAllText(sciezkaPliku);
                wysokoscMapy = linie.Length;
                szerokoscMapy = linie[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                mapa = new int[wysokoscMapy, szerokoscMapy];

                await Task.Run(() =>
                {
                    for (int y = 0; y < wysokoscMapy; y++)
                    {
                        var czesci = linie[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        for (int x = 0; x < szerokoscMapy; x++)
                        {
                            mapa[y, x] = int.Parse(czesci[x]);
                        }
                    }
                });

                await Dispatcher.InvokeAsync(() =>
                {
                    SiatkaMapy.Children.Clear();
                    SiatkaMapy.RowDefinitions.Clear();
                    SiatkaMapy.ColumnDefinitions.Clear();

                    for (int y = 0; y < wysokoscMapy; y++)
                    {
                        SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(config.SegmentSize) });
                    }
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(config.SegmentSize) });
                    }

                    tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
                    for (int y = 0; y < wysokoscMapy; y++)
                    {
                        for (int x = 0; x < szerokoscMapy; x++)
                        {
                            Image obraz = new Image
                            {
                                Width = config.SegmentSize,
                                Height = config.SegmentSize
                            };

                            int rodzaj = mapa[y, x];
                            if (rodzaj >= 1 && rodzaj <= MapGenerator.LASLECZNICZY)
                            {
                                obraz.Source = obrazyTerenu[rodzaj];
                            }
                            else
                            {
                                obraz.Source = null;
                            }

                            Grid.SetRow(obraz, y);
                            Grid.SetColumn(obraz, x);
                            SiatkaMapy.Children.Add(obraz);
                            tablicaTerenu[y, x] = obraz;
                        }
                    }

                    SiatkaMapy.Children.Add(obrazGracza);
                    Panel.SetZIndex(obrazGracza, 1);
                    playerMovement = new PlayerMovement(config, obrazGracza, mapa, szerokoscMapy, wysokoscMapy);
                    mining = new Mining(config, obrazGracza, mapa, tablicaTerenu, obrazyTerenu,
                        (amount) => { iloscDrewna += amount; L_drewno.Content = "Drewno: " + iloscDrewna; },
                        (amount) => { if (zycie < config.PlayerMaxHealth) { zycie += amount; L_zycie.Content = "Życia: " + zycie; } },
                        SiatkaMapy, playerMovement.GetPlayerPosition);

                    iloscDrewna = 0;
                    L_drewno.Content = "Zebrane Drewno: " + iloscDrewna;
                    iloscBomb = config.BombCount;
                    L_bomba.Content = "Ilość bomb: " + iloscBomb;
                    zycie = config.PlayerHealth;
                    L_zycie.Content = "Życia: " + zycie;
                });

                PlayRandomGameMusic();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania mapy: " + ex.Message);
                currentMapText = null;
            }

            isLoadingMap = false;
        }

        private async void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            if (playerMovement != null && !isPlayerDead)
            {
                playerMovement.HandleKeyDown(e);
                if (e.Key == Key.C)
                {
                    var (x, y) = playerMovement.GetPlayerPosition();
                    await mining.ChopTree(x, y);
                }
                else if (e.Key == Key.F)
                {
                    if (isLoadingMap || iloscBomb <= 0) return;
                    var (bombaX, bombaY) = playerMovement.GetPlayerPosition();
                    if (mapa[bombaY, bombaX] == 4) return;

                    iloscBomb--;
                    L_bomba.Content = "Ilość bomb: " + iloscBomb;
                    mapa[bombaY, bombaX] = 4;
                    tablicaTerenu[bombaY, bombaX].Source = obrazyTerenu[4];

                    var cts = new CancellationTokenSource();
                    bombTokens[(bombaX, bombaY)] = cts;
                    var bombTask = Task.Delay(config.BombExplosionDelay, cts.Token).ContinueWith(async task =>
                    {
                        if (isLoadingMap || cts.Token.IsCancellationRequested)
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
            }
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
                        if (playerX == nowyX && playerY == nowyY && !damagedByBombs.Contains((bombaX, bombaY)) && zycie > 0)
                        {
                            zycie -= config.BombDamage;
                            L_zycie.Content = "Życia: " + zycie;
                            damagedByBombs.Add((bombaX, bombaY));
                            if (zycie <= 0)
                            {
                                isPlayerDead = true;
                                MessageBox.Show("Przegrałeś");
                                if (currentMapText != null)
                                {
                                    string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempMap.txt");
                                    File.WriteAllText(tempPath, currentMapText);
                                    await WczytajMapeAsync(tempPath);
                                }
                                else
                                {
                                    await WczytajMapeAsync("mapaWygenerowana.txt");
                                }
                                return;
                            }
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

        private void OknoGlowne_KeyUp(object sender, KeyEventArgs e)
        {
            if (playerMovement != null && !isPlayerDead)
            {
                playerMovement.HandleKeyUp(e);
            }
        }

        private async void B_WczytajMape_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog oknoDialogowe = new OpenFileDialog();
            oknoDialogowe.Filter = "Plik mapy (*.txt)|*.txt";
            oknoDialogowe.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            bool? czyOtwartoMape = oknoDialogowe.ShowDialog();
            if (czyOtwartoMape == true)
            {
                await WczytajMapeAsync(oknoDialogowe.FileName);
            }
        }

        private async void B_wklejTXTGry_Click(object sender, RoutedEventArgs e)
        {
            string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempMap.txt");
            currentMapText = Clipboard.GetText();
            File.WriteAllText(tempPath, currentMapText);
            await WczytajMapeAsync(tempPath);
        }

        private void B_GenerujLiczbyMapy_Click(object sender, RoutedEventArgs e)
        {
            bool czyWierszeOK = int.TryParse(TB_ileWierszy.Text, out int ileWierszy);
            bool czyKolumnyOK = int.TryParse(TB_ileKolumn.Text, out int ileKolumn);
            var sb = mapGenerator.GenerateMap(ileWierszy, czyKolumnyOK ? ileKolumn : config.MaxColumns);
            if (sb != null)
            {
                currentMapText = sb.ToString();
                MessageBox.Show(currentMapText);
            }
        }

        private void B_Kopiuj_Click(object sender, RoutedEventArgs e)
        {
            mapGenerator.SaveToClipboard();
        }

        private void TB_ileWierszy_GotFocus(object sender, RoutedEventArgs e)
        {
            TB_ileWierszy.Text = "";
        }

        private void TB_ileKolumn_GotFocus(object sender, RoutedEventArgs e)
        {
            TB_ileKolumn.Text = "";
        }

        private void PlayRandomGameMusic()
        {
            if (config.GameMusicFiles.Length > 0)
            {
                Random rnd = new Random();
                string musicFile = config.GameMusicFiles[rnd.Next(0, config.GameMusicFiles.Length)];
                PlayBackgroundMusic(musicFile);
            }
        }

        private void PlayBackgroundMusic(string fileName)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            if (File.Exists(filePath))
            {
                ME_MUSIC.Source = new Uri(filePath);
                ME_MUSIC.MediaEnded += (s, args) =>
                {
                    ME_MUSIC.Position = TimeSpan.Zero;
                    ME_MUSIC.Play();
                };
                ME_MUSIC.Play();
            }
        }

        private void B_instrukcja_Click(object sender, RoutedEventArgs e)
        {
            Window instrukcje = new Window();
            instrukcje.Title = "Instrukcje";
            instrukcje.Width = 400;
            instrukcje.Height = 300;
            instrukcje.ResizeMode = ResizeMode.NoResize;
            instrukcje.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            instrukcje.Background = new LinearGradientBrush(
                Colors.DarkSlateGray,
                Colors.LightSlateGray,
                new Point(0, 0),
                new Point(0, 1)
            );

            instrukcje.Content = new TextBlock
            {
                Text = "WASD - ruch gracza\n" +
                "C - wycinanie lasu\n" +
                "F - postawienie bomby\n" +
                "Space - Szybkie zamykanie message boxów\n" +
                "\n" +
                "Porady i wskazówki:\n" +
                "- Koty miau miau kici kici\n" +
                "\n",
                FontSize = 20,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.White
            };
            instrukcje.Show();
        }
    }
}