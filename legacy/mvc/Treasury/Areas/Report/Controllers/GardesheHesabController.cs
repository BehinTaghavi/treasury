using CSD.Treasury.UI.Areas.Report.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.ProceduresDB;
using CSD.Treasury.UI.Controllers;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;
using CSD.Treasury.Service.BankService;
using CSD.Treasury.Service.BasicBanksService;
using System.Text;
using Stimulsoft.Report.Dictionary;
using CSD.Treasury.UI.Models;
using System.Data;
using Stimulsoft.Report.Components;
using Stimulsoft.Base;
using Stimulsoft.Base.Drawing;
using System.Drawing;
using System.Web.UI.WebControls;
using System.Collections;
using System.Data.SqlClient;
using System.Configuration;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Areas.Report.Controllers
{
    [Authorize(Roles = "Admin,Manager,Report,ReportInApp")]
    public class GardesheHesabController : BaseController
    {
        public ActionResult SearchView()
        {
            int TotalRow;
            //Icls_BankClient BankCLient = new Icls_BankClient();
            Icls_BasicBanksClient Basikclient = new Icls_BasicBanksClient();
            var db = new TreasuryEntities();
            //var BankLS= BankCLient.SelectBy_DynamicWhereClause("IsActive="+true, 1, 1000, "BankID", "int", false,out TotalRow);
            var BankLS = db.Banks.Where(d => d.IsActive == true);
            var BasicBankLS = Basikclient.SelectAll(1, 1000, "BasicBankId", "int", false, out TotalRow);

            //ViewBag.BasicBank = Basikclient.SelectAll(1, 1000, "BasicBankId", "int", false, out TotalRow);
            //ViewBag.Bankls = BankLS;



            List<BasicBankAndBank> CustomBankLS = new List<BasicBankAndBank>();

            foreach (var item in BasicBankLS)
            {
                BasicBankAndBank obj = new BasicBankAndBank();
                obj.BasicBankDesc = item.BasicBankDescription;
                //obj.BankLS = BankCLient.SelectBy_DynamicWhereClause("BasicBankID=" + item.BasicBankId + "and IsActive=" + true, 1, 10 * 10, "BankID", "int", false, out TotalRow).ToList();
                obj.BankLS =db.Banks.Where(d=>d.BasicBankID == item.BasicBankId).ToList();
                CustomBankLS.Add(obj);
            }
            ViewBag.CustomBankLS = CustomBankLS;
            return View();
        }
        public ActionResult SearchView1()
        {
            int TotalRow;
            //Icls_BankClient BankCLient = new Icls_BankClient();
            Icls_BasicBanksClient Basikclient = new Icls_BasicBanksClient();
            var db = new TreasuryEntities();
            //var BankLS = BankCLient.SelectBy_DynamicWhereClause("IsActive=" + true, 1, 1000, "BankID", "int", false, out TotalRow);
            var BankLS = db.Banks.Where(d => d.IsActive == true);
            var BasicBankLS = Basikclient.SelectAll(1, 1000, "BasicBankId", "int", false, out TotalRow).Where(d => BankLS.Select(i => i.BasicBankID).Contains(d.BasicBankId));


            ViewBag.BasicBank = BasicBankLS;
            ViewBag.Bankls = BankLS;



            //List<BasicBankAndBank> CustomBankLS = new List<BasicBankAndBank>();

            //foreach (var item in BasicBankLS)
            //{
            //    BasicBankAndBank obj = new BasicBankAndBank();
            //    obj.BasicBankDesc = item.BasicBankDescription;
            //    obj.BankLS = BankCLient.SelectBy_DynamicWhereClause("BasicBankID=" + item.BasicBankId + "and IsActive=" + true, 1, 10 * 10, "BankID", "int", false, out TotalRow).ToList();
            //    CustomBankLS.Add(obj);
            //}
            //ViewBag.CustomBankLS = CustomBankLS;
            return View();
        }
        public ActionResult SearchViewDayReport()
        {
            int TotalRow;
            //Icls_BankClient BankCLient = new Icls_BankClient();
            var db = new TreasuryEntities();
            Icls_BasicBanksClient Basikclient = new Icls_BasicBanksClient();
            //var BankLS = BankCLient.SelectBy_DynamicWhereClause("IsActive=" + true, 1, 1000, "BankID", "int", false, out TotalRow);
            var BankLS = db.Banks.Where(d => d.IsActive == true);
            var BasicBankLS = Basikclient.SelectAll(1, 1000, "BasicBankId", "int", false, out TotalRow).Where(d => BankLS.Select(i => i.BasicBankID).Contains(d.BasicBankId));
            ViewBag.BasicBank = BasicBankLS;
            ViewBag.Bankls = BankLS;

            return View();
        }
        // GET: Report/GardesheHesab
        public ActionResult Index1(GardeshHesabSearchModel SearchObj)
        {
            //if (Request.Form["BankIdList"] != null)
            //{
            //    SearchObj.BankIdList = Request.Form["BankIdList"];
            //}
            return View(SearchObj);
        }

        public ActionResult List1(GardeshHesabSearchModel SearchObj)
        {
            StiReport report = new StiReport();
            Icls_SPsClient spsClient = new Icls_SPsClient();
            var db = new TreasuryEntities();
            //Icls_BankClient BankClient = new Icls_BankClient();
            Icls_BasicBanksClient BasicClient = new Icls_BasicBanksClient();
            int totalRow;
            var ls = new List<GetBankBalance_Result>();
            if (string.IsNullOrEmpty(SearchObj.ToDate))
            {
                SearchObj.ToDate = SearchObj.FromDate;
            }

            if (SearchObj.ShowSum)
            {
                //نمایش خلاصه :صفر
                ls = spsClient.Sp_GetBankBalance(SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", ""), SearchObj.BankId, 2, false).Skip(1).ToList();
            }
            else
            {

                ls = spsClient.Sp_GetBankBalance(SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", ""), SearchObj.BankId, 2, true).Skip(1).ToList();
            }


            // var ls = spsClient.Sp_GardesheHesabBank(SearchObj.BankId.ToString(), SearchObj.UniqueAccountNO, SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", ""), false);
            // var ls = spsClient.Sp_GardesheHesabBank("5", "010101", "13960101", "13960625", false);
              //var BankLS = BankClient.SelectAll(1, 100, "BankID", "int", false, out totalRow);
            var BankLS = db.Banks;
          

            string Path = Server.MapPath("~/Report/GardeshHesabReport.mrt");

            report.Load(Path);


            if (SearchObj.BankId != 0)
            {
                var l = spsClient.Sp_GetBankBalance(SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", ""), SearchObj.BankId, 2, false);
                var l1 = spsClient.Sp_GetBankBalance(SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", ""), SearchObj.BankId, 1, true);


                //var Bank = BankClient.SelectRow(SearchObj.BankId).SingleOrDefault();
                var Bank = db.Banks.SingleOrDefault(d=>d.BankID == SearchObj.BankId);
                var BasicBAnk = BasicClient.SelectRow(Bank.BasicBankID).SingleOrDefault();
                report.Dictionary.Variables["CompanyName"].Value = ConfigurationManager.AppSettings["CompanyTitle"];
                report.Dictionary.Variables["DateNow"].Value = Utility.GetPersianDate(Utility.ToPersianDate(DateTime.Now));
                report.Dictionary.Variables["BankAccountNum"].Value = Bank.Bank_AccountNumber;
                report.Dictionary.Variables["Bank"].Value = BasicBAnk.BasicBankDescription;
                report.Dictionary.Variables["Branch"].Value = Bank.BranchName;
                if(Bank.ExchangeTypeID.HasValue)
                {
                    report.Dictionary.Variables["ExchangeType"].Value = Utility.GetExchaneName(Bank.ExchangeTypeID.Value);
                }
                

                if (l.Count() != 0)
                {
                   //be jaie balance az Debit khande mishod 
                    report.Dictionary.Variables["OpeningValue"].Value = l.FirstOrDefault().balance.ToString();
                }
                if (l1.Count() != 0)
                {

                    report.Dictionary.Variables["Balance"].Value = l1.SingleOrDefault().balance.ToString();
                }

                report.Dictionary.Variables["DateFrom"].Value = SearchObj.FromDate;
                report.Dictionary.Variables["DateTO"].Value = SearchObj.ToDate;

            }
            //report.RegBusinessObject("BankLs", BankLS);
            //سورت در خود کوئری getbankbalance گذاشته شده است
            //ls = ls.OrderBy(q => q.Value_field).OrderBy(q => q.TransactionDate).ToList();
            
            report.RegBusinessObject("GardeshHesab", ls);


            report.Dictionary.Databases.Clear();
            //string cs = @"Password=***MASKED***;Data Source=CSDSERVER2003\SQL2008;Integrated Security=False;Initial Catalog=Commision;User ID=mahmadi;";

            //string cs = @"Integrated Security = False; Data Source = CSDSEaRVER2003\SQL2008; Initial Catalog = Treasury; Password =; User ID = sa";
            //report.Dictionary.Databases.Add(new Stimulsoft.Report.Dictionary.StiSqlDatabase("Connection", cs));
            //StiSqlSource Banks = new StiSqlSource("Connection", "Banks", "Banks", "SELECT * FROM Banks", true, false);
            //var count = Banks.Count;

            //StiSqlSource GardeshHesabLs = new StiSqlSource("Connection", "GardeshHesabLs", "GardeshHesabLs", "exec  [dbo].[GardesheHesabBank] '5', '010101', '13960101', '13960625',0", true, false);
            //var count2 = GardeshHesabLs.RealCount;
            //report.Dictionary.DataSources.Add(Banks);
            //StiDataRelation dataRelation = new StiDataRelation("MyRelation", Banks, GardeshHesabLs, new string[] { "BankID" }, new string[] { "BankID" });
            //report.Dictionary.RegRelations();
            //report.Dictionary.Relations.Add(dataRelation);



            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);



        }
        // GET: Report/GardesheHesab
        public ActionResult Index(GardeshHesabSearchModel SearchObj)
        {
            if (Request.Form["BankIdList"] != null)
            {
                SearchObj.BankIdList = Request.Form["BankIdList"];
            }
            return View(SearchObj);
        }

        public ActionResult List(GardeshHesabSearchModel SearchObj)
        {
            if (SearchObj.BankIdList != null)
            {
                //StiReport report = GetReport();
                //report.Show();
                //return report;
                StiReport report = new StiReport();
                //Icls_BankClient BankCLient = new Icls_BankClient();
                var db = new TreasuryEntities();
                string Path = Server.MapPath("~/Report/GardeshHesabReport1.mrt");
                report.Load(Path);
                string BankUnique_Account_No = "";
                if (!string.IsNullOrEmpty(SearchObj.BankIdList))
                {
                    var Bankls = SearchObj.BankIdList.Split(',');
                    string Unique_Account_No = "";
                    foreach (var item in Bankls)
                    {

                        //Unique_Account_No = BankCLient.SelectRow(int.Parse(item.Trim())).SingleOrDefault().Unique_Account_No;
                        Unique_Account_No = db.Banks.SingleOrDefault(d => d.BankID == int.Parse(item.Trim())).Unique_Account_No;
                        BankUnique_Account_No = BankUnique_Account_No + Unique_Account_No + ",";
                    }

                }

                BankUnique_Account_No = BankUnique_Account_No.Remove(BankUnique_Account_No.Length - 1);
                report.Dictionary.Variables["BankIdList"].Value = SearchObj.BankIdList;
                report.Dictionary.Variables["BankUnique_Account_No"].Value = BankUnique_Account_No;
                report.Dictionary.Variables["DateFrom"].Value = SearchObj.FromDate.Replace(@"/", "");
                report.Dictionary.Variables["DateTo"].Value = SearchObj.ToDate.Replace(@"/", "");
                report.Dictionary.Variables["DateNow"].Value = Utility.ToPersianDate(DateTime.Now);

                report.Dictionary.Variables["BankIdList"].RequestFromUser = false;
                report.Dictionary.Variables["BankUnique_Account_No"].RequestFromUser = false;
                report.Dictionary.Variables["DateFrom"].RequestFromUser = false;
                report.Dictionary.Variables["DateTo"].RequestFromUser = false;
                report.Dictionary.Variables["ShowRial"].RequestFromUser = false;
                // report.DataSources["MSSQL1"].Parameters["ShowRial"].ParameterValue = false;


                //report.Dictionary.SynchronizeBusinessObjects();
                return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);

                //StiReport report = new StiReport();
                //Icls_SPsClient spsClient = new Icls_SPsClient();
                //Icls_BankClient BankClient = new Icls_BankClient();
                //int totalRow;



                //var ls = spsClient.Sp_GardesheHesabBank(SearchObj.BankId.ToString(), SearchObj.UniqueAccountNO, SearchObj.FromDate.Replace(@"/", ""), SearchObj.ToDate.Replace(@"/", ""), false);
                //// var ls = spsClient.Sp_GardesheHesabBank("5", "010101", "13960101", "13960625", false);
                //var BankLS = BankClient.SelectAll(1, 100, "BankID", "int", false, out totalRow);


                //string Path = Server.MapPath("~/Report/GardeshHesabReport.mrt");

                //report.Load(Path);


                //if (SearchObj.BankId != 0)
                //{
                //    var Bank = BankClient.SelectRow(SearchObj.BankId).SingleOrDefault();
                //    report.Dictionary.Variables["DateNow"].Value = Utility.ToPersianDate( DateTime.Now);
                //    report.Dictionary.Variables["BankAccountNum"].Value = Bank.Bank_AccountNumber;
                //    report.Dictionary.Variables["Bank"].Value = Bank.BranchName;
                //    report.Dictionary.Variables["Branch"].Value = Bank.BranchName;
                //    report.Dictionary.Variables["ExchangeType"].Value = Utility.GetExchaneName( Bank.ExchangeTypeID);
                //    report.Dictionary.Variables["OpeningValue"].Value = Bank.OpeningValue.ToString();
                //    report.Dictionary.Variables["DateFrom"].Value = SearchObj.FromDate;
                //    report.Dictionary.Variables["DateTO"].Value = SearchObj.ToDate;
                //}
                //report.RegBusinessObject("BankLs", BankLS);
                //report.RegBusinessObject("GardeshHesab", ls);

                //report.Dictionary.Databases.Clear();
                // string cs = @"Password=***MASKED***;Data Source=CSDSERVER2003\SQL2008;Integrated Security=False;Initial Catalog=Commision;User ID=mahmadi;";

                // string cs = @"Integrated Security = False; Data Source = CSDSEaRVER2003\SQL2008; Initial Catalog = Treasury; Password =; User ID = sa";
                // report.Dictionary.Databases.Add(new Stimulsoft.Report.Dictionary.StiSqlDatabase("Connection",cs));
                // StiSqlSource Banks = new StiSqlSource("Connection", "Banks", "Banks", "SELECT * FROM Banks", true, false);
                //var count=  Banks.Count;

                // StiSqlSource GardeshHesabLs = new StiSqlSource("Connection", "GardeshHesabLs", "GardeshHesabLs", "exec  [dbo].[GardesheHesabBank] '5', '010101', '13960101', '13960625',0", true, false);
                // var count2 = GardeshHesabLs.RealCount;
                // report.Dictionary.DataSources.Add(Banks);
                // StiDataRelation dataRelation = new StiDataRelation("MyRelation", Banks, GardeshHesabLs, new string[] { "BankID" }, new string[] { "BankID" });
                // report.Dictionary.RegRelations();
                // report.Dictionary.Relations.Add(dataRelation);



                //report.Dictionary.SynchronizeBusinessObjects();
                //return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);
            }
            else
            {
                return View(SearchObj);
            }


        }
        //public ActionResult Action()
        //{
        //    //parent
        //    DataTable dataTable = stiDataStoreSource.createNewTable();
        //    for (int i = 0; i < 5; i++)
        //    {
        //        DataRow dataRow = dataTable.createNewRow();
        //        dataRow.addCell("cId", new ObjectCell("Object" + i));
        //        dataRow.addCell("cName", "Parent cId: " + i);
        //    }
        //    stiDataStoreSource.setDataTable(dataTable);

        //    //child
        //    DataTable dataTable = stiDataStoreSource.createNewTable();
        //    for (int i = 0; i < 5; i++)
        //    {
        //        for (int k = 0; k < 5; k++)
        //        {
        //            DataRow dataRow = dataTable.createNewRow();
        //            dataRow.addCell("cId", new ObjectCell("Object" + i));
        //            for (int j = 1; j < dataRow.getColumns().size(); j++)
        //            { // fill row wiht my data
        //                dataRow.addCell(dataRow.getColumns().get(j).getColumnName(), "Child cId: " + i + " value: " + j);
        //            }
        //        }
        //    }
        //    stiDataStoreSource.setDataTable(dataTable);
        //    StiReport report = new StiReport();

        //    StiPage page = new StiPage(report);
        //    report.getPages().add(page);
        //    page.setName(StiNameCreation.createName(report, StiNameCreation.generateName(page)));
        //    report.setDictionary(new StiDictionary(report));
        //    report.getDictionary().getDatabases().add(new ParentDatabase());
        //    report.getDictionary().getDatabases().add(new ChildDatabase());



        //    List<StiDataTableSource> tableSources = new ArrayList<StiDataTableSource>();
        //    // parent
        //    StiDataTableSource tSource = new StiDataTableSource("Demo.Parent", "Parent", "Parent");
        //    tSource.setColumns(new StiDataColumnsCollection());
        //    tSource.getColumns().add(
        //        new StiDataColumn("cId", "cId", StiSystemType.getSystemType("System.Object")));
        //    tSource.getColumns().add(
        //        new StiDataColumn("cName", "cName", StiSystemType.getSystemType("System.String")));
        //    tSource.setDictionary(report.getDictionary());
        //    report.getDictionary().getDataSources().add(tSource);
        //    tableSources.add(tSource);
        //    // child
        //    tSource = new StiDataTableSource("Demo.Child", "Child", "Child");
        //    tSource.setColumns(new StiDataColumnsCollection());
        //    tSource.getColumns().add(
        //        new StiDataColumn("cId", "cId", StiSystemType.getSystemType("System.Object")));
        //    for (int i = 0; i < 4; i++)
        //    {
        //        tSource.getColumns().add(
        //           new StiDataColumn("cData" + i, "cData" + i,
        //           StiSystemType.getSystemType("System.String")));
        //    }
        //    tSource.setDictionary(report.getDictionary());
        //    report.getDictionary().getDataSources().add(tSource);
        //    tableSources.add(tSource);



        //    // Create TitleBand
        //    StiHeaderBand titleBand = new StiHeaderBand();
        //    titleBand.setHeight(0.85);
        //    titleBand.setName("TitleBand");
        //    page.getComponents().add(titleBand);

        //    // Create Title text on header
        //    StiText headerText = new StiText(new StiRectangle(0, 0, page.getWidth(), 0.85));
        //    headerText.setTextInternal("Tacticdescription");
        //    headerText.setHorAlignment(StiTextHorAlignment.Left);
        //    headerText.setName("TitleHeader");
        //    headerText.setFont(new StiFont("Arial", 12F, StiFontStyle.Bold));
        //    titleBand.getComponents().add(headerText);
        //    Integer nameIndex = 1;
        //    List<StiDataBand> dataBands = new ArrayList<StiDataBand>();
        //    for (StiDataTableSource tableSource : tableSources)
        //    {

        //        // Create Databand
        //        StiDataBand dataBand = new StiDataBand();
        //        dataBand.setDataSourceName(tableSource.getName());
        //        dataBand.setHeight(0.5);
        //        dataBand.setName("DataBand" + nameIndex);
        //        nameIndex++;
        //        page.getComponents().add(dataBand);

        //        double pos = 0;
        //        double columnWidth = page.getWidth() / tableSource.getColumns().size();

        //        for (StiDataColumn dataColumn : tableSource.getColumns())
        //        {

        //            StiText dataText = new StiText(new StiRectangle(pos, 0, columnWidth, 0.5));
        //            dataText.setText("{" + tableSource.getName() + "." + dataColumn.getName() + "}");
        //            dataText.setName("DataText" + nameIndex.toString());
        //            dataText.getBorder().setSide(StiBorderSides.All);
        //            dataBand.getComponents().add(dataText);

        //            pos = pos + columnWidth;
        //            nameIndex++;
        //        }
        //        dataBands.add(dataBand);
        //    }

        //    dataBands.get(1).setDataRelationName("Relation");
        //    dataBands.get(1).setMasterComponent(dataBands.get(0));

        //    ArrayList<String> cols = new ArrayList<String>();
        //    cols.add("cId");

        //    StiDataRelation rel = new StiDataRelation("Relation", tableSources.get(0),
        //    tableSources.get(1), cols, cols);

        //    report.getDictionary().getRelations().add(rel);

        //    report.Render();

        //    viewerPanel.getStiViewModel()
        //        .getEventDispatcher()
        //        .dispatchStiEvent(
        //            new StiViewCommonEvent(StiViewCommonEvent.DOCUMENT_FILE_LOADED,
        //            new StiDocument(report), null));
        //    return View();
        //}
        //private StiReport GetReport()
        //{
        //    StiReport report = new StiReport();
        //    Icls_SPsClient spsClient = new Icls_SPsClient();
        //    Icls_BankClient BankClient = new Icls_BankClient();
        //    int totalRow;
        //    //report.Dictionary.Variables["BankAccountNum"].Value = SearchObj

        //    //var ls = spsClient.Sp_GardesheHesabBank(SearchObj.BankIdList, SearchObj.UniqueAccountNO_Ls, "13960101", "13960625", false);
        //    var ls = spsClient.Sp_GardesheHesabBank("5", "010101", "13960101", "13960625", false);
        //    var BankLS = BankClient.SelectAll(1, 100, "BankID", "int", false, out totalRow);


        //    string Path = Server.MapPath("~/Report/TestReport.mrt");
        //    report.Load(Path);

        //    report.RegBusinessObject("BankLs", BankLS);
        //    report.RegBusinessObject("GardeshHesab", ls);
        //    return report;
        //}


        public ActionResult Report1()
        {
            return View();
        }
        public ActionResult ReportTest()
        {

            StiReport report = new StiReport();
            //Icls_BankClient BankCLient = new Icls_BankClient();
            var db = new TreasuryEntities();
            Icls_BasicBanksClient BsikBankClient = new Icls_BasicBanksClient();
            int TotalRow;
            //var BAnkLS = BankCLient.SelectAll(1, 100, "BankID", "int", false, out TotalRow);
            var BAnkLS = db.Banks;
            var BasicBankLs = BsikBankClient.SelectAll(1, 100, "BasicBankId", "int", false, out TotalRow);
            var CommonLs = from p in BAnkLS
                           join b in BasicBankLs on p.BasicBankID equals b.BasicBankId
                           select p;
            var count = CommonLs.Count();
            string Path = Server.MapPath("~/Report/Report1.mrt");
            report.Load(Path);
            report.RegBusinessObject("MyData", "Bank", BAnkLS);
            report.RegBusinessObject("MyData", "BasicBank", BasicBankLs);
            report.RegBusinessObject("MyData", "Common", CommonLs);


            StiDataRelation dataRelation = new StiDataRelation("MyRelation", report.Dictionary.DataSources["MyDataSource1"], report.Dictionary.DataSources["MyDataSource2"], new System.String[] { "MyDataSource1ID" }, new System.String[] { "MyDataSource2ID" });
            report.Dictionary.RegRelations();
            report.Dictionary.Relations.Add(dataRelation);

            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);

        }

        public ActionResult Testi()
        {
            string sConnectionString;
            sConnectionString = "Password=;User ID=sa;"
                 + "Initial Catalog=Treasury;"
            + @"Data Source=CSDSERVER2003\SQL2008";
            SqlConnection objConn
            = new SqlConnection(sConnectionString);
            objConn.Open();


            SqlDataAdapter daBasicBank = new SqlDataAdapter("Select * From BasicBanks", objConn);
            SqlDataAdapter daBanks = new SqlDataAdapter("Select * From Banks", objConn);

            DataSet dsTreasury = new DataSet("Treasury");

            // daBasicBank.FillSchema(dsTreasury, SchemaType.Source, "BasicBanks");
            //daBasicBank.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            //daBasicBank.Fill(dsTreasury, "BasicBanks");
            //DataTable tblBasicBank;
            // tblBasicBank = dsTreasury.Tables["BasicBanks"];



            //daBanks.FillSchema(dsTreasury, SchemaType.Source, "Banks");
            daBanks.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            daBanks.Fill(dsTreasury, "Banks");


            //DataTable tblBanks;
            //tblBanks = dsTreasury.Tables["Banks"];


            StiReport report = new StiReport();
            report.Load(Server.MapPath("~/Report/ReportTest.mrt"));


            report.Dictionary.Databases.Clear();
            report.RegData(dsTreasury);




            report.Dictionary.SynchronizeBusinessObjects();
            return StiMvcViewer.GetReportSnapshotResult(HttpContext, report);

        }


        //public string GetBasicBank(int BankType)
        //{
        //    var res = new StringBuilder();
        //    return res.ToString();
        //}
        public string GetBanks(int BasicBankID, bool BankType)
        {
            int totalrow;
            //Icls_BankClient BankClient = new Icls_BankClient();
            string where = "BasicBankID=" + BasicBankID + " and Deposited =" + BankType;
            var db = new TreasuryEntities();
            //var query = BankClient.SelectBy_DynamicWhereClause(where, 1, 1000, "BankID", "int", false, out totalrow);
            //var query = db.Banks;
            var query = db.Banks.Where(d => d.BasicBankID == BasicBankID);
            var res = new StringBuilder();
            res.Append("<option >" + Resourse.Resources.PleaseSelect + "</option>");

            foreach (var i in query)
            {
                res.Append("<option value='" + i.BankID + "'>" + i.BranchName + "-" + i.BranchCode + "-" + i.Bank_AccountNumber + "</option>");
            }
            return res.ToString();
        }
        //ایجاد پرینت
        public ActionResult PrintReport()
        {
            return StiMvcViewer.PrintReportResult();

        }
        //ایجاد خروجی
        public ActionResult ExportReport()
        {
            return StiMvcViewer.ExportReportResult();
        }

        public ActionResult ViewerEvent()
        {
            return StiMvcViewer.ViewerEventResult();
        }

        public ActionResult Interaction()
        {
            return StiMvcViewer.InteractionResult();
        }


    }
}