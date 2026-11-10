using System.Collections.Generic;

namespace TreasuryPrintService.Models
{
    public class Cheque
    {
        public List<ChequeElement> ChequeElements { get; set; }
        public virtual ChequeDetails ChequeDetails { get; set; }
    }
    public class ChequeDetails
    {
        public decimal RequestId { get; set; }
        public int ChequeFormatID { get; set; }
        public string DueDate { get; set; }
        public string TableName { get; set; }
        public bool RemoveRemit { get; set; }
        public bool RemoveOrdersFor { get; set; }
        public bool Bearer { get; set; }
        public bool WithoutCrossing { get; set; }
        public bool NonTransferable { get; set; }
        public bool CurrentAccount { get; set; }
        public int CurrentAccountId { get; set; }
        public bool ShebaAccount { get; set; }
        public int ShebaAccountId { get; set; }
        public bool BranchExporterReceivable { get; set; }
        public bool ApproveIssuance { get; set; }
        public string BeneficiarDetail { get; set; }
        public bool Unique_Account_No2 { get; set; }
        public string Unique_Account_No2Val { get; set; }
        public bool checkIsUnderwritten { get; set; }
    }
    public class ChequeElement
    {
        public decimal m_TOP { get; set; }
        public decimal m_Left { get; set; }
        public string m_FontName { get; set; }
        public int m_FontSize { get; set; }
        public string m_TextAlign { get; set; }
        public string m_fontweight { get; set; }
        public decimal m_Width { get; set; }
        public decimal m_height { get; set; }
        public string m_Direction { get; set; }
        public string ChequeLable { get; set; }
        public string labelValue { get; set; }
        public string labelMapToField { get; set; }
    }
}
