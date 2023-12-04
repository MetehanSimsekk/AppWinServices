using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportationConnectionMonitoring.ConnectionPingInfoModel
{
    public class ConnectionPingModel
    {
        public ConnectionPingModel()
        {
            CCMail = new List<string>();
        }
        public string ToMail { get; set; }
        public List<string> CCMail { get; set; }
        public string EndPoint { get; set; }
        public string Interval { get; set; }

        public string ProdServer { get; set; }

        public string Port { get; set; }

        public int FileSize { get; set; }
        public string MailTitle { get; set; }
        public string FolderPath { get; set; }
        public int mailWaitTimeMinute { get; set; }
        public string LogFileNameIpEnd { get; set; }
        //public string Protocol { get; set; }
        public string ApplicationPoolName { get; set; }
        public string SmtpFromMail { get; set; }
        public string SmptPassword { get; set; }
        public string SmtpAddress { get; set; }
        public int SmptPort { get; set; }







    }
}
