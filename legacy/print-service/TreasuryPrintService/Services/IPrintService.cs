using System.ServiceModel;
using TreasuryPrintService.Models;

namespace TreasuryPrintService
{
    [ServiceContract]
    public interface IPrintService
    {
        [OperationContract]
        bool PrintCheque(Cheque cheque, string password);
    }
}
