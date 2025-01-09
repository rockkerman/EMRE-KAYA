using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq; // Include Newtonsoft.Json for parsing JSON responses

namespace OnlineFlightTicket
{
    class Ucus
    {
        public string Havayolu { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public string Kalkis { get; set; }
        public string Varis { get; set; }
        public DateTime Tarih { get; set; }
        public int BosKoltuk { get; set; }
        public decimal Fiyat { get; set; }
        public List<string> Rezervasyonlar { get; private set; } = new();

        public Ucus(string havayolu, string marka, string model, string kalkis, string varis, DateTime tarih, int bosKoltuk, decimal fiyat)
        {
            Havayolu = havayolu;
            Marka = marka;
            Model = model;
            Kalkis = kalkis;
            Varis = varis;
            Tarih = tarih;
            BosKoltuk = bosKoltuk;
            Fiyat = fiyat;
        }

        public string Bilgi()
        {
            return $"{Havayolu} ({Marka} {Model}): {Kalkis} -> {Varis}, Tarih: {Tarih:dd.MM.yyyy HH:mm}, Boş Koltuk: {BosKoltuk}, Fiyat: {Fiyat:C}, Rezervasyonlar: {Rezervasyonlar.Count}";
        }

        public bool RezervasyonYap(string kullaniciAdi)
        {
            if (BosKoltuk > Rezervasyonlar.Count)
            {
                Rezervasyonlar.Add(kullaniciAdi);
                return true;
            }
            return false;
        }

        public bool RezervasyonuSat(string kullaniciAdi)
        {
            if (Rezervasyonlar.Contains(kullaniciAdi))
            {
                Rezervasyonlar.Remove(kullaniciAdi);
                BosKoltuk--;
                return true;
            }
            return false;
        }

        public bool BiletSat()
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
        public List<Ucus> SatinAlinanUcuslar { get; private set; } = new();
        public List<Ucus> Rezervasyonlar { get; private set; } = new();

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

        public async Task<string> RezervasyonYapAsync(Ucus ucus)
        {
            if (ucus.Tarih <= DateTime.Now)
            {
                return "Rezervasyon başarısız: Uçuş tarihi gelecekte olmalı.";
            }

            if (ucus.RezervasyonYap(KullaniciAdi))
            {
                Rezervasyonlar.Add(ucus);
                string weather = await GetWeatherAsync(ucus.Varis);
                SendNotification("Rezervasyon yapıldı", KullaniciAdi, ucus.Tarih.ToString("dd.MM.yyyy HH:mm"), ucus.Varis, GeneratePNR(), weather, ucus.Fiyat, TCKimlik);
                return $"Rezervasyon yapıldı: {ucus.Kalkis} -> {ucus.Varis}, {ucus.Tarih:dd.MM.yyyy HH:mm}. Hava Durumu: {weather}";
            }
            return "Rezervasyon başarısız: Boş koltuk yok.";
        }

        public async Task<string> RezervasyonuSatAsync(Ucus ucus)
        {
            if (ucus.RezervasyonuSat(KullaniciAdi))
            {
                Rezervasyonlar.Remove(ucus);
                SatinAlinanUcuslar.Add(ucus);
                string weather = await GetWeatherAsync(ucus.Varis);
                SendNotification("Bilet satın alındı", KullaniciAdi, ucus.Tarih.ToString("dd.MM.yyyy HH:mm"), ucus.Varis, GeneratePNR(), weather, ucus.Fiyat, TCKimlik);
                return $"Bilet satın alındı: {ucus.Kalkis} -> {ucus.Varis}, {ucus.Tarih:dd.MM.yyyy HH:mm}. Hava Durumu: {weather}";
            }
            return "Satın alma başarısız: Rezervasyon bulunamadı.";
        }

        private void SendNotification(string subject, string kullaniciAdi, string gidisTarihi, string varisSehri, string pnrKodu, string weather, decimal fiyat, string tcKimlik)
        {
            try
            {
                var fromAddress = new MailAddress("airlineskaya@gmail.com", "Kaya Airlines");
                var toAddress = new MailAddress(Gmail, kullaniciAdi);
                const string fromPassword = "yawu wegy rrcn ktqp"; // Gmail uygulama şifresi

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
                            <p>Sayın {kullaniciAdi},</p>
                            <p>Uçuş bilgilerinize aşağıdan ulaşabilirsiniz:</p>
                            <ul>
                                <li>T.C. Kimlik Numarası: {tcKimlik}</li>
                                <li>Gidiş: {gidisTarihi}</li>
                                <li>Varış: {varisSehri}</li>
                                <li>Fiyat: {fiyat:C}</li>
                                <li>Hava Durumu: {weather}</li>
                            </ul>
                            <p class='pnr'>PNR Kodunuz: {pnrKodu}</p>
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
    }

    class SistemYoneticisi
    {
        public List<Ucus> Ucuslar { get; private set; } = new();
        public List<Kullanici> Kullanicilar { get; private set; } = new();

        public void UcusEkle(Ucus ucus) => Ucuslar.Add(ucus);

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

            sistemYoneticisi.UcusEkle(new Ucus("THY", "Boeing", "737", "Istanbul", "Ankara", new DateTime(2025, 1, 10, 10, 0, 0), 20, 350.50m));
            sistemYoneticisi.UcusEkle(new Ucus("Pegasus", "Airbus", "A320", "Istanbul", "Izmir", new DateTime(2025, 1, 10, 14, 30, 0), 25, 300.00m));
            sistemYoneticisi.UcusEkle(new Ucus("SunExpress", "Embraer", "E190", "Antalya", "Istanbul", new DateTime(2025, 1, 11, 18, 0, 0), 15, 280.75m));
            sistemYoneticisi.UcusEkle(new Ucus("Onur Air", "Boeing", "777", "Izmir", "Antalya", new DateTime(2025, 1, 12, 9, 30, 0), 10, 450.00m));
            sistemYoneticisi.UcusEkle(new Ucus("AtlasGlobal", "Airbus", "A319", "Ankara", "Bodrum", new DateTime(2025, 1, 13, 16, 15, 0), 12, 400.00m));
            sistemYoneticisi.UcusEkle(new Ucus("Corendon", "Boeing", "737", "Istanbul", "Trabzon", new DateTime(2025, 1, 14, 13, 45, 0), 18, 325.25m));
            sistemYoneticisi.UcusEkle(new Ucus("Kaya Airlines", "Airbus", "A380", "Antalya", "London", new DateTime(2025, 1, 15, 8, 0, 0), 30, 950.00m));

            Console.WriteLine("*******");
            Console.WriteLine("*                                             *");
            Console.WriteLine("*          WELCOME TO KAYA AIRLINES           *");
            Console.WriteLine("*                                             *");
            Console.WriteLine("*******");
            Console.WriteLine("*");


            Console.WriteLine("       ██   ██    █████   ██    ██   █████                                            ");
            Console.WriteLine("       ██ ██     ██   ██   ██  ██   ██   ██                                             ");
            Console.WriteLine("       ████      ███████     ██     ███████                                           ");
            Console.WriteLine("       ██ ██     ██   ██     ██     ██   ██                                      ");
            Console.WriteLine("       ██   ██   ██   ██     ██     ██   ██                                      ");
            Console.WriteLine("                                                                     ");

            Console.WriteLine("*");
            Console.WriteLine("       █████   ██  ██████    ██      ██  ████   ██  ██████  ██████  ");
            Console.WriteLine("      ██   ██      ██   ██   ██          ██ ██  ██  ██      ██      ");
            Console.WriteLine("      ███████  ██  ██████    ██      ██  ██  ██ ██  ██████  ██████  ");
            Console.WriteLine("      ██   ██  ██  ██   ██   ██      ██  ██   ████  ██          ██  ");
            Console.WriteLine("      ██   ██  ██  ██    ██  ██████  ██  ██    ███  ██████  ██████  ");
            Console.WriteLine("                                                                     ");
            Console.WriteLine("*");

            Console.WriteLine("*******");

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
                            Console.WriteLine("Geçersiz T.C. Kimlik numarası. 11 haneli ve sadece rakamlardan oluşmalıdır.");
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
                            Console.WriteLine("Giriş başarısız. Kullanıcı adı veya şifre hatalı.\n");
                            break;
                        }
                        Console.WriteLine("Giriş başarılı!\n");

                        while (true)
                        {
                            Console.WriteLine("Menü:");
                            Console.WriteLine("1. Uçuşları Listele\n2. Rezervasyon Yap\n3. Rezervasyon Satın Al\n4. Çıkış");
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
                                    sistemYoneticisi.UcuslariListele();
                                    Console.Write("Satın almak istediğiniz uçuş numarası: ");
                                    if (int.TryParse(Console.ReadLine(), out int satinAlNo) && satinAlNo > 0 && satinAlNo <= sistemYoneticisi.Ucuslar.Count)
                                    {
                                        Console.WriteLine(await kullanici.RezervasyonuSatAsync(sistemYoneticisi.Ucuslar[satinAlNo - 1]));
                                    }
                                    else
                                    {
                                        Console.WriteLine("Geçersiz uçuş numarası.");
                                    }
                                    break;
                                case "4":
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