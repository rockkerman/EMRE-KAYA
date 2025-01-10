using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OnlineFlightTicket
{
    abstract class UcusBase
    {
        public string Havayolu { get; set; }
        public string Kalkis { get; set; }
        public string Varis { get; set; }
        public DateTime Tarih { get; set; }
        public int BosKoltuk { get; set; }
        public decimal Fiyat { get; set; }

        protected UcusBase(string havayolu, string kalkis, string varis, DateTime tarih, int bosKoltuk, decimal fiyat)
        {
            Havayolu = havayolu;
            Kalkis = kalkis;
            Varis = varis;
            Tarih = tarih;
            BosKoltuk = bosKoltuk;
            Fiyat = fiyat;
        }

        public abstract bool RezervasyonYap(string kullaniciAdi);

        public string Bilgi()
        {
            return $"{Havayolu}: {Kalkis} -> {Varis}, Tarih: {Tarih:dd.MM.yyyy HH:mm}, Boş Koltuk: {BosKoltuk}, Fiyat: {Fiyat:C}";
        }
    }

    class IhlasUcus : UcusBase
    {
        public IhlasUcus(string havayolu, string kalkis, string varis, DateTime tarih, int bosKoltuk, decimal fiyat)
            : base(havayolu, kalkis, varis, tarih, bosKoltuk, fiyat)
        {
        }

        public override bool RezervasyonYap(string kullaniciAdi)
        {
            if (BosKoltuk > 0)
            {
                BosKoltuk--;
                return true;
            }
            return false;
        }
    }

    class DisHatlarUcus : UcusBase
    {
        public DisHatlarUcus(string havayolu, string kalkis, string varis, DateTime tarih, int bosKoltuk, decimal fiyat)
            : base(havayolu, kalkis, varis, tarih, bosKoltuk, fiyat)
        {
        }

        public override bool RezervasyonYap(string kullaniciAdi)
        {
            if (BosKoltuk > 0)
            {
                BosKoltuk--;
                return true;
            }
            return false;
        }
    }

    class Kullanici
    {
        public string KullaniciAdi { get; set; }
        public string Gmail { get; set; }
        public string TCKimlik { get; set; }
        public string SifreHash { get; set; }
        public List<UcusBase> SatinAlinanUcuslar { get; private set; } = new();
        public List<UcusBase> Rezervasyonlar { get; private set; } = new();

        public Kullanici(string kullaniciAdi, string gmail, string sifre, string tcKimlik)
        {
            if (!TCKimlikKontrol(tcKimlik))
                throw new ArgumentException("T.C. Kimlik numarası 11 haneli olmalı ve geçerli bir formatta olmalı.");

            KullaniciAdi = kullaniciAdi;
            Gmail = gmail;
            TCKimlik = tcKimlik;
            SifreHash = SifreyiHashle(sifre);
        }

        public static bool TCKimlikKontrol(string tcKimlik)
        {
            return tcKimlik.Length == 11 && tcKimlik.All(char.IsDigit) && tcKimlik[0] != '0';
        }

        public static string SifreyiHashle(string sifre)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sifre));
            return Convert.ToBase64String(bytes);
        }

        public bool SifreDogrula(string sifre)
        {
            return SifreyiHashle(sifre) == SifreHash;
        }

        public void MevcutRezervasyonlariListele()
        {
            Console.WriteLine("Rezervasyonlarınız:");
            foreach (var ucus in Rezervasyonlar)
            {
                Console.WriteLine(ucus.Bilgi());
            }
        }

        public void SatinAlinanUcuslariListele()
        {
            Console.WriteLine("Satın Aldığınız Uçuşlar:");
            foreach (var ucus in SatinAlinanUcuslar)
            {
                Console.WriteLine(ucus.Bilgi());
            }
        }

        public async Task<string> RezervasyonYapAsync(UcusBase ucus)
        {
            if (ucus.Tarih <= DateTime.Now)
            {
                return "Rezervasyon başarısız: Uçuş tarihi gelecekte olmalı.";
            }

            if (ucus.RezervasyonYap(KullaniciAdi))
            {
                Rezervasyonlar.Add(ucus);
                string weather = await GetWeatherAsync(ucus.Varis);
                SendNotification("Rezervasyon Yapıldı", ucus);
                return $"Rezervasyon yapıldı: {ucus.Kalkis} -> {ucus.Varis}, {ucus.Tarih:dd.MM.yyyy HH:mm}. Hava Durumu: {weather}";
            }
            return "Rezervasyon başarısız: Boş koltuk yok.";
        }

        public async Task<string> SatinAlAsync(UcusBase ucus)
        {
            if (!Rezervasyonlar.Contains(ucus))
            {
                return "Satın alma başarısız: Önce rezervasyon yapmalısınız.";
            }

            Rezervasyonlar.Remove(ucus);
            SatinAlinanUcuslar.Add(ucus);
            SendNotification("Bilet Satın Alındı", ucus);
            return $"Bilet satın alındı: {ucus.Kalkis} -> {ucus.Varis}, {ucus.Tarih:dd.MM.yyyy HH:mm}.";
        }

        private async Task<string> GetWeatherAsync(string city)
        {
            try
            {
                using var client = new HttpClient();
                string apiKey = "f917260d34a7f5ecfc0d8c6db46eecf0"; // OpenWeatherMap API anahtarı
                string url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric&lang=tr";

                var response = await client.GetStringAsync(url);
                var weatherData = JObject.Parse(response);
                string description = weatherData["weather"][0]["description"].ToString();
                string temperature = weatherData["main"]["temp"].ToString();

                return $"{description}, {temperature}°C";
            }
            catch
            {
                return "Hava durumu alınamadı";
            }
        }

        private void SendNotification(string subject, UcusBase ucus)
        {
            try
            {
                var fromAddress = new MailAddress("airlineskaya@gmail.com", "Kaya Airlines");
                var toAddress = new MailAddress(Gmail, KullaniciAdi);
                const string fromPassword = "yawu wegy rrcn ktqp";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                string htmlContent = $@"
                    <html>
                    <head>
                        <style>
                            body {{
                                font-family: Arial, sans-serif;
                                background-color: #f9f9f9;
                            }}
                            .container {{
                                max-width: 600px;
                                margin: auto;
                                padding: 20px;
                                background-color: white;
                                border: 1px solid #ddd;
                                border-radius: 5px;
                            }}
                            h1 {{
                                color: #333;
                            }}
                            .pnr {{
                                color: red;
                                font-weight: bold;
                            }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h1>Kaya Airlines</h1>
                            <p>Sayın {KullaniciAdi},</p>
                            <p>Uçuş bilgilerinize aşağıdan ulaşabilirsiniz:</p>
                            <ul>
                                <li>Havayolu: {ucus.Havayolu}</li>
                                <li>Kalkış: {ucus.Kalkis}</li>
                                <li>Varış: {ucus.Varis}</li>
                                <li>Tarih: {ucus.Tarih:dd.MM.yyyy HH:mm}</li>
                                <li>Fiyat: {ucus.Fiyat:C}</li>
                                <li>T.C. Kimlik: {TCKimlik}</li>
                            </ul>
                            <p class='pnr'>PNR Kodunuz: {GeneratePNR()}</p>
                            <p>İyi yolculuklar dileriz!</p>
                        </div>
                    </body>
                    </html>";

                using var emailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = htmlContent,
                    IsBodyHtml = true
                };

                smtp.Send(emailMessage);
                Console.WriteLine("[E-posta gönderildi]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[E-posta gönderim hatası]: {ex.Message}");
            }
        }

        private string GeneratePNR()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    class SistemYoneticisi
    {
        public List<UcusBase> Ucuslar { get; private set; } = new();
        public List<Kullanici> Kullanicilar { get; private set; } = new();

        public void UcusEkle(UcusBase ucus) => Ucuslar.Add(ucus);

        public void KullaniciKaydet(Kullanici kullanici) => Kullanicilar.Add(kullanici);

        public Kullanici GirisYap(string kullaniciAdi, string sifre)
        {
            var kullanici = Kullanicilar.FirstOrDefault(k => k.KullaniciAdi == kullaniciAdi);
            return kullanici != null && kullanici.SifreDogrula(sifre) ? kullanici : null;
        }

        public void UcuslariListele()
        {
            Console.WriteLine("\nMevcut Uçuşlar:");
            for (int i = 0; i < Ucuslar.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {Ucuslar[i].Bilgi()}");
            }
        }
    }

    class Program
    {
        static async Task Main()
        {
            var sistemYoneticisi = new SistemYoneticisi();

            sistemYoneticisi.UcusEkle(new IhlasUcus("THY", "Istanbul", "Ankara", new DateTime(2025, 2, 10, 10, 0, 0), 20, 350.50m));
            sistemYoneticisi.UcusEkle(new DisHatlarUcus("Kaya Airlines", "Antalya", "London", new DateTime(2025, 2, 15, 8, 0, 0), 30, 950.00m));
            sistemYoneticisi.UcusEkle(new IhlasUcus("Pegasus", "Izmir", "Istanbul", new DateTime(2025, 2, 18, 12, 0, 0), 25, 300.75m));
            sistemYoneticisi.UcusEkle(new DisHatlarUcus("SunExpress", "Ankara", "Berlin", new DateTime(2025, 2, 20, 14, 0, 0), 15, 600.00m));
            sistemYoneticisi.UcusEkle(new IhlasUcus("AnadoluJet", "Trabzon", "Izmir", new DateTime(2025, 2, 25, 16, 30, 0), 18, 275.00m));

            Console.WriteLine("Welcome to Kaya Airlines!");

            while (true)
            {
                Console.WriteLine("\nMenü:");
                Console.WriteLine("1. Kayıt Ol\n2. Giriş Yap\n3. Çıkış");
                Console.Write("Bir seçenek giriniz: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        Console.Write("Kullanıcı Adı: ");
                        string yeniKullaniciAdi = Console.ReadLine();
                        Console.Write("Gmail: ");
                        string yeniGmail = Console.ReadLine();
                        Console.Write("T.C. Kimlik Numarası: ");
                        string yeniTCKimlik = Console.ReadLine();
                        Console.Write("Şifre: ");
                        string yeniSifre = Console.ReadLine();

                        if (!Kullanici.TCKimlikKontrol(yeniTCKimlik))
                        {
                            Console.WriteLine("Geçersiz T.C. Kimlik numarası.");
                            break;
                        }

                        try
                        {
                            var yeniKullanici = new Kullanici(yeniKullaniciAdi, yeniGmail, yeniSifre, yeniTCKimlik);
                            sistemYoneticisi.KullaniciKaydet(yeniKullanici);
                            Console.WriteLine("Kayıt başarılı!\n");
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine($"Hata: {ex.Message}");
                        }
                        break;

                    case "2":
                        Console.Write("Kullanıcı Adı: ");
                        string kullaniciAdi = Console.ReadLine();
                        Console.Write("Şifre: ");
                        string sifre = Console.ReadLine();
                        var kullanici = sistemYoneticisi.GirisYap(kullaniciAdi, sifre);

                        if (kullanici == null)
                        {
                            Console.WriteLine("Giriş başarısız.\n");
                            break;
                        }

                        Console.WriteLine("Giriş başarılı!\n");

                        while (true)
                        {
                            Console.WriteLine("Menü:");
                            Console.WriteLine("1. Uçuşları Listele\n2. Rezervasyon Yap\n3. Rezervasyonları Görüntüle\n4. Satın Alma\n5. Çıkış");
                            Console.Write("Bir seçenek giriniz: ");

                            switch (Console.ReadLine())
                            {
                                case "1":
                                    sistemYoneticisi.UcuslariListele();
                                    break;
                                case "2":
                                    sistemYoneticisi.UcuslariListele();
                                    Console.Write("Rezervasyon yapmak istediğiniz uçuş numarası: ");
                                    if (int.TryParse(Console.ReadLine(), out int rezervasyonNo) && rezervasyonNo > 0 && rezervasyonNo <= sistemYoneticisi.Ucuslar.Count)
                                    {
                                        Console.WriteLine(await kullanici.RezervasyonYapAsync(sistemYoneticisi.Ucuslar[rezervasyonNo - 1]));
                                    }
                                    else
                                    {
                                        Console.WriteLine("Geçersiz uçuş numarası.");
                                    }
                                    break;
                                case "3":
                                    kullanici.MevcutRezervasyonlariListele();
                                    break;
                                case "4":
                                    kullanici.MevcutRezervasyonlariListele();
                                    Console.Write("Satın almak istediğiniz uçuş numarası: ");
                                    if (int.TryParse(Console.ReadLine(), out int satinAlNo) && satinAlNo > 0 && satinAlNo <= sistemYoneticisi.Ucuslar.Count)
                                    {
                                        Console.WriteLine(await kullanici.SatinAlAsync(sistemYoneticisi.Ucuslar[satinAlNo - 1]));
                                    }
                                    else
                                    {
                                        Console.WriteLine("Geçersiz uçuş numarası.");
                                    }
                                    break;
                                case "5":
                                    Console.WriteLine("Çıkış yapıldı.");
                                    return;
                                default:
                                    Console.WriteLine("Geçersiz seçenek.");
                                    break;
                            }
                        }
                    case "3":
                        Console.WriteLine("Çıkış yapıldı.");
                        return;
                    default:
                        Console.WriteLine("Geçersiz seçenek.");
                        break;
                }
            }
        }
    }
} 
