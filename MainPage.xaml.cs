using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Dispatching;

namespace HafizaAvcisi;

public partial class MainPage : ContentPage
{
    // Yeni tema eklemek için bu listeye adı, kalıcı anahtarı ve görselleri eklemek yeterlidir.
    private sealed record OyunTemasi(string Ad, string Anahtar, string MolaBasligi,
        string MolaAciklamasi, string Ikon, string[] Resimler);
    private readonly OyunTemasi[] temalar =
    [
        new("Tatlılar", "tatlilar", "Bugünkü tatlı molan",
            "Biraz dikkat, biraz şeker. Zorluğunu seç, keyfine bak!", "cupcake.png",
        [
            "birthday_cake.png", "bubble_tea.png", "candy.png", "chocolate_bar.png",
            "cookie.png", "cupcake.png", "custard.png", "dango.png",
            "doughnut.png", "fortune_cookie.png"
        ])
    ];
    private readonly string[] seviyeler = ["Kolay", "Orta", "Zor"];
    private readonly string[] seviyeAnahtarlari = ["kolay", "orta", "zor"];
    private readonly int[] ciftSayilari = [6, 8, 10];
    private readonly Stopwatch kronometre = new();
    private readonly IDispatcherTimer zamanlayici;
    private readonly List<OyunKarti> kartlar = [];
    private List<string> kartSirasi = [];
    private CancellationTokenSource oyunIptali = new();
    private OyunKarti? ilkKart;
    private int hamleSayisi;
    private int eslesmeSayisi;
    private int seviye = 1;
    private int tema;
    private readonly List<Button> zorlukButonlari = [];
    private readonly List<Button> temaButonlari = [];
    private bool oyunEkraninda;
    private bool kartlarKilitli;
    private bool oyunBitti;
    private int CiftSayisi => ciftSayilari[seviye];
    private string RekorAnahtari => $"EnIyiSure_v2_{temalar[tema].Anahtar}_{seviyeAnahtarlari[seviye]}";

    private sealed class OyunKarti
    {
        public required string ResimAdi { get; init; }
        public required Grid Alan { get; init; }
        public required Grid Pariltilar { get; init; }
        public required Border Kenar { get; init; }
        public required Image Resim { get; init; }
        public required View ArkaYuz { get; init; }
        public bool Eslesmis { get; set; }
    }

    public MainPage()
    {
        InitializeComponent();
        zamanlayici = Dispatcher.CreateTimer();
        zamanlayici.Interval = TimeSpan.FromMilliseconds(250);
        zamanlayici.Tick += (_, _) => SureLabel.Text = SureyiYaz(kronometre.Elapsed.TotalSeconds);
        SecimButonlariniOlustur();
        AnaSayfaBilgileriniGuncelle();
    }

    // Dakikaları 60'ta başa sarmadan süreyi gösterir.
    private static string SureyiYaz(double saniye)
    {
        int toplam = (int)Math.Floor(saniye);
        return $"{toplam / 60:D2}:{toplam % 60:D2}";
    }

    // Fisher–Yates: her konuma eşit olasılıkla bir kart yerleştirir.
    private static void Karistir<T>(IList<T> liste)
    {
        for (int i = liste.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (liste[i], liste[j]) = (liste[j], liste[i]);
        }
    }

    // Eski gecikmeleri iptal eder; eski kartlar yeni oyuna dokunamaz.
    private void KartlariHazirla(bool yenidenKaristir)
    {
        oyunIptali.Cancel();
        oyunIptali.Dispose();
        oyunIptali = new CancellationTokenSource();
        foreach (var kart in kartlar)
        {
            kart.Kenar.CancelAnimations();
            foreach (var parilti in kart.Pariltilar.Children.OfType<Label>())
                parilti.CancelAnimations();
        }
        zamanlayici.Stop();
        kronometre.Reset();
        ilkKart = null;
        hamleSayisi = eslesmeSayisi = 0;
        kartlarKilitli = oyunBitti = false;
        BitisPanel.IsVisible = false;
        RekorLabel.IsVisible = false;

        if (yenidenKaristir || kartSirasi.Count != CiftSayisi * 2)
        {
            var resimler = temalar[tema].Resimler.ToList();
            Karistir(resimler);
            var secilen = resimler.Take(CiftSayisi).ToList();
            kartSirasi = secilen.Concat(secilen).ToList();
            Karistir(kartSirasi);
        }

        kartlar.Clear();
        OyunGrid.Children.Clear();
        OyunGrid.RowDefinitions.Clear();
        for (int satir = 0; satir < kartSirasi.Count / 4; satir++)
            OyunGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int i = 0; i < kartSirasi.Count; i++)
        {
            var kart = KartOlustur(kartSirasi[i], i);
            kartlar.Add(kart);
            OyunGrid.Add(kart.Alan, i % 4, i / 4);
        }
        KartBoyutlariniGuncelle();
        SureLabel.Text = "00:00";
        HamleLabel.Text = "0";
        IlerlemeyiGuncelle();
        OyunBaslikLabel.Text = $"{seviyeler[seviye]} · {CiftSayisi} tatlı çift";
        DurumLabel.Text = "Aynı tatlıları bul ve eşleştir!";
        EnIyiSureyiGoster();
    }

    // Kartın iki yüzü aynı alanda durur; görsel kenarlardan boşluk bırakır.
    private OyunKarti KartOlustur(string resimAdi, int sira)
    {
        var resim = new Image { Source = resimAdi, Aspect = Aspect.AspectFit, Margin = 9, IsVisible = false };
        var arka = new Grid { InputTransparent = true };
        arka.Children.Add(new Label
        {
            Text = "♡", FontSize = 51, TextColor = Color.FromArgb("#F8E5F2"),
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
        });
        arka.Children.Add(new Label
        {
            Text = "✦", FontSize = 19, TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
        });
        var ici = new Grid();
        ici.Children.Add(resim);
        ici.Children.Add(arka);
        var kenar = new Border
        {
            Padding = 0, Stroke = Color.FromArgb("#AD8AC8"), StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Background = KapaliKartFirca(),
            Shadow = new Shadow { Brush = Colors.Purple, Offset = new Point(0, 2), Radius = 5, Opacity = .12f },
            Content = ici, HorizontalOptions = LayoutOptions.Fill
        };
        // Dış hücre sabit kalır; kart gizlense de diğer kartlar yer değiştirmez.
        var alan = new Grid();
        var pariltilar = new Grid { InputTransparent = true, IsVisible = false };
        for (int i = 0; i < 5; i++)
            pariltilar.Children.Add(new Label
            {
                Text = i % 2 == 0 ? "✦" : "•", FontSize = i % 2 == 0 ? 19 : 13,
                TextColor = Color.FromArgb(i % 2 == 0 ? "#BF8BC6" : "#E7ABCB"),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
                Opacity = 0, InputTransparent = true
            });
        alan.Children.Add(kenar);
        alan.Children.Add(pariltilar);
        var kart = new OyunKarti { ResimAdi = resimAdi, Alan = alan, Pariltilar = pariltilar, Kenar = kenar, Resim = resim, ArkaYuz = arka };
        SemanticProperties.SetDescription(kenar, $"{sira + 1}. kart, kapalı");
        var dokunma = new TapGestureRecognizer();
        dokunma.Tapped += async (_, _) => await KartaTiklandi(kart);
        kenar.GestureRecognizers.Add(dokunma);
        return kart;
    }

    // Sütun genişliği ekranla değişir; küçük ekranlarda sayfa kaydırılabilir.
    private void KartBoyutlariniGuncelle()
    {
        if (OyunGrid.Width <= 0) return;
        double boyut = Math.Max(1, (OyunGrid.Width - 3 * OyunGrid.ColumnSpacing) / 4);
        foreach (var kart in kartlar)
            kart.Alan.HeightRequest = boyut;
    }

    // Kilit ilk await öncesinde alınır; hızlı dokunmalar üçüncü kartı açamaz.
    private async Task KartaTiklandi(OyunKarti kart)
    {
        if (!oyunEkraninda || kartlarKilitli || oyunBitti || kart.Eslesmis || kart == ilkKart) return;
        var iptal = oyunIptali.Token;
        kartlarKilitli = true;
        try
        {
            if (!kronometre.IsRunning)
            {
                kronometre.Start();
                zamanlayici.Start();
            }
            await KartiCevir(kart, true, iptal);
            if (ilkKart == null)
            {
                ilkKart = kart;
                DurumLabel.Text = "Şimdi eşini bul!";
                return;
            }
            // Yerel referanslar, sıfırlama sırasında alanlar değişse de güvenlidir.
            var onceki = ilkKart;
            hamleSayisi++;
            HamleLabel.Text = hamleSayisi.ToString();
            if (onceki.ResimAdi == kart.ResimAdi)
            {
                onceki.Eslesmis = kart.Eslesmis = true;
                eslesmeSayisi++;
                IlerlemeyiGuncelle();
                DurumLabel.Text = "Harika, bir çift daha!";
                // Son çift bulunduğu anda süre durur; kutlama süreye eklenmez.
                if (eslesmeSayisi == CiftSayisi) OyunuBitir();
                await Task.WhenAll(EslesmeyiKutla(onceki, iptal), EslesmeyiKutla(kart, iptal));
            }
            else
            {
                DurumLabel.Text = "Aklında tut, tekrar dene!";
                await Task.Delay(1000, iptal);
                await Task.WhenAll(KartiCevir(onceki, false, iptal), KartiCevir(kart, false, iptal));
            }
            iptal.ThrowIfCancellationRequested();
            ilkKart = null;
        }
        catch (OperationCanceledException) when (iptal.IsCancellationRequested)
        {
            // Yeni oyuna ait sayaçlara ve kilide dokunulmaz.
        }
        finally
        {
            if (!iptal.IsCancellationRequested) kartlarKilitli = false;
        }
    }

    // Yüz tam kenara döndüğünde değiştirilir; ikinci yarıda resim görünür.
    private static async Task KartiCevir(OyunKarti kart, bool ac, CancellationToken iptal)
    {
        iptal.ThrowIfCancellationRequested();
        await kart.Kenar.RotateYToAsync(90, 120, Easing.CubicIn);
        iptal.ThrowIfCancellationRequested();
        kart.Resim.IsVisible = ac;
        kart.ArkaYuz.IsVisible = !ac;
        kart.Kenar.Background = ac ? new SolidColorBrush(Colors.White) : KapaliKartFirca();
        kart.Kenar.RotationY = -90;
        await kart.Kenar.RotateYToAsync(0, 140, Easing.CubicOut);
        iptal.ThrowIfCancellationRequested();
        kart.Kenar.RotationY = 0;
        SemanticProperties.SetDescription(kart.Kenar, ac ? $"Açık kart: {kart.ResimAdi.Replace('_', ' ').Replace(".png", "")}" : "Kapalı kart");
    }

    // Kapalı yüzün rengi, her kapanışta aynı yumuşak geçişe döner.
    private static Brush KapaliKartFirca() => new LinearGradientBrush
    {
        StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
        GradientStops = new GradientStopCollection
        {
            new GradientStop(Color.FromArgb("#DEC5EC"), 0),
            new GradientStop(Color.FromArgb("#B58ACB"), 1)
        }
    };

    // Önce küçük bir kutlama, sonra kaybolma ve beş hafif parıltı.
    private static async Task EslesmeyiKutla(OyunKarti kart, CancellationToken iptal)
    {
        iptal.ThrowIfCancellationRequested();
        kart.Kenar.InputTransparent = true;
        kart.Kenar.Background = new SolidColorBrush(Color.FromArgb("#FFF0F7"));
        await kart.Kenar.ScaleToAsync(1.05, 130, Easing.CubicOut);
        iptal.ThrowIfCancellationRequested();
        await Task.WhenAll(kart.Kenar.ScaleToAsync(.85, 220, Easing.CubicIn),
            kart.Kenar.FadeToAsync(0, 220, Easing.CubicIn));
        iptal.ThrowIfCancellationRequested();
        kart.Kenar.IsVisible = false;
        kart.Kenar.Scale = 1;
        kart.Kenar.RotationY = 0;
        kart.Pariltilar.IsVisible = true;
        await Task.WhenAll(kart.Pariltilar.Children.OfType<Label>()
            .Select((parilti, i) => PariltiyiOynat(parilti, i, kart.Alan.Width, iptal)));
        iptal.ThrowIfCancellationRequested();
        kart.Pariltilar.IsVisible = false;
    }

    // Parçacıklar kısa bir yay boyunca açılır; dosya, çizim motoru veya paket gerekmez.
    private static async Task PariltiyiOynat(Label parilti, int sira, double genislik, CancellationToken iptal)
    {
        iptal.ThrowIfCancellationRequested();
        double aci = (sira * 72 - 90) * Math.PI / 180;
        double uzaklik = Math.Max(12, genislik * .32);
        parilti.TranslationX = parilti.TranslationY = 0;
        parilti.Scale = .35;
        parilti.Opacity = 0;
        await Task.WhenAll(parilti.FadeToAsync(1, 90), parilti.ScaleToAsync(1.1, 90, Easing.CubicOut));
        iptal.ThrowIfCancellationRequested();
        await Task.WhenAll(
            parilti.TranslateToAsync(Math.Cos(aci) * uzaklik, Math.Sin(aci) * uzaklik, 340, Easing.CubicOut),
            parilti.ScaleToAsync(.4, 340, Easing.CubicIn), parilti.FadeToAsync(0, 340));
        iptal.ThrowIfCancellationRequested();
        parilti.Opacity = 0;
        parilti.Scale = 1;
        parilti.TranslationX = parilti.TranslationY = 0;
    }
    private void IlerlemeyiGuncelle()
    {
        EslesmeLabel.Text = $"{eslesmeSayisi}/{CiftSayisi}";
        IlerlemeLabel.Text = $"{eslesmeSayisi} / {CiftSayisi} çift tamamlandı";
        IlerlemeCubugu.Progress = (double)eslesmeSayisi / CiftSayisi;
    }

    // Eski EnIyiSure kaydı korunur ve hiçbir seviyeye aktarılmaz.
    private void OyunuBitir()
    {
        oyunBitti = true;
        kronometre.Stop();
        zamanlayici.Stop();
        double saniye = kronometre.Elapsed.TotalSeconds;
        int enIyi = Preferences.Default.Get(RekorAnahtari, 0);
        int bitisSuresi = Math.Max(1, (int)Math.Ceiling(saniye));
        bool yeniRekor = enIyi == 0 || bitisSuresi < enIyi;
        if (yeniRekor) Preferences.Default.Set(RekorAnahtari, bitisSuresi);
        // Tamamlanma ve rekor aynı yuvarlama ile gösterilir.
        SureLabel.Text = SureyiYaz(bitisSuresi);
        DurumLabel.Text = "Bütün çiftleri buldun!";
        BitisLabel.Text = $"{seviyeler[seviye]} · {SureyiYaz(bitisSuresi)} · {hamleSayisi} hamle";
        RekorLabel.IsVisible = yeniRekor;
        BitisPanel.IsVisible = true;
        EnIyiSureyiGoster();
    }

    private void EnIyiSureyiGoster()
    {
        int enIyi = Preferences.Default.Get(RekorAnahtari, 0);
        RekorBaslikLabel.Text = $"{seviyeler[seviye]} · EN İYİ SÜRE";
        EnIyiLabel.Text = enIyi > 0 ? SureyiYaz(enIyi) : "Henüz skor yok";
        AnaRekorBaslikLabel.Text = RekorBaslikLabel.Text;
        AnaRekorLabel.Text = EnIyiLabel.Text;
    }

    // Gerçek Button denetimleri pencere açmaz; klavye ve ekran okuyucu ile de seçilebilir.
    private void SecimButonlariniOlustur()
    {
        for (int i = 0; i < seviyeler.Length; i++)
        {
            int secenek = i;
            var buton = SecimButonuOlustur();
            buton.Clicked += (_, _) =>
            {
                if (oyunEkraninda) return;
                seviye = secenek;
                AnaSayfaBilgileriniGuncelle();
            };
            zorlukButonlari.Add(buton);
            ZorlukSecenekleri.Add(buton, i, 0);
        }
        // Yeni temalar listeye eklendiğinde kapsülleri otomatik oluşur ve alt satıra sarılır.
        for (int i = 0; i < temalar.Length; i++)
        {
            int secenek = i;
            var buton = SecimButonuOlustur();
            buton.Margin = new Thickness(0, 0, 7, 4);
            buton.Padding = new Thickness(16, 10);
            buton.Clicked += (_, _) =>
            {
                if (oyunEkraninda) return;
                tema = secenek;
                AnaSayfaBilgileriniGuncelle();
            };
            temaButonlari.Add(buton);
            TemaSecenekleri.Children.Add(buton);
        }
    }

    private static Button SecimButonuOlustur() => new()
    {
        CornerRadius = 20, FontSize = 14, FontAttributes = FontAttributes.Bold,
        MinimumWidthRequest = 0, MinimumHeightRequest = 48,
        Padding = new Thickness(5, 10), BorderWidth = 1
    };

    // Renk, kenarlık ve onay işareti seçimi birlikte belirtir; hızlı seçimde animasyon yarışı olmaz.
    private static void SecimGorunumunuGuncelle(Button buton, string ad, bool secili)
    {
        buton.Text = secili ? $"✓ {ad}" : ad;
        buton.BackgroundColor = Color.FromArgb(secili ? "#E5D4F2" : "#FAF5FD");
        buton.BorderColor = Color.FromArgb(secili ? "#AE85C9" : "#EEE3F5");
        buton.TextColor = Color.FromArgb(secili ? "#633B82" : "#897394");
        SemanticProperties.SetDescription(buton, $"{ad}, {(secili ? "seçili" : "seçili değil")}");
    }

    // Tema metinleri ve ikonu tema tanımından gelir; zorluk bilgisi seçime göre yenilenir.
    private void AnaSayfaBilgileriniGuncelle()
    {
        var secilenTema = temalar[tema];
        MolaBaslikLabel.Text = secilenTema.MolaBasligi;
        MolaAciklamaLabel.Text = secilenTema.MolaAciklamasi;
        MolaIkonu.Source = secilenTema.Ikon;
        SeviyeLabel.Text = $"{secilenTema.Ad} · {seviyeler[seviye]} · {CiftSayisi} çift / {CiftSayisi * 2} kart";
        for (int i = 0; i < zorlukButonlari.Count; i++)
            SecimGorunumunuGuncelle(zorlukButonlari[i], seviyeler[i], i == seviye);
        for (int i = 0; i < temaButonlari.Count; i++)
            SecimGorunumunuGuncelle(temaButonlari[i], temalar[i].Ad, i == tema);
        EnIyiSureyiGoster();
    }
    private void OyunaBasla_Clicked(object? sender, EventArgs e)
    {
        oyunEkraninda = true;
        KartlariHazirla(true);
        AnaSayfaGorunumu.IsVisible = false;
        OyunGorunumu.IsVisible = true;
    }

    // Geri dönüş oyunu sonlandırır, rekorları korur ve bekleyen efektleri iptal eder.
    private void AnaSayfayaDon()
    {
        oyunEkraninda = false;
        KartlariHazirla(false);
        OyunGorunumu.IsVisible = false;
        AnaSayfaGorunumu.IsVisible = true;
        AnaSayfaBilgileriniGuncelle();
    }

    private void AnaSayfa_Clicked(object? sender, EventArgs e) => AnaSayfayaDon();

    protected override bool OnBackButtonPressed()
    {
        if (!oyunEkraninda) return base.OnBackButtonPressed();
        AnaSayfayaDon();
        return true;
    }
    private void OyunAlani_BoyutDegisti(object? sender, EventArgs e) => KartBoyutlariniGuncelle();
    private void YeniOyun_Clicked(object? sender, EventArgs e) => KartlariHazirla(true);
    private void Sifirla_Clicked(object? sender, EventArgs e) => KartlariHazirla(false);

    // Sayfa görünmüyorken UI zamanlayıcısı çalışmaz; geçen süre Stopwatch'ta korunur.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        zamanlayici.Stop();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (oyunEkraninda && kronometre.IsRunning && !oyunBitti) zamanlayici.Start();
    }
}
