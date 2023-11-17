using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TcpClientService
{
    static class Program
    {
        private static ILogger logger;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string logFilePath = Path.Combine(projectDirectory, "File.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logFilePath)
                .CreateLogger();

            logger = Log.ForContext<TcpClientWindowsService>();

            try
            {




                TcpClientWindowsService myService = new TcpClientWindowsService();
                myService.onDebug();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                // Log dosyasını açın veya oluşturun

                // Başlangıç zamanını log dosyasına yazın

                //logger.Information("ServicesToRun...");
                //ServiceBase[] ServicesToRun;
                //ServicesToRun = new ServiceBase[]
                //{
                //        new TcpClientWindowsService()
                //};

                //ServiceBase.Run(ServicesToRun);

                // Hizmet başlatma başarılıysa, başarı mesajını log dosyasına yazın


            }
            catch (Exception ex)
            {
                // Hata durumunda hata mesajını log dosyasına yazın

                Console.WriteLine("Hata : "+ex.Message);   
                
            }
        }
    }
}
