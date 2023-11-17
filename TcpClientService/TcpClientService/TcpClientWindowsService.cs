using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using TcpClientWindowsService.MessageModel;
using StackExchange.Redis;

namespace TcpClientService
{
    public partial class TcpClientWindowsService : ServiceBase
    {
        //tÜM DEĞERLERİ XML DOSYASINDAN OKU
        #region privatefield
        private readonly string serverAddress = "85.132.41.242";
        private readonly int serverPort = 4545;
        private TcpClient client;
        private readonly MessageModel messageModel;
        private ILogger logger;
        private Timer reconnectTimer;
        private int Interval = 5;
        private string signOnMessage;
        private string echoMessage;
        private string signOffMessage;
        private string response;
        int maxRetryCount = 3;
        int retry = 0;
        #endregion

        public TcpClientWindowsService()
        {


            logger = Log.ForContext<TcpClientWindowsService>();
            InitializeComponent();
            messageModel = new MessageModel();
            client = new TcpClient();
            reconnectTimer = new Timer();

        }
        public void onDebug()
        {
            OnStart(null);
        }
        protected async Task OnStart(string[] args)
        {
            signOnMessage = messageModel.SignOnMessage;
            signOffMessage = messageModel.SignOffMessage;
            echoMessage = messageModel.EchoMessage;
            reconnectTimer.Interval = Interval * 1000;
            reconnectTimer.Elapsed += TimerElapsed;
            reconnectTimer.Start();
            logger.Information("Servis başlatılıyor...");

            ConnectToServer(signOffMessage);
            ConnectToServer(signOnMessage);




        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {


            ConnectToServer(echoMessage);
        }
        public class SocketManager
        {
            public Socket MySocket { get; set; }

            public SocketManager()
            {
                MySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
              
            }
        }
        
        private async Task ConnectToServer(string message)
        {
            try
            {



                if (!(client != null && client.Client != null && client.Client.Connected))
                {

                    //client.Client.Bind(new IPEndPoint(IPAddress.Any, 1413));

                    string endpointString = "[::ffff:192.168.113.2]:62063";

                    // String'i ayrıştırma
                    int startIndex = endpointString.IndexOf(":") + 1;
                    int endIndex = endpointString.LastIndexOf("]");

                    string ipAddressString = endpointString.Substring(0, endIndex + 1);
                    string portString = endpointString.Substring(startIndex, endpointString.Length - startIndex);
                    IPAddress ipAddress = IPAddress.Parse(ipAddressString);
                     

                    if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
                        ipAddress.IsIPv4MappedToIPv6)
                    {
                        long ipAddressAsLong = BitConverter.ToInt32(ipAddress.GetAddressBytes(), 12);
                        try
                        {
                            if (!(client != null && client.Client != null && client.Client.Connected))
                            {
                                // Bağlantı yoksa bağlantı yap...
                                int port = int.Parse("65065");
                                IPEndPoint endpoint = new IPEndPoint(ipAddress, port);
                                // Socket veya başka bir bağlantı nesnesine bu endpoint'i atayabilirsiniz
                                SocketManager socketManager = new SocketManager();
                                SocketManager socketManagerFromProject1 = new SocketManager();
                                socketManagerFromProject1.MySocket = socketManager.MySocket;

                                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                
                                if (socket.SocketType == SocketType.Stream)
                                {
                                    // Socket bir akış soketidir (TCP bağlantısı)
                                    Console.WriteLine("Socket bir akış soketidir (TCP bağlantısı)");
                                }
                               
                                socketManagerFromProject1.MySocket.Bind(endpoint);
                            }
                          




                            //client.Client.LocalEndPoint = endpoint;

                            client.Connect(serverAddress, serverPort);
                            logger.Information("Bağlantı kuruluyor...");
                            // endpoint'i kullan
                        }
                        catch (Exception ex)
                        {
                            var d = ex.Message;
                        }
                    }
                }


                // Ayrıştırılmış bilgileri kullanarak EndPoint oluşturma

              

                  


            
            
                string messageToSend = message;                
                logger.Information("Servera gönderilen "+ messageToSend + " mesaj: " + messageToSend);

                

                byte[] messageBuffer = Encoding.UTF8.GetBytes(messageToSend);
                NetworkStream stream = client.GetStream();
                stream.Write(messageBuffer, 0, messageBuffer.Length);

                byte[] responseBuffer = new byte[1024];

                int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
                try
                {
                    response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

                }
                catch (Win32Exception ex)
                {
                    logger.Information("Servera gönderilen -ECHO- yanıt: " + ex.Message);
                    retry++;
                    //if (retry >= maxRetryCount && String.IsNullOrEmpty(response) && ex.ErrorCode != 10022)
                    if (retry >= maxRetryCount)
                    {
                        logger.Information("Exception Tekrar değeri: " + retry);
                        logger.Information("Maksimum yeniden deneme sayısına ulaşıldı. Echo SignOn-SignOff işlemi tekrar başlatılıyor.");
                        ConnectToServer(signOffMessage);
                        ConnectToServer(signOnMessage);
                        retry = 0;
                    }
                }
                if (message == echoMessage)
                {
                    logger.Information("Servera gönderilen -ECHO- yanıt: " + response);

                }
                else if (message == signOffMessage)
                {
                    logger.Information("Servera gönderilen -SIGNOFF- yanıt: " + response);

                }
                else
                {
                    logger.Information("Servera gönderilen -SIGNON- yanıt: " + response);
                }


            }
            catch (Win32Exception ex)
            {
                logger.Information("Bağlantı Hata: ---->  " + ex.Message);
                retry++;
                logger.Information("Exception Tekrar değeri: " + retry);
                //retry >= maxRetryCount && String.IsNullOrEmpty(response) && ex.ErrorCode != 10022
                if (retry >= maxRetryCount)
                {
                    logger.Information("Maksimum yeniden deneme sayısına ulaşıldı. Echo SignOn-SignOff işlemi tekrar başlatılıyor.");
                    ConnectToServer(signOffMessage);
                    ConnectToServer(signOnMessage);
                    retry = 0;
                }


            }


        }
        public void PerformControlAction(string controlAction)
        {
            // Kontrol isteğine göre belirli bir işlem gerçekleştir
            if (controlAction == "start")
            {
                // Servisi başlatma işlemleri burada gerçekleştirilir
                // Örneğin: OnStart(null);
            }
            else if (controlAction == "stop")
            {
                // Servisi durdurma işlemleri burada gerçekleştirilir
                // Örneğin: OnStop();
            }
            // Diğer kontrol isteklerine göre gereken işlemleri ekleyebilirsiniz
        }
        protected override void OnStop()
        {

            if (client != null)
            {
                logger.Information("Bağlantı Durduruldu.");
                client.Close();
            }
        }
    }
}
