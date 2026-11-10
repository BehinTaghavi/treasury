using System;
using System.ServiceModel;

namespace TreasuryPrintService_Host
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(TreasuryPrintService.PrintService));
            host.Open();
            Console.WriteLine("Treasury print service hosted successfully");
            Console.Read();
        }
    }
} 
