using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Net;

namespace KayaAirlines
{
    // Kullanıcı için soyut sınıf
    public abstract class User
    {
        public string KullaniciAdi { get; set; }
        public string Sifre { get; set; }
        public string Eposta { get; set; }
        public string TcKimlikNo { get; set; }

        public abstract void BilgileriGoster();
    }

    // Müşteri sınıfı
    public class Musteri : User
    {
        public override void BilgileriGoster()
        {
            Console.WriteLine($"Kullanıcı Adı: {KullaniciAdi}, E-posta: {Eposta}, T.C. Kimlik No: {TcKimlikNo}");
        }

        public virtual void MusteriMesaj()
        {
            Console.WriteLine("Sayın Müşterimiz, Kaya Airlines ailesine hoş geldiniz.");
        }

        public static bool TcKimlikNoDogrula(string tcKimlikNo)
        {
            return tcKimlikNo.Length == 11 && long.TryParse(tcKimlikNo, out _);
        }

        public static string HashSifre(string sifre)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] sifreBytes = Encoding.UTF8.GetBytes(sifre);
                byte[] hashBytes = sha256.ComputeHash(sifreBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }

    // Özel müşteri sınıfı
    public class OzelMusteri : Musteri
    {
        public override void MusteriMesaj()
        {
            Console.WriteLine("Sayın Özel Müşterimiz, Kaya Airlines'ın premium hizmetlerinden yararlanmaya hoş geldiniz.");
        }
    }

    // Uçuş bilgisi sınıfı
    public class Ucus
    {
        public string UcusNo { get; set; }
        public string KalkisYeri { get; set; }
        public string VarisYeri { get; set; }
        public DateTime Tarih { get; set; }
        public double Fiyat { get; set; }

        public void DinamikFiyatHesapla()
        {
            TimeSpan kalanSure = Tarih - DateTime.Now;
            if (kalanSure.TotalDays <= 7)
            {
                Fiyat *= 1.2; // Son 7 gün: %20 zam
            }
            else if (kalanSure.TotalDays <= 30)
            {
                Fiyat *= 1.1; // Son 30 gün: %10 zam
            }
        }

        public void UcusBilgileriniGoster()
        {
            Console.WriteLine($"Uçuş No: {UcusNo}, Kalkış: {KalkisYeri}, Varış: {VarisYeri}, Tarih: {Tarih}, Fiyat: {Fiyat:F2} TL");
        }

        public virtual void UcusMesaji()
        {
            Console.WriteLine("Uçuş bilgilerinizi lütfen kontrol edin.");
        }
    }

    // Uluslararası uçuş sınıfı
    public class UluslararasiUcus : Ucus
    {
        public override void UcusMesaji()
        {
            Console.WriteLine("Bu, uluslararası bir uçuş. Pasaport ve vize bilgilerinizi kontrol etmeyi unutmayın.");
        }
    }

    class Program
    {
        static List<Musteri> musteriler = new List<Musteri>();
        static List<Ucus> ucuslar = new List<Ucus>();
        static List<Ucus> rezervasyonlar = new List<Ucus>();
        static List<Ucus> satinAlinanUcuslar = new List<Ucus>();

