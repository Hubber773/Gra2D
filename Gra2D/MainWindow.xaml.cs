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
using System.Collections.Generic;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        public StringBuilder wygenerowanySB = new StringBuilder();
        //--//Stałe reprezentujące rodzaje terenu
        public const int LAS = 1;
        public const int LAKA = 2;
        public const int SKALA = 3;
        public const int BOMBA = 4;
        public const int ZNISZCZONE = 5;
        public const int WODA = 6;
        public const int LASLECZNICZY = 7;
        public const int ILE_TERENOW = 8;
        //--//Mapa przechowywana jako tablica dwuwymiarowa int
        private int[,] mapa;
        private int szerokoscMapy;
        private int wysokoscMapy;
        //--//Dwuwymiarowa tablica kontrolek Image reprezentujących segmenty mapy
        private Image[,] tablicaTerenu;
        //--//Rozmiar jednego segmentu mapy w pikselach
        private const int RozmiarSegmentu = 32;

        //--//Tablica obrazków terenu – indeks odpowiada rodzajowi terenu
        //--//Indeks 1: las, 2: łąka, 3: skały
        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];

        //--//Pozycja gracza na mapie
        private int pozycjaGraczaX = 0;
        private int pozycjaGraczaY = 0;
        //--//Obrazek reprezentujący gracza
        private Image obrazGracza;
        //--//Licznik zgromadzonego drewna
        private int iloscDrewna = 0;
        private int iloscBomb = 3;
        private int zycie = 3;
        private bool isLoadingMap = false;
        private CancellationTokenSource bombCancellationTokenSource;
        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();

            //--//Inicjalizacja obrazka gracza
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu
            };
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;
            PlayBackgroundMusic("Preparation.mp3");
        }
        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
            obrazyTerenu[BOMBA] = new BitmapImage(new Uri("bomb.png", UriKind.Relative));
            obrazyTerenu[ZNISZCZONE] = new BitmapImage(new Uri("zniszczone.png", UriKind.Relative));
            obrazyTerenu[WODA] = new BitmapImage(new Uri("woda.png", UriKind.Relative));
            obrazyTerenu[LASLECZNICZY] = new BitmapImage(new Uri("lasleczniczy.png", UriKind.Relative));
        }

        private async Task WczytajMapeAsync(string sciezkaPliku)
        {
            isLoadingMap = true;
            bombCancellationTokenSource?.Cancel();

            try
            {
                var linie = await Task.Run(() => File.ReadAllLines(sciezkaPliku));
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
                        SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });
                    }
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });
                    }

                    tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
                    for (int y = 0; y < wysokoscMapy; y++)
                    {
                        for (int x = 0; x < szerokoscMapy; x++)
                        {
                            Image obraz = new Image
                            {
                                Width = RozmiarSegmentu,
                                Height = RozmiarSegmentu
                            };

                            int rodzaj = mapa[y, x];
                            if (rodzaj >= 1 && rodzaj < ILE_TERENOW)
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
                    pozycjaGraczaX = 0;
                    pozycjaGraczaY = 0;
                    AktualizujPozycjeGracza();

                    iloscDrewna = 0;
                    L_drewno.Content = "Zebrane Drewno: " + iloscDrewna;
                    iloscBomb = 3;
                    L_bomba.Content = "Ilość bomb: " + iloscBomb;
                    zycie = 3;
                    L_zycie.Content = "Życia: " + zycie;
                });

                Random rnd = new Random();
                if (rnd.Next(1, 4) == 1)
                {
                    PlayBackgroundMusic("background.mp3");
                }
                else if (rnd.Next(1, 4) == 2)
                {
                    PlayBackgroundMusic("background2.mp3");
                }
                else
                {
                    PlayBackgroundMusic("background3.mp3");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania mapy: " + ex.Message);
            }

            isLoadingMap = false;
        }

        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;
            //--//zmiana pozycji gracza
            if (e.Key == Key.W) nowyY--;
            else if (e.Key == Key.S) nowyY++;
            else if (e.Key == Key.A) nowyX--;
            else if (e.Key == Key.D) nowyX++;
            //--//Gracz nie może wyjść poza mapę
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                //--//Gracz nie może wejść na pole ze skałami
                if (mapa[nowyY, nowyX] != SKALA && mapa[nowyY, nowyX] != WODA)
                {
                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }
            }

            //--//Obsługa wycinania lasu – naciskamy klawisz C
            if (e.Key == Key.C)
            {
                if (mapa[pozycjaGraczaY, pozycjaGraczaX] == LAS)
                {
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    iloscDrewna++;
                    L_drewno.Content = "Drewno: " + iloscDrewna;
                }
                else if (mapa[pozycjaGraczaY, pozycjaGraczaX] == LASLECZNICZY)
                {
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    iloscDrewna++;
                    L_drewno.Content = "Drewno: " + iloscDrewna;
                    if (zycie < 3)
                    {
                        zycie++;
                        L_zycie.Content = "Życia: " + zycie;
                    }
                }
            }
            if (e.Key == Key.F)
            {
                if (isLoadingMap)
                {
                    return;
                }

                if (iloscBomb > 0)
                {
                    iloscBomb -= 1;
                    L_bomba.Content = "Ilość bomb: " + iloscBomb;

                    int bombaX = pozycjaGraczaX;
                    int bombaY = pozycjaGraczaY;

                    mapa[bombaY, bombaX] = BOMBA;
                    tablicaTerenu[bombaY, bombaX].Source = obrazyTerenu[BOMBA];

                    bombCancellationTokenSource = new CancellationTokenSource();

                    Task.Delay(2000).ContinueWith(task =>
                    {
                        if (isLoadingMap || bombCancellationTokenSource.Token.IsCancellationRequested)
                        {
                            return;
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            for (int y = -1; y <= 1; y++)
                            {
                                for (int x = -1; x <= 1; x++)
                                {
                                    int nowyX = bombaX + x;
                                    int nowyY = bombaY + y;

                                    if (nowyX >= 0 && nowyX < mapa.GetLength(1) && nowyY >= 0 && nowyY < mapa.GetLength(0))
                                    {
                                        if (pozycjaGraczaX == nowyX && pozycjaGraczaY == nowyY)
                                        {
                                            zycie -= 1;
                                            L_zycie.Content = "Życia: " + zycie;
                                            if (zycie == 0)
                                            {
                                                MessageBox.Show("Przegrałeś");
                                                WczytajMapeAsync("mapaWygenerowana.txt");
                                            }
                                        }

                                        if (mapa[nowyY, nowyX] != WODA)
                                        {
                                            mapa[nowyY, nowyX] = ZNISZCZONE;
                                            tablicaTerenu[nowyY, nowyX].Source = obrazyTerenu[ZNISZCZONE];
                                        }
                                    }
                                }
                            }
                        });
                    }, bombCancellationTokenSource.Token);
                }
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
            File.WriteAllText("mapaWygenerowana.txt", Clipboard.GetText());
            await WczytajMapeAsync("mapaWygenerowana.txt");
        }

        private void B_GenerujLiczbyMapy_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            StringBuilder sb = new StringBuilder();

            bool czyWierszeOK = int.TryParse(TB_ileWierszy.Text, out int ileWierszy);
            bool czyKolumnyOK = int.TryParse(TB_ileKolumn.Text, out int ileKolumn);

            ileWierszy = czyWierszeOK && ileWierszy > 0 ? ileWierszy : 20;
            ileKolumn = czyKolumnyOK && ileKolumn > 0 ? ileKolumn : 40;

            if (ileWierszy > 20 || ileKolumn > 40)
            {
                MessageBox.Show("Za duża liczba wierszy/kolumn");
                return;
            }

            int[,] tempMapa = new int[ileWierszy, ileKolumn];
            for (int i = 0; i < ileWierszy; i++)
            {
                for (int j = 0; j < ileKolumn; j++)
                {
                    tempMapa[i, j] = rnd.Next(1, 4);
                }
            }

            for (int i = 0; i < ileWierszy * ileKolumn / 100; i++)
            {
                int x = rnd.Next(0, ileKolumn);
                int y = rnd.Next(0, ileWierszy);
                tempMapa[y, x] = LASLECZNICZY;
            }

            int puddleCount = rnd.Next(1, 3);
            for (int p = 0; p < puddleCount; p++)
            {
                int puddleX = rnd.Next(0, ileKolumn);
                int puddleY = rnd.Next(0, ileWierszy);
                int puddleSize = rnd.Next(5, 9);

                Queue<(int, int)> waterQueue = new Queue<(int, int)>();
                waterQueue.Enqueue((puddleY, puddleX));
                tempMapa[puddleY, puddleX] = WODA;

                while (waterQueue.Count > 0 && puddleSize > 0)
                {
                    var (y, x) = waterQueue.Dequeue();
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int newX = x + dx;
                            int newY = y + dy;
                            if (newX >= 0 && newX < ileKolumn && newY >= 0 && newY < ileWierszy && tempMapa[newY, newX] != WODA && rnd.Next(0, 3) == 0)
                            {
                                tempMapa[newY, newX] = WODA;
                                waterQueue.Enqueue((newY, newX));
                                puddleSize--;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < ileWierszy; i++)
            {
                for (int j = 0; j < ileKolumn; j++)
                {
                    sb.Append(tempMapa[i, j] + " ");
                }
                sb.AppendLine();
            }
            MessageBox.Show(sb.ToString());
            wygenerowanySB = sb;
        }
        private void B_Kopiuj_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(wygenerowanySB.ToString());
            MessageBox.Show("Skopiowano do schowka");
        }

        private void TB_ileWierszy_GotFocus(object sender, RoutedEventArgs e)
        {
            TB_ileWierszy.Text = "";
        }

        private void TB_ileKolumn_GotFocus(object sender, RoutedEventArgs e)
        {
            TB_ileKolumn.Text = "";
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
                "\n" +
                "Porady i wskazówki:\n" +
                "-Koty miau miau kici kici\n" +
                "\n",
                FontSize = 20,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.White
            };
            instrukcje.Show();
        }
    }
}