using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKMListBinTableUpdateService.BinInfo
{
    public class ExcelData
    {
        
        public string Status { get; set; }
        public string Bin { get; set; }
        public string CardBrand { get; set; }
        public string CardType { get; set; }



    }
    public class ExcelDataListStatus
    {
        public ExcelDataListStatus()
        {
            Delete = new List<ExcelData>();
            Add = new List<ExcelData>();
        } 

        public List<ExcelData> Delete { get; set; }
        public List<ExcelData> Add { get; set; }
    }
    


}
