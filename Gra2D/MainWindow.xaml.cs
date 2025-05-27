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
using System.Linq;
using System.Collections.Generic;
using System.Windows.Shapes;

namespace Gra2D
{
    public enum ItemType
    {
        Wood = 0,
        Bomb = 1
    }

    public partial class MainWindow : Window
    {
        private GameConfig config = null!;
        private MapGenerator mapGenerator = null!;
        private PlayerMovement? playerMovement;
        private Mining? mining;
        private MusicStuff musicStuff = null!;
        private BombasticStuff? bombasticStuff;
        private int[,]? mapa;
        private int szerokoscMapy;
        private int wysokoscMapy;
        private Image[,]? tablicaTerenu;
        private BitmapImage[] obrazyTerenu = new BitmapImage[MapGenerator.LASLECZNICZY + 1];
        private Image obrazGracza = null!;
        private int iloscDrewna = 0;
        private int zycie;
        private bool isLoadingMap = false;
        private bool isPlayerDead = false;
        private bool isNewGame = true;
        private string? currentMapText;
        private List<ItemType> inventoryItems = new List<ItemType>();
        private InventoryWindow? inventoryWindow;
        private string savesFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "saves");
        private Dictionary<string, ItemType?> hotbarItems = new Dictionary<string, ItemType?>
        {
            { "I_stuffes1", null },
            { "I_stuffes2", null },
            { "I_stuffes3", null },
            { "I_toolbar1", null },
            { "I_toolbar2", null },
            { "I_armorbar1", null },
            { "I_abilitybar1", null }
        };
        private string? selectedHotbarSlot;
        private Dictionary<string, Rectangle> hotbarRectangles = new Dictionary<string, Rectangle>();
        public Dictionary<string, ItemType?> GetHotbarItems() => hotbarItems;

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            LoadConfig();
            mapGenerator = new MapGenerator(config);
            var mediaElement = FindName("ME_MUSIC") as MediaElement ?? throw new InvalidOperationException("MediaElement 'ME_MUSIC' not found in XAML.");
            musicStuff = new MusicStuff(config, mediaElement);
            WczytajObrazyTerenu();

            obrazGracza = new Image
            {
                Width = config.SegmentSize,
                Height = config.SegmentSize,
                Source = new BitmapImage(new Uri("gracz.png", UriKind.Relative))
            };

            zycie = config.PlayerHealth;
            L_zycie.Content = "Życie: " + zycie + "/100";

            musicStuff.PlayBackgroundMusic(config.MenuMusic);
            KeyDown += OknoGlowne_KeyDown;
            KeyUp += OknoGlowne_KeyUp;
            InitializeHotbarRectangles();

