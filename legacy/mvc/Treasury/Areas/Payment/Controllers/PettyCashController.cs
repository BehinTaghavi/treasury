using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CSD.Treasury.Service.PettyCash_TransactionDetailService;
using CSD.Treasury.Service.PettyCash_TransactionHeaderService;
using CSD.Treasury.Service.Request_Payment_BreakPriceService;
using CSD.Treasury.UI.Controllers;
using CSD.Treasury.Service.PettyCashService;
using CSD.Treasury.Service.BehalfGroupService;
using CSD.Treasury.Service.ReferenceTypeService;
using CSD.Treasury.Service.ReferenceTypes_By_BehalfGroupService;
using CSD.Treasury.Service.PettyCash_ReferenceTypes_ValueService;
using System.Text;
using CSD.Treasury.UI.Models;
using CSD.Treasury.DataModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;
using CSD.Treasury.Service.Request_Payment_StatusDetailsService;
using CSD.Treasury.Common.Enumeration;
using CSD.Treasury.Service.Request_PaymentService;
using System.Globalization;
using System.Configuration;
using CSD.Treasury.Service.Reciept_ReferenceTypes_ValueService;
using System.Data.Entity.Validation;

namespace CSD.Treasury.UI.Areas.Payment.Controllers
{
    [Authorize(Roles = "Admin,Manager,PettyCash")]
    public class PettyCashController : BaseController
    {
        // GET: Payment/PettyCash
        public ActionResult Index(int headerId, int pageIndex = 0)
        {
            ViewBag.pageIndex = pageIndex;
            ViewBag.HeaderId = headerId;
            ViewBag.DateNow = Utility.DateStructure(Utility.ToPersianDate(DateTime.Now));
           
            return View();
        }
        public ActionResult Search(string code, string decsription, int pageIndex = 0)
        {
            ViewBag.pageIndex = pageIndex;
            ViewBag.code = code;
            ViewBag.decsription = decsription;
            return View();
        }
        public ActionResult _Index()
        {
            Icls_PettyCash_TransactionHeaderClient pth = new Icls_PettyCash_TransactionHeaderClient();
            Icls_PettyCashClient petyCashClient = new Icls_PettyCashClient();
            int TotalRow;
            int petycashId;
            string where = "1=1";
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                var Petycash = petyCashClient.SelectBy_DynamicWhereClause("UserID=" + User.Id, 1, 100, "PettyCashID", "int", false, out TotalRow);
                if (Petycash != null && Petycash.Length > 0)
                {
                    petycashId = Petycash.SingleOrDefault().PettyCashID;
                    where = where + " and PettyCashID = " + petycashId;
                }
            }
            var query = pth.SelectBy_DynamicWhereClause(where, 1, 1000, "PettyCashTransactionHeaderID", "dec", false, out TotalRow);
            return View(query.ToList());
        }
        public ActionResult ListHeader(SearchModelPettyCash obj)
        {
            int TotalRow;
            Icls_PettyCash_TransactionHeaderClient pth = new Icls_PettyCash_TransactionHeaderClient();
            int pageNum = obj.PageIndex;
            int pageSize = 10;

            var query = pth.SelectBy_DynamicWhereClause("1=1", pageNum + 1, pageSize, "PettyCashTransactionHeaderID", "dec", false, out TotalRow);


            obj.TotalPageCount = (int)Math.Ceiling(TotalRow / (double)pageSize);
            obj.CurrentPage = pageNum;
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = TotalRow;
            obj.List = query.ToList();

            return PartialView(obj);
        }

        public JsonResult Show(int HeaderId = 0)
        {
            int TotalRow;
            var db = new TreasuryEntities();
            List<CustomPettyCash_TransactionDetail> list = new List<CustomPettyCash_TransactionDetail>();
            Icls_PettyCash_TransactionDetailClient pcd = new Icls_PettyCash_TransactionDetailClient();
            Icls_PettyCash_TransactionHeaderClient pcH = new Icls_PettyCash_TransactionHeaderClient();
            Icls_ReferenceTypes_By_BehalfGroupClient RB = new Icls_ReferenceTypes_By_BehalfGroupClient();
            Icls_PettyCash_ReferenceTypes_ValueClient referenceTypes_ValueClient = new Icls_PettyCash_ReferenceTypes_ValueClient();
            //CustomPettyCash_TransactionDetail objn = new CustomPettyCash_TransactionDetail() { BehalfGroupList = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"])).ToList(), AccountNumLs = Utility.v_accountsLS(), UniqueAccount1 = new UCItem(), UniqueAccount2 = new UCItem() };
            CustomPettyCash_TransactionDetail objn = new CustomPettyCash_TransactionDetail();
            Icls_PettyCashClient pc = new Icls_PettyCashClient();
            var v_accountsLS = Utility.v_accountsLS();
            var customaccountsLS = db.v_CostAccounts.ToList();


            string where = "1=1";
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                where = where + " and  UserID= " + User.Id;
            }
            var pettyCahLs = pc.SelectBy_DynamicWhereClause(where, 1, 100, "PettyCashID", "int", false, out TotalRow).ToList();

            foreach (var item in pettyCahLs)
            {
                item.UniqueAccountTitle = Utility.GetUniqueAccount(item.Unique_Account_No);
                item.Mande = Utility.GetPettyCashBalance(item.PettyCashID);
            }

            //Edit Mode
            if (HeaderId != 0)
            {
                //var PettyCash_Header = pcH.SelectAll(1, 100, "PettyCashTransactionHeaderID", "dec", true, out TotalRow);

                //foreach (var item in PettyCash_Header)
                //{
                //    item.Unique_Account_no_two = Utility.GetUniqueAccount(item.Unique_Account_no_two);
                //}
                var selectedHeader = pcH.SelectRow(HeaderId).SingleOrDefault();
                var SelectedpettyCash = pc.SelectRow(selectedHeader.PettyCashID).SingleOrDefault();
                var query = pcd.SelectBy_DynamicWhereClause("PettyCashTransactionHeaderID=" + HeaderId, 1, 2000, "PettyCashTransactionDetailID", "dec", true, out TotalRow).ToList();
                
                CustomPettyCash_TransactionHeader HeaderObj = new CustomPettyCash_TransactionHeader()
                {
                    PettyCashLs = pettyCahLs,
                    SelectedPettyCashId = SelectedpettyCash.PettyCashID,
                    PayCeiling = (int)SelectedpettyCash.PayCeiling.Value,
                    Settle_Limit = (int)SelectedpettyCash.Settle_Limit.Value,
                    PettyCash_Ammount = (int)SelectedpettyCash.PettyCash_Ammount.Value,
                    PettyCashTransactionDate = selectedHeader.PettyCashTransactionDate,
                    UniqueAccountTitle1 = SelectedpettyCash.Unique_Account_No + " - " + Utility.GetUniqueAccount(SelectedpettyCash.Unique_Account_No),
                    UniqueAccountTitle2 = selectedHeader.Unique_Account_no_two + " - " + Utility.GetUniqueAccount(selectedHeader.Unique_Account_no_two),
                    PettyCashID = SelectedpettyCash.PettyCashID,
                    ShowVakhahiBtn = Utility.HasVakhahiToAddHeader(HeaderId),
                    Mande = Utility.GetPettyCashBalance(selectedHeader.PettyCashID),

                };


                foreach (var item in query)
                {
                    var BehalfGroupId = Utility.GetBehalfGroupBybehalfId(item.BehalfID).BehalfGroupID;
                    CustomPettyCash_TransactionDetail obj = new CustomPettyCash_TransactionDetail();
                    obj.PayDate = item.PayDate;
                    obj.PettyCashTransactionDetailID = (int)item.PettyCashTransactionDetailID;
                    obj.Price = (int)item.Price;
                    obj.IsAccept = item.IsAccept;
                    obj.IsVakhahi = item.IsVakhahi;
                    obj.EllateVakhahi = item.EllateVakhahi;
                    obj.Description = item.Description;
                    obj.BehalfGroupID = item.BehalfGroupID;
                    obj.BehalfID = item.BehalfID;
                    obj.UniqueAccountTitle1 = new UCItem { Name = Utility.GetUniqueAccount(item.Unique_Account_No), Code = (!string.IsNullOrEmpty(item.Unique_Account_No) && item.Unique_Account_No.Trim() != "0" ? item.Unique_Account_No.Trim() : null) };
                    obj.UniqueAccountTitle2 = new UCItem { Name = Utility.GetUniqueAccount(item.Unique_Account_No_two), Code = (!string.IsNullOrEmpty(item.Unique_Account_No_two) && item.Unique_Account_No_two.Trim() != "0" ? item.Unique_Account_No_two.Trim() : null) };
                    obj.BehalfList = Utility.GetBehalfLs(item.BehalfGroupID);
                    obj.uniqAccountType = item.Account_no;

                    //عطف های یابت
                    var Refrencels = new List<Reference>();
                    var query1 = referenceTypes_ValueClient.SelectBy_DynamicWhereClause("PettyCashTransactionDetailID =" + item.PettyCashTransactionDetailID, 1, 50, "PettyCash_ReferenceTypesValueID", "int", false, out TotalRow).Select(q => new Reference() { Id = q.ReferenceID, Value = q.ReferenceValue, Title = Utility.GetReferenceTitleByRefrenceId(q.ReferenceID) }).ToList();
                    var query2 = RB.SelectBy_DynamicWhereClause("BehalfGroupID=" + BehalfGroupId, 1, 10000, "ReferenceID", "int", true, out TotalRow).Select(q => new Reference() { Id = q.ReferenceID, Value = "", Title = Utility.GetReferenceTitleByRefrenceId(q.ReferenceID) }).ToList();
                    var ls = query2.Where(q => !query1.Select(d => d.Id).Contains(q.Id)).ToList();
                    Refrencels.AddRange(query1);
                    Refrencels.AddRange(ls);

                    obj.Refrences = Refrencels;



                    obj.IsAcceptVakhahi = item.IsAcceptVakhahi;
                    obj.AcceptVakhahiDate = item.AcceptVakhahiDate;

                    //obj.BehalfGroupList = Utility.GetBehalfsGroupLs(item.BehalfGroupID);
                    obj.BehalfTitle = Utility.GetBehalf(item.BehalfID);
                    //obj.UniqueAccountTitle1 = Utility.GetUniqueAccount(item.Unique_Account_No);
                    //obj.UniqueAccountTitle2 = Utility.GetUniqueAccount(item.Unique_Account_No_two);

                    //obj.AccountNumLs = Utility.v_accountsLS();
                    obj.UniqueAccount1 = new UCItem();
                    obj.UniqueAccount2 = new UCItem();

                    //obj.References = Utility.GetReferenceTypeByBehalfGroupLs(item.BehalfGroupID);


                    list.Add(obj);
                }

                if (list != null)
                {
                    return Json(new
                    {                        
                        Header = HeaderObj,
                        RequestId = selectedHeader.RequestID,
                        AcceptPettyCash = (User.IsInRole("Admin") || User.IsInRole("AcceptPettyCash") || User.IsInRole("Manager")),
                        BehalfGroupList = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"])),
                        AccountNumLs = customaccountsLS,
                        RefrenceTypeLs = Utility.GetReferenceTypeLs(),
                        PettyCash_TransactionLs = list.Select(q => new
                        {
                            AcceptVakhahiDate = Utility.GetPersianDate(q.AcceptVakhahiDate),
                            PettyCashTransactionDetailID = q.PettyCashTransactionDetailID,
                            AccountNumLs = q.AccountNumLs,
                            Account_No = q.Account_No,
                            BehalfGroupID = q.BehalfGroupID,
                            BehalfGroupList = q.BehalfGroupList,
                            BehalfGroupTiltle = q.BehalfGroupTiltle,
                            BehalfID = q.BehalfID,
                            BehalfList = q.BehalfList,
                            BehalfTitle = q.BehalfTitle,
                            EllateVakhahi = q.EllateVakhahi,
                            Description = q.Description,
                            IsAccept = q.IsAccept,
                            IsAcceptVakhahi = q.IsAcceptVakhahi,
                            IsVakhahi = q.IsVakhahi,
                            PayDate = Utility.GetPersianDate(q.PayDate),
                            PettyCashName = q.PettyCashName,
                            Price = q.Price,
                            Refrences = q.References,
                            RefrenceTitle1 = q.RefrenceTitle1,
                            RefrenceTitle2 = q.RefrenceTitle2,
                            RefrenceTypeLs = q.RefrenceTypeLs,
                            SelectedRefrence = q.SelectedRefrence,
                            uniqAccountType = q.uniqAccountType,
                            UniqueAccount1 = q.UniqueAccountTitle1,
                            UniqueAccount2 = q.UniqueAccountTitle2,
                            UniqueAccountLs1 = q.UniqueAccountLs1,
                            UniqueAccountLs2 = q.UniqueAccountLs2,
                            UniqueAccountTitle1 = q.UniqueAccountTitle1,
                            UniqueAccountTitle2 = q.UniqueAccountTitle2,
                            Unique_Account_No1 = q.Unique_Account_No1,
                            Unique_Account_No2 = q.Unique_Account_No2,
                            newItem = false
                        })
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            //Create Mode

            //list.Add(objn);

            var Header = pcH.SelectAll(1, 100, "PettyCashTransactionHeaderID", "dec", true, out TotalRow);
            var lastHeaderId = 0;
            if (Header != null && Header.LastOrDefault() != null)
            {
                lastHeaderId = Header.LastOrDefault().PettyCashID + 1;
            }
            else
            {
                lastHeaderId = 1;

            }
            return Json(new
            {
                BehalfGroupID = ConfigurationManager.AppSettings["BehalfGroupID"],
                BehalfId = ConfigurationManager.AppSettings["BehalfId"],
                Header = new CustomPettyCash_TransactionHeader() { PettyCashLs = pettyCahLs, lastHeaderId = lastHeaderId },
                BehalfGroupList = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"])),
                AcceptPettyCash = (User.IsInRole("Admin") || User.IsInRole("AcceptPettyCash") || User.IsInRole("Manager")),
                AccountNumLs = customaccountsLS,
                RefrenceTypeLs = Utility.GetReferenceTypeLs(),
                PettyCash_TransactionLs = list
            }, JsonRequestBehavior.AllowGet);

            //return Json(new
            //{       
            //    PettyCash_Header = PettyCash_Header,
            //    UCA_one_Title = Utility.GetUniqueAccount(PettyCash_Header.FirstOrDefault().Unique_Account_no_two),
            //    BehalfGroupList = Utility.GetBehalfsGroupLs(),
            //    AccountNumLs = Utility.v_accountsLS(),
            //    PettyCash_TransactionLs = list
            //}, JsonRequestBehavior.AllowGet);
        }

        //public JsonResult Show(string code, string description)
        //{
        //    int TotalRow;
        //    List<CustomPettyCash_TransactionDetail> list = new List<CustomPettyCash_TransactionDetail>();
        //    Icls_PettyCash_TransactionDetailClient pcd = new Icls_PettyCash_TransactionDetailClient();
        //    Icls_PettyCash_TransactionHeaderClient pcH = new Icls_PettyCash_TransactionHeaderClient();
        //    Icls_ReferenceTypes_By_BehalfGroupClient RB = new Icls_ReferenceTypes_By_BehalfGroupClient();
        //    Icls_PettyCash_ReferenceTypes_ValueClient referenceTypes_ValueClient = new Icls_PettyCash_ReferenceTypes_ValueClient();
        //    //CustomPettyCash_TransactionDetail objn = new CustomPettyCash_TransactionDetail() { BehalfGroupList = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"])).ToList(), AccountNumLs = Utility.v_accountsLS(), UniqueAccount1 = new UCItem(), UniqueAccount2 = new UCItem() };
        //    CustomPettyCash_TransactionDetail objn = new CustomPettyCash_TransactionDetail();
        //    Icls_PettyCashClient pc = new Icls_PettyCashClient();

        //    string where = "1=1";
        //    if (!User.IsInRole("Admin") && User.Id != 48)
        //    {
        //        where = where + " and  UserID= " + User.Id;

        //    }
        //    var pettyCahLs = pc.SelectBy_DynamicWhereClause(where, 1, 100, "PettyCashID", "int", false, out TotalRow).ToList();

        //    foreach (var item in pettyCahLs)
        //    {
        //        item.UniqueAccountTitle = Utility.GetUniqueAccount(item.Unique_Account_No);
        //    }

        //    //Edit Mode
        //    if (!string.IsNullOrEmpty(code) || !str)
        //    {
        //        //var PettyCash_Header = pcH.SelectAll(1, 100, "PettyCashTransactionHeaderID", "dec", true, out TotalRow);

        //        //foreach (var item in PettyCash_Header)
        //        //{
        //        //    item.Unique_Account_no_two = Utility.GetUniqueAccount(item.Unique_Account_no_two);
        //        //}
        //       // var selectedHeader = pcH.SelectRow(HeaderId).SingleOrDefault();
        //        //var SelectedpettyCash = pc.SelectRow(selectedHeader.PettyCashID).SingleOrDefault();
        //        var query = pcd.SelectBy_DynamicWhereClause("Unique_Account_No=" + code + " and Description like '%" + description + "%'", 1, 100, "PettyCashTransactionDetailID", "dec", true, out TotalRow).ToList();

        //        //CustomPettyCash_TransactionHeader HeaderObj = new CustomPettyCash_TransactionHeader()
        //        //{
        //        //    PettyCashLs = pettyCahLs,
        //        //    SelectedPettyCashId = SelectedpettyCash.PettyCashID,
        //        //    PayCeiling = (int)SelectedpettyCash.PayCeiling.Value,
        //        //    Settle_Limit = (int)SelectedpettyCash.Settle_Limit.Value,
        //        //    PettyCash_Ammount = (int)SelectedpettyCash.PettyCash_Ammount.Value,
        //        //    PettyCashTransactionDate = selectedHeader.PettyCashTransactionDate,
        //        //    UniqueAccountTitle1 = SelectedpettyCash.Unique_Account_No + " - " + Utility.GetUniqueAccount(SelectedpettyCash.Unique_Account_No),
        //        //    UniqueAccountTitle2 = selectedHeader.Unique_Account_no_two + " - " + Utility.GetUniqueAccount(selectedHeader.Unique_Account_no_two),
        //        //    PettyCashID = SelectedpettyCash.PettyCashID
        //        //};


        //        foreach (var item in query)
        //        {
        //            var BehalfGroupId = Utility.GetBehalfGroupBybehalfId(item.BehalfID).BehalfGroupID;
        //            CustomPettyCash_TransactionDetail obj = new CustomPettyCash_TransactionDetail();
        //            obj.PayDate = item.PayDate;
        //            obj.PettyCashTransactionDetailID = (int)item.PettyCashTransactionDetailID;
        //            obj.Price = (int)item.Price;
        //            obj.IsAccept = item.IsAccept;
        //            obj.IsVakhahi = item.IsVakhahi;
        //            obj.EllateVakhahi = item.EllateVakhahi;
        //            obj.Description = item.Description;
        //            obj.BehalfGroupID = item.BehalfGroupID;
        //            obj.BehalfID = item.BehalfID;
        //            obj.UniqueAccountTitle1 = new UCItem { Name = Utility.GetUniqueAccount(item.Unique_Account_No), Code = (!string.IsNullOrEmpty(item.Unique_Account_No) && item.Unique_Account_No.Trim() != "0" ? item.Unique_Account_No.Trim() : null) };
        //            obj.UniqueAccountTitle2 = new UCItem { Name = Utility.GetUniqueAccount(item.Unique_Account_No_two), Code = (!string.IsNullOrEmpty(item.Unique_Account_No_two) && item.Unique_Account_No_two.Trim() != "0" ? item.Unique_Account_No_two.Trim() : null) };
        //            obj.BehalfList = Utility.GetBehalfLs(item.BehalfGroupID);
        //            obj.uniqAccountType = item.Account_no;

        //            //عطف های یابت
        //            var Refrencels = new List<Reference>();
        //            var query1 = referenceTypes_ValueClient.SelectBy_DynamicWhereClause("PettyCashTransactionDetailID =" + item.PettyCashTransactionDetailID, 1, 50, "PettyCash_ReferenceTypesValueID", "int", false, out TotalRow).Select(q => new Reference() { Id = q.ReferenceID, Value = q.ReferenceValue, Title = Utility.GetReferenceTitleByRefrenceId(q.ReferenceID) }).ToList();
        //            var query2 = RB.SelectBy_DynamicWhereClause("BehalfGroupID=" + BehalfGroupId, 1, 10000, "ReferenceID", "int", true, out TotalRow).Select(q => new Reference() { Id = q.ReferenceID, Value = "", Title = Utility.GetReferenceTitleByRefrenceId(q.ReferenceID) }).ToList();
        //            var ls = query2.Where(q => !query1.Select(d => d.Id).Contains(q.Id)).ToList();
        //            Refrencels.AddRange(query1);
        //            Refrencels.AddRange(ls);

        //            obj.Refrences = Refrencels;



        //            obj.IsAcceptVakhahi = item.IsAcceptVakhahi;
        //            obj.AcceptVakhahiDate = item.AcceptVakhahiDate;

        //            //obj.BehalfGroupList = Utility.GetBehalfsGroupLs(item.BehalfGroupID);
        //            obj.BehalfTitle = Utility.GetBehalf(item.BehalfID);
        //            //obj.UniqueAccountTitle1 = Utility.GetUniqueAccount(item.Unique_Account_No);
        //            //obj.UniqueAccountTitle2 = Utility.GetUniqueAccount(item.Unique_Account_No_two);

        //            //obj.AccountNumLs = Utility.v_accountsLS();
        //            obj.UniqueAccount1 = new UCItem();
        //            obj.UniqueAccount2 = new UCItem();

        //            //obj.References = Utility.GetReferenceTypeByBehalfGroupLs(item.BehalfGroupID);


        //            list.Add(obj);
        //        }

        //        if (list != null)
        //        {
        //            return Json(new
        //            {
        //                //Header = HeaderObj,
        //                //RequestId = selectedHeader.RequestID,
        //                AcceptPettyCash = (User.IsInRole("Admin") || User.IsInRole("AcceptPettyCash")),
        //                BehalfGroupList = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"])),
        //                AccountNumLs = Utility.v_accountsLS(),
        //                RefrenceTypeLs = Utility.GetReferenceTypeLs(),
        //                PettyCash_TransactionLs = list.Select(q => new
        //                {
        //                    AcceptVakhahiDate = Utility.GetPersianDate(q.AcceptVakhahiDate),
        //                    PettyCashTransactionDetailID = q.PettyCashTransactionDetailID,
        //                    AccountNumLs = q.AccountNumLs,
        //                    Account_No = q.Account_No,
        //                    BehalfGroupID = q.BehalfGroupID,
        //                    BehalfGroupList = q.BehalfGroupList,
        //                    BehalfGroupTiltle = q.BehalfGroupTiltle,
        //                    BehalfID = q.BehalfID,
        //                    BehalfList = q.BehalfList,
        //                    BehalfTitle = q.BehalfTitle,
        //                    EllateVakhahi = q.EllateVakhahi,
        //                    Description = q.Description,
        //                    IsAccept = q.IsAccept,
        //                    IsAcceptVakhahi = q.IsAcceptVakhahi,
        //                    IsVakhahi = q.IsVakhahi,
        //                    PayDate = Utility.GetPersianDate(q.PayDate),
        //                    PettyCashName = q.PettyCashName,
        //                    Price = q.Price,
        //                    Refrences = q.References,
        //                    RefrenceTitle1 = q.RefrenceTitle1,
        //                    RefrenceTitle2 = q.RefrenceTitle2,
        //                    RefrenceTypeLs = q.RefrenceTypeLs,
        //                    SelectedRefrence = q.SelectedRefrence,
        //                    uniqAccountType = q.uniqAccountType,
        //                    UniqueAccount1 = q.UniqueAccountTitle1,
        //                    UniqueAccount2 = q.UniqueAccountTitle2,
        //                    UniqueAccountLs1 = q.UniqueAccountLs1,
        //                    UniqueAccountLs2 = q.UniqueAccountLs2,
        //                    UniqueAccountTitle1 = q.UniqueAccountTitle1,
        //                    UniqueAccountTitle2 = q.UniqueAccountTitle2,
        //                    Unique_Account_No1 = q.Unique_Account_No1,
        //                    Unique_Account_No2 = q.Unique_Account_No2,
        //                    newItem = false
        //                })
        //            }, JsonRequestBehavior.AllowGet);
        //        }
        //    }
        //    //Create Mode

        //    //list.Add(objn);

        //    var Header = pcH.SelectAll(1, 100, "PettyCashTransactionHeaderID", "dec", true, out TotalRow);
        //    var lastHeaderId = 0;
        //    if (Header != null && Header.LastOrDefault() != null)
        //    {
        //        lastHeaderId = Header.LastOrDefault().PettyCashID + 1;
        //    }
        //    else
        //    {
        //        lastHeaderId = 1;

        //    }
        //    return Json(new
        //    {
        //        Header = new CustomPettyCash_TransactionHeader() { PettyCashLs = pettyCahLs, lastHeaderId = lastHeaderId },
        //        BehalfGroupList = Utility.GetBehalfsGroupLs().Where(d => d.Receipt_Pay_TypeID == int.Parse(ConfigurationManager.AppSettings["Payment_Type_ID"])),
        //        AcceptPettyCash = (User.IsInRole("Admin") || User.IsInRole("AcceptPettyCash")),
        //        AccountNumLs = Utility.v_accountsLS(),
        //        RefrenceTypeLs = Utility.GetReferenceTypeLs(),
        //        PettyCash_TransactionLs = list
        //    }, JsonRequestBehavior.AllowGet);

        //    //return Json(new
        //    //{       
        //    //    PettyCash_Header = PettyCash_Header,
        //    //    UCA_one_Title = Utility.GetUniqueAccount(PettyCash_Header.FirstOrDefault().Unique_Account_no_two),
        //    //    BehalfGroupList = Utility.GetBehalfsGroupLs(),
        //    //    AccountNumLs = Utility.v_accountsLS(),
        //    //    PettyCash_TransactionLs = list
        //    //}, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult getPettyCashUC()
        {
            var dc = new TreasuryEntities();
            //نوع ذینفع  
            var unique_account_type = dc.v_unique_account_type.ToList();
            unique_account_type.Add(new v_unique_account_type { Code = 0, Description = Resourse.Resources.Others });
            ViewBag.unique_account_type = unique_account_type;
            return PartialView();
        }

        public JsonResult ChangeBehalfGroup(int BehalfGroupId)
        {
            //behalf list by  BehalfGroupId
            int totalRows;
            Service.BehalfsService.Icls_BehalfsClient obj = new Service.BehalfsService.Icls_BehalfsClient();
            List<Service.BehalfsService.Behalf> ls = new List<Service.BehalfsService.Behalf>();
            string whereclouse = "1=1";
            whereclouse = " BehalfGroupId=" + BehalfGroupId.ToString();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "BehalfGroupID", "int", true, out totalRows);
            ls.AddRange(query);

            //Refrence by BehalfGroupId
            Icls_ReferenceTypes_By_BehalfGroupClient RTBG = new Icls_ReferenceTypes_By_BehalfGroupClient();
            var Ls = RTBG.SelectBy_DynamicWhereClause("BehalfGroupID = " + BehalfGroupId, 1, 10000, "ReferenceID", "int", true, out totalRows).ToList();
            List<Service.ReferenceTypeService.ReferenceType> lsRefrence = new List<Service.ReferenceTypeService.ReferenceType>();
            foreach (var item in Ls)
            {
                Service.ReferenceTypeService.ReferenceType objRefrence = new Service.ReferenceTypeService.ReferenceType();
                objRefrence.referenceTypeID = item.ReferenceID;
                objRefrence.referenceTypeDescription = Utility.GetReferenceTitle(item.referenceTypeID);
                lsRefrence.Add(objRefrence);
            }
            List<Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup> refList = new List<Service.ReferenceTypes_By_BehalfGroupService.ReferenceTypes_By_BehalfGroup>();
            return Json(new { BehalfList = ls, Refrences = lsRefrence }, JsonRequestBehavior.AllowGet);

        }
        public List<Service.ReferenceTypeService.ReferenceType> getRefrences(int BehalfGroupId)
        {
            //behalf list by  BehalfGroupId
            int totalRows;
            Service.BehalfsService.Icls_BehalfsClient obj = new Service.BehalfsService.Icls_BehalfsClient();
            List<Service.BehalfsService.Behalf> ls = new List<Service.BehalfsService.Behalf>();
            string whereclouse = "1=1";
            whereclouse = " BehalfGroupId=" + BehalfGroupId.ToString();
            var query = obj.SelectBy_DynamicWhereClause(whereclouse, 1, 100, "BehalfGroupID", "int", true, out totalRows);
            ls.AddRange(query);

            //Refrence by BehalfGroupId
            Icls_ReferenceTypes_By_BehalfGroupClient RTBG = new Icls_ReferenceTypes_By_BehalfGroupClient();
            var Ls = RTBG.SelectBy_DynamicWhereClause("BehalfGroupID = " + BehalfGroupId, 1, 10000, "ReferenceID", "int", true, out totalRows).ToList();
            List<Service.ReferenceTypeService.ReferenceType> lsRefrence = new List<Service.ReferenceTypeService.ReferenceType>();
            foreach (var item in Ls)
            {
                Service.ReferenceTypeService.ReferenceType objRefrence = new Service.ReferenceTypeService.ReferenceType();
                objRefrence.referenceTypeID = item.referenceTypeID;
                objRefrence.referenceTypeDescription = Utility.GetReferenceTitle(item.referenceTypeID);
                lsRefrence.Add(objRefrence);
            }
            return lsRefrence;
        }

        #region Create
        [HttpPost]
        public ActionResult Save()
        {
            try
            {

            Icls_PettyCash_TransactionDetailClient pctd = new Icls_PettyCash_TransactionDetailClient();
            Icls_PettyCash_TransactionHeaderClient pcth = new Icls_PettyCash_TransactionHeaderClient();
            Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
            Icls_PettyCashClient pettyclient = new Icls_PettyCashClient();
            Icls_PettyCash_ReferenceTypes_ValueClient RefrencesClient = new Icls_PettyCash_ReferenceTypes_ValueClient();
            Icls_Request_Payment_BreakPriceClient BreakPriceClient = new Icls_Request_Payment_BreakPriceClient();
            var dateNow = DateTime.Now;
            int totalrow;
            string message = "", message1, message2, message3, message4, message5, messege6, message7, message8, messageBreak;

            dynamic DaynamicObj = JsonConvert.DeserializeObject(Request.Form[0]);
            var _header = DaynamicObj.header;
            //if (_header.id != null && _header.id.Value != 0)
            //{
            //    //Edit
            //    int TotalRow;
            //    var DetailsLs = pctd.SelectBy_DynamicWhereClause("PettyCashTransactionHeaderID=" + (int)_header.id.Value, 1, 100, "PettyCashTransactionDetailID", "dec", true, out TotalRow);
            //    foreach (var item in DetailsLs)
            //    {
            //        //حذف نمودن details های موجوددر حالت ویرایش
            //        pctd.Delete(item.PettyCashTransactionDetailID, out message2);
            //    }
            //}
            int SumPrice = _header.SumPrice;
            bool AddRequestPayment = _header.AddRequestPayment;



            int pettycashId = (int)_header.PettyCashID.Value;
            var pettyCash = pettyclient.SelectRow(pettycashId).SingleOrDefault();
            //pettyCash.PettyCash_Ammount =pettyCash.PettyCash_Ammount- SumPrice;
            ////update pettyCash /ویرایش مانده تن خواه 
            //pettyclient.Update(pettyCash, out message2);
            //Add PettyCash_TransactionHeader

            Service.PettyCash_TransactionHeaderService.PettyCash_TransactionHeader header = new Service.PettyCash_TransactionHeaderService.PettyCash_TransactionHeader();
            header.PettyCashTransactionDate = Utility.ToPersianDate(dateNow);
            header.PettyCashID = (int)_header.PettyCashID.Value;
            header.Unique_Account_no_two = _header.UniqueAccount2;
            Session.Abandon();
            var HeaderID = 0;
            var Result = false;
            if (_header.id != null && _header.id.Value != 0)
            {
                HeaderID = (int)_header.id.Value;
            }
            else
            {
                Result = pcth.Insert(header, out message1);
                HeaderID = int.Parse(message1.Split(';')[1]);
            }

            var _details = DaynamicObj.details;
            var deletedItem = DaynamicObj.deletedItem;

            foreach (var item in _details)
            {
                //string Account_no = item.Account_no;

                //Add PettyCash_TransactionDetail
                Service.PettyCash_TransactionDetailService.PettyCash_TransactionDetail details = new Service.PettyCash_TransactionDetailService.PettyCash_TransactionDetail();
                string PayDate = item.PayDate;
                details.PayDate = PayDate.Replace("/", "");
                details.PettyCashTransactionDetailID = (item.PettyCashTransactionDetailID.Value != null ? item.PettyCashTransactionDetailID.Value : 0);
                details.PettyCashTransactionHeaderID = HeaderID;
                details.BehalfGroupID = int.Parse(ConfigurationManager.AppSettings["BehalfGroupID"]); //item.BehalfGroupID;
                details.BehalfID = int.Parse(ConfigurationManager.AppSettings["BehalfId"]);//  item.BehalfID;
              
                details.Account_no = item.Account_no;
                var HasUniqueAccountTwo = Utility.HasUniqueAccountTwo(details.Account_no);
                details.Unique_Account_No = item.UniqueAccount1;
                if (HasUniqueAccountTwo)
                {
                    details.Unique_Account_No_two = item.UniqueAccount2;
                }
                details.Price = item.Price;
                details.IsAccept = item.IsAccept;
                details.IsVakhahi = item.IsVakhahi;
                details.EllateVakhahi = item.EllateVakhahi;

                details.Description = item.Description;
                if(string.IsNullOrEmpty(details.Description))
                {
                    details.Description = ";";
                }
                details.IsAcceptVakhahi = item.IsAcceptVakhahi;
                bool isacceptVakhahi = item.IsAcceptVakhahi;
                if (isacceptVakhahi)
                {
                    details.AcceptVakhahiDate = Utility.ToPersianDate(dateNow);
                }
                if (!item.newItem.Value)
                {
                    var Result3 = pctd.Update(details, out message3);
                }
                else
                {

                    var Result2 = pctd.Insert(details, out message3);

                }

                //var detailsObjID = int.Parse(message3.Split(';')[1]);

                //if (item.Refrences != null)
                //{
                //    foreach (var itemRefrence in item.Refrences)
                //    {
                //        Service.PettyCash_ReferenceTypes_ValueService.PettyCash_ReferenceTypes_Value OBJ = new Service.PettyCash_ReferenceTypes_ValueService.PettyCash_ReferenceTypes_Value();
                //        if (itemRefrence.value != null)
                //        {
                //            OBJ.ReferenceID = itemRefrence.referenceTypeID;
                //            OBJ.PettyCashTransactionDetailID = detailsObjID;
                //            OBJ.ReferenceValue = itemRefrence.value;
                //            RefrencesClient.Insert(OBJ, out messege6);
                //        }
                //    }
                //}
            }
            int TotalRow;
            var DetailsLs = pctd.SelectBy_DynamicWhereClause("PettyCashTransactionHeaderID=" + HeaderID, 1, 100, "PettyCashTransactionDetailID", "dec", true, out TotalRow);

            foreach (var item in deletedItem)
            {
                pctd.Delete(item.Value, out message2);
            }

            return Json(HeaderID, JsonRequestBehavior.AllowGet);
               
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public JsonResult CreateRequest(int HeaderId = 0)
        {
            try
            {
                //Icls_PettyCash_TransactionDetailClient pctd = new Icls_PettyCash_TransactionDetailClient();
                Icls_PettyCash_TransactionHeaderClient pcth = new Icls_PettyCash_TransactionHeaderClient();
                //Icls_Request_PaymentClient RP = new Icls_Request_PaymentClient();
                
                Icls_PettyCashClient pettyclient = new Icls_PettyCashClient();
                //Icls_PettyCash_ReferenceTypes_ValueClient RefrencesClient = new Icls_PettyCash_ReferenceTypes_ValueClient();
                //Icls_Request_Payment_BreakPriceClient BreakPriceClient = new Icls_Request_Payment_BreakPriceClient();
                var dateNow = DateTime.Now;
                int TotalRow;
                var db = new TreasuryEntities();
                Icls_PettyCash_TransactionDetailClient PettyCashDetailsclient = new Icls_PettyCash_TransactionDetailClient();
                var detailsLs = PettyCashDetailsclient.SelectBy_DynamicWhereClause("PettyCashTransactionHeaderID =" + HeaderId, 1, 1000, "PettyCashTransactionDetailID", "dec", false, out TotalRow);
                var petycash = pcth.SelectRow(HeaderId);
                var IsNotAcceptORVakhahi = detailsLs.Any(d => d.IsAccept == false && d.IsVakhahi==false);
                //درصورتی که تمام جزییات تنخواه تایید شده باشدمی تواند تنخواه را ببندد  
                if (!IsNotAcceptORVakhahi)
                {
                    string   message5;
                    var pettyCash = pettyclient.SelectRow(petycash.SingleOrDefault().PettyCashID).SingleOrDefault();
                    //تمایل به ایجاد درخواست وجه
                    //if (AddRequestPayment)
                    //{
                    PersianCalendar pc = new PersianCalendar();
                    pc.GetYear(DateTime.Now);
                    var BehalfId = ConfigurationManager.AppSettings["BehalfId"];
                    var pettyCashHeader = pcth.SelectRow(HeaderId).FirstOrDefault();
                    var SumPrice = detailsLs.Where(d=>d.IsAccept == true).Sum(d => d.Price);
                    //var IfAllIsaccepted = true;

                    //Icls_PettyCash_TransactionDetailClient PettyCashDetailsclient = new Icls_PettyCash_TransactionDetailClient();
                    //var detailsLs = PettyCashDetailsclient.SelectBy_DynamicWhereClause("PettyCashTransactionHeaderID =" + HeaderID, 1, 1000, "PettyCashTransactionDetailID", "dec", false, out TotalRow);
                    //foreach (var item in detailsLs)
                    //{
                    //    if (!item.IsAccept)
                    //    {
                    //        IfAllIsaccepted = false;
                    //    }
                    //} 
                    //&& IfAllIsaccepted
                    //درصورتی که تمام جزییات تنخواه تایید شده باشدتنخواه بسته و درخواست وجه صادر می شود
                    if (pettyCashHeader.RequestID == null)
                    {
                         CSD.Treasury.DataModel.Request_Payment RequestObj = new CSD.Treasury.DataModel.Request_Payment();
                        RequestObj.RequestDate = Utility.ToPersianDate(DateTime.Now);
                        var Month = pc.GetMonth(DateTime.Now);
                        RequestObj.FiscalYear = pc.GetYear(DateTime.Now).ToString();
                        if (Month >= 10 && ConfigurationManager.AppSettings["YearPlus"] == "True")
                        {
                            RequestObj.FiscalYear = (pc.GetYear(DateTime.Now) + 1).ToString();
                        }

                        RequestObj.UserID = pettyCash.UserID;
                        RequestObj.RequestDate = Utility.ToPersianDate(DateTime.Now);
                        RequestObj.RequestCreatedTime = DateTime.Now.TimeOfDay;
                        RequestObj.Unique_Account_No = pettyCash.Unique_Account_No;
                        RequestObj.PayAmount = SumPrice;                        
                        RequestObj.Priority = 1;                      
                        RequestObj.BehalfID = int.Parse(BehalfId);
                        RequestObj.IsWithdrawType = false;
                        RequestObj.Pay_TypeDetailID_source = 1;
                        RequestObj.Source_TypeID = 1;
                        //RequestObj.Pay_TypeDetailID_source = request.Pay_TypeDetailID_source;
                        //obj.Source_TypeID = request.Source_TypeID;
                        //obj.Source_PaymentValue = request.Source_PaymentValue;          
                        //obj.Destination_PaymentValue = request.Destination_PaymentValue;  

                        db.Request_Payment.Add(RequestObj);

                        //var result = RP.Insert(RequestObj, out message4);


                        //var RequestID = message4.Split(';')[1];
                        Icls_Request_Payment_StatusDetailsClient RPS = new Icls_Request_Payment_StatusDetailsClient();
                        //ثبت نامشخص در جدول وضعیت ها 
                        //Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails objStatus = new Service.Request_Payment_StatusDetailsService.Request_Payment_StatusDetails();
                        DataModel.Request_Payment_StatusDetails objStatus = new DataModel.Request_Payment_StatusDetails();
                        var persianDate = Utility.ToPersianDate(DateTime.Now);
                        objStatus.UserID = User.Id;
                        objStatus.ActionDate = persianDate;
                        objStatus.ActionTime = DateTime.Now.TimeOfDay;
                        objStatus.RequestStatusTypeID = (decimal)RequestStatusTypes.Unknown;
                        //objStatus.RequestID = decimal.Parse(RequestID);
                        objStatus.HostName = System.Net.Dns.GetHostName();
                        //var result2 = RPS.Insert(objStatus, out message);
                        //db.Request_Payment_StatusDetails.Add(objStatus);
                        RequestObj.Request_Payment_StatusDetails.Add(objStatus);

                        //ایجاد مبلغ در جدول Request_Payment_BreakPrice
                        DataModel.Request_Payment_BreakPrice RequestBreakPriceObj = new DataModel.Request_Payment_BreakPrice();
                        //RequestBreakPriceObj.RequestID = decimal.Parse(RequestID);
                        RequestBreakPriceObj.Price_Rial = SumPrice;
                        RequestBreakPriceObj.ExchangeTypeID = null;
                        RequestBreakPriceObj.Exchange_Rate = 1;
                        RequestBreakPriceObj.Exchange_Amount = SumPrice;
                        RequestObj.Request_Payment_BreakPrice.Add(RequestBreakPriceObj);

                        //BreakPriceClient.Insert(RequestBreakPriceObj, out messageBreak);

                        db.SaveChanges();
                        //Edit PettyCash Header
                       
                            pettyCashHeader.RequestID = RequestObj.RequestID;
                            //pettyCashHeader.IsConfirm = true;
                            pettyCashHeader.ConfirmDate = Utility.ToPersianDate(DateTime.Now);

                            var _result = pcth.Update(pettyCashHeader, out message5);
                        
                    }
                }
                //}
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult Confirm(int HeaderId = 0)
        {
            var db = new TreasuryEntities();
            var message ="";
            try
            {
                int TotalRow;
                //Icls_PettyCash_TransactionDetailClient PettyCashDetailsclient = new Icls_PettyCash_TransactionDetailClient();
                //var detailsLs = PettyCashDetailsclient.SelectBy_DynamicWhereClause("PettyCashTransactionHeaderID =" + HeaderId, 1, 1000, "PettyCashTransactionDetailID", "dec", false, out TotalRow);
                //var IsNotAcceptORVakhahi = detailsLs.Any(d => d.IsAccept == false && d.IsVakhahi == false);
                var detailsLs = db.PettyCash_TransactionDetail.Where(d=>d.PettyCashTransactionHeaderID == HeaderId);
                var IsNotAcceptORVakhahi = detailsLs.Any(d => d.IsAccept == false && d.IsVakhahi == false);
                //درصورتی که تمام جزییات تنخواه تایید شده باشدمی تواند تنخواه را ببندد  
                if (!IsNotAcceptORVakhahi)
                {
                    //Icls_PettyCash_TransactionHeaderClient pcH = new Icls_PettyCash_TransactionHeaderClient();
                    //var selectedHeader = pcH.SelectRow(HeaderId).SingleOrDefault();
                    var selectedHeader = db.PettyCash_TransactionHeader.SingleOrDefault(d=>d.PettyCashTransactionHeaderID == HeaderId);
                    selectedHeader.IsConfirm = true;
                    selectedHeader.ConfirmDate = Utility.ToPersianDate(DateTime.Now);
                    db.SaveChanges();
                    message = "بستن تنخواه با موفقیت انجام شد";
                    //string _out;
                    //var result = pcH.Update(selectedHeader, out _out);
                    return JsonMessage(true,message , null, false);

                }
                return JsonMessage(false, message, null, false);
            }
            catch (Exception ex)
            {
                 message = "با خطا مواجه شد";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("\r\n"))
                    {
                        message = ex.InnerException.InnerException.Message.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];
                    }
                    else
                    {
                        message += "</br>" + ex.InnerException.InnerException.Message;
                    }

                }
                else
                {
                    message += "</br>" + ex.Message;
                }
                return JsonMessage(false, message, null, false);
            }

        }
        public JsonResult SendVoucherPettyCash(decimal RefId, string ConfirmDate)
        {
            try
            {
                var db = new TreasuryEntities();
                var ls = db.PostVoucher(ConfirmDate, ConfirmDate, RefId, 0, false).ToList();
                var voucherNo = db.Voucher_Row.Where(d => d.ReferenceId == RefId && d.StatusID == 0).FirstOrDefault().AccVoucherNo;
                //if (voucherNo!= null)
                //{
                //    var PettyCash_TransactionHeader = db.PettyCash_TransactionHeader.SingleOrDefault(d => d.PettyCashTransactionHeaderID == RefId);
                //    PettyCash_TransactionHeader.AccVoucherNo = voucherNo;
                //}
                //db.SaveChanges();

                if (ls.Count() == 0)
                {
                    return JsonMessage(true, voucherNo + ";ارسال اسناد با موفقیت انجام شد", null, false);
                }
                else
                {
                    return JsonMessage(false, voucherNo + ";ارسال اسناد با خطا مواجه شد", null, false);
                }
              
            }
            catch(Exception ex)
            {
                return JsonMessage(false, "خطا در ارسال اسناد", null, false);
            }
        }
        public JsonResult Open(int HeaderId = 0)
        {
            try
            {
                Icls_PettyCash_TransactionHeaderClient pcH = new Icls_PettyCash_TransactionHeaderClient();
                var selectedHeader = pcH.SelectRow(HeaderId).SingleOrDefault();
                selectedHeader.IsConfirm = false;
                selectedHeader.ConfirmDate = null;
                string _out;
                var result = pcH.Update(selectedHeader, out _out);
                if (result)
                {
                    return Json(true, JsonRequestBehavior.AllowGet);
                }
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

        }

        public JsonResult Delete(int HeaderId = 0)
        {
            try
            {
                Icls_PettyCash_TransactionHeaderClient pcH = new Icls_PettyCash_TransactionHeaderClient();
                Icls_PettyCash_TransactionDetailClient pcD = new Icls_PettyCash_TransactionDetailClient();
                Icls_PettyCash_ReferenceTypes_ValueClient pcR = new Icls_PettyCash_ReferenceTypes_ValueClient();
                var selectedHeader = pcH.SelectRow(HeaderId).SingleOrDefault();
                int totalrow;
                var _Details = pcD.SelectBy_DynamicWhereClause("PettyCashTransactionHeaderID =" + HeaderId, 1, 100, "PettyCashTransactionDetailID", "dec", false, out totalrow);
                if (_Details.Any())
                {
                    if (_Details.Where(q => q.IsAccept || q.IsAcceptVakhahi || q.IsVakhahi).Any())
                    {
                        return Json(false, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        foreach (var item in _Details)
                        {
                            int totalrowR;
                            var _Ref = pcR.SelectBy_DynamicWhereClause("PettyCashTransactionDetailID =" + Convert.ToInt32(item.PettyCashTransactionDetailID), 1, 100, "PettyCashTransactionDetailID", "dec", false, out totalrowR);
                            foreach (var itemRef in _Ref)
                            {
                                string _outR;
                                var resultR = pcR.Delete(itemRef.PettyCash_ReferenceTypesValueID, out _outR);
                                if (!resultR)
                                {
                                    return Json(false, JsonRequestBehavior.AllowGet);
                                }
                            }
                            string _outD;
                            var resultD = pcD.Delete(item.PettyCashTransactionDetailID, out _outD);
                            if (!resultD)
                            {
                                return Json(false, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                }
                string _outH;
                var resultH = pcH.Delete(selectedHeader.PettyCashTransactionHeaderID, out _outH);
                if (!resultH)
                {
                    return Json(false, JsonRequestBehavior.AllowGet);
                }
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion

        #region PettyCash
        public JsonResult GetPettyCash(int UserId)
        {
            int TotalRow;
            Icls_PettyCashClient PC = new Icls_PettyCashClient();
            var obj = PC.SelectBy_DynamicWhereClause("UserID=" + UserId, 1, 100, "PettyCashID", "int", true, out TotalRow).SingleOrDefault();
            if (obj != null)
            {
                return Json(obj, JsonRequestBehavior.AllowGet);
            }
            return null;
        }
        #endregion

        #region PettyCashDetails
        public ActionResult GetHeaders(SearchModelPettyCash obj)
        {
            int TotalRow;
            int pageNum = obj.PageIndex;
            int pageSize = 10;
            Icls_PettyCash_TransactionHeaderClient PHClient = new Icls_PettyCash_TransactionHeaderClient();
            var ls = PHClient.SelectBy_DynamicWhereClause("PettyCashID=" + obj.PettyCashID, pageNum + 1, pageSize, "PettyCashTransactionHeaderID", "dec", false, out TotalRow).ToList();

            obj.TotalPageCount = (int)Math.Ceiling(TotalRow / (double)pageSize);
            obj.CurrentPage = pageNum;
            obj.CurrentPage = obj.PageIndex;
            obj.TotalItemCount = TotalRow;
            obj.List = ls.ToList();

            return PartialView(obj);
        }
        public JsonResult UniqueAccountLs(string q, string typeid, int pageSize, int page)
        {
            q = q.Replace("ی", "ي");//.Replace("ک", "ك");
            var dc = new TreasuryEntities();
            var ls = (from v in dc.v_unique_accounts
                      where v.Type_Code == typeid && (v.Account_Code.Trim().Contains(q.Trim()) || v.Account_Name.Trim().Contains(q.Trim()))
                      select new { id = v.Account_Code.Trim(), code = v.Account_Code.Trim(), text = v.Account_Code.Trim() + " - " + v.Account_Name });
            //var excludedRow = (page - 1) * pageSize;
            //.Skip(excludedRow)
            var query = ls.OrderBy(c => c.id).Take(pageSize).ToList();
            return Json(new { query, total_count = ls.Count() }, JsonRequestBehavior.AllowGet);
        }
        public PartialViewResult UniqueAccount(SearchModelUniqueAccount obj)
        {
            var dc = new TreasuryEntities();
            obj.Name = obj.Name.Replace("ی", "ي").Replace("ک", "ك");
            if (obj.UniqueAccountTypeId == "0")
            {
                List<v_unique_accounts> UniqueAccountLs = new List<v_unique_accounts>();
                v_unique_accounts objunique_account = new v_unique_accounts() { Account_Code = "0", Account_Name = "---" };
                UniqueAccountLs.Add(objunique_account);
                foreach (var item in UniqueAccountLs)
                {
                    item.Account_Name = item.Account_Name.Replace("ی", "ي").Replace("ک", "ك");
                }
                obj.List = UniqueAccountLs;
            }
            else
            {
                //obj.List = (from u in dc.v_unique_accounts
                //           where u.Type_Code == obj.UniqueAccountTypeId && u.Account_Name.Contains(obj.Name) && u.Account_Code.Contains(obj.Code)
                //           select new v_unique_accounts { Account_Code=u.Account_Code.Replace(" ",""), Account_Name=u.Account_Name }).OrderBy(c => c.Account_Name).Skip((obj.PageIndex - 1) * obj.PageSize).Take(obj.PageSize).ToList();
                obj.List = dc.v_unique_accounts.Where(d => d.Type_Code == obj.UniqueAccountTypeId && d.Account_Name.Contains(obj.Name) && d.Account_Code.Trim().Contains(obj.Code)).ToList();

            }

            obj.TotalPageCount = Convert.ToInt32(Math.Ceiling((decimal)dc.v_unique_accounts.Where(d => d.Type_Code == obj.UniqueAccountTypeId && d.Account_Name.Contains(obj.Name) && d.Account_Code.Contains(obj.Code)).Count() / obj.PageSize));
            obj.CurrentPage = obj.PageIndex;
            return PartialView(obj);
        }

        [HttpGet]
        public ActionResult ChangeVakhahiHeader(int HeaderID)
        {
            var db = new TreasuryEntities();
            var result= db.ChangeVakhahiHeader(HeaderID);  
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

    }
}