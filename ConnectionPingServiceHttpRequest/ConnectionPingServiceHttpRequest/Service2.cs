using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using ConnectionPingService.ConnectionPingInfoModel;
using System.Net.Mail;

namespace ConnectionPingService
{
    public partial class Service2 : ServiceBase
    {
        Timer timer = new Timer();
        private HttpClient httpClient = new HttpClient();
        private ConnectionPingModel _connectionPingModel;
        private string _localIP;
        public Service2()
        {
            _localIP = GetLocalIPAddress();
            _connectionPingModel = GetConnectionPingInfo();
            InitializeComponent();
        }
        public void onDebug()
        {
            OnStart(null);
        }
        static string GetLocalIPAddress()
        {
            string localIP = string.Empty;
            try
            {

                string hostName = Dns.GetHostName();


                IPAddress[] localIPs = Dns.GetHostAddresses(hostName);


                foreach (IPAddress ipAddress in localIPs)
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ipAddress.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("IP Adresi alınamadı: " + ex.Message);
            }
            return localIP;
        }
        private void BaseAddress(string ipAddress)
        {
            if (String.IsNullOrEmpty(_connectionPingModel.Port))
            {
                httpClient.BaseAddress = new Uri("http://localhost/");
            }
            else
            {
                httpClient.BaseAddress = new Uri("http://localhost:"+ _connectionPingModel.Port + "/");
            }

 
        }
        public async Task SendEmail(string logMessage)
        {
            var mail = new MailMessage();
            mail.To.Add(_connectionPingModel.ToMail);
            mail.From = new MailAddress(_connectionPingModel.SmtpFromMail);
            mail.BodyEncoding = Encoding.UTF8;
            mail.Subject = "ALERT! " + _connectionPingModel.ServerInfo + " - Veritabanına Erişimde Hata";
            try
            {

                        mail.Body += "<h2>Connection Ping Service</h2>";
                        mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";
                        mail.Body += "<h4> IP Bilgisi : <strong>" + _localIP + "</strong></h4>";
                        mail.Body += "<h4>EndPoint: <strong>" + httpClient.BaseAddress + _connectionPingModel.EndPoint + " </strong></h4>";
                        mail.Body += "<h4>Server : <strong>" + _connectionPingModel.ServerInfo + "</strong></h4>";
                        mail.Body += "</body></html>";
                        mail.IsBodyHtml = true;
                    



                foreach (var item in _connectionPingModel.CCMail)
                {
                    mail.CC.Add(new MailAddress(item));
                }

                using (SmtpClient client = new SmtpClient())
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_connectionPingModel.SmtpFromMail, _connectionPingModel.SmptPassword);
                    client.Host = "smtp.gmail.com";
                    client.Port = 587;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                     client.Send(mail);
                }
            }
            catch (Exception ex)
            {

                FileWriter(ex.Message);

            }
            
         




        }

        protected override void OnStart(string[] args)
        {

            GetConnectionPingInfo();
            string localIP = GetLocalIPAddress();
            FileWriter("Local IP Adres : " + localIP);
            BaseAddress(localIP);           
            timer.Elapsed += new ElapsedEventHandler(onElapsedTime);
            timer.Interval = Convert.ToDouble(_connectionPingModel.Interval);
            timer.Enabled = true;
            FileWriter("Servis Çalışıyor " + DateTime.Now);

        }

        protected override void OnStop()
        {
            FileWriter("Servis Durdu" + DateTime.Now);
            SendEmail("Servis Durdu Veya Durduruldu: " + DateTime.Now.TimeOfDay.ToString());
        }

        private async void onElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {


                await SendHttpRequest();
               
                FileWriter("Servis çalışmaya devam ediyor " + DateTime.Now);
            }
            catch (AggregateException ex)
            {
                foreach (var innerException in ex.InnerExceptions)
                {
                    FileWriter("Servis çalışırken bir hata oluştu: " + innerException.Message + DateTime.Now.TimeOfDay.ToString());
                    SendEmail("Servis çalışırken bir hata oluştu:" + ex.Message + DateTime.Now.TimeOfDay.ToString());
                }
            }
        }


        private XmlDocument LoadXmlDocument(string filePath)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);
                return doc;
            }
            catch (Exception ex)
            {
                FileWriter("XML Yükleme Hatası: " + ex.Message);
                SendEmail("XML Yükleme Hatası: " + ex.Message + DateTime.Now.TimeOfDay.ToString());
                return null;
            }
        }

        public ConnectionPingModel GetConnectionPingInfo()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConnectionPingInfo.xml");
            ConnectionPingModel connectionPingModel = new ConnectionPingModel();

            try
            {
                XmlDocument doc = LoadXmlDocument(filePath);
                if (doc != null)
                {
                    XmlNode root = doc.DocumentElement;

                    foreach (XmlNode node in root.ChildNodes)
                    {
                        if (node.NodeType == XmlNodeType.Element)
                        {
                            string elementName = node.Name;
                            string elementValue = node.InnerText;

                            switch (elementName)
                            {
                                case "ccMail":
                                    string[] ccMails = elementValue.Split(',');
                                    connectionPingModel.CCMail.AddRange(ccMails);
                                    break;
                                case "toMail":
                                    connectionPingModel.ToMail = elementValue;
                                    break;
                                case "endPoint":
                                    connectionPingModel.EndPoint = elementValue;
                                    break;
                                case "Interval":
                                    connectionPingModel.Interval = elementValue;
                                    break;
                                case "prodFirstServer":
                                    connectionPingModel.ProdFirstServer = elementValue;
                                    break;
                                case "prodSecondServer":
                                    connectionPingModel.ProdSecondServer = elementValue;
                                    break;
                                case "folderName":
                                    connectionPingModel.FolderName = elementValue;
                                    break;
                                case "folderPath":
                                    connectionPingModel.FolderPath = elementValue;
                                    break;
                                case "port":
                                    connectionPingModel.Port = elementValue;
                                    break;
                                case "smtpFromMail":
                                    connectionPingModel.SmtpFromMail = elementValue;
                                    break;
                                case "smptPassword":
                                    connectionPingModel.SmptPassword = elementValue;
                                    break;
                                case "smtpAddress":
                                    connectionPingModel.SmtpAddress = elementValue;
                                    break;                                
                                case "smptPort":
                                    connectionPingModel.SmptPort = int.Parse(elementValue);
                                    break;
                                case "serverInfo":
                                    connectionPingModel.ServerInfo = elementValue;
                                    break;



                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileWriter("Hata Oluştu : " + ex.Message);
            }

            return connectionPingModel;
        }
        private async Task SendHttpRequest()
        {
            try
            {



                var response = await httpClient.GetAsync(_connectionPingModel.EndPoint);


                if (response.IsSuccessStatusCode)
                {

                    var responseContent = await response.Content.ReadAsStringAsync();
                    FileWriter("İstek başarıyla tamamlandı.URL: "+httpClient.BaseAddress+_connectionPingModel.EndPoint+" Yanıt: Status Kodu : " + response.StatusCode + " Content :" + responseContent + "Süre: " + DateTime.Now.TimeOfDay.ToString());
                
                }
                else
                {
                    FileWriter("İstek başarısız. Hata kodu: " + response.StatusCode + DateTime.Now.TimeOfDay.ToString());
                    SendEmail("İstek başarısız. Hata kodu: " + response.StatusCode + DateTime.Now.TimeOfDay.ToString());
                }
            }
            catch (Exception ex)
            {
                FileWriter("Hata oluştu: " + ex.Message);
                SendEmail("Hata oluştu: " + ex.Message);
            }
        }

        public void FileWriter(string message)
        {

            string dosyaYolu = AppDomain.CurrentDomain.BaseDirectory + _connectionPingModel.FolderName;
            if (!Directory.Exists(dosyaYolu))
            {
                Directory.CreateDirectory(dosyaYolu);
            }
            string textYolu = AppDomain.CurrentDomain.BaseDirectory + _connectionPingModel.FolderPath;
            if (!File.Exists(textYolu))
            {
                using (StreamWriter sw = File.CreateText(textYolu))
                {
                    sw.WriteLine(message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(textYolu))
                {

                    sw.WriteLine(message);
                }
            }
        }
    }
}
