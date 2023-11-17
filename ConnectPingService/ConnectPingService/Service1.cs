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
using TransportationConnectionMonitoring.ConnectionPingInfoModel;
using System.Text.RegularExpressions;
using System.Net.Mail;

namespace TransportationConnectionMonitoring
{
    public partial class Service1 : ServiceBase
    {
        Timer timer = new Timer();

        private ConnectionPingModel _connectionPingModel;
        private string _localIp;
        private string _serverType;
        public Service1()
        {
            _localIp = GetLocalIPAddress();
            _serverType = GetModeFromIpInfo(_localIp);
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
        protected override void OnStart(string[] args)
        {
            
            GetConnectionPingInfo();
            string localIP = GetLocalIPAddress();
            FileWriter("Local IP Adres : " + localIP, "");
            timer.Elapsed += new ElapsedEventHandler(onElapsedTime);
            timer.Interval = Convert.ToDouble(_connectionPingModel.Interval);
            timer.Enabled = true;
            FileWriter("Servis Çalışmaya Başladı. Tarih: " + DateTime.Now, "IP : "+ _localIp + "");

        }

        protected override void OnStop()
        {
            FileWriter("Servis Durduruldu Süre:" + DateTime.Now.TimeOfDay.ToString(), "IP : " + _localIp + "");
            SendEmail("Servis Durduruldu Süre:" + DateTime.Now.TimeOfDay.ToString(), "IP : " + _localIp + "");

        }

        private async void onElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {



                FirstProdServer();
                SecondProdServer();


                FileWriter("Servis Çalışmaya Devam Ediyor: Süre:" + DateTime.Now.TimeOfDay.ToString(), "IP : " + _localIp + "");
            }
            catch (AggregateException ex)
            {
                foreach (var innerException in ex.InnerExceptions)
                {
                    FileWriter("Servis Çalışırken Bir Hata Oluştu Hata:" + innerException.Message + "Süre: " + DateTime.Now.TimeOfDay.ToString(), "IP : " + _localIp + "");
                    SendEmail("Servis Çalışırken Bir Hata Oluştu Hata:" + innerException.Message + "Süre: " + DateTime.Now.TimeOfDay.ToString(), "IP : " + _localIp + "");

                }
            }
        }
        private string GetModeFromIpInfo(string ipInfo)
        {
            if (ipInfo.Contains(_connectionPingModel.FirstProdServer) || ipInfo.Contains(_connectionPingModel.SecondProdServer))
            {
                return "PROD";
            }
            else
            {
                return "TEST";
            }
        }
        private async Task FirstProdServer()
        {


            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(_connectionPingModel.FirstProdServer);

                    if (response.IsSuccessStatusCode)
                    {
                        FileWriter("Uygulama havuzu çalışıyor. IP Bilgisi : " + _connectionPingModel.FirstProdServer + " Süre : " + DateTime.Now, Regex.Replace(_connectionPingModel.FirstProdServer, "^(http|https)://", ""));
                    }
                    else
                    {
                        FileWriter("Uygulama havuzunda sorun oluştu. IP Bilgisi : " + _connectionPingModel.FirstProdServer + "Status Kodu : " + response.StatusCode + " Süre : " + DateTime.Now, Regex.Replace(_connectionPingModel.FirstProdServer, "^(http|https)://", ""));
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                FileWriter("Uygulama havuzu kapanmış olabilir. IP Bilgisi : " + _connectionPingModel.FirstProdServer + " Hata Mesajı : " + ex.Message + " Süre : " + DateTime.Now, Regex.Replace(_connectionPingModel.FirstProdServer, "^(http|https)://", ""));
                SendEmail("Uygulama havuzu kapanmış olabilir. <br>  IP Bilgisi : " + _connectionPingModel.FirstProdServer + "<br> Hata Mesajı : " + ex.Message + "<br>  Süre : " + DateTime.Now, _connectionPingModel.FirstProdServer);

            }
        }
        private async Task SecondProdServer()
        {


            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(_connectionPingModel.SecondProdServer);

                    if (response.IsSuccessStatusCode)
                    {
                        FileWriter("Uygulama havuzu çalışıyor. IP Bilgisi : " + _connectionPingModel.SecondProdServer + " Süre : " + DateTime.Now, _connectionPingModel.SecondProdServer);
                    }
                    else
                    {
                        FileWriter("Uygulama havuzunda sorun oluştu. IP Bilgisi : " + _connectionPingModel.SecondProdServer + "Status Kodu : " + response.StatusCode + " Süre : " + DateTime.Now, _connectionPingModel.SecondProdServer);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                FileWriter("Uygulama havuzu kapanmış olabilir. IP Bilgisi : " + _connectionPingModel.SecondProdServer + "Hata Mesajı : " + ex.Message + " Süre : " + DateTime.Now, _connectionPingModel.SecondProdServer);
                SendEmail("Uygulama havuzu kapanmış olabilir.<br>  IP Bilgisi : " + _connectionPingModel.SecondProdServer + "<br> Hata Mesajı : " + ex.Message + "<br>  Süre : " + DateTime.Now, _connectionPingModel.SecondProdServer);
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
                FileWriter("XML Yükleme Hatası: " + ex.Message, "IP : " + _localIp + "");
                return null;
            }
        }