        static void Main(string[] args)
        {
            // Örnek uçuşlar ekleyelim
            ucuslar.Add(new Ucus { UcusNo = "TK1001", KalkisYeri = "İstanbul", VarisYeri = "Ankara", Tarih = new DateTime(2025, 2, 15, 10, 30, 0), Fiyat = 500 });
            ucuslar.Add(new Ucus { UcusNo = "TK1002", KalkisYeri = "İstanbul", VarisYeri = "İzmir", Tarih = new DateTime(2025, 2, 16, 14, 0, 0), Fiyat = 450 });
            ucuslar.Add(new UluslararasiUcus { UcusNo = "TK2001", KalkisYeri = "İstanbul", VarisYeri = "Londra", Tarih = new DateTime(2025, 3, 20, 14, 0, 0), Fiyat = 2000 });
            ucuslar.Add(new UluslararasiUcus { UcusNo = "TK2002", KalkisYeri = "Ankara", VarisYeri = "Berlin", Tarih = new DateTime(2025, 3, 25, 10, 30, 0), Fiyat = 2200 });
            ucuslar.Add(new Ucus { UcusNo = "TK1003", KalkisYeri = "İzmir", VarisYeri = "Antalya", Tarih = new DateTime(2025, 2, 18, 9, 0, 0), Fiyat = 300 });

            foreach (var ucus in ucuslar)
            {
                ucus.DinamikFiyatHesapla();
            }

            while (true)
            {
                Console.WriteLine("Kaya Airlines Bilet Sistemine Hoş Geldiniz!");
                Console.WriteLine("1. Kayıt Ol\n2. Giriş Yap\n3. Çıkış Yap");
                Console.Write("Bir seçenek seçin: ");
                string secim = Console.ReadLine();

                switch (secim)
                {
                    case "1":
                        KayitOl();
                        break;
                    case "2":
                        GirisYap();
                        break;
                    case "3":
                        Console.WriteLine("Kaya Airlines Bilet Sistemini kullandığınız için teşekkür ederiz!");
                        return;
                    default:
                        Console.WriteLine("Geçersiz seçenek. Lütfen tekrar deneyin.");
                        break;
                }
            }
        }

        static void KayitOl()
        {
            Console.Write("Kullanıcı adınızı girin: ");
            string kullaniciAdi = Console.ReadLine();

            Console.Write("Şifrenizi girin: ");
            string sifre = Console.ReadLine();
            string hashedSifre = Musteri.HashSifre(sifre);

            Console.Write("E-posta adresinizi girin: ");
            string eposta = Console.ReadLine();

            Console.Write("T.C. kimlik numaranızı girin: ");
            string tcKimlikNo = Console.ReadLine();

            while (!Musteri.TcKimlikNoDogrula(tcKimlikNo))
            {
                Console.WriteLine("Geçersiz T.C. kimlik numarası. Lütfen 11 haneli bir numara girin.");
                tcKimlikNo = Console.ReadLine();
            }

            Musteri musteri = new Musteri
            {
                KullaniciAdi = kullaniciAdi,
                Sifre = hashedSifre,
                Eposta = eposta,
                TcKimlikNo = tcKimlikNo
            };

            musteriler.Add(musteri);
            Console.WriteLine("Kayıt başarılı!");
        }

        static void GirisYap()
        {
            Console.Write("Kullanıcı adınızı girin: ");
            string kullaniciAdi = Console.ReadLine();

            Console.Write("Şifrenizi girin: ");
            string sifre = Console.ReadLine();
            string hashedSifre = Musteri.HashSifre(sifre);

            Musteri girisYapanMusteri = musteriler.Find(m => m.KullaniciAdi == kullaniciAdi && m.Sifre == hashedSifre);

            if (girisYapanMusteri != null)
            {
                Console.WriteLine("Giriş başarılı! Hoş geldiniz, " + girisYapanMusteri.KullaniciAdi + "!");
                girisYapanMusteri.MusteriMesaj();
                AnaMenu(girisYapanMusteri);
            }
            else
            {
                Console.WriteLine("Geçersiz kullanıcı adı veya şifre. Lütfen tekrar deneyin.");
            }
        }

        static void AnaMenu(Musteri musteri)
        {
            while (true)
            {
                Console.WriteLine("\nKaya Airlines Ana Menü:");
                Console.WriteLine("1. Uçuşları Listele");
                Console.WriteLine("2. Rezervasyon Yapılan Uçuşları Görüntüle");
                Console.WriteLine("3. Satın Alınan Uçuşları Görüntüle");
                Console.WriteLine("4. Çıkış Yap");
                Console.Write("Bir seçenek seçin: ");
                string secim = Console.ReadLine();

                switch (secim)
                {
                    case "1":
                        UcuslariListele(musteri);
                        break;
                    case "2":
                        RezervasyonlariGoruntule();
                        break;
                    case "3":
                        SatinAlinanUcuslariGoruntule();
                        break;
                    case "4":
                        Console.WriteLine("Çıkış yapılıyor...");
                        return;
                    default:
                        Console.WriteLine("Geçersiz seçenek. Lütfen tekrar deneyin.");
                        break;
                }
            }
        }

