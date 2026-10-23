
using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Dispatching;

namespace HafizaAvcisi
{
    public partial class MainPage : ContentPage
    {
        // Oyunun genel değişkenleri
        private readonly Stopwatch kronometre = new();
        private readonly IDispatcherTimer zamanlayici;

        private List<string> kartSirasi = new();
        private OyunKarti? ilkKart;
        private OyunKarti? ikinciKart;

        private int hamleSayisi = 0;
        private int eslesmeSayisi = 0;
        private int oyunNumarasi = 0;

        private bool kartlarKilitli = false;
        private bool oyunBitti = false;

        // Her kartın bilgilerini tutan sınıf
        private class OyunKarti
        {
            public string ResimAdi { get; set; } = "";
            public Border Kenar { get; set; } = null!;
            public Image Resim { get; set; } = null!;
            public Label ArkaYuz { get; set; } = null!;

            public bool Eslesmis { get; set; } = false;
        }

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Süre göstergesini güncelleyen zamanlayıcı
            zamanlayici = Dispatcher.CreateTimer();
            zamanlayici.Interval = TimeSpan.FromSeconds(1);

            zamanlayici.Tick += (sender, e) =>
            {
                SureLabel.Text = SureyiYaz();
            };

            EnIyiSureyiGoster();

            KartlariHazirla(true);
        }

        // Süreyi dakika:saniye olarak yaz
        private string SureyiYaz()
        {
            int toplamSaniye =
                (int)kronometre.Elapsed.TotalSeconds;

            int dakika = toplamSaniye / 60;
            int saniye = toplamSaniye % 60;

            return $"{dakika:D2}:{saniye:D2}";
        }

        // Oyunu hazırla
        private void KartlariHazirla(bool yenidenKaristir)
        {
            oyunNumarasi++;

            zamanlayici.Stop();
            kronometre.Reset();

            hamleSayisi = 0;
            eslesmeSayisi = 0;

            ilkKart = null;
            ikinciKart = null;

            kartlarKilitli = false;
            oyunBitti = false;

            // Ekrandaki eski kartları temizle
            OyunGrid.Children.Clear();

            // Oyundaki 10 farklı resim
            List<string> resimler = new()
            {
                "birthday_cake.png",
                "bubble_tea.png",
                "candy.png",
                "chocolate_bar.png",
                "cookie.png",
                "cupcake.png",
                "custard.png",
                "dango.png",
                "doughnut.png",
                "fortune_cookie.png"
            };

            // Yeni oyunda resimleri çiftle ve karıştır
            if (yenidenKaristir || kartSirasi.Count == 0)
            {
                kartSirasi = resimler
                    .Concat(resimler)
                    .ToList();

                for (int i = kartSirasi.Count - 1; i > 0; i--)
                {
                    int j = Random.Shared.Next(i + 1);

                    (kartSirasi[i], kartSirasi[j]) =
                        (kartSirasi[j], kartSirasi[i]);
                }
            }

            // 20 kartı ekranda oluştur
            for (int index = 0; index < kartSirasi.Count; index++)
            {
                OyunKarti kartBilgisi = new OyunKarti
                {
                    ResimAdi = kartSirasi[index]
                };

                // Kartın içindeki gerçek resim
                Image resim = new Image
                {
                    Source = kartBilgisi.ResimAdi,
                    Aspect = Aspect.AspectFit,
                    IsVisible = false
                };

                // Kartın kapalı yüzündeki soru işareti
                Label arkaYuz = new Label
                {
                    Text = "?",
                    FontSize = 32,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                // Resim ve arka yüzü aynı alana koy
                Grid kartIci = new Grid();
                kartIci.Children.Add(resim);
                kartIci.Children.Add(arkaYuz);

                // Kartın kenarlığı
                Border kart = new Border
                {
                    WidthRequest = 70,
                    HeightRequest = 70,
                    Padding = new Thickness(0),
                    Stroke = Color.FromArgb("#AA70CE"),
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle
                    {
                        CornerRadius = new CornerRadius(10)
                    },
                    BackgroundColor = Color.FromArgb("#CFB1E9"),
                    Content = kartIci,
                    HorizontalOptions = LayoutOptions.Center
                };

                kartBilgisi.Kenar = kart;
                kartBilgisi.Resim = resim;
                kartBilgisi.ArkaYuz = arkaYuz;

                // Dokunma olayını ekle
                TapGestureRecognizer dokunma =
                    new TapGestureRecognizer();

                dokunma.Tapped += async (sender, e) =>
                {
                    await KartaTiklandi(kartBilgisi);
                };

                kart.GestureRecognizers.Add(dokunma);

                // Kartın satır ve sütununu hesapla
                Grid.SetRow(kart, index / 4);
                Grid.SetColumn(kart, index % 4);

                OyunGrid.Children.Add(kart);
            }

            // Sayaçları başlangıç değerlerine getir
            SureLabel.Text = "00:00";
            HamleLabel.Text = "0";
            EslesmeLabel.Text = "0/10";
            IlerlemeLabel.Text = "0 / 10 çift tamamlandı";
            DurumLabel.Text = "Aynı tatlıları bul ve eşleştir!";
        }

        // Bir karta dokunulduğunda
        private async Task KartaTiklandi(OyunKarti kart)
        {
            // Geçici kilit veya oyun bitişi varsa işlem yapma
            if (kartlarKilitli || oyunBitti || kart.Eslesmis)
                return;

            // Açılmış karta tekrar dokunulmasını engelle
            if (kart == ilkKart)
                return;

            // İlk dokunuşta süreyi başlat
            if (!kronometre.IsRunning)
            {
                kronometre.Start();
                zamanlayici.Start();
            }

            // Kartı aç
            KartiAc(kart);

            // Birinci kart seçildiyse
            if (ilkKart == null)
            {
                ilkKart = kart;
                return;
            }

            // İkinci kart seçildi
            ikinciKart = kart;
            kartlarKilitli = true;

            hamleSayisi++;
            HamleLabel.Text = hamleSayisi.ToString();

            // İki kartın resimleri aynı mı?
            if (ilkKart.ResimAdi == ikinciKart.ResimAdi)
            {
                ilkKart.Eslesmis = true;
                ikinciKart.Eslesmis = true;

                eslesmeSayisi++;

                EslesmeLabel.Text = $"{eslesmeSayisi}/10";
                IlerlemeLabel.Text =
                    $"{eslesmeSayisi} / 10 çift tamamlandı";

                DurumLabel.Text = "Harika, eşleşme bulundu!";

                ilkKart = null;
                ikinciKart = null;
                kartlarKilitli = false;

                // Bütün çiftler bulunduysa oyun biter
                if (eslesmeSayisi == 10)
                {
                    await OyunuBitir();
                }
            }
            else
            {
                DurumLabel.Text = "Eşleşmedi, tekrar dene!";

                // Bu oyunun numarasını sakla
                int mevcutOyun = oyunNumarasi;

                // Resimler kısa süre açık kalsın
                await Task.Delay(1000);

                // Oyuncu bu sırada yeni oyun başlattıysa
                // eski kartlara dokunma
                if (mevcutOyun != oyunNumarasi)
                    return;

                KartiKapat(ilkKart!);
                KartiKapat(ikinciKart!);

                ilkKart = null;
                ikinciKart = null;
                kartlarKilitli = false;
            }
        }

        // Kartı görünür hale getir
        private void KartiAc(OyunKarti kart)
        {
            kart.ArkaYuz.IsVisible = false;
            kart.Resim.IsVisible = true;
            kart.Kenar.BackgroundColor = Colors.White;
        }

        // Kartı tekrar kapat
        private void KartiKapat(OyunKarti kart)
        {
            kart.Resim.IsVisible = false;
            kart.ArkaYuz.IsVisible = true;

            kart.Kenar.BackgroundColor =
                Color.FromArgb("#CFB1E9");
        }

        // Oyun tamamlanınca
        private async Task OyunuBitir()
        {
            oyunBitti = true;

            kronometre.Stop();
            zamanlayici.Stop();

            SureLabel.Text = SureyiYaz();
            DurumLabel.Text = "Tebrikler! Bütün çiftleri buldun!";

            int bitisSuresi = Math.Max(
                1,
                (int)Math.Ceiling(kronometre.Elapsed.TotalSeconds)
            );

            // Eski rekoru oku
            int enIyi = Preferences.Default.Get("EnIyiSure", 0);

            // İlk skor veya daha iyi skor
            if (enIyi == 0 || bitisSuresi < enIyi)
            {
                Preferences.Default.Set("EnIyiSure", bitisSuresi);
            }

            EnIyiSureyiGoster();

            await DisplayAlert(
                "Tebrikler!",
                $"Oyunu {SureyiYaz()} sürede ve " +
                $"{hamleSayisi} hamlede bitirdin!",
                "Tamam"
            );
        }

        // Kaydedilmiş en iyi süreyi göster
        private void EnIyiSureyiGoster()
        {
            int enIyi = Preferences.Default.Get("EnIyiSure", 0);

            if (enIyi == 0)
            {
                EnIyiLabel.Text = "En iyi süre: Henüz yok";
            }
            else
            {
                int dakika = enIyi / 60;
                int saniye = enIyi % 60;

                EnIyiLabel.Text =
                    $"En iyi süre: {dakika:D2}:{saniye:D2}";
            }
        }

        // Yeni oyun: kartların yerleri yeniden karışır
        private void YeniOyun_Clicked(object sender, EventArgs e)
        {
            KartlariHazirla(true);
        }

        // Sıfırla: aynı kart dizilimi baştan başlar
        private void Sifirla_Clicked(object sender, EventArgs e)
        {
            KartlariHazirla(false);
        }
    }
}
