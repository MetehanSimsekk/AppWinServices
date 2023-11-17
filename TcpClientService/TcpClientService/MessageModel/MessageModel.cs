using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpClientWindowsService.MessageModel
{
    public class MessageModel 
    {
        public string SignOnMessage { get; set; } = "A4M080000800822000000000000004000000000000000620122519018565001";

        public string SignOffMessage { get; set; } = "A4M080000800822000000000000004000000000000000620122519018565002";

        public string EchoMessage { get; set; } = "A4M080000800822000000000000004000000000000001107104103171078301";

        

    }
    
}