            I_stuffes1.MouseLeftButtonDown += HotbarSlot_Click;
            I_stuffes2.MouseLeftButtonDown += HotbarSlot_Click;
            I_stuffes3.MouseLeftButtonDown += HotbarSlot_Click;
            I_toolbar1.MouseLeftButtonDown += HotbarSlot_Click;
            I_toolbar2.MouseLeftButtonDown += HotbarSlot_Click;
            I_armorbar1.MouseLeftButtonDown += HotbarSlot_Click;
            I_abilitybar1.MouseLeftButtonDown += HotbarSlot_Click;
        }

        private void HotbarSlot_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image)
            {
                string slotName = image.Name;

                if (selectedHotbarSlot == slotName)
                {
                    hotbarRectangles[slotName].Stroke = GetDefaultStroke(slotName);
                    selectedHotbarSlot = null;
                }
                else
                {
                    if (selectedHotbarSlot != null && hotbarRectangles.ContainsKey(selectedHotbarSlot))
                    {
                        hotbarRectangles[selectedHotbarSlot].Stroke = GetDefaultStroke(selectedHotbarSlot);
                    }

                    selectedHotbarSlot = slotName;
                    hotbarRectangles[slotName].Stroke = Brushes.Yellow;
                }
            }
        }

        private void InitializeHotbarRectangles()
        {
            hotbarRectangles["I_stuffes1"] = (Rectangle)FindName("Rectangle_stuffes1") ?? new Rectangle();
            hotbarRectangles["I_stuffes2"] = (Rectangle)FindName("Rectangle_stuffes2") ?? new Rectangle();
            hotbarRectangles["I_stuffes3"] = (Rectangle)FindName("Rectangle_stuffes3") ?? new Rectangle();
            hotbarRectangles["I_toolbar1"] = (Rectangle)FindName("Rectangle_toolbar1") ?? new Rectangle();
            hotbarRectangles["I_toolbar2"] = (Rectangle)FindName("Rectangle_toolbar2") ?? new Rectangle();
            hotbarRectangles["I_armorbar1"] = (Rectangle)FindName("Rectangle_armorbar1") ?? new Rectangle();
            hotbarRectangles["I_abilitybar1"] = (Rectangle)FindName("Rectangle_abilitybar1") ?? new Rectangle();
        }

        private void LoadConfig()
        {
            config = new GameConfig();
            string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_config.txt");

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
                    defaultConfig.AppendLine("RockChance=2");
                    defaultConfig.AppendLine("HealingForestChance=5");
                    defaultConfig.AppendLine("HealingForestHealthGain=25");
                    defaultConfig.AppendLine("WaterChance=60");
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
            bombasticStuff?.ClearBombs();
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

                inventoryItems.Clear();
                iloscDrewna = 0;
                if (isNewGame)
                {
                    foreach (var key in hotbarItems.Keys.ToList())
                    {
                        hotbarItems[key] = null;                        // Resetowanie tego hotbara po wczytaniu nowej gry
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        inventoryItems.Add(ItemType.Bomb);
                    }
                    isNewGame = false;
                }

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
                        (amount) => { iloscDrewna += amount; AddToInventory(ItemType.Wood); },
                        (amount) => { if (zycie < config.PlayerMaxHealth) { zycie += amount; L_zycie.Content = "Życie: " + zycie; } },
                        SiatkaMapy, playerMovement.GetPlayerPosition);
                    bombasticStuff = new BombasticStuff(config, mapa, tablicaTerenu, obrazyTerenu, playerMovement, SiatkaMapy,
                        async (damage) =>
                        {
                            zycie -= damage;
                            L_zycie.Content = "Życie: " + zycie + "/100";
                            if (zycie <= 0)
                            {
                                isPlayerDead = true;
                                MessageBox.Show("Przegrałeś");
                                string tempPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempMap.txt");
                                File.WriteAllText(tempPath, currentMapText);
                                await WczytajMapeAsync(tempPath);
                            }
                        });

                    zycie = config.PlayerHealth;
                    L_zycie.Content = "Życie: " + zycie + "/100";

                    if (inventoryWindow != null && inventoryWindow.IsLoaded)
                    {
                        inventoryWindow.UpdateInventoryUI();
                    }
                    UpdateHotbarUI();
                });

                musicStuff.PlayRandomGameMusic();
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
                    await mining!.ChopTree(x, y);
                }
                else if (e.Key == Key.F)
                {
                    if (isLoadingMap || selectedHotbarSlot == null) return;
                    if (hotbarItems[selectedHotbarSlot] != ItemType.Bomb) return;
                    var (bombaX, bombaY) = playerMovement.GetPlayerPosition();
                    if (mapa![bombaY, bombaX] == 4) return;

                    bombasticStuff!.PlaceBomb(bombaX, bombaY);
                    hotbarItems[selectedHotbarSlot] = null;
                    UpdateHotbarUI();

                    if (inventoryWindow != null && inventoryWindow.IsLoaded)
                    {
                        inventoryWindow.UpdateInventoryUI();
                    }

                    if (selectedHotbarSlot != null && hotbarRectangles.ContainsKey(selectedHotbarSlot))
                    {
                        hotbarRectangles[selectedHotbarSlot].Stroke = GetDefaultStroke(selectedHotbarSlot);
                        selectedHotbarSlot = null;
                    }
                }
                else if (e.Key >= Key.D1 && e.Key <= Key.D7)
                {
                    int slotNum = e.Key - Key.D0;
                    SelectHotbarSlot(slotNum);
                }
                else if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad7)
                {
                    int slotNum = e.Key - Key.NumPad0;
                    SelectHotbarSlot(slotNum);
                }
            }
        }

        private void OknoGlowne_KeyUp(object sender, KeyEventArgs e)
        {
            if (playerMovement != null && !isPlayerDead)
            {
                playerMovement.HandleKeyUp(e);
            }
        }

        private void SelectHotbarSlot(int slotNum)
        {
            string? slotName = null;
            switch (slotNum)
            {
                case 1: slotName = "I_stuffes1"; break;
                case 2: slotName = "I_stuffes2"; break;
                case 3: slotName = "I_stuffes3"; break;
                case 4: slotName = "I_toolbar1"; break;
                case 5: slotName = "I_toolbar2"; break;
                case 6: slotName = "I_armorbar1"; break;
                case 7: slotName = "I_abilitybar1"; break;
            }

            if (slotName != null)
            {
                if (selectedHotbarSlot == slotName)
                {
                    if (hotbarRectangles.ContainsKey(slotName))
                    {
                        hotbarRectangles[slotName].Stroke = GetDefaultStroke(slotName);
                    }
                    selectedHotbarSlot = null;
                }
                else
                {
                    if (selectedHotbarSlot != null && hotbarRectangles.ContainsKey(selectedHotbarSlot))
                    {
                        hotbarRectangles[selectedHotbarSlot].Stroke = GetDefaultStroke(selectedHotbarSlot);
                    }
                    if (hotbarRectangles.ContainsKey(slotName))
                    {
                        hotbarRectangles[slotName].Stroke = Brushes.Yellow;
                    }
                    selectedHotbarSlot = slotName;
                }
            }
        }

        private Brush GetDefaultStroke(string slotName)
        {
            switch (slotName)
            {
                case "I_stuffes1":
                case "I_stuffes2":
                case "I_stuffes3":
                    return Brushes.Black;
                case "I_toolbar1":
                case "I_toolbar2":
                case "I_armorbar1":
                case "I_abilitybar1":
                    return new LinearGradientBrush
                    {
                        EndPoint = new Point(0.5, 1),
                        StartPoint = new Point(0.5, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(255, 164, 0), 0),
                            slotName == "I_abilitybar1" ? new GradientStop(Color.FromRgb(0, 255, 144), 1) :
                            slotName == "I_armorbar1" ? new GradientStop(Color.FromRgb(218, 0, 255), 1) :
                            new GradientStop(Color.FromRgb(255, 47, 0), 1)
                        }
                    };
                default:
                    return Brushes.Black;
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
                isNewGame = true;
                await WczytajMapeAsync(oknoDialogowe.FileName);
            }
        }

        private async void B_wklejTXTGry_Click(object sender, RoutedEventArgs e)
        {
            string tempPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempMap.txt");
            currentMapText = Clipboard.GetText();
            File.WriteAllText(tempPath, currentMapText);
            isNewGame = true;
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

        private void B_instrukcja_Click(object sender, RoutedEventArgs e)
        {
            Window instrukcje = new Window();
            instrukcje.Title = "Instrukcje";
            instrukcje.Width = 600;
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
                "C - nieszczenie\n" +
                "F - kładzenie bloków\n" +
                "R - użycie umiejętności\n" +
                "Space - Szybkie zamykanie message boxów\n" +
                "Space - Ponowne kliknięcie ostatnio użytego przycisku\n" +
                "\n" +
                "Ekwipunek:\n" +
                "Dwuklik na hotbar - schowaj przedmiot\n" +
                "Kliknięcie - od/wybieranie przedmiotu\n" +
                "1,2,3,4,5,6,7 - wstawienie wybranego przedmiotu do hotbaru\n" +
                "\n" +
                "Porady i wskazówki:\n" +
                "- Bomba posiada reakcje łańcuchową\n" +
                "\n",
                FontSize = 20,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.White
            };
            instrukcje.Show();
        }

        private void B_inventory_Click(object sender, RoutedEventArgs e)
        {
            if (inventoryWindow == null || !inventoryWindow.IsLoaded)
            {
                inventoryWindow = new InventoryWindow(this);
                inventoryWindow.Closed += (s, ev) => inventoryWindow = null;
                inventoryWindow.UpdateInventoryUI();
                inventoryWindow.Show();
            }
            else
            {
                inventoryWindow.Activate();
            }
        }

        private void B_save_Click(object sender, RoutedEventArgs e)
        {
            if (mapa == null)
            {
                MessageBox.Show("No map loaded to save.");
                return;
            }
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string saveFileName = $"save_{timestamp}.txt";
            string savePath = System.IO.Path.Combine(savesFolder, saveFileName);
            Directory.CreateDirectory(savesFolder);
            string inventoryData = string.Join(",", inventoryItems.Select(item => (int)item));
            string hotbarData = string.Join(",", hotbarItems.Select(kvp => $"{kvp.Key}:{(kvp.Value.HasValue ? (int)kvp.Value.Value : -1)}"));
            using (StreamWriter writer = new StreamWriter(savePath))
            {
                for (int y = 0; y < wysokoscMapy; y++)
                {
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        writer.Write(mapa[y, x] + " ");
                    }
                    writer.WriteLine();
                }
                writer.WriteLine("INVENTORY:");
                writer.WriteLine(inventoryData);
                writer.WriteLine("HOTBAR:");
                writer.WriteLine(hotbarData);
            }
        }

        private async void B_load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog oknoDialogowe = new OpenFileDialog();
            oknoDialogowe.Filter = "Save files (*.txt)|*.txt";
            oknoDialogowe.InitialDirectory = savesFolder;
            bool? result = oknoDialogowe.ShowDialog();
            if (result == true)
            {
                await LoadSaveAsync(oknoDialogowe.FileName);
            }
        }

        private async Task LoadSaveAsync(string sciezkaPliku)
        {
            try
            {
                var lines = await Task.Run(() => File.ReadAllLines(sciezkaPliku));
                int inventoryIndex = Array.FindIndex(lines, line => line.StartsWith("INVENTORY:"));
                int hotbarIndex = Array.FindIndex(lines, line => line.StartsWith("HOTBAR:"));
                if (inventoryIndex == -1 || hotbarIndex == -1 || inventoryIndex >= lines.Length - 1 || hotbarIndex >= lines.Length - 1)
                {
                    MessageBox.Show("Invalid save file.");
                    return;
                }
                var mapLines = lines.Take(inventoryIndex).ToArray();
                var inventoryLine = lines[inventoryIndex + 1].Trim();
                var hotbarLine = lines[hotbarIndex + 1].Trim();
                var inventoryItemsFromFile = new List<ItemType>();
                if (!string.IsNullOrEmpty(inventoryLine))
                {
                    try
                    {
                        inventoryItemsFromFile = inventoryLine.Split(',')
                            .Select(s =>
                            {
                                if (int.TryParse(s.Trim(), out int id) && Enum.IsDefined(typeof(ItemType), id))
                                    return (ItemType)id;
                                throw new FormatException($"Invalid item ID: {s}");
                            }).ToList();
                    }
                    catch (FormatException ex)
                    {
                        MessageBox.Show($"Error parsing inventory: {ex.Message}");
                        return;
                    }
                }
                var hotbarItemsFromFile = new Dictionary<string, ItemType?>();
                if (!string.IsNullOrEmpty(hotbarLine))
                {
                    try
                    {
                        foreach (var pair in hotbarLine.Split(','))
                        {
                            var parts = pair.Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int id))
                            {
                                string slot = parts[0].Trim();
                                if (hotbarItems.ContainsKey(slot))
                                {
                                    hotbarItemsFromFile[slot] = id == -1 ? null : (ItemType)id;
                                }
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Error parsing hotbar data.");
                        return;
                    }
                }
                int rows = mapLines.Length;
                int columns = mapLines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                mapa = new int[rows, columns];
                inventoryItems.Clear();
                inventoryItems.AddRange(inventoryItemsFromFile);
                iloscDrewna = inventoryItems.Count(item => item == ItemType.Wood);
                hotbarItems.Clear();
                foreach (var slot in hotbarItemsFromFile)
                {
                    hotbarItems[slot.Key] = slot.Value;
                }
                for (int y = 0; y < rows; y++)
                {
                    var parts = mapLines[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int x = 0; x < columns; x++)
                    {
                        mapa[y, x] = int.Parse(parts[x]);
                    }
                }
                wysokoscMapy = rows;
                szerokoscMapy = columns;
                await Dispatcher.InvokeAsync(() =>
                {
                    SiatkaMapy.Children.Clear();
                    SiatkaMapy.RowDefinitions.Clear();
                    SiatkaMapy.ColumnDefinitions.Clear();
                    for (int y = 0; y < rows; y++)
                    {
                        SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(config.SegmentSize) });
                    }
                    for (int x = 0; x < columns; x++)
                    {
                        SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(config.SegmentSize) });
                    }
                    tablicaTerenu = new Image[rows, columns];
                    for (int y = 0; y < rows; y++)
                    {
                        for (int x = 0; x < columns; x++)
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
                            Grid.SetRow(obraz, y);
                            Grid.SetColumn(obraz, x);
                            SiatkaMapy.Children.Add(obraz);
                            tablicaTerenu[y, x] = obraz;
                        }
                    }
                    SiatkaMapy.Children.Add(obrazGracza);
                    Panel.SetZIndex(obrazGracza, 1);
                    playerMovement = new PlayerMovement(config, obrazGracza, mapa, columns, rows);
                    mining = new Mining(config, obrazGracza, mapa, tablicaTerenu, obrazyTerenu,
                        (amount) => { iloscDrewna += amount; AddToInventory(ItemType.Wood); },
                        (amount) => { if (zycie < config.PlayerMaxHealth) { zycie += amount; L_zycie.Content = "Życie: " + zycie; } },
                        SiatkaMapy, playerMovement.GetPlayerPosition);
                    bombasticStuff = new BombasticStuff(config, mapa, tablicaTerenu, obrazyTerenu, playerMovement, SiatkaMapy,
                        async (damage) =>
                        {
                            zycie -= damage;
                            L_zycie.Content = "Życie: " + zycie + "/100";
                            if (zycie <= 0)
                            {
                                isPlayerDead = true;
                                MessageBox.Show("Przegrałeś");
                                string tempPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempMap.txt");
                                File.WriteAllText(tempPath, currentMapText);
                                await WczytajMapeAsync(tempPath);
                            }
                        });
                    zycie = config.PlayerHealth;
                    L_zycie.Content = "Życie: " + zycie + "/100";
                    if (inventoryWindow != null)
                    {
                        if (inventoryWindow.IsLoaded)
                        {
                            inventoryWindow.Close();
                        }
                        inventoryWindow = null;
                    }
                    inventoryWindow = new InventoryWindow(this);
                    inventoryWindow.Closed += (s, ev) => inventoryWindow = null;
                    inventoryWindow.UpdateInventoryUI();
                    UpdateHotbarUI();
                });
                musicStuff.PlayRandomGameMusic();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania zapisu: " + ex.Message);
            }
        }

        private void AddToInventory(ItemType item)
        {
            inventoryItems.Add(item);
            if (item == ItemType.Wood)
            {
                iloscDrewna++;
            }
            if (inventoryWindow != null && inventoryWindow.IsLoaded)
            {
                inventoryWindow.UpdateInventoryUI();
            }
        }

        public List<ItemType> GetInventoryItems() => inventoryItems;

        public void UpdateHotbarItem(string slotName, ItemType? item)
        {
            if (hotbarItems.ContainsKey(slotName))
            {
                hotbarItems[slotName] = item;
            }
        }

        public void UpdateHotbarUI()
        {
            foreach (var slot in hotbarItems)
            {
                Image image = (Image)FindName(slot.Key);
                if (image != null)
                {
                    image.Source = slot.Value.HasValue
                        ? new BitmapImage(new Uri(slot.Value == ItemType.Wood ? "inv_drewno.png" : "inv_bomb.png", UriKind.Relative))
                        : null;
                }
            }
        }
    }
}