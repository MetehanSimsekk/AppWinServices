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
                    client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                    client.Connect(serverAddress, serverPort);
                }

              

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
                    if (retry >= maxRetryCount && String.IsNullOrEmpty(response))
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
                    logger.Information("Serverdan dönen -ECHO- yanıt: " + response);

                }
                else if (message == signOffMessage)
                {
                    logger.Information("Serverdan dönen -SIGNOFF- yanıt: " + response);

                }
                else
                {
                    logger.Information("Serverdan dönen -SIGNON- yanıt: " + response);
                }
                client.Close();

            }
            catch (Win32Exception ex)
            {
                logger.Information("Bağlantı Hata: ---->  " + ex.Message);
                retry++;
                logger.Information("Exception Tekrar değeri: " + retry);
                //retry >= maxRetryCount && String.IsNullOrEmpty(response) && ex.ErrorCode != 10022
                if (retry >= maxRetryCount && String.IsNullOrEmpty(response))
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
           
            if (controlAction == "start")
            {
                
            }
            else if (controlAction == "stop")
            {
              
            }
            
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