        static void UcuslariListele(Musteri musteri)
        {
            Console.WriteLine("Mevcut Uçuşlar:");
            for (int i = 0; i < ucuslar.Count; i++)
            {
                Console.WriteLine($"{i + 1}. ");
                ucuslar[i].UcusBilgileriniGoster();
                ucuslar[i].UcusMesaji();
            }

            Console.Write("Rezervasyon yapmak istediğiniz uçuşun numarasını seçin: ");
            if (int.TryParse(Console.ReadLine(), out int secim) && secim > 0 && secim <= ucuslar.Count)
            {
                Ucus secilenUcus = ucuslar[secim - 1];
                Console.WriteLine("Rezervasyon başarılı! Rezervasyon yaptığınız uçuş:");
                secilenUcus.UcusBilgileriniGoster();
                rezervasyonlar.Add(secilenUcus);

                Console.Write("Bu uçuşu satın almak ister misiniz? (E/H): ");
                string satinAlma = Console.ReadLine();

                if (satinAlma.ToUpper() == "E")
                {
                    Console.WriteLine("Satın alma işlemi başarılı! Uçuş detayları:");
                    secilenUcus.UcusBilgileriniGoster();
                    satinAlinanUcuslar.Add(secilenUcus);
                    MailGonder(musteri, secilenUcus);
                }
                else
                {
                    Console.WriteLine("Satın alma işlemi iptal edildi.");
                }
            }
            else
            {
                Console.WriteLine("Geçersiz seçim. Lütfen tekrar deneyin.");
            }
        }

        static void RezervasyonlariGoruntule()
        {
            if (rezervasyonlar.Count == 0)
            {
                Console.WriteLine("Henüz rezervasyon yapılmış bir uçuş bulunmamaktadır.");
            }
            else
            {
                Console.WriteLine("Rezervasyon Yapılan Uçuşlar:");
                foreach (var ucus in rezervasyonlar)
                {
                    ucus.UcusBilgileriniGoster();
                }
            }
        }

        static void SatinAlinanUcuslariGoruntule()
        {
            if (satinAlinanUcuslar.Count == 0)
            {
                Console.WriteLine("Henüz satın alınmış bir uçuş bulunmamaktadır.");
            }
            else
            {
                Console.WriteLine("Satın Alınan Uçuşlar:");
                foreach (var ucus in satinAlinanUcuslar)
                {
                    ucus.UcusBilgileriniGoster();
                }
            }
        }

        static void MailGonder(Musteri musteri, Ucus ucus)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("your_email@gmail.com");
                mail.To.Add(musteri.Eposta);
                mail.Subject = "Satın Alma Onayı - Kaya Airlines";

                string htmlBody = $@"<html>
                                    <body>
                                        <h1>Kaya Airlines</h1>
                                        <p>Sayın {musteri.KullaniciAdi},</p>
                                        <p>Uçuş bilgilerinize aşağıdan ulaşabilirsiniz:</p>
                                        <ul>
                                            <li><strong>Havayolu:</strong> Kaya Airlines</li>
                                            <li><strong>Kalkış:</strong> {ucus.KalkisYeri}</li>
                                            <li><strong>Varış:</strong> {ucus.VarisYeri}</li>
                                            <li><strong>Tarih:</strong> {ucus.Tarih}</li>
                                            <li><strong>Fiyat:</strong> {ucus.Fiyat:F2} TL</li>
                                            <li><strong>T.C. Kimlik:</strong> {musteri.TcKimlikNo}</li>
                                        </ul>
                                        <p><strong style='color: red;'>PNR Kodunuz:</strong> {Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}</p>
                                        <p>İyi yolculuklar dileriz!</p>
                                    </body>
                                  </html>";

                mail.IsBodyHtml = true;
                mail.Body = htmlBody;

                smtpServer.Port = 587;
                smtpServer.Credentials = new NetworkCredential("airlineskaya@gmail.com", "yawu wegy rrcn ktqp");
                smtpServer.EnableSsl = true;

                smtpServer.Send(mail);
                Console.WriteLine("Onay e-postası başarıyla gönderildi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"E-posta gönderiminde bir hata oluştu: {ex.Message}");
            }
        }
    }
}