        public ConnectionPingModel GetConnectionPingInfo()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TransportationConnectionMonitoring.xml");
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
                                case "firstProdServer":
                                    connectionPingModel.FirstProdServer = elementValue;
                                    break;
                                case "secondProdServer":
                                    connectionPingModel.SecondProdServer = elementValue;
                                    break;
                                case "folderName":
                                    connectionPingModel.FolderName = elementValue;
                                    break;
                                case "folderPathFirst":
                                    connectionPingModel.FolderPathFirst = elementValue;
                                    break;
                                case "folderPathSecond":
                                    connectionPingModel.FolderPathSecond = elementValue;
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
                FileWriter("Hata Oluştu : " + ex.Message, "");
                SendEmail("Hata Oluştu : " + ex.Message + " <br>  Süre : " + DateTime.Now.TimeOfDay.ToString(),"");
            }

            return connectionPingModel;
        }

        public void FileWriter(string message, string IpAddress)
        {
            #region private fields
            string firstProdServerPath = AppDomain.CurrentDomain.BaseDirectory + _connectionPingModel.FolderPathFirst;
            string secondProdServerPath = AppDomain.CurrentDomain.BaseDirectory + _connectionPingModel.FolderPathSecond;
            string filePath = AppDomain.CurrentDomain.BaseDirectory + _connectionPingModel.FolderName;
            string serverPath = AppDomain.CurrentDomain.BaseDirectory + (_connectionPingModel.FolderPathFirst);
            #endregion


            try
            {

                if (!string.IsNullOrEmpty(IpAddress) && IpAddress == _connectionPingModel.SecondProdServer)
                {
                    serverPath = AppDomain.CurrentDomain.BaseDirectory + (_connectionPingModel.FolderPathSecond);
                }

                if (!Directory.Exists(_connectionPingModel.FolderName))
                {
                    Directory.CreateDirectory(_connectionPingModel.FolderName);
                }
                if (!Directory.Exists(_connectionPingModel.FolderPathFirst))
                {
                    Directory.CreateDirectory(_connectionPingModel.FolderPathFirst);
                }
                if (!Directory.Exists(_connectionPingModel.FolderPathSecond))
                {
                    Directory.CreateDirectory(_connectionPingModel.FolderPathSecond);
                }
                if (string.IsNullOrEmpty(IpAddress))
                {
                    WriteLogToServer(firstProdServerPath, message);
                    WriteLogToServer(secondProdServerPath, message);
                }
                else
                {
                    using (StreamWriter sw = File.Exists(serverPath) ? File.AppendText(serverPath) : File.CreateText(serverPath))
                    {
                        sw.WriteLine(message);
                    }
                }
               
            }
            catch (Exception ex)
             {

                using (StreamWriter sw = File.Exists(serverPath) ? File.AppendText(serverPath) : File.CreateText(serverPath))
                {
                    sw.WriteLine("Servis Çalışırken Hata Oluştu "+ex.Message+"  Süre : " + DateTime.Now.TimeOfDay.ToString());
                    SendEmail("Servis Çalışırken Hata Oluştu " + ex.Message + " <br>  Süre : " + DateTime.Now.TimeOfDay.ToString(), IpAddress);
                }
            }
            





        }


        private void WriteLogToServer(string serverPath, string message)
        {
            try
            {
                if (!Directory.Exists(_connectionPingModel.FolderName))
                {
                    Directory.CreateDirectory(_connectionPingModel.FolderName);
                }

                using (StreamWriter sw = File.Exists(serverPath) ? File.AppendText(serverPath) : File.CreateText(serverPath))
                {
                    sw.WriteLine(message);
                }
            }
            catch (Exception ex)
            {

                FileWriter("Hata Oluştu : " + ex.Message, "IP : " + _localIp + "");
            }
            
        }

    

      
        public string SendEmail(string logMessage,string ipInfo)
        {
            var mail = new MailMessage();
            mail.To.Add(_connectionPingModel.ToMail);
            mail.From = new MailAddress(_connectionPingModel.SmtpFromMail);            
            mail.Subject = "ALERT! " + _serverType + " - " + ipInfo + " Ip Adresindeki Servis Erişiminde Hata";
            mail.BodyEncoding = Encoding.UTF8;
            try
            {

                if (String.IsNullOrEmpty(ipInfo))
                {
                    mail.Body = "<html><body>";
                    mail.Body += "<h2>Transportation Connection Monitoring</h2>";
                    mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";                   
                    mail.Body += "<h4>Server : " + _serverType + "</h4>";
                    mail.IsBodyHtml = true;
                }
                else
                {
                    if (ipInfo == _connectionPingModel.FirstProdServer)
                    {
                     
                        mail.Body += "<h2>Transportation Connection Monitoring</h2>";
                        mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";
                        mail.Body += "<h4> IP Bilgisi : <strong>" + _localIp + "</strong></h4>";
                        mail.Body += "<h4>Application Pool: <strong>" + _connectionPingModel.FirstProdServer + " </strong></h4>";
                        mail.Body += "<h4>Server : <strong>" + _serverType + "</strong></h4>";
                        mail.Body += "</body></html>";
                        mail.IsBodyHtml = true;
                    }
                    else
                    {
                      
                        mail.Body += "<h2>Transportation Connection Monitoring</h2>";
                        mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";
                        mail.Body += "<h4>IP Bilgisi : <strong>" + _localIp + "</strong> </h4>";
                        mail.Body += "<h4>Application Pool : <strong>" + _connectionPingModel.SecondProdServer + "</strong> </h4>";
                        mail.Body += "<h4>Server :  <strong>" + _serverType + "</strong> </h4>";
                        mail.Body += "</body></html>";
                        mail.IsBodyHtml = true;
                    }

                 

                }




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

                FileWriter(ex.Message, "IP : " + _localIp + "");

            }

            return string.Empty;




        }
    }
}
