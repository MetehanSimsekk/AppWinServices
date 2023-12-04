using BKMListBinTableUpdateService.ConnectionPingInfoModel;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Text.Json;
using TransportationConnectionMonitoring.ConnectionPingInfoModel;
using LicenseContext = OfficeOpenXml.LicenseContext;
using BKMListBinTableUpdateService.BinInfo;
using System.Net.Sockets;

namespace BKMListBinTableUpdateService
{
    public partial class Service1 : ServiceBase
    {
        private HttpClient _httpClient = new HttpClient();
        private ConnectionPingModel _connectionPingModel;
        private List<ExcelData> excelDataList = new List<ExcelData>();

        private ExcelDataListStatus excelDataListStatus = new ExcelDataListStatus();
        public Service1()
        {
            _connectionPingModel = GetConnectionPingInfo();
            InitializeComponent();
        }
        public void onDebug()
        {
            OnStart(null);
        }
        protected override void OnStart(string[] args)
        {
            string localIP = GetLocalIPAddress();
            FileWriter("Local IP Adres : " + localIP);
            BaseAddress(localIP);
            FileWriter("Servis Çalışıyor " + DateTime.Now);
            ReadExcelFile();
        }

        public void ReadExcelFile()
        {

            string excelFilePath = @"C:\Users\smskm\Desktop\AppTech\BKM.xlsx"; //excel local file path

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(excelFilePath)))
            {
                try
                {
                    var worksheet = package.Workbook.Worksheets[0];

                    for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                    {

                        ExcelData excelData = new ExcelData
                        {

                            Status = worksheet.Cells[row, 1].Text,
                            Bin = worksheet.Cells[row, 4].Text.Substring(0, Convert.ToInt32(worksheet.Cells[row, 7].Text)), //worksheet.Cells[row, 7].Text) -->BinLength
                            CardBrand = worksheet.Cells[row, 12].Text,
                            CardType = worksheet.Cells[row, 15].Text,
                          


                        };



                        excelDataList.Add(excelData);

                        if (excelData.Status == "DELETE")
                        {
                            excelData.Status = worksheet.Cells[row, 8].Text;
                            excelDataListStatus.Delete.Add(excelData);
                        }
                        else if (excelData.Status == "ADD")
                        {
                            excelData.Status = "00";

                            excelDataListStatus.Add.Add(excelData);
                        }



                    }
                }
                catch(Exception ex)
                {
                    var d = ex.Message;
                }
                
                SendHttpPostRequest(excelDataListStatus);
            }
            //OnStop();
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
                FileWriter("XML Yükleme Hatası: " + ex.Message, "");
                return null;
            }
        }
        public ConnectionPingModel GetConnectionPingInfo()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppWinServicesInfo.xml");
            ConnectionPingModel connectionPingModel = new ConnectionPingModel();

            try
            {
                XmlDocument doc = LoadXmlDocument(filePath);
                if (doc != null)
                {
                    XmlNodeList rootNodes = doc.SelectNodes("/roots/root/bkm");
                    //XmlNode root = doc.DocumentElement;


                    foreach (XmlNode node in rootNodes)
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
                                case "folderName":
                                    connectionPingModel.FolderName = elementValue;
                                    break;
                                case "port":
                                    connectionPingModel.Port = elementValue;
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
                SendEmail("Hata Oluştu : " + ex.Message + " <br>  Süre : " + DateTime.Now.TimeOfDay.ToString(), "");
            }

            return connectionPingModel;
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
                _httpClient.BaseAddress = new Uri("https://localhost/");
            }
            else
            {
                _httpClient.BaseAddress = new Uri("https://localhost:" + _connectionPingModel.Port + "/");
            }


        }
        public string SendEmail(string logMessage, string ipInfo)
        {
            var mail = new MailMessage();
            mail.To.Add(_connectionPingModel.ToMail);
            mail.From = new MailAddress(_connectionPingModel.SmtpFromMail);
            mail.Subject = "BKM - " + _connectionPingModel.ServerInfo + "";
            mail.BodyEncoding = Encoding.UTF8;
            try
            {

                if (String.IsNullOrEmpty(ipInfo))
                {
                    mail.Body = "<html><body>";
                    mail.Body += "<h2>Transportation Connection Monitoring</h2>";
                    mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";
                    mail.Body += "<h4>Server : " + _connectionPingModel.ServerInfo + "</h4>";
                    mail.IsBodyHtml = true;
                }
                else
                {
                    if (ipInfo == _connectionPingModel.FirstProdServer)
                    {

                        mail.Body += "<h2>Transportation Connection Monitoring</h2>";
                        mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";
                        mail.Body += "<h4>Application Pool: <strong>" + _connectionPingModel.FirstProdServer + " </strong></h4>";
                        mail.Body += "<h4>Server : <strong>" + _connectionPingModel.ServerInfo + "</strong></h4>";
                        mail.Body += "</body></html>";
                        mail.IsBodyHtml = true;
                    }
                    else
                    {

                        mail.Body += "<h2>Transportation Connection Monitoring</h2>";
                        mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";
                        mail.Body += "<h4>Application Pool : <strong>" + _connectionPingModel.SecondProdServer + "</strong> </h4>";
                        mail.Body += "<h4>Server :  <strong>" + _connectionPingModel.ServerInfo + "</strong> </h4>";
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

                FileWriter(ex.Message, "");

            }

            return string.Empty;




        }
        public void FileWriter(string message, string IpAddress)
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

        
        private async Task SendHttpPostRequest(ExcelDataListStatus excelDataListStatus)
        {
            try
            {

           
                var combinedData = new
                {
                    Delete = excelDataListStatus.Delete,
                    Add = excelDataListStatus.Add
                };

                string jsonData = JsonSerializer.Serialize(combinedData);
              
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                string apiUrl = _httpClient.BaseAddress + _connectionPingModel.EndPoint;


              
               string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJleHAiOjE3MDAxOTAxMTEsIkNsaWVudElkIjoiU0EiLCJJbnN0aXR1dGlvbklkIjoxMDIsIlNlc3Npb25JZCI6MTExMTExMTExMTE2NTAwMH0.PrHgHxoing4Z65UvUriInUDAVeSuu50Byy0fYs8iXOg"; // Bu kısmı kendi token'ınızla değiştirin
                
                //_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);



                var response = await _httpClient.PostAsync(apiUrl, content);
                

                using (var responses = await _httpClient.PostAsync(_httpClient.BaseAddress + _connectionPingModel.EndPoint, content))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                    {

                        await responses.Content.ReadAsStringAsync();
                        FileWriter("İstek başarıyla tamamlandı.URL: " + _httpClient.BaseAddress + _connectionPingModel.EndPoint + " Yanıt: Status Kodu : " + response.StatusCode + " Content :" + response.Content + "Süre: " + DateTime.Now.TimeOfDay.ToString());

                    }
                    else
                    {
                        FileWriter("İstek başarısız. Hata kodu: " + response.StatusCode + DateTime.Now.TimeOfDay.ToString());
                        SendEmail("İstek başarısız. Hata kodu: " + response.StatusCode + DateTime.Now.TimeOfDay.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                FileWriter("Hata oluştu: " + ex.Message);
                SendEmail("Hata oluştu: " + ex.Message);
            }
        }
       
        public async Task SendEmail(string logMessage)
        {
            var mail = new MailMessage();
            mail.To.Add(_connectionPingModel.ToMail);
            mail.From = new MailAddress(_connectionPingModel.SmtpFromMail);
            mail.Subject = "Connection Ping Service " + _connectionPingModel.ServerInfo + " ";
            mail.BodyEncoding = Encoding.UTF8;
            try
            {

                mail.Body += "<h2>Connection Ping Service</h2>";
                mail.Body += "<p> Sorunun Nedeni : <strong>" + logMessage + "</strong> </p>";
                mail.Body += "<h4>EndPoint: <strong>" + _httpClient.BaseAddress + _connectionPingModel.EndPoint + " </strong></h4>";
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
                    //client.Send(mail);
                }
            }
            catch (Exception ex)
            {

                FileWriter(ex.Message);

            }
        }
        public void FileWriter(string message)
        {

            string dosyaYolu = AppDomain.CurrentDomain.BaseDirectory + _connectionPingModel.FolderPath;
            if (!Directory.Exists(dosyaYolu))
            {
                Directory.CreateDirectory(dosyaYolu);
            }
            string textYolu = AppDomain.CurrentDomain.BaseDirectory + _connectionPingModel.FolderName;
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
        protected override void OnStop()
        {
        }

    }
}
