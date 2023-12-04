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
        #region privateFields
        private List<ConnectionPingModel> _connectionPingModels = new List<ConnectionPingModel>();
        private DateTime lastRequestTime;    
        private static readonly object lockObject = new object();
        private string _localIp;
        private string _serverType;
        #endregion

        public Service1()
        {

            InitializeComponent();
        }
        public void onDebug()
        {
            OnStart(null);
        }
        static string Get_localIpAddress()
        {
            string _localIp = string.Empty;
            try
            {

                string hostName = Dns.GetHostName();


                IPAddress[] _localIps = Dns.GetHostAddresses(hostName);


                foreach (IPAddress ipAddress in _localIps)
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        _localIp = ipAddress.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("IP Adresi alınamadı: " + ex.Message);
            }
            return _localIp;
        }
        protected override void OnStart(string[] args)
        {

            _connectionPingModels = GetConnectionPingInfo();
            _localIp = Get_localIpAddress();
             FileWriter("Local IP Adres Bilgisi : " + _localIp, "", "");
            _serverType = GetModeFromIpInfo(_localIp);
            timer.Elapsed += new ElapsedEventHandler(onElapsedTime);
            timer.Interval = Convert.ToDouble(_connectionPingModels[0].Interval);
            timer.Enabled = true;
            FileWriter("Servis Çalışmaya Başladı. Tarih: " + DateTime.Now, "", "");

        }

        protected override void OnStop()
        {
            FileWriter("Servis Durduruldu Süre:" + DateTime.Now.TimeOfDay.ToString(), "", "");
            SendEmail("Servis Durduruldu Süre:" + DateTime.Now.TimeOfDay.ToString(), "", "");

        }

        private async void onElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                MoveFileBySize();

                DetectServerStatus();

                FileWriter("Servis Çalışmaya Devam Ediyor: Süre:" + DateTime.Now.TimeOfDay.ToString(), "", "");
            }
            catch (AggregateException ex)
            {
                foreach (var innerException in ex.InnerExceptions)
                {
                    FileWriter("Servis Çalışırken Bir Hata Oluştu Hata:" + innerException.Message + "Süre: " + DateTime.Now.TimeOfDay.ToString(), "", "");

                    SendEmail("Servis Çalışırken Bir Hata Oluştu Hata:" + innerException.Message + "Süre: " + DateTime.Now.TimeOfDay.ToString(), "", "");

                }
            }
        }

        public List<ConnectionPingModel> GetConnectionPingInfo()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppWinServicesInfo.xml");

            try
            {
                XmlDocument doc = LoadXmlDocument(filePath);
                if (doc != null)
                {
                    XmlNodeList rootNodes = doc.SelectNodes("/roots/root");

                    for (int i = 0; i < rootNodes.Count; i++)
                    {
                        XmlNode rootNode = rootNodes[i];
                        ConnectionPingModel connectionPingModel = new ConnectionPingModel();

                        // Mail bilgilerini sadece ilk root elemanına ekle
                       
                            XmlNode mailNode = doc.SelectSingleNode("/roots/mail");

                            foreach (XmlNode mailChildNode in mailNode.ChildNodes)
                            {
                                if (mailChildNode.NodeType == XmlNodeType.Element)
                                {
                                    string mailElementName = mailChildNode.Name;
                                    string mailElementValue = mailChildNode.InnerText;

                                    switch (mailElementName)
                                    {
                                        case "smtpFromMail":
                                            connectionPingModel.SmtpFromMail = mailElementValue;
                                            break;
                                        case "smptPassword":
                                            connectionPingModel.SmptPassword = mailElementValue;
                                            break;
                                        case "smtpAddress":
                                            connectionPingModel.SmtpAddress = mailElementValue;
                                            break;
                                        case "smptPort":
                                            connectionPingModel.SmptPort = int.Parse(mailElementValue);
                                            break;
                                    case "ccMail":
                                        string[] ccMails = mailElementValue.Split(',');
                                        connectionPingModel.CCMail.AddRange(ccMails);
                                        break;
                                    case "toMail":
                                        connectionPingModel.ToMail = mailElementValue;
                                        break;
                                }
                                }
                            }
                        XmlNode timeNode = doc.SelectSingleNode("/roots/timeInfo");

                        foreach (XmlNode timeChildNode in timeNode.ChildNodes)
                        {
                            if (timeChildNode.NodeType == XmlNodeType.Element)
                            {
                                string timeElementName = timeChildNode.Name;
                                string timeElementValue = timeChildNode.InnerText;

                                switch (timeElementName)
                                {
                                    case "Interval":
                                        connectionPingModel.Interval = timeElementValue;
                                        break; 
                                    case "fileSize":
                                        connectionPingModel.FileSize = int.Parse(timeElementValue);
                                        break;
                                    case "mailWaitTimeMinute":
                                        connectionPingModel.mailWaitTimeMinute = int.Parse(timeElementValue);
                                        break;
                                }
                            }
                        }

                        foreach (XmlNode node in rootNode.ChildNodes)
                        {
                            if (node.NodeType == XmlNodeType.Element)
                            {
                                string elementName = node.Name;
                                string elementValue = node.InnerText;

                                switch (elementName)
                                {
                                   
                                    case "endPoint":
                                        connectionPingModel.EndPoint = elementValue;
                                        break;
                                    case "Interval":
                                        connectionPingModel.Interval = elementValue;
                                        break;
                                    case "prodServer":
                                        connectionPingModel.ProdServer = elementValue;
                                        break;                                  
                                    case "applicationPoolName":
                                        connectionPingModel.ApplicationPoolName = elementValue;
                                        break;
                                    case "port":
                                        connectionPingModel.Port = elementValue;
                                        break;
                                    case "mailTitle":
                                        connectionPingModel.MailTitle = elementValue;
                                        break;
                                    case "folderPath":
                                        connectionPingModel.FolderPath = elementValue;
                                        break;
                                    case "logFileNameIpEnd":
                                        connectionPingModel.LogFileNameIpEnd = elementValue;
                                        break;
                                }
                            }
                        }

                        
                        _connectionPingModels.Add(connectionPingModel);
                    }
                }
            }
           
            catch (Exception ex)
            {
                FileWriter("Hata Oluştu : " + ex.Message, "", "");
                SendEmail("Hata Oluştu : " + ex.Message + " <br>  Süre : " + DateTime.Now.TimeOfDay.ToString(), "", "");
            }

            return _connectionPingModels;
        }

        private string GetModeFromIpInfo(string ipInfo)
        {
            for (int i = 0; i < _connectionPingModels.Count - 1; i++)
            {
                if (ipInfo.Contains(_connectionPingModels[i].ProdServer))
                {
                    return "PROD";
                }
            }

            return "TEST";
        }

        public async Task DetectServerStatus()
        {
            try
            {
                foreach (var connectionPingModel in _connectionPingModels)
                {
                    if (connectionPingModel.ProdServer != null && connectionPingModel.EndPoint == null)
                    {
                        await CheckServerStatus(connectionPingModel.ProdServer);
                    }
                }
                await SendHttpRequest();
            }
            catch (Exception ex)
            {

                FileWriter(ex.Message, "", "");
            }

        }
        private async Task CheckServerStatus(string serverUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(serverUrl);
                    HttpResponseMessage response = await client.GetAsync(serverUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        FileWriter($"Uygulama havuzu çalışıyor. IP Bilgisi: {serverUrl} Süre: {DateTime.Now}", Regex.Replace(serverUrl, "^(http|https)://", ""), "");

                    }
                    else
                    {
                        FileWriter($"Uygulama havuzunda sorun oluştu. IP Bilgisi: {serverUrl} Status Kodu: {response.StatusCode} Süre: {DateTime.Now}", Regex.Replace(serverUrl, "^(http|https)://", ""), "");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                FileWriter($"Uygulama havuzu kapanmış olabilir. IP Bilgisi: {serverUrl} Hata Mesajı: {ex.Message} Süre: {DateTime.Now}", Regex.Replace(serverUrl, "^(http|https)://", ""), "");
                SendEmail($"Uygulama havuzu kapanmış veya bağlantı kopmuş olabilir . <br> IP Bilgisi: {serverUrl}<br> Hata Mesajı: {ex.Message}<br> Süre: {DateTime.Now}", serverUrl, "");
            }
        }

        private async Task SendHttpRequest()
        {

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    BaseAddress(httpClient, _localIp);


                    var response = await httpClient.GetAsync(_connectionPingModels[2].EndPoint);

                    if (response.IsSuccessStatusCode)
                    {

                        var responseContent = await response.Content.ReadAsStringAsync();
                        FileWriter("İstek başarıyla tamamlandı.URL: " + httpClient.BaseAddress + _connectionPingModels[2].EndPoint + " Yanıt: Status Kodu : " + response.StatusCode + " Content :" + responseContent + "Süre: " + DateTime.Now.TimeOfDay.ToString(), "", _connectionPingModels[2].EndPoint);

                    }
                    else
                    {
                        FileWriter("İstek başarısız. Hata kodu: " + response.StatusCode +" "+ DateTime.Now.TimeOfDay.ToString(), "", _connectionPingModels[2].EndPoint);
                        SendEmail("İstek başarısız. Hata kodu: " + response.StatusCode + " " + DateTime.Now.TimeOfDay.ToString(), "", _connectionPingModels[2].EndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                FileWriter("Hata oluştu: " + ex.InnerException.Message, " || " + ex.InnerException.InnerException.Message + "", _connectionPingModels[2].EndPoint);
                SendEmail("Hata oluştu: " + ex.InnerException.Message , "", _connectionPingModels[2].EndPoint);
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
                FileWriter("XML Yükleme Hatası: " + ex.Message, "", "");
                return null;
            }
        }

        int i = 0;

        public void FileWriter(string message, string ipAddress, string endPoint)
        {
            List<string> fileNames = GetFileNames(ipAddress, endPoint);
            try
            {
                foreach (var connectionPingModel in _connectionPingModels)
                {

                    string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, connectionPingModel.FolderPath);

                    if (string.IsNullOrEmpty(ipAddress) && string.IsNullOrEmpty(endPoint))
                    {
                        WriteLogToServerAsync(folderPath, message);
                    }
                }
                foreach (var fileName in fileNames)
                {

                    if (!string.IsNullOrEmpty(ipAddress))
                    {
                        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                        using (StreamWriter sw = File.Exists(filePath) ? File.AppendText(filePath) : File.CreateText(filePath))
                        {
                            sw.WriteLine(message);
                        }
                    }

                }

                if (!string.IsNullOrEmpty(endPoint))
                {
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _connectionPingModels[2].FolderPath);

                    using (StreamWriter sw = File.Exists(filePath) ? File.AppendText(filePath) : File.CreateText(filePath))
                    {

                        sw.WriteLine(message);
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                for (int i = 0; i < _connectionPingModels.Count; i++)
                {
                    string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _connectionPingModels[i].FolderPath);
                    WriteLogToServerAsync(FilePath, ex.Message);
                }
            }
        }

        public void MoveFileBySize()
        {


            foreach (var connectionPingModel in _connectionPingModels)
            {
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, connectionPingModel.FolderPath);
                FileInfo fileInfo = new FileInfo(folderPath);


                double megaByte = fileInfo.Length / (1024.0 * 1024.0);

                if (megaByte >= connectionPingModel.FileSize)
                {
                    NewCreateAndMoveFile(folderPath, Path.GetFileNameWithoutExtension(connectionPingModel.FolderPath),connectionPingModel.EndPoint);
                }
            }


        }

        public void NewCreateAndMoveFile(string sourceDirectory, string folderPath,string endPoint)
        {
            string formatDate = DateTime.Now.ToString("yyyyMMdd");
            string destinationDirectory = AppDomain.CurrentDomain.BaseDirectory + @"Log\" + folderPath + "" + formatDate;

            Directory.CreateDirectory(destinationDirectory);

            try
            {
                string fileName = Path.GetFileName(sourceDirectory);

                string destinationPath = Path.Combine(destinationDirectory, fileName);

                File.Move(sourceDirectory, destinationPath);
                FileWriter("Dosya başarıyla taşındı.", endPoint == null ? _localIp: endPoint, endPoint == null ? "" : endPoint);
              
            }
            catch (Exception ex)
            {
                FileWriter("Hata oluştu: " + ex.InnerException.Message + " " , endPoint == null ? _localIp : endPoint, endPoint == null ? "" : endPoint);
            }
        }

        private List<string> GetFileNames(string ipAddress, string endPoint)
        {
            List<string> fileNames = new List<string>();


            if (!String.IsNullOrEmpty(ipAddress))
            {

                if (ipAddress.EndsWith(_connectionPingModels[0].LogFileNameIpEnd))
                {
                    fileNames.Add(_connectionPingModels[0].FolderPath);
                }

                if (ipAddress.EndsWith(_connectionPingModels[1].LogFileNameIpEnd))
                {
                    fileNames.Add(_connectionPingModels[1].FolderPath);
                }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    fileNames.Add(_connectionPingModels[i].FolderPath);
                }
            }

            return fileNames;
        }


        private async Task WriteLogToServerAsync(string serverPath, string message)
        {
            try
            {

                using (StreamWriter sw = File.Exists(serverPath) ? File.AppendText(serverPath) : File.CreateText(serverPath))
                {
                    await sw.WriteLineAsync(message);
                }
            }
            catch (Exception ex)
            {
                FileWriter("Hata Oluştu : " + ex.Message, "", "");
            }
        }


        private void BaseAddress(HttpClient httpClient, string ipAddress)
        {
            try
            {
                
              
                    if (String.IsNullOrEmpty(_connectionPingModels[2].Port.ToString()))
                    {
                        httpClient.BaseAddress = new Uri("http://"+ipAddress+""+ _connectionPingModels[2].ApplicationPoolName + "/");
                    }
                    else
                    {
                        httpClient.BaseAddress = new Uri("http://"+ipAddress+":"+ _connectionPingModels[2].Port + _connectionPingModels[2].ApplicationPoolName + "/");
                    }
               
               
            }
            catch (Exception ex)
            {

                FileWriter(ex.Message, "",  _connectionPingModels[2].EndPoint);
            }
           






        }


        public string SendEmail(string logMessage, string ipInfo, string endPoint)
        {
           
           TimeSpan minimumRequestInterval = TimeSpan.FromMinutes(_connectionPingModels[0].mailWaitTimeMinute);

            lock (lockObject)
            {

                if ((DateTime.Now - lastRequestTime) < minimumRequestInterval)
                {
                      
                        return "Mail gönderimi için henüz yeterli süre geçmedi";
                    
                }
                    var mail = new MailMessage();
                    mail.To.Add(_connectionPingModels[0].ToMail);
                    mail.From = new MailAddress(_connectionPingModels[0].SmtpFromMail);
                    mail.Subject = "ALERT! " + _serverType + " - " + _localIp + " Ip Adresindeki Servis Erişiminde Hata";
                    mail.BodyEncoding = Encoding.UTF8;

                    if (string.IsNullOrEmpty(ipInfo) && string.IsNullOrEmpty(endPoint))
                    {
                        mail.Body = "<html><body>";
                        mail.Body += "<h2>" + _connectionPingModels[0].MailTitle + "</h2>";
                        mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";
                        mail.Body += "<h4>Server : " + _serverType + "</h4>";
                        mail.IsBodyHtml = true;
                    }
                    else if (!string.IsNullOrEmpty(endPoint))
                    {
                        mail.Body += "<h2>" + _connectionPingModels[2].MailTitle + "</h2>";
                        mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";
                        mail.Body += "<h4> IP Bilgisi : <strong>" + _localIp + "</strong></h4>";


                        mail.Body += "<h4>Connection EndPoint : <strong>" + _connectionPingModels[2].EndPoint + " </strong></h4>";


                        mail.Body += "<h4>Server :  <strong>" + _serverType + "</strong> </h4>";
                        mail.Body += "</body></html>";
                        mail.IsBodyHtml = true;
                    }

                    else if (!String.IsNullOrEmpty(ipInfo))
                    {
                        mail.Body += "<h2>" + _connectionPingModels[0].MailTitle + "</h2>";
                        mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";
                        mail.Body += "<h4> IP Bilgisi : <strong>" + _localIp + "</strong></h4>";

                        if (ipInfo == _connectionPingModels[0].ProdServer)
                        {
                            mail.Body += "<h4>Application Pool: <strong>" + _connectionPingModels[0].ProdServer + " </strong></h4>";
                        }
                        else
                        {
                            mail.Body += "<h4>Application Pool: <strong>" + _connectionPingModels[1].ProdServer + " </strong></h4>";

                        }

                        mail.Body += "<h4>Server :  <strong>" + _serverType + "</strong> </h4>";
                        mail.Body += "</body></html>";
                        mail.IsBodyHtml = true;
                    }

                    foreach (var item in _connectionPingModels[0].CCMail)
                    {
                        mail.CC.Add(new MailAddress(item));
                    }
                try
                {
                    using (SmtpClient client = new SmtpClient())
                    {
                        FileWriter("Mail Gonderildi", "", "");
                        client.EnableSsl = true;
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(_connectionPingModels[0].SmtpFromMail, _connectionPingModels[0].SmptPassword);
                        client.Host = "smtp.gmail.com";
                        client.Port = _connectionPingModels[0].SmptPort;
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        //client.Send(mail);
                    }
                    lastRequestTime = DateTime.Now;

                }
                catch (Exception ex)
                {

                    FileWriter(ex.Message, ipInfo == null?"": ipInfo, endPoint == null ? "" : endPoint);
                }
                  
                
            }
            return string.Empty;



        }
    }
}
