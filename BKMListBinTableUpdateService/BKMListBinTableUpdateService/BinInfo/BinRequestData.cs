using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKMListBinTableUpdateService.ConnectionPingInfoModel
{
    public class BinRequestData
    {
        public string Bin { get; set; }
        public string BankCode { get; set; }
        public int OfflineStatus { get; set; }
        public int OnlineStatus { get; set; }
        public int DelayedAuthStatus { get; set; }
        public int IsMbrInserted { get; set; }
        public int IsMbrStatusUpdated { get; set; }
        public int IsUpdateMbrDelayedAuthStt { get; set; }
    }
}
