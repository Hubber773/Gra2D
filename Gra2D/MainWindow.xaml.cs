using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        public StringBuilder wygenerowanySB = new StringBuilder();
        //--//Stałe reprezentujące rodzaje terenu
        public const int LAS = 1;     //--//las
        public const int LAKA = 2;     //--//łąka
        public const int SKALA = 3;   //--//skały
        public const int BOMBA = 4;
        public const int ZNISZCZONE = 5;
        public const int ILE_TERENOW = 6;   //--//ile terenów
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
            //--//Zakładamy, że tablica jest indeksowana od 0, ale używamy indeksów 1-3
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
            obrazyTerenu[BOMBA] = new BitmapImage(new Uri("bomb.png", UriKind.Relative));
            obrazyTerenu[ZNISZCZONE] = new BitmapImage(new Uri("zniszczone.png", UriKind.Relative));
        }

        //--//Wczytuje mapę z pliku tekstowego i dynamicznie tworzy tablicę kontrolek Image
        private void WczytajMape(string sciezkaPliku)
        {
            try
            {
                var linie = File.ReadAllLines(sciezkaPliku);//--//zwraca tablicę stringów, np. linie[0] to pierwsza linia pliku
                wysokoscMapy = linie.Length;
                szerokoscMapy = linie[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;//--//zwraca liczbę elementów w tablicy
                mapa = new int[wysokoscMapy, szerokoscMapy];

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    var czesci = linie[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);//--//zwraca tablicę stringów np. czesci[0] to pierwszy element linii
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        mapa[y, x] = int.Parse(czesci[x]);//--//wczytanie mapy z pliku
                    }
                }

                //--//Przygotowanie kontenera SiatkaMapy – czyszczenie elementów i definicji wierszy/kolumn
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

                //--//Tworzenie tablicy kontrolk Image i dodawanie ich do siatki
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
                            obraz.Source = obrazyTerenu[rodzaj];//--//wczytanie obrazka terenu
                        }
                        else
                        {
                            obraz.Source = null;
                        }

                        Grid.SetRow(obraz, y);
                        Grid.SetColumn(obraz, x);
                        SiatkaMapy.Children.Add(obraz);//--//dodanie obrazka do siatki na ekranie
                        tablicaTerenu[y, x] = obraz;
                    }
                }

                Random rnd = new Random();
                rnd.Next(1, 4);

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
                //--//Dodanie obrazka gracza – ustawiamy go na wierzchu
                SiatkaMapy.Children.Add(obrazGracza);
                Panel.SetZIndex(obrazGracza, 1);//--//ustawienie obrazka gracza na wierzchu
                pozycjaGraczaX = 0;
                pozycjaGraczaY = 0;
                AktualizujPozycjeGracza();

                iloscDrewna = 0;
                L_drewno.Content = "Zebrane Drewno: " + iloscDrewna;
                iloscBomb = 3;
                L_bomba.Content = "Ilość bomb: " + iloscBomb;
                zycie = 3;
                L_zycie.Content = "Życia: " + zycie;
            }//--//koniec try
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania mapy: " + ex.Message);
            }
        }

        //--//Aktualizuje pozycję obrazka gracza w siatce
        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        //--//Obsługa naciśnięć klawiszy – ruch gracza oraz wycinanie lasu
        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;
            //--//zmiana pozycji gracza
            if (e.Key == Key.W) nowyY--; // Zmieniłem na WASD bo strzałki przechodziły pomiędzy inne przyciski i inne takie obiekty*/
            else if (e.Key == Key.S) nowyY++;
            else if (e.Key == Key.A) nowyX--;
            else if (e.Key == Key.D) nowyX++;
            //--//Gracz nie może wyjść poza mapę
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                //--//Gracz nie może wejść na pole ze skałami
                if (mapa[nowyY, nowyX] != SKALA)
                {
                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }
            }

            //--//Obsługa wycinania lasu – naciskamy klawisz C
            if (e.Key == Key.C)
            {
                if (mapa[pozycjaGraczaY, pozycjaGraczaX] == LAS)//--//jeśli gracz stoi na polu lasu
                {
                    mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                    iloscDrewna++;
                    L_drewno.Content = "Drewno: " + iloscDrewna;
                }
            }
            if (e.Key == Key.F)
            {
                if (iloscBomb > 0)
                {
                    iloscBomb -= 1;
                    L_bomba.Content = "Ilość bomb: " + iloscBomb;

                    int bombaX = pozycjaGraczaX;
                    int bombaY = pozycjaGraczaY;

                    mapa[bombaY, bombaX] = BOMBA;
                    tablicaTerenu[bombaY, bombaX].Source = obrazyTerenu[BOMBA];

                    Task.Delay(2000).ContinueWith(_ =>
                    {
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
                                                WczytajMape("mapaWygenerowana.txt");
                                            }
                                        }

                                        mapa[nowyY, nowyX] = ZNISZCZONE;
                                        tablicaTerenu[nowyY, nowyX].Source = obrazyTerenu[ZNISZCZONE];
                                    }
                                }
                            }
                        });
                    });
                }
            }
        }

        //--//Obsługa przycisku "Wczytaj mapę"
        private void B_WczytajMape_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog oknoDialogowe = new OpenFileDialog();
            oknoDialogowe.Filter = "Plik mapy (*.txt)|*.txt";
            oknoDialogowe.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory; //--//Ustawienie katalogu początkowego
            bool? czyOtwartoMape = oknoDialogowe.ShowDialog();
            if (czyOtwartoMape == true)
            {
                WczytajMape(oknoDialogowe.FileName);
            }
        }

        private void B_wklejTXTGry_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText("mapaWygenerowana.txt", Clipboard.GetText());
            WczytajMape("mapaWygenerowana.txt");
        }

        private void B_GenerujLiczbyMapy_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            StringBuilder sb = new StringBuilder();

            bool czyWierszeOK = int.TryParse(TB_ileWierszy.Text, out int ileWierszy);
            bool czyKolumnyOK = int.TryParse(TB_ileKolumn.Text, out int ileKolumn);

            ileWierszy = czyWierszeOK && ileWierszy > 0 ? ileWierszy : 5;
            ileKolumn = czyKolumnyOK && ileKolumn > 0 ? ileKolumn : 5;

            if (ileWierszy > 20 || ileKolumn > 40)
            {
                MessageBox.Show("Za duża liczba wierszy/kolumn");
                return;
            }

            for (int i = 0; i < ileWierszy; i++)
            {
                for (int j = 0; j < ileKolumn; j++)
                {
                    sb.Append(rnd.Next(1, 4) + " ");
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
                "-Zniszczone pole zostają na jednej rundzie\n" +
                "\n",
                FontSize = 20,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.White
            };
            instrukcje.Show();
        }
    }
}




