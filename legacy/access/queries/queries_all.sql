-- [~sq_c~tmp_UpsizingObject~sq_cAccno]
SELECT Accq.Ano0,Accq.Ano,Accq.Aname,Accq.Hm,Accq.Ht FROM [Accq] 

-- [~sq_c~tmp_UpsizingObject~sq_cCombo5]
SELECT EMP_AD.Personel,EMP_AD.N FROM [EMP_AD] ORDER BY EMP_AD.N

-- [~sq_c~tmp_UpsizingObject~sq_cCombo8]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid FROM [Bankacc] 

-- [~sq_cAccamounts~sq_cBaccno]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Iscur FROM [Bankacc] 

-- [~sq_cAdvance~sq_cChild13]
SELECT DISTINCTROW  FROM [] WHERE ([__ADVANCEID] = ADVANCEID) 

-- [~sq_cAuditform~sq_cperson]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Person

-- [~sq_cBabsrepfrm~sq_cCurid]
SELECT DISTINCT Subs.Curid FROM [Subs] WHERE (((Subs.Curid) Is Not Null)) 

-- [~sq_cBabss~sq_cAccno]
SELECT Accq.Ano0,Accq.Ano,Accq.Aname,Accq.Hm,Accq.Ht FROM [Accq] 

-- [~sq_cBank_rep0~sq_cBacc]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Iscur FROM [Bankacc] ORDER BY Bankacc.Acctype

-- [~sq_cBankacc~sq_cCombo59]
SELECT DISTINCT Bankacc.Branch FROM [Bankacc] WHERE (((Bankacc.Branch) Is Not Null)) ORDER BY Bankacc.Branch

-- [~sq_cBankacc~sq_cCombo60]
SELECT DISTINCT Bankacc.Branchcode FROM [Bankacc] WHERE (((Bankacc.Branchcode) Is Not Null)) ORDER BY Bankacc.Branchcode

-- [~sq_cBankacc~sq_cComboB]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cBankacc~sq_cComboC]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cBankacc~sq_cCombocur]
SELECT DISTINCT Bankacc.Curid FROM [Bankacc] WHERE (((Bankacc.Curid) Is Not Null)) ORDER BY Bankacc.Curid

-- [~sq_cBundle_m~sq_cBundle]
SELECT Bundle.* FROM [Bundle] WHERE (((Bundle.Baccno)=[combo0])) 

-- [~sq_cBundle_m~sq_cCombo0]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Bankid FROM [Bankacc] WHERE (((Bankacc.Acctype)=0) AND ((Bankacc.Active)=True)) 

-- [~sq_cCashandpetty~sq_cComboA]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cCashandpetty~sq_cComboB]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cCheck_d_dates~sq_cAcckharj]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cCheck_d_dates~sq_cCheckbacked]
SELECT DISTINCTROW  FROM [Checkbacked] WHERE ([__Checkid] = Checkid) 

-- [~sq_cCheck_d_dates~sq_cComboBacc]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_cCheck_d_dates~sq_cTafKharj]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cCheck_d_dates0~sq_cAcckharj]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cCheck_d_dates0~sq_cTafKharj]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cCheck_deliv_m~sq_cComboEmp]
SELECT Emps.Empid,Emps.Empname FROM [Emps] 

-- [~sq_cCheck_deliv_m~sq_cPorder_search]
SELECT Check.* FROM [Check] WHERE (((Check.D1) Between [dd1] And [dd2]) AND ((Check.Signed)=True) AND ((Check.Delivervag)=False) AND ((Check.Dp)=2) AND ((Check.Checktype)=0 Or (Check.Checktype)=1 Or (Check.Checktype)=2) AND ((Check.EbtorKharj)=False)) ORDER BY Check.D1

-- [~sq_cCheck_longtocash_m~sq_cCheck_s]
SELECT Check.*,-DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) FROM [Check] WHERE (((Check.Checktype)<>0 And (Check.Checktype)<>1) AND ((Check.Ctocash)=False) AND ((Check.Dp)=2) AND ((Check.Paied)=True) AND ((Check.EbtorKharj)=False)) ORDER BY Check.Maturity

-- [~sq_cCheck_p_vos_m~sq_cPorder_search]
SELECT Check.* FROM [Check] WHERE (((Check.Signed)=True) AND ((Check.D1) Between [dd1] And [dd2]) AND ((Check.RegVos)=False)) 

-- [~sq_cCheck_pay_m~sq_cCheck_s]
SELECT Check.*,Switch([Checktype]=0,"چك نقدي",[Checktype]=1,"چك انتظامي",[Checktype]=2,"چك مدت دار",[Checktype]=9,"پرداخت نقدي") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Payit)=True) AND ((Check.Paied)=False) AND ((Check.Checktype) In (0,1,2))) ORDER BY Check.Rfpdate

-- [~sq_cCheck_Print_single~sq_cBalance]
SELECT DISTINCTROW  FROM [] WHERE ([__Baccno] = Baccno) 

-- [~sq_cCopy of Porder~sq_cAccnotcash]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cCopy of Porder~sq_cBabat]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null)) ORDER BY Check.Babat

-- [~sq_cCopy of Porder~sq_cBaccno]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Acctype,Bankacc.Iscur,Bankacc.Curid,Bankacc.Accno,Bankacc.Tafno FROM [Bankacc] WHERE (((Bankacc.Acctype)=0 Or (Bankacc.Acctype)=1)) 

-- [~sq_cCopy of Porder~sq_cCombocur]
SELECT DISTINCT Check.Curid FROM [Check] WHERE (((Check.Curid) Is Not Null And (Check.Curid)<>"ريال")) 

-- [~sq_cCopy of Porder~sq_cComboinvno]
SELECT Check.Invno FROM [Check] WHERE (((Check.Invno) Is Not Null)) ORDER BY Check.Invno

-- [~sq_cCopy of Porder~sq_cDepname]
SELECT Deps.Depid,Deps.Depname FROM [Deps] ORDER BY Deps.Depname

-- [~sq_cCopy of Porder~sq_cPbyCheckno]
SELECT DISTINCT Check.PbyCheckno FROM [Check] WHERE (((Check.PbyCheckno) Is Not Null)) ORDER BY Check.PbyCheckno

-- [~sq_cCopy of Porder~sq_cTafno]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cCopy of Porder~sq_cTafnotcash]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cD_Check_Kh_m~sq_cCheck_s]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Checktype)=0 Or (Check.Checktype)=2) AND ((Check.Stcode)=11)) ORDER BY Check.Maturity

-- [~sq_cD_Check_kh~sq_cComboC]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cD_Check_Od_m~sq_cCheck_s]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Stcode)=11)) ORDER BY Check.Maturity

-- [~sq_cD_Check_resid~sq_cRfpdate]
SELECT DISTINCT Check.Rfpdate FROM [Check] WHERE (((Check.Rfpdate) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Rfpdate

-- [~sq_cD_Variz~sq_cCombocontno]
SELECT Check.Contract FROM [Check] ORDER BY Check.Contract

-- [~sq_cPayables~sq_cAccnotcash]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_fD_Check_Od]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Stcode)=11)) ORDER BY Check.Maturity

-- [~sq_fCheck_p_vos]
SELECT Check.* FROM [Check] WHERE (((Check.Signed)=True) AND ((Check.D1) Between [dd1] And [dd2]) AND ((Check.RegVos)=False)) 

-- [~sq_cCheck_Print_single~sq_cPbyCheckno]
SELECT DISTINCT Check.PbyCheckno FROM [Check] WHERE (((Check.PbyCheckno) Is Not Null)) ORDER BY Check.PbyCheckno

-- [~sq_cD_Check_Vag_m~sq_cCheck_s]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Checktype)=0 Or (Check.Checktype)=2) AND ((Check.Stcode)=11)) ORDER BY Check.Maturity

-- [~sq_cD_Check_vos_m~sq_cCheck_s]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Vagno)=[ComboBacc]) AND ((Check.Stcode)=12)) ORDER BY Check.D3

-- [~sq_cD_Check_vos_m~sq_cComboBacc]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_cD_Check~sq_cPo_vouch]
SELECT DISTINCTROW  FROM [] WHERE ([__Checkid] = Checkid) 

-- [~sq_cD_Naghd~sq_cAccno]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen) And ((Acc.Has_Sub_Subsidery)=False)) ORDER BY Acc.Accno

-- [~sq_cD_Naghd~sq_cbabat]
SELECT DISTINCT Subs.Babat FROM [Subs] WHERE (((Subs.Babat) Is Not Null)) ORDER BY Subs.Babat

-- [~sq_cD_Naghd~sq_cBabatid]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=1) AND ((Babs.GBid)<>0)) 

-- [~sq_cD_Naghd~sq_cBasetype]
SELECT DISTINCT Subs.Basetype FROM [Subs] WHERE (((Subs.Basetype) Is Not Null)) ORDER BY Subs.Basetype

-- [~sq_cD_Naghd~sq_cComboBacc]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype FROM [Bankacc] WHERE (((Bankacc.Acctype)<>0)) 

-- [~sq_cD_Naghd~sq_cCombocontno]
SELECT Check.Contract FROM [Check] ORDER BY Check.Contract

-- [~sq_cD_Naghd~sq_cComboinvno]
SELECT Check.Invno FROM [Check] ORDER BY Check.Invno

-- [~sq_cD_Naghd~sq_cComboP]
SELECT DISTINCT Subs.Person FROM [Subs] WHERE (((Subs.Person) Is Not Null)) ORDER BY Subs.Person

-- [~sq_cD_Naghd~sq_cSubsrow]
SELECT DISTINCTROW  FROM [Subsrow] WHERE ([__Sid] = Sid) 

-- [~sq_cD_Naghd~sq_cTafno]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cD_Variz~sq_cbabat]
SELECT DISTINCT Subs.Babat FROM [Subs] WHERE (((Subs.Babat) Is Not Null)) ORDER BY Subs.Babat

-- [~sq_cD_Variz~sq_cBabatid]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=1) AND ((Babs.GBid)<>0)) 

-- [~sq_cD_Variz~sq_cBasetype]
SELECT DISTINCT Subs.Basetype FROM [Subs] WHERE (((Subs.Basetype) Is Not Null) AND ((Subs.Scode)=16)) ORDER BY Subs.Basetype

-- [~sq_cD_Variz~sq_cComboBacc]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_cD_Variz~sq_cComboinvno]
SELECT Check.Invno FROM [Check] ORDER BY Check.Invno

-- [~sq_cD_Variz~sq_cComboP]
SELECT DISTINCT Subs.Person FROM [Subs] WHERE (((Subs.Person) Is Not Null)) ORDER BY Subs.Person

-- [~sq_cDate_dcheck_entez~sq_cperson]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Person

-- [~sq_cDate_dcheck_entez~sq_cVamno]
SELECT DISTINCT Check.Rfpref FROM [Check] WHERE (((Check.Rfpref) Is Not Null) AND ((Check.Dp)=2) AND ((Check.Checktype)=3)) ORDER BY Check.Rfpref

-- [~sq_cDate_notchecks~sq_cFindtextnaghd]
SELECT Findtext.* FROM [Findtext] WHERE (((Findtext.Dp)>10)) 

-- [~sq_cDate_Sarresid_alarm~sq_cBabat]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Babat

-- [~sq_cDate_Sarresid~sq_cBabat]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Babat

-- [~sq_cDate_Sarresid~sq_cperson]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Person

-- [~sq_cEmps_advance~sq_cCombo5]
SELECT EMP_AD.Personel,EMP_AD.N FROM [EMP_AD] ORDER BY EMP_AD.N

-- [~sq_cForm2~sq_cCombo41]
SELECT Bankacc.Baccid,Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch FROM [Bankacc] 

-- [~sq_cGlobals~sq_cCombo80]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals~sq_cComboB]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals~sq_cComboD]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals~sq_cComboF]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals~sq_cComboH]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals~sq_cComboi]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cCombo77]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cCombo80]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cComboB]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cComboE]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cComboF]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cComboG]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cComboi]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cHepcoBrief~sq_cMah1]
SELECT Month.Mno,Month.Mname FROM [Month] 

-- [~sq_cHepcoBrief~sq_cMah2]
SELECT Month.Mno,Month.Mname FROM [Month] 

-- [~sq_cHepcoForm~sq_cperson]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Person

-- [~sq_cInvsamount~sq_cInvid]
SELECT Invsforcash.invid,Invsforcash.cusid,Invsforcash.invno,Invsforcash.invdate,Invsforcash.tafno,Invsforcash.cusname FROM [Invsforcash] WHERE (((Invsforcash.tafno)=Forms!D_check!Tafnotcash)) ORDER BY Invsforcash.invno

-- [~sq_cMainrep_m~sq_cSACC_SUB]
SELECT Mainrep.* FROM [Mainrep] WHERE (((Mainrep.Scode) Not In (1,6))) ORDER BY Mainrep.Sdate

-- [~sq_cNajorepfrm~sq_cBabat]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Babat

-- [~sq_cNajorepfrm~sq_cperson]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Person

-- [~sq_cp_Naghd~sq_cAccno]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen) And ((Acc.Has_Sub_Subsidery)=False)) ORDER BY Acc.Accno

-- [~sq_cp_Naghd~sq_cBabatid]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=2) AND ((Babs.GBid)<>0)) 

-- [~sq_cp_Naghd~sq_cBasetype]
SELECT DISTINCT Subs.Basetype FROM [Subs] WHERE (((Subs.Basetype) Is Not Null)) ORDER BY Subs.Basetype

-- [~sq_cp_Naghd~sq_cCombocontno]
SELECT Check.Contract FROM [Check] ORDER BY Check.Contract

-- [~sq_cp_Naghd~sq_cComboP]
SELECT DISTINCT Subs.Person FROM [Subs] WHERE (((Subs.Person) Is Not Null)) ORDER BY Subs.Person

-- [~sq_cp_Naghd~sq_cPo_vouch]
SELECT DISTINCTROW  FROM [] WHERE ([__Sid] = Sid) 

-- [~sq_cRights_m~sq_cEmpid]
SELECT Emps.Empid,Emps.Empname FROM [Emps] 

-- [~sq_cRights_m~sq_cRights]
SELECT DISTINCTROW  FROM [] WHERE ([__Empid] = Empid) 

-- [~sq_cSal~sq_cCoyear]
SELECT  FROM  

-- [~sq_cSal1~sq_cCoyear]
SELECT Coyearinfo.Rowid,Coyearinfo.Sal,Coyearinfo.Accdb,Coyearinfo.Cashdb,Coyearinfo.Invdb,Coyearinfo.Salesdb,Coyearinfo.Dsndb,Coyearinfo.Convertdb,Coyearinfo.Notchangeddb,Coyearinfo.Coname,Coyearinfo.Displaysal,Trim([Coname]) & "     " & [Displaysal] FROM [Coyearinfo] ORDER BY Coyearinfo.Sal

-- [~sq_cShart_m~sq_cSHART]
SELECT SHART.* FROM [SHART] WHERE (((SHART.Ittype)=[text0])) 

-- [~sq_cShart_m~sq_cSORTS]
SELECT SORTS.* FROM [SORTS] WHERE (((SORTS.Ittype)=[text0])) 

-- [~sq_cSm0~sq_cSACC_SUB]
SELECT Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cV_search_m~sq_cPorder_search]
SELECT Voucher.* FROM [Voucher] WHERE (((Voucher.Vdate) Between [D1] And [D2])) ORDER BY Voucher.Vno

-- [~sq_cV_search_row_m~sq_cPorder_search]
SELECT Voucher.*,Voucher_row.* FROM [Voucher],[Voucher_row] WHERE (((Voucher.Vdate) Between [D1] And [D2])) ORDER BY Voucher.Vdate

-- [~sq_cVoucher0~sq_cVoucher_row]
SELECT DISTINCTROW  FROM [] WHERE ([__Vid] = Vid) 

-- [~sq_dADVANCE_REP~sq_dChild13]
SELECT DISTINCTROW  FROM [] WHERE ([__ADVANCEID] = ADVANCEID) 

-- [~sq_dB_D_printnaghd~sq_dChild387]
SELECT DISTINCTROW  FROM [Subsrow] WHERE ([__Sid] = Sid) 

-- [~sq_dB_D_printnaghd~sq_dChild416]
SELECT DISTINCTROW  FROM [Subsrow] WHERE ([__Sid] = Sid) 

-- [~sq_dB_P_print_Safteh~sq_dRep_voucher_row_bp]
SELECT DISTINCTROW  FROM [] WHERE ([__Checkid] = Checkid) 

-- [~sq_dB_P_print~sq_dRep_voucher_row_bp]
SELECT DISTINCTROW  FROM [] WHERE ([__Checkid] = Checkid) 

-- [~sq_dCard~sq_dChildFb]
SELECT IIf(IsNull(Sum([Bed])-Sum([Bes])),0,Sum([Bed])-Sum([Bes])) FROM [Subs] WHERE (((Subs.Baccno)=Forms!Date_card!ComboBacc) And ((Subs.Sdate)<Forms!Date_card!D1)) 

-- [~sq_dCardcur~sq_dChildFb]
SELECT IIf(IsNull(Sum([Bed])-Sum([Bes])),0,Sum([Bed])-Sum([Bes])) FROM [Subs] WHERE (((Subs.Baccno)=Forms!Date_card!ComboBacc) And ((Subs.Sdate)<Forms!Date_card!D1)) 

-- [~sq_dCheck_p_longsrep~sq_dCombobaccno]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Accno,Bankacc.Tafno FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_dCheck_p_longsrepalarm~sq_dCombobaccno]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Accno,Bankacc.Tafno FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_dCopy of Resid_check~sq_dRep_voucher_row_bp]
SELECT Voucher_row_tmp.*,t_accounts.Account_Name,t_unique_accounts.Account_Name FROM [Voucher_row_tmp],[t_accounts],[t_unique_accounts] 

-- [~sq_dD_Check_Audit~sq_dCombo97]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Babat

-- [~sq_dD_Check~sq_dFindtext]
SELECT DISTINCTROW  FROM [Findtext] 

-- [~sq_dD_search_print~sq_dShart_print]
SELECT SHART.* FROM [SHART] WHERE (((SHART.Ittype)=Forms!Shart_m!SHART.Form!Text0)) 

-- [~sq_dDailyrep_notcheck~sq_dFindtextnaghd]
SELECT Findtext.* FROM [Findtext] WHERE (((Findtext.Dp)>10)) 

-- [~sq_dDailyrepbanks_sub_sub~sq_dComboBacc]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Check.Vagno FROM [Check],[Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_dHepcomainnag~sq_dMah1]
SELECT Month.Mno,Month.Mname FROM [Month] 

-- [~sq_dHepcomainnag~sq_dMah2]
SELECT Month.Mno,Month.Mname FROM [Month] 

-- [~sq_dJaam_Residacc~sq_dResidcheckallsub]
SELECT Check.* FROM [Check] WHERE (((Check.Person)=Forms!D_Check!Person) And ((Check.Dp)=1) And ((Check.Rfpdate)=Forms!D_Check!Rfpdate)) 

-- [~sq_dJaam_Residsales~sq_dChild23]
SELECT Check.* FROM [Check] WHERE (((Check.Person)=Forms!D_Check!Person) And ((Check.Dp)=1) And ((Check.Rfpdate)=Forms!D_Check!Rfpdate)) 

-- [~sq_dP_search_print~sq_dShart_print]
SELECT SHART.* FROM [SHART] WHERE (((SHART.Ittype)=Forms!Shart_m!SHART.Form!Text0)) 

-- [~sq_dP_search_print~sq_dSorts_print]
SELECT SORTS.* FROM [SORTS] WHERE (((SORTS.Ittype)=forms!sal!ttype)) 

-- [~sq_dPorder_search_print~sq_dSorts_print]
SELECT SORTS.* FROM [SORTS] WHERE (((SORTS.Ittype)=forms!sal!ttype)) 

-- [~sq_dPorder~sq_dAccno]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen) And ((Acc.Has_Sub_Subsidery)=False)) ORDER BY Acc.Accno

-- [~sq_dPorder~sq_dBabat]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null)) ORDER BY Check.Babat

-- [~sq_dPorder~sq_dBabg]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=2) And ((Babs.GBid)<>0)) 

-- [~sq_dPorder~sq_dCombocontno]
SELECT Check.Contract FROM [Check] WHERE (((Check.Contract) Is Not Null)) ORDER BY Check.Contract

-- [~sq_dPorder~sq_dComboinvno]
SELECT Check.Invno FROM [Check] WHERE (((Check.Invno) Is Not Null)) ORDER BY Check.Invno

-- [~sq_dPorder~sq_dComboP]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null)) ORDER BY Check.Person

-- [~sq_dPorder~sq_dPbyCheckno]
SELECT DISTINCT Check.PbyCheckno FROM [Check] WHERE (((Check.PbyCheckno) Is Not Null)) ORDER BY Check.PbyCheckno

-- [~sq_dPorder~sq_dTafno]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_dPorder~sq_dTafnotcash]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_dRep_Voucher~sq_dVOUCHER_ROW]
SELECT DISTINCTROW  FROM [] WHERE ([__Vid] = Vid) 

-- [~sq_cp_Naghd~sq_cTafno]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cPayables~sq_cAccno]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen) And ((Acc.Has_Sub_Subsidery)=False)) ORDER BY Acc.Accno

-- [~sq_cPayables~sq_cBabatid]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=2) AND ((Babs.GBid)<>0)) 

-- [~sq_cPayables~sq_cCombo31]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Accno,Bankacc.Tafno FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_cPayables~sq_cCombocur]
SELECT DISTINCT Check.Curid FROM [Check] WHERE (((Check.Curid) Is Not Null And (Check.Curid)<>"ريال")) 

-- [~sq_cPayables~sq_cTafno]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cPayables~sq_cTafnotcash]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cPorder_Search_m~sq_cPorder_search]
SELECT Check.*,Switch([Checktype]=0,"چك نقدي",[Checktype]=1,"چك انتظامي",[Checktype]=2,"چك مدت دار"),IIf([Payit]=True,1,0),IIf([paied]=True,1,0) FROM [Check] WHERE (((IIf([paied]=True,1,0))=1) And ((Check.Dp)=2) And ((Check.Sal)=Forms!sal!Tsal) And ((Check.Empid)=Forms!sal!tempid)) 

-- [~sq_cPorder~sq_cAccnotcash]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cPorder~sq_cBabat]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null)) ORDER BY Check.Babat

-- [~sq_cPorder~sq_cBaccno]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Acctype,Bankacc.Iscur,Bankacc.Curid,Bankacc.Accno,Bankacc.Tafno FROM [Bankacc] WHERE (((Bankacc.Acctype)=0 Or (Bankacc.Acctype)=1)) 

-- [~sq_cPorder~sq_cCombocur]
SELECT DISTINCT Check.Curid FROM [Check] WHERE (((Check.Curid) Is Not Null And (Check.Curid)<>"ريال")) 

-- [~sq_cPorder~sq_cComboinvno]
SELECT Check.Invno FROM [Check] WHERE (((Check.Invno) Is Not Null)) ORDER BY Check.Invno

-- [~sq_cPorder~sq_cDepname]
SELECT Deps.Depname,Deps.Depid FROM [Deps] ORDER BY Deps.Depname

-- [~sq_cPorder~sq_cPo_vouch]
SELECT DISTINCTROW  FROM [] WHERE ([__Checkid] = Checkid) 

-- [~sq_cPorder~sq_cTafno]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cPorder~sq_cTransaccid]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Tafno,Bankacc.Accno FROM [Bankacc] 

-- [~sq_cPordersub_vouch~sq_cBabat]
SELECT Povouch.rowdesc FROM [Povouch] ORDER BY Povouch.rowdesc

-- [~sq_cPordersub_vouch~sq_cComboA]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname FROM [Acc] ORDER BY Acc.Accno

-- [~sq_cCheck_Print_single~sq_cTafnotcash]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_dRfp_print~sq_dRep_voucher_row_bp]
SELECT DISTINCTROW  FROM [] WHERE ([__Checkid] = Checkid) 

-- [~sq_f21]
SELECT DISTINCTROW  FROM [Globals] 

-- [~sq_fAdvance]
SELECT DISTINCTROW  FROM [Advance] 

-- [~sq_fBabss]
SELECT Babs.* FROM [Babs] WHERE (((Babs.GBid)<>0) AND ((Babs.Dp)=2)) ORDER BY Babs.Ord

-- [~sq_fBankacc]
SELECT Bankacc.* FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) ORDER BY Bankacc.Baccno

-- [~sq_fCh_dups]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1)) 

-- [~sq_fCheck_d_dates]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1)) 

-- [~sq_fCheck_deliv_m]
SELECT Mains.* FROM [Mains] WHERE (((Mains.Sal)=Forms!sal!Tsal) And ((Mains.Empid)=Forms!sal!Tempid) And ((Mains.Gcode)=22)) 

-- [~sq_fCheck_longtocash_m]
SELECT Mains.* FROM [Mains] WHERE (((Mains.Sal)=Forms!sal!Tsal) And ((Mains.Empid)=Forms!sal!Tempid) And ((Mains.Gcode)=25)) 

-- [~sq_fCheck_p_vos_m]
SELECT Mains.* FROM [Mains] WHERE (((Mains.Sal)=Forms!sal!Tsal) And ((Mains.Empid)=Forms!sal!Tempid) And ((Mains.Gcode)=23)) 

-- [~sq_fD_Check_kh]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Checktype)=0 Or (Check.Checktype)=2) AND ((Check.Stcode)=11)) ORDER BY Check.Maturity

-- [~sq_fD_Check_Od_m]
SELECT Mains.* FROM [Mains] WHERE (((Mains.Sal)=Forms!sal!Tsal) And ((Mains.Empid)=Forms!sal!Tempid) And ((Mains.Gcode)=18)) 

-- [~sq_fD_Check_Vag_m]
SELECT Mains.* FROM [Mains] WHERE (((Mains.Gcode)=12)) 

-- [~sq_fD_Check_vos_m]
SELECT Mains.* FROM [Mains] WHERE (((Mains.Gcode)=13)) 

-- [~sq_fD_Naghd]
SELECT Subs.* FROM [Subs] WHERE (((Subs.Scode)=17)) ORDER BY Subs.Sdate

-- [~sq_fDate_Sarresid_alarm]
SELECT Globals.befpayablec,Globals.befrecievables FROM [Globals] 

-- [~sq_fEbtit]
SELECT DISTINCTROW  FROM [Check] 

-- [~sq_fEmps_advance]
SELECT EMPS_ADVANCE.EMPNAME,EMPS_ADVANCE.EMPNO,EMPS_ADVANCE.AMOUNT FROM [EMPS_ADVANCE] ORDER BY EMPS_ADVANCE.EMPNAME

-- [~sq_fGlobals]
SELECT DISTINCTROW  FROM [Globals] 

-- [~sq_fMain]
SELECT DISTINCTROW  FROM [Globals] 

-- [~sq_fNotes]
SELECT DISTINCTROW  FROM [Notes] 

-- [~sq_cCheck_Print_single~sq_cAccnotcash]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_fPayables]
SELECT Check.* FROM [Check] WHERE (((Check.Checktype) In (3,4,5,6)) AND ((Check.Dp)=2)) ORDER BY Check.Rfpdate

-- [~sq_fPorder]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) And ((Check.Checktype)=0 Or (Check.Checktype)=1 Or (Check.Checktype)=2 Or (Check.Checktype)=9)) ORDER BY Check.Rfpdate

-- [~sq_fPorder_search]
SELECT Check.*,Switch([Checktype]=0,"چك نقدي",[Checktype]=1,"چك انتظامي",[Checktype]=2,"چك مدت دار"),IIf([Payit]=True,1,0),IIf([paied]=True,1,0) FROM [Check] WHERE (((IIf([paied]=True,1,0))=1) And ((Check.Dp)=2) And ((Check.Sal)=Forms!sal!Tsal) And ((Check.Empid)=Forms!sal!tempid)) 

-- [~sq_fPordersub_vouch]
SELECT DISTINCTROW  FROM [Povouch] 

-- [~sq_fRights_m]
SELECT DISTINCTROW  FROM [Rights_m] 

-- [~sq_fV_errors]
SELECT Voucher_errors.* FROM [Voucher_errors] 

-- [~sq_fV_notok]
SELECT Voucher_notok.* FROM [Voucher_notok] ORDER BY Voucher_notok.Notdate

-- [~sq_fVoucher_create]
SELECT DISTINCTROW  FROM [Globals] 

-- [~sq_fVoucher_send]
SELECT DISTINCTROW  FROM [Globals] 

-- [~sq_rB_D_printvariz]
SELECT Subs.* FROM [Subs] 

-- [~sq_rB_P_print]
SELECT Check.*,Bankacc.Bank,Bankacc.Branch FROM [Check],[Bankacc] WHERE (((Check.Dp)=2)) 

-- [~sq_rB_P_print1]
SELECT Check.*,Bankacc.Bank,Bankacc.Branch FROM [Check],[Bankacc] WHERE (((Check.Dp)=2)) 

-- [~sq_rB_P_printnaghd]
SELECT Subs.* FROM [Subs] 

-- [~sq_rBabrepnew1]
SELECT Val(Mid([Scode],1,1)),Subs.Curid,IIf([Scode]=10,0,1),Subs.Babatid,Babs_1.Bdesc,Babs.Bdesc,Sum(Subs.Bed),Sum(Subs.Bes),Sum([Currate]*[Bed]),Sum([Currate]*[Bes]),IIf(Val(Mid([Scode],1,1))=1,"دريافتها","پرداختها"),Babs.Ord FROM [Subs],[Babs],[Babs] WHERE (((Subs.Sdate) Between Forms!Babsrepfrm!DD1 And Forms!Babsrepfrm!DD2) And ((Subs.Ebt)=0)) ORDER BY Subs.Curid

-- [~sq_rBabrepnew1month]
SELECT Val(Mid([Scode],1,1)),Subs.Curid,IIf([Scode]=10,0,1),Subs.Babatid,Babs_1.Bdesc,Babs.Bdesc,Nz(Sum([Currate]*IIf(Mid([Scode],1,1)=1,[Bed],[Bes])),0)/1000,IIf(Val(Mid([Scode],1,1))=1,"دريافتها","پرداختها"),Babs.Ord,nz(Sum(IIf(Mid([Sdate],5,2)='01',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,Nz(Sum(IIf(Mid([Sdate],5,2)='02',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,Nz(Sum(IIf(Mid([Sdate],5,2)='03',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,nz(Sum(IIf(Mid([Sdate],5,2)='04',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,nz(Sum(IIf(Mid([Sdate],5,2)='05',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,nz(Sum(IIf(Mid([Sdate],5,2)='06',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,nz(Sum(IIf(Mid([Sdate],5,2)='07',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,nz(Sum(IIf(Mid([Sdate],5,2)='08',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,nz(Sum(IIf(Mid([Sdate],5,2)='09',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,Nz(Sum(IIf(Mid([Sdate],5,2)='10',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,Nz(Sum(IIf(Mid([Sdate],5,2)='11',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000,nz(Sum(IIf(Mid([Sdate],5,2)='12',IIf(Mid([Scode],1,1)=1,[Bed],[Bes]))),0)/1000 FROM [Subs],[Babs],[Babs] WHERE (((Subs.Sdate) Between Forms!Babsrepfrm!DD1 And Forms!Babsrepfrm!DD2) And ((Subs.Ebt)=0)) ORDER BY Subs.Curid

-- [~sq_rBankrep]
SELECT Bankrep.*,Bankacc.Iscur,Bankacc.Curid FROM [Bankrep],[Bankacc] WHERE (((Bankacc.Active)=True)) 

-- [~sq_rCard]
SELECT Subs.* FROM [Subs] WHERE (((Subs.Baccno)=Forms!Date_card!ComboBacc) And ((Subs.Sdate) Between Forms!Date_card!D1 And Forms!Date_card!D2)) 

-- [~sq_rCard_subcur]
SELECT IIf(IsNull(Sum([Bed])-Sum([Bes])),0,Sum([Bed])-Sum([Bes])) FROM [Subs] WHERE (((Subs.Baccno)=Forms!Date_card!ComboBacc) And ((Subs.Sdate)<Forms!Date_card!D1)) 

-- [~sq_rCH_KAR50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Kesh25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Kesh50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Maskan10]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Maskan50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_MELLAT50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_MELLAT50_newest]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_MELLI25_new]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_MELLI50_new]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Parsian100]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Parsian50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Pasargaad10]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_REFAH100]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_REFAH50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Saman25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_SEPAH100]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_SEPAH50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_SINA25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_TEJARAT25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_TEJARAT50new]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCheck_Bundle]
SELECT Check.*,Switch([Checktype]=0,"چك نقدي",[Checktype]=1,"چك انتظامي",[Checktype]=2,"چك مدت دار",[Checktype]=3,"اقساط پرداختني",[Checktype]=4,"سفته پرداختني",[Checktype]=5,"ساير اقلام پرداختني",[Checktype]=6,"سفته هاي انتظامي") FROM [Check] WHERE (((Check.Dp)=2) And ((Check.bundleid)=forms!filter_bundle!combo02)) 

-- [~sq_rCheck_p_longsrepalarm]
SELECT Check.*,-DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) FROM [Check] WHERE (((Check.Maturity)<="13" & AddNumberAndConvertToNewDate(Currentdatefarsi(Date()),DLookUp("befpayablec","Globals"),True)) AND ((Check.Checktype)<>0 And (Check.Checktype)<>1) AND ((Check.Ctocash)=False) AND ((Check.Dp)=2) AND ((Check.Paied)=True) AND ((Check.EbtorKharj)=False)) ORDER BY Check.Maturity

-- [~sq_rCheck_Print]
SELECT Check.* FROM [Check] WHERE (((Check.D1) Between [dd1] And [dd2]) AND ((Check.Paied)=True) AND ((Check.Signed)=True) AND ((Check.Checktype) In (0,1,2)) AND ((Check.Dp)=2)) ORDER BY Check.D1

-- [~sq_rD_Check_Jaam]
SELECT Check.*,Trim([Checktypen]) FROM [Check] WHERE (((Check.Maturity) Between Forms!DATE_DCHECK!D1 And Forms!DATE_DCHECK!D2) And ((Check.Dp)=1) And ((Check.Checktype)=0 Or (Check.Checktype)=2)) ORDER BY Check.Maturity

-- [~sq_rDailyrep0]
SELECT Dailyrep.*,IIf([Mtype]=0,"عمليات موثر بر موجودي روز ","عمليات غير موثر بر موجودي روز") FROM [Dailyrep] 

-- [~sq_rDailyrepbanks]
SELECT Subs.Baccno,Bankacc.Accno,Bankacc.Tafno,Sum(IIf([SDATE]<Forms!Deaily_rep!Text0,[bed],0))-Sum(IIf([SDATE]<Forms!Deaily_rep!Text0,[bes],0)),Sum(IIf([SDATE]=Forms!Deaily_rep!Text0,[bed],0)),Sum(IIf([SDATE]=Forms!Deaily_rep!Text0,[bes],0)),Bankacc.Bank,Bankacc.Branch,Bankacc.Acctype,Bankacc.Curid FROM [Subs],[Bankacc] WHERE (((Subs.Sal)=Forms!sal!Tsal) And ((Bankacc.Active)=True)) 

-- [~sq_rDailyrepbanks_sub_sub_p]
SELECT Check.Maturity,Check.Checkno,Check.Person,Check.Curid,Check.Bankid,Check.Branchname,Check.Babat,IIf([Maturity]<=forms!deaily_rep!text0,[Amount],0),IIf([Maturity] Between forms!deaily_rep!Dw0 And forms!deaily_rep!Dw,[Amount],0),IIf([Maturity] Between forms!deaily_rep!Dm0 And forms!deaily_rep!Dm,[Amount],0),[D]+[W]+[M],Check.Baccno,Check.Checktype,Switch([Checktype]=0,"  چك نقدي ",[Checktype]=1,"چك انتظامي",[Checktype]=2,"چك مدت دار",[Checktype]=3,"اقساط پرداختني",[Checktype]=4,"سفته پرداختني",[Checktype]=5,"ساير اقلام پرداختني",[Checktype]=6,"سفته هاي انتظامي") FROM [Check] WHERE (((Check.Checktype) Between 2 And 5) And ((IIf([Maturity]<=forms!deaily_rep!text0,[Amount],0))<>0) And ((Check.Dp)=2) And ((Check.Signed)=True) And ((Check.Ctocash)=False)) Or (((Check.Dp)=2) And ((IIf([Maturity] Between forms!deaily_rep!Dw0 And forms!deaily_rep!Dw,[Amount],0))<>0)) Or (((Check.Dp)=2) And ((IIf([Maturity] Between forms!deaily_rep!Dm0 And forms!deaily_rep!Dm,[Amount],0))<>0)) ORDER BY Check.Maturity

-- [~sq_rlongs_indetail]
SELECT Check.*,IIf([Paied]=True,'چك صادره','درخواست پرداخت') FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype)=2) AND ((Check.Ctocash)=0) AND ((Check.EbtorKharj)=0)) 

-- [~sq_rlongs_indetailmonth]
SELECT IIf([Paied]=True,'چك صادره','درخواست پرداخت'),Nz(Mid([Maturity],1,4),1384),Sum(IIf(Mid([Maturity],5,2)='01',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='02',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='03',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='04',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='05',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='06',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='07',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='08',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='09',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='10',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='11',[Amount],0)),Sum(IIf(Mid([Maturity],5,2)='12',[Amount],0)),IIf([Check.Checktype]=2,IIf([Paied]=True,'چك صادره','درخواست پرداخت'),Trim([Check.Checktypen])) FROM [Check],[Checktype] WHERE (((Check.Dp)=2) AND ((Check.Checktype) In (2,3)) AND ((Check.Ctocash)=0) AND ((Check.EbtorKharj)=0)) 

-- [~sq_rPorder_search_print]
SELECT Check.*,Switch([Checktype]=0,"چك نقدي",[Checktype]=1,"چك انتظامي",[Checktype]=2,"چك مدت دار") FROM [Check] WHERE (((Check.Dp)=2) And ((Check.Sal)=Forms!sal!Tsal) And ((Check.Empid)=Forms!sal!tempid)) 

-- [~sq_rRep_Voucher]
SELECT VOUCHER.* FROM [VOUCHER] 

-- [~sq_rResid_Check]
SELECT Check.*,Check.Tafnotcash FROM [Check] WHERE (((Check.Dp)=1) And ((Check.Rfpdate)=Forms!D_Check!Rfpdate) And ((Check.Tafnotcash)=Forms!D_Check!Tafnotcash)) 

-- [~sq_rRfp_print]
SELECT Check.*,Bankacc.Bank,Bankacc.Branch,t_accounts.Account_Code,t_accounts.Account_Name,t_unique_accounts.Account_Code,t_unique_accounts.Account_Name,t_accounts_1.Account_Code,t_accounts_1.Account_Name,t_unique_accounts_1.Account_Code,t_unique_accounts_1.Account_Name,IIf(IsNull([Amount]),"",NUMFARSI([Amount])) & "****/",Format([Amount],"#,##0") & "/****",IIf(IsNull([Person]),"",[Person]) & "****/",IIf(IsNull([Babat]),"",[Babat]) & "****/" FROM [t_unique_accounts],[Check],[Bankacc],[t_accounts],[t_unique_accounts],[t_accounts] WHERE (((Check.Dp)=2)) 

-- [~sq_rSubsrowrep]
SELECT DISTINCTROW  FROM [Subsrow] 

-- [~sq_rTaaodrep]
SELECT DISTINCTROW  FROM [Taaod] 

-- [~sq_rTaaodrepalltamin]
SELECT Taaod.* FROM [Taaod] 

-- [~sq_rTaaodrepcompact]
SELECT DISTINCTROW  FROM [Taaod] 

-- [~sq_rTaaodrepcompactonerow]
SELECT DISTINCTROW  FROM [Taaodonerow] 

-- [~sq_rVams]
SELECT Check.*,IIf([Ctocash]=True,'پرداخت شده','پرداخت نشده') FROM [Check] WHERE (((Check.Dp)=2) And ((Check.Checktype)=3) And ((Check.Paied)=True) And ((Check.Rfpref)=Forms!Date_dcheck_entez!Vamno)) 

-- [aaaa]
SELECT Voucher_row.accno,t_accounts.Account_Name,Sum(Voucher_row.bed),Sum(Voucher_row.bes) FROM [Voucher_row],[t_accounts] 

-- [Acc]
SELECT t_accounts.Account_Code,t_accounts.Account_Code,t_accounts.Account_Name,t_accounts.Has_Unique_Account,t_accounts.Has_Sub_Subsidery,t_accounts.Has_Value_Field,t_accounts.Has_Date_Field,t_accounts.Has_Exchange_Transaction FROM [t_accounts] ORDER BY t_accounts.Account_Code

-- [Acctoacc]
SELECT [Amount]*Check.Currate FROM [Check],[Bankacc],[Bankacc],[Subs] WHERE (((Check.D5)>='13840101') And ((Check.Transaccid) Is Not Null) And ((Check.Dp)=2) And ((Check.Calcit)=True) And ((Bankacc_1.Curid)<>Bankacc.Curid) And ((Subs.Bes)=0)) 

-- [Babatrepbase]
SELECT Sum(Subs.Bed),Sum(Subs.Bes),Sum([Currate]*[Bed]),Sum([Currate]*[Bes]),[Rbed]-[Rbes] FROM [Subs],[Babs],[Babs] WHERE (((Subs.Sdate)<Forms!Babsrepfrm!DD1) And ((Subs.Ebt)=0) And ((Subs.Iscur)=0)) 

-- [Bankbalance]
SELECT Subs.Baccno,Sum(Subs.Bed),Sum(Subs.Bes),Sum([Bed])-Sum([Bes]) FROM [Subs] 

-- [Brep_inpayday]
SELECT Bankrep_t.am FROM [Bankrep],[Bankrep_t] 

-- [Brep_inpaynext]
SELECT Bankrep_t.am FROM [Bankrep],[Bankrep_t] 

-- [Brep_inpaypast]
SELECT Bankrep_t.am FROM [Bankrep],[Bankrep_t] 

-- [brep1]
SELECT Subs.Baccno,Sum(IIf([SDATE]<=Forms!Bank_rep!Text0,[bed],0))-Sum(IIf([SDATE]<=Forms!Bank_rep!Text0,[bes],0)),Bankacc.Bank,Bankacc.Branch FROM [Subs],[Bankacc] WHERE (((Subs.Sal)=Forms!sal!Tsal)) 

-- [brepinpayday]
SELECT ‍‍Check.Baccno,Sum(‍‍Check.Amount),1 FROM [‍‍Check] WHERE (((‍‍Check.Maturity)=Forms!Bank_rep!Text0) And ((‍‍Check.Dp)=2) And ((‍‍Check.Checktype)<>1 And (‍‍Check.Checktype)<>6) And ((‍‍Check.Calcit)=False)) ORDER BY ‍‍Check.Baccno

-- [brepinpaynext]
SELECT ‍‍Check.Baccno,Sum(‍‍Check.Amount),3 FROM [‍‍Check] WHERE (((‍‍Check.Maturity) Between Forms!Bank_rep!D0 And Forms!Bank_rep!D1) And ((‍‍Check.Dp)=2) And ((‍‍Check.Checktype)<>1 And (‍‍Check.Checktype)<>6) And ((‍‍Check.Calcit)=False)) ORDER BY ‍‍Check.Baccno

-- [brepjvoaoolm]
SELECT Sum(‍‍Check.Amount),‍‍Check.Far,IIf([far]=True,"چكهاي نزد صندوق - شهرستان","چكهاي نزد صندوق - تهران") FROM [‍‍Check] WHERE (((‍‍Check.Dp)=1) AND ((‍‍Check.Checktype)<>1 And (‍‍Check.Checktype)<>6) AND ((‍‍Check.Stcode)=11)) 

-- [~sq_rRep_voucher_row]
SELECT Voucher_row.*,t_accounts.Account_Name,t_unique_accounts.Account_Name FROM [Voucher_row],[t_accounts],[t_unique_accounts] 

-- [Copy of Query44]
SELECT Check1.D4 FROM [Check1],[Check] WHERE (((Check1.Stcode)=14)) 

-- [Copy of Vcreate]
SELECT Voucher.Vid,Voucher.Vdate,3,0,0,0,Int(Sum([bed])),'ارسال سند از سيستم خزانه داري' FROM [Voucher],[Voucher_row] 

-- [eli1]
SELECT Voucher_row.Vid,Voucher_row.accno,Voucher_row.tafno,Voucher_row.bed,Voucher_row.bes,CInt([Type_Code]) FROM [Voucher_row],[t_unique_accounts],[Voucher] WHERE (((Voucher.Vdate)>=Forms!Voucher_send!LastCreatec)) 

-- [eli3]
SELECT eli1.Vid,eli1.accno,eli1.tafno,eli1.bed,eli1.bes,'نوع حساب تفصيل نادرست است' FROM [eli1],[t_RelatedSubsideryToUniques] WHERE (((t_RelatedSubsideryToUniques.Account_Code) Is Null)) 

-- [Emp_ad]
SELECT DISTINCT dbo_Emploeey.Personel,Trim([NAME]) & "      " & Trim([FAMILY]) FROM [dbo_Emploeey] ORDER BY dbo_Emploeey.Personel

-- [Hepcomain]
SELECT  FROM  

-- [Hepcomainrep]
SELECT Hepcomain.Hepcost,Round(Sum(IIf([Hepcost]<13,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M1,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M2,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M3,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M4,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M5,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M6,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M7,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M8,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M9,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M10,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M11,[Amount],0))/1000000),Round(Sum(IIf([itmat]=forms!Hepcobrief!M12,[Amount],0))/1000000),Round(Sum(IIf([itmat]>forms!Hepcobrief!M12,[Amount],0))/1000000),Hepconames.Itname FROM [Hepcomain],[Hepconames] ORDER BY Hepcomain.Hepcost

-- [Mahsarresid]
SELECT IIf(Val(Mid(nz([maturity]),5,2))=1,Sum([amount]),0),Check.Person,Sum(Check.Amount) FROM [Check] WHERE (((Check.Dp)=1)) 

-- [Query1]
SELECT Check.*,Bankacc.Bank FROM [Check],[Bankacc] WHERE (((Check.Dp)=2) AND ((Check.Paied)=True)) 

-- [Query2]
SELECT t_voucher_row.Voucher_No,t_voucher_row.Record_No,Count(t_voucher_row.Voucher_No),t_voucher.Source_Type FROM [t_voucher],[t_voucher_row] 

-- [Query3]
SELECT 1 FROM [t_voucher_row],[t_voucher] WHERE (((t_voucher_row.Value_Field) Is Null) AND ((t_voucher.Source_Type)=3)) 

-- [Query4]
SELECT "1" FROM [Voucher_row],[t_accounts] WHERE (((Voucher_row.ValueField) Is Null)) 

-- [Query6]
SELECT Sum([bed])-Sum([bes]) FROM [Subs] WHERE (((Subs.Sdate) Between '13910101' And '13910200') AND ((Subs.Curid)='0' Or (Subs.Curid)='ریال')) 

-- [Query7]
SELECT t_voucher_row.* FROM [t_voucher],[t_voucher_row] WHERE (((t_voucher.Source_Type)=3)) 

-- [SubAuto]
SELECT Subs.Sid,Subs.Gid,Subs.Scode,Subs.Checkid,Subs.Sdate,Subs.Stime,Subs.Empid,Subs.Basetype,Subs.Transtype,Subs.Baccno,Subs.Bed,Subs.Bes,Subs.Iscur,Subs.Curid,Subs.Currate,Subs.Sal,Subs.Babat,Subs.Invno,Subs.Contract,Subs.Checkno,Subs.Person,Subs.Ourno,Subs.Card,Subs.Tafno,Subs.Accno,Subs.Tcode,Subs.Vno FROM [Subs] ORDER BY Subs.Sid

-- [taamin]
SELECT Check.Dp,Check.Person,Check.Checkno,Check.Maturity,Check.Checktypen,Bank.Bankname,Check.Stcode,Check.Status,Sum(Mid([Maturity],1,4)),Sum(IIf(Mid([Maturity],5,2)='01',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='02',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='03',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='04',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='05',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='06',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='07',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='08',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='09',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='10',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='11',[amount],0)),Sum(IIf(Mid([Maturity],5,2)='12',[amount],0)) FROM [Check],[Bank] 

-- [Taf]
SELECT t_unique_accounts.Account_Code,t_unique_accounts.Account_Code,t_unique_accounts.Account_Name FROM [t_unique_accounts] ORDER BY t_unique_accounts.Account_Code

-- [Tvraccess]
SELECT Sum(Voucher_row.bed),Sum(Voucher_row.bes) FROM [Voucher_row] 

-- [Tvrmain]
SELECT Sum(Voucher_row0.bed),Sum(Voucher_row0.bes) FROM [Voucher_row0] 

-- [Vcreate]
SELECT Voucher.Vid,Voucher.Vdate,3,0,0,0,Int(Sum([bed])),'ارسال سند از سيستم خزانه داري',Voucher.Vgroup FROM [Voucher],[Voucher_row] 

-- [~sq_fFindtext]
SELECT Findtext.* FROM [Findtext] WHERE (((Findtext.Dp)=1)) 

-- [~sq_cCheck_Print_single~sq_cTafno]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_c\~sq_cCheck_s]
SELECT Check.*,Switch([Checktype]=0,"چك نقدي",[Checktype]=1,"چك انتظامي",[Checktype]=2,"چك مدت دار",[Checktype]=9,"پرداخت نقدي") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Payit)=True) AND ((Check.Paied)=False) AND ((Check.Checktype) In (0,1,2))) ORDER BY Check.Rfpdate

-- [~sq_c~tmp_UpsizingObject~sq_cChild13]
SELECT DISTINCTROW  FROM [] WHERE ([__ADVANCEID] = ADVANCEID) 

-- [~sq_c~tmp_UpsizingObject~sq_cGBid]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=[Frame2]) AND ((Babs.GBid)=0)) 

-- [~sq_cAuditform~sq_cBabat]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Babat

-- [~sq_cBabsrepfrm~sq_cBabs]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc,IIf([Babs.Dp]=1,"دریافت","پرداخت"),Babs_1.Bdesc FROM [Babs],[Babs] WHERE (((Babs.GBid)>0)) ORDER BY Babs.Dp

-- [~sq_cBabss~sq_cGBid]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=forms!Babss!Frame2) And ((Babs.GBid)=0)) 

-- [~sq_cBankacc~sq_cComboA]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cBankacc~sq_ccombobankid]
SELECT DISTINCT Bank.* FROM [Bank] ORDER BY Bank.Bankname

-- [~sq_cBankacc~sq_cComboD]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cCashandpetty~sq_cCombocur]
SELECT DISTINCT Bankacc.Curid FROM [Bankacc] WHERE (((Bankacc.Curid) Is Not Null)) ORDER BY Bankacc.Curid

-- [~sq_cCheck_d_dates0~sq_cComboBacc]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_cCheck_p_longsalarm~sq_cCombobaccno]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Accno,Bankacc.Tafno FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_cCheck_Print_m0~sq_cPorder_search]
SELECT Check.* FROM [Check] WHERE (((Check.D1) Between [dd1] And [dd2]) AND ((Check.Paied)=True) AND ((Check.Signed)=True) AND ((Check.Checktype) In (0,1,2)) AND ((Check.Dp)=2)) ORDER BY Check.D1

-- [~sq_cCheck_Print_single~sq_cCheckl]
SELECT DISTINCTROW  FROM [Checkl] WHERE ([__Checkid] = Checkid) 

-- [~sq_cCopy of Porder~sq_cAccno]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen) And ((Acc.Has_Sub_Subsidery)=False)) ORDER BY Acc.Accno

-- [~sq_cCopy of Porder~sq_cBabg]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=2) And ((Babs.GBid)<>0)) 

-- [~sq_cCopy of Porder~sq_cCombocontno]
SELECT Check.Contract FROM [Check] WHERE (((Check.Contract) Is Not Null)) ORDER BY Check.Contract

-- [~sq_cCopy of Porder~sq_cComboP]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null)) ORDER BY Check.Person

-- [~sq_cCopy of Porder~sq_cPo_vouch]
SELECT DISTINCTROW  FROM [] WHERE ([__Checkid] = Checkid) 

-- [~sq_cCopy of Porder~sq_cTransaccid]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Tafno,Bankacc.Accno FROM [Bankacc] 

-- [~sq_cD_Check_kh~sq_cComboD]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cD_Check_resid~sq_cComboP]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Person

-- [~sq_cD_Naghd~sq_cPo_vouch]
SELECT DISTINCTROW  FROM [] WHERE ([__Sid] = Sid) 

-- [~sq_cD_Variz~sq_cPo_vouch]
SELECT DISTINCTROW  FROM [] WHERE ([__Sid] = Sid) 

-- [~sq_cDate_dcheck_entez~sq_cBabat]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Babat

-- [~sq_cDate_dcheck~sq_cFindtext1]
SELECT Findtext.* FROM [Findtext] WHERE (((Findtext.Dp)=1)) 

-- [~sq_cDate_Sarresid_alarm~sq_cperson]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Person

-- [~sq_cDP_s_all_m~sq_cDP_s_all]
SELECT Balance.* FROM [Balance] 

-- [~sq_cGlobals~sq_cCombo77]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals~sq_cComboA]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals~sq_cComboE]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals~sq_cComboG]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals~sq_cComboJ]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cComboA]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cComboD]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cComboH]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cGlobals0~sq_cComboJ]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Acc.Has_Sub_Subsidery)=False) And ((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cSubsrow~sq_cBankid]
SELECT Bank.Bankid,Bank.Bankname FROM [Bank] 

-- [~sq_fSubsrow]
SELECT DISTINCTROW  FROM [Subsrow] 

-- [~sq_cHepcoForm~sq_cBabat]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Babat

-- [~sq_cHepcoForm~sq_cVagno]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_cNaghd_pay_m~sq_cCheck_s]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Payit)=True) AND ((Check.Paied)=False) AND ((Check.Checktype)=9) AND ((Check.Baccno) Is Not Null)) 

-- [~sq_cNajorepfrm~sq_cTafno]
SELECT t_unique_accounts.Account_Code,t_unique_accounts.Account_Name FROM [t_unique_accounts] ORDER BY t_unique_accounts.Account_Code

-- [~sq_cp_Naghd~sq_cbabat]
SELECT DISTINCT Subs.Babat FROM [Subs] WHERE (((Subs.Babat) Is Not Null)) ORDER BY Subs.Babat

-- [~sq_cp_Naghd~sq_cComboBacc]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype FROM [Bankacc] WHERE (((Bankacc.Acctype)<>0)) 

-- [~sq_cp_Naghd~sq_cComboinvno]
SELECT Check.Invno FROM [Check] ORDER BY Check.Invno

-- [~sq_cp_Naghd~sq_cTransaccid]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Tafno,Bankacc.Accno FROM [Bankacc] 

-- [~sq_cPayables~sq_cBabat]
SELECT DISTINCT Check.Babat FROM [Check] WHERE (((Check.Babat) Is Not Null)) ORDER BY Check.Babat

-- [~sq_cPayables~sq_cCombocontno]
SELECT Check.Contract FROM [Check] WHERE (((Check.Contract) Is Not Null)) ORDER BY Check.Contract

-- [~sq_cPayables~sq_cComboP]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null)) ORDER BY Check.Person

-- [~sq_cPorder~sq_cAccno]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen) And ((Acc.Has_Sub_Subsidery)=False)) ORDER BY Acc.Accno

-- [~sq_cPorder~sq_cBabg]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=2) And ((Babs.GBid)<>0)) 

-- [~sq_cPorder~sq_cCombocontno]
SELECT Check.Contract FROM [Check] WHERE (((Check.Contract) Is Not Null)) ORDER BY Check.Contract

-- [~sq_cPorder~sq_cPbyCheckno]
SELECT DISTINCT Check.PbyCheckno FROM [Check] WHERE (((Check.PbyCheckno) Is Not Null)) ORDER BY Check.PbyCheckno

-- [~sq_cPorder~sq_cComboP]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null)) ORDER BY Check.Person

-- [~sq_fD_Check_Vos]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Vagno)=[ComboBacc]) AND ((Check.Stcode)=12)) ORDER BY Check.D3

-- [~sq_cPorder~sq_cTafnotcash]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cPordersub_vouch~sq_cBid]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs_1.Bdesc,Babs.Bdesc FROM [Babs],[Babs] ORDER BY Babs.Dp

-- [~sq_cPordersub_vouch~sq_cComboB]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cSacc_m~sq_cSACC_SUB]
SELECT Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cVoucher~sq_cVoucher_row]
SELECT DISTINCTROW  FROM [] WHERE ([__Vid] = Vid) 

-- [~sq_dB_D_printnaghd~sq_dChild358]
SELECT DISTINCTROW  FROM [Subsrow] WHERE ([__Sid] = Sid) 

-- [~sq_dB_D_printnaghd~sq_dSubsrowrep]
SELECT DISTINCTROW  FROM [Subsrow] WHERE ([__Sid] = Sid) 

-- [~sq_dBankrep~sq_dBankrep_s]
SELECT DISTINCTROW  FROM [Bankrep_t] 

-- [~sq_dCheck_d_longsrepalarm~sq_dCombobaccno]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Accno,Bankacc.Tafno FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_dCheck_Print_m~sq_dList1]
SELECT Check.Checkid,Check.Dp,Check.Checktype,Check.Currate,Check.Sal,Check.Iscur,Check.Empid,Check.Rfpid,Format([D1],'@@@@\/@@\/@@'),Trim([Checktypen]),IIf([Iscur]=0,Format([Amount],'#,##0'),Format([Amount],'Standard')),Check.Curid,Check.Baccno,Check.Checkno,Format([Maturity],'@@@@\/@@\/@@'),Check.Person,Check.Babat FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype) In (0,1,2)) AND ((Check.D1) Between [dd1] And [dd2]) AND ((Check.Paied)=True) AND ((Check.Signed)=True)) ORDER BY Check.D1

-- [~sq_dD_Check_Audit~sq_dCombo96]
SELECT DISTINCT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null) AND ((Check.Dp)=1)) ORDER BY Check.Person

-- [~sq_dD_Check_manager~sq_dFindtext]
SELECT Findtext.* FROM [Findtext] WHERE (((Findtext.Dp)=1)) 

-- [~sq_dD_search_print~sq_dSorts_print]
SELECT SORTS.* FROM [SORTS] WHERE (((SORTS.Ittype)=forms!sal!ttype)) 

-- [~sq_dDP_search~sq_dCombo1]
SELECT Bankacc.Baccno FROM [Bankacc] ORDER BY Bankacc.Baccno

-- [~sq_dJaam_Residacc~sq_dRep_voucher_row_bp]
SELECT Voucher_row_tmp.*,t_accounts.Account_Name,t_unique_accounts.Account_Name FROM [Voucher_row_tmp],[t_accounts],[t_unique_accounts] 

-- [~sq_dJaam_Residsales~sq_dResidcheckallsub]
SELECT Check.* FROM [Check] WHERE (((Check.Person)=Forms!D_Check!Person) And ((Check.Dp)=1) And ((Check.Rfpdate)=Forms!D_Check!Rfpdate)) 

-- [~sq_dPorder_search_print~sq_dShart_print]
SELECT SHART.* FROM [SHART] WHERE (((SHART.Ittype)=Forms!Shart_m!SHART.Form!Text0)) 

-- [~sq_dPorder~sq_dAccnotcash]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account,Acc.Has_Sub_Subsidery FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen)) ORDER BY Acc.Accno

-- [~sq_cCheck_Print_single~sq_cAccno]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen) And ((Acc.Has_Sub_Subsidery)=False)) ORDER BY Acc.Accno

-- [~sq_dPorder~sq_dBaccno]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Acctype,Bankacc.Iscur,Bankacc.Curid,Bankacc.Accno,Bankacc.Tafno FROM [Bankacc] WHERE (((Bankacc.Acctype)=0 Or (Bankacc.Acctype)=1)) 

-- [~sq_dPorder~sq_dCombocur]
SELECT DISTINCT Check.Curid FROM [Check] WHERE (((Check.Curid) Is Not Null And (Check.Curid)<>"ريال")) 

-- [~sq_dPorder~sq_dDepname]
SELECT Deps.Depid,Deps.Depname FROM [Deps] ORDER BY Deps.Depname

-- [~sq_dPorder~sq_dPo_vouch]
SELECT DISTINCTROW  FROM [] WHERE ([__Checkid] = Checkid) 

-- [~sq_dPorder~sq_dTransaccid]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Tafno,Bankacc.Accno FROM [Bankacc] 

-- [~sq_dResid_Check~sq_dRep_voucher_row_bp]
SELECT Voucher_row_tmp.*,t_accounts.Account_Name,t_unique_accounts.Account_Name FROM [Voucher_row_tmp],[t_accounts],[t_unique_accounts] 

-- [~sq_fAccamounts]
SELECT Subs.* FROM [Subs] WHERE (((Subs.Scode)=10)) ORDER BY Subs.Baccno

-- [~sq_fBabs]
SELECT Babs.* FROM [Babs] WHERE (((Babs.GBid)=0) AND ((Babs.Dp)=2)) 

-- [~sq_fBank]
SELECT Bank.Bankid,Bank.Bankname FROM [Bank] 

-- [~sq_fCashandpetty]
SELECT Bankacc.* FROM [Bankacc] WHERE (((Bankacc.Acctype)<>0)) 

-- [~sq_fCheck_p_longsalarm]
SELECT Check.*,-DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) FROM [Check] WHERE (((Check.Maturity)<="13" & AddNumberAndConvertToNewDate(Currentdatefarsi(Date()),DLookUp("befpayablec","Globals"),True)) AND ((Check.Checktype)<>0 And (Check.Checktype)<>1) AND ((Check.Ctocash)=False) AND ((Check.Dp)=2) AND ((Check.Paied)=True) AND ((Check.EbtorKharj)=False)) ORDER BY Check.Maturity

-- [~sq_fCopy of Porder]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) And ((Check.Checktype)=0 Or (Check.Checktype)=1 Or (Check.Checktype)=2 Or (Check.Checktype)=9)) ORDER BY Check.Rfpdate

-- [~sq_fD_Check_Kh_m]
SELECT Mains.* FROM [Mains] WHERE (((Mains.Sal)=Forms!sal!Tsal) And ((Mains.Empid)=Forms!sal!Tempid) And ((Mains.Gcode)=19)) 

-- [~sq_fD_Variz]
SELECT Subs.* FROM [Subs] WHERE (((Subs.Scode)=16)) ORDER BY Subs.Sdate

-- [~sq_fDP_s_all]
SELECT Balance.* FROM [Balance] 

-- [~sq_fEmps]
SELECT DISTINCTROW  FROM [Emps] 

-- [~sq_fFindtextnaghd]
SELECT Findtext.* FROM [Findtext] WHERE (((Findtext.Dp)>10)) 

-- [~sq_fInvsamount]
SELECT DISTINCTROW  FROM [Invsamount] 

-- [~sq_fNaghd_s]
SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Payit)=True) AND ((Check.Paied)=False) AND ((Check.Checktype)=9) AND ((Check.Baccno) Is Not Null)) 

-- [~sq_fp_Naghd]
SELECT Subs.* FROM [Subs] WHERE (((Subs.Scode)=27)) ORDER BY Subs.Sdate

-- [~sq_fPordernew]
SELECT DISTINCTROW  FROM [Check] 

-- [~sq_fRights]
SELECT Rights.* FROM [Rights] WHERE (((Rights.App)=[frame1])) 

-- [~sq_fVoucher]
SELECT Voucher.* FROM [Voucher] ORDER BY Voucher.Vdate

-- [~sq_rB_D_printnaghd]
SELECT Subs.* FROM [Subs] 

-- [~sq_rB_P_print_Safteh]
SELECT Check.*,Bankacc.Bank,Bankacc.Branch FROM [Check],[Bankacc] WHERE (((Check.Dp)=2)) 

-- [~sq_rB_P_printbardasht]
SELECT Subs.* FROM [Subs] 

-- [~sq_rBabrepnew1curs]
SELECT Val(Mid([Scode],1,1)),Subs.Curid,IIf([Scode]=10,0,1),Subs.Babatid,Babs_1.Bdesc,Babs.Bdesc,Sum(Subs.Bed),Sum(Subs.Bes),Sum([Currate]*[Bed]),Sum([Currate]*[Bes]),IIf(Val(Mid([Scode],1,1))=1,"دريافتها","پرداختها") FROM [Subs],[Babs],[Babs] WHERE (((Subs.Sdate) Between Forms!Babsrepfrm!DD1 And Forms!Babsrepfrm!DD2)) ORDER BY Subs.Curid

-- [~sq_rBabrepnewbalance]
SELECT Subs.Curid,Subs.Baccno,Sum(IIf([Scode]=10,[Bed],0)),Sum(IIf([Scode]=10,[Bed]*[Currate],0)),Sum(IIf([Scode]=10,0,[Bed])),Sum(IIf([Scode]=10,0,[Bes])),Sum((IIf([Scode]=10,0,[Bed]*[Currate]))),Sum((IIf([Scode]=10,0,[Bes]*[Currate]))) FROM [Subs] WHERE (((Subs.Sdate) Between Forms!Babsrepfrm!DD1 And Forms!Babsrepfrm!DD2)) ORDER BY Subs.Curid

-- [~sq_rCardcur]
SELECT Subs.* FROM [Subs] WHERE (((Subs.Baccno)=Forms!Date_card!ComboBacc) And ((Subs.Sdate) Between Forms!Date_card!D1 And Forms!Date_card!D2)) 

-- [~sq_rCH_Kesh10]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_maadan50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Maskan25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_MELLAT100]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_MELLAT25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_MELLAT50_new]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_MELLI50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Novin25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Parsian25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_Pasargaad25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_REFAH25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_SADERAT50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_SEPAH25]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_SEPAH50carbon]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_TEJARAT50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCH_TOSE50]
SELECT Check.*,IIf([Hazfhavale]=True,"*******","") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Signed)=True)) 

-- [~sq_rCheck_d_longsrepalarm]
SELECT Check.*,-DefranceOfTwoDate(nz([maturity],CurrentDateFarsi(Date())),CurrentDateFarsi(Date()),False) FROM [Check] WHERE (((Check.Maturity)<="13" & AddNumberAndConvertToNewDate(Currentdatefarsi(Date()),DLookUp("befrecievables","Globals"),True)) AND ((Check.Checktype) In (0,2,3,4)) AND ((Check.Dp)=1) AND ((Check.Stcode)=11)) ORDER BY Check.Maturity

-- [~sq_rCopy of Resid_check]
SELECT Check.*,Check.Person FROM [Check] WHERE (((Check.Dp)=1) And ((Check.Rfpdate)=Forms!D_Check!Rfpdate) And ((Check.Person)=Forms!D_Check!Person)) 

-- [~sq_rDailyrep]
SELECT Dailyrep.*,IIf([Mtype]=0,"بله ","خير") FROM [Dailyrep] 

-- [~sq_rDailyrepbanks_sub_sub]
SELECT Check.Maturity,Check.Checkno,Check.Person,Check.Vagno,Check.Curid,Check.Bankid,Check.Branchname,Check.Babat,IIf([Maturity]=forms!deaily_rep!text0,[Amount],0),IIf([Maturity]>forms!deaily_rep!text0 And [Maturity]<=forms!deaily_rep!Dw,[Amount],0),IIf([Maturity]>forms!deaily_rep!DW And [Maturity]<=forms!deaily_rep!Dm,[Amount],0),[D]+[W]+[M] FROM [Check] WHERE (((IIf([Maturity]=forms!deaily_rep!text0,[Amount],0))<>0) And ((Check.Dp)=1) And ((Check.Checktype)=0 Or (Check.Checktype)=2) And ((Check.Stcode)=11)) Or (((Check.Dp)=1) And ((IIf([Maturity] Between forms!deaily_rep!Dw0 And forms!deaily_rep!Dw,[Amount],0))<>0)) Or (((Check.Dp)=1) And ((IIf([Maturity] Between forms!deaily_rep!Dm0 And forms!deaily_rep!Dm,[Amount],0))<>0)) ORDER BY Check.Maturity

-- [~sq_rDP_search]
SELECT Subs.* FROM [Subs] WHERE (((Subs.Sdate) Between '13820101' And '13820703') And ((Subs.Card)=False) And ((Subs.Scode)<>10)) ORDER BY Subs.Person

-- [~sq_rResidcheckallsub]
SELECT Check.* FROM [Check] WHERE (((Check.Person)=Forms!D_Check!Person) And ((Check.Dp)=1) And ((Check.Rfpdate)=Forms!D_Check!Rfpdate)) 

-- [~sq_rSarresid_p_byperson]
SELECT Check.*,Switch([Checktype]=0,"چك دريافتني",[Checktype]=1,"چك انتظامي",[Checktype]=2,"چك بانكي",[Checktype]=3,"اقساط دريافتني",[Checktype]=4,"سفته دريافتني",[Checktype]=5,"ساير اقلام ريافتني",[Checktype]=6,"سفته هاي انتظامي"),[PT] & " -" & [Status] FROM [Check] WHERE (((Check.Dp)=1) AND ((Check.Checktype)=0)) 

-- [~sq_rTaaodrepall]
SELECT DISTINCTROW  FROM [Taaod] 

-- [~sq_rTaaodrepalltaminkoli]
SELECT Taaod.* FROM [Taaod] 

-- [~sq_rV_errors_rep]
SELECT Voucher_errors.* FROM [Voucher_errors] 

-- [badscc_check]
SELECT Subs.Sdate,Subs.Sid,Subs.Scode,Subs.Baccno,Povouch.accno,Povouch.tafno,Subs.Transtype FROM [Povouch],[Subs] ORDER BY Subs.Sdate

-- [Brep_jvosoolm]
SELECT Bankrep_t.am FROM [Bankrep],[Bankrep_t] 

-- [brepinpaypast]
SELECT ‍‍Check.Baccno,‍‍Check.Amount,‍‍Check.Checktypen,‍‍Check.Rfpdate,‍‍Check.Checkno,‍‍Check.Maturity,‍‍Check.Iscur,‍‍Check.Curid,‍‍Check.Person,‍‍Check.Babat FROM [‍‍Check] WHERE (((‍‍Check.Maturity)<[Forms]![Bank_rep]![Text0]) AND ((‍‍Check.Dp)=2) AND ((‍‍Check.Checktype)<>1 And (‍‍Check.Checktype)<>6) AND ((‍‍Check.Calcit)=False)) 

-- [countme]
SELECT count (*) FROM [Voucher_row] 

-- [eli2]
SELECT eli1.Vid,eli1.accno,eli1.tafno,eli1.bed,eli1.bes,'نوع حساب تفصيل نادرست است' FROM [eli1],[t_RelatedSubsideryToUniques] WHERE (((t_RelatedSubsideryToUniques.Account_Code) Is Null)) 

-- [Hepconak]
SELECT 13 FROM [Hepcomain] WHERE (((Hepcomain.ItMat) Between forms!hepcobrief!Mymat And forms!hepcobrief!Mymat2)) 

-- [Query5]
SELECT '1' FROM [Voucher_row],[t_accounts] WHERE (((Voucher_row.ValueField) Is Null Or (Voucher_row.ValueField)="0")) OR (((Voucher_row.ValueField)=' ')) 

-- [ut_qry7]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=[Frame2]) AND ((Babs.GBid)=0)) 

-- [~sq_cp_bardasht~sq_cPo_vouch]
SELECT DISTINCTROW  FROM [] WHERE ([__Sid] = Sid) 

-- [~sq_cp_bardasht~sq_cTransaccid]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Tafno,Bankacc.Accno FROM [Bankacc] 

-- [~sq_cp_bardasht~sq_cTafno]
SELECT Taf.Tafno0,Taf.Tafno,Taf.Tafname FROM [Taf] ORDER BY Taf.Tafno

-- [~sq_cp_bardasht~sq_cAccno]
SELECT Acc.Accno0,Acc.Accno,Acc.Accname,Acc.Has_Unique_Account FROM [Acc] WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen) And ((Acc.Has_Sub_Subsidery)=False)) ORDER BY Acc.Accno

-- [~sq_cp_bardasht~sq_cBabatid]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=2) AND ((Babs.GBid)<>0)) 

-- [~sq_cp_bardasht~sq_cComboBacc]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_cp_bardasht~sq_cBasetype]
SELECT DISTINCT Subs.Basetype FROM [Subs] WHERE (((Subs.Basetype) Is Not Null) AND ((Subs.Scode)=26)) ORDER BY Subs.Basetype

-- [~sq_cp_bardasht~sq_cbabat]
SELECT DISTINCT Subs.Babat FROM [Subs] WHERE (((Subs.Babat) Is Not Null)) ORDER BY Subs.Babat

-- [~sq_cp_bardasht~sq_cCombocontno]
SELECT Check.Contract FROM [Check] ORDER BY Check.Contract

-- [~sq_cp_bardasht~sq_cComboinvno]
SELECT Check.Invno FROM [Check] ORDER BY Check.Invno

-- [~sq_cp_bardasht~sq_cComboP]
SELECT DISTINCT Subs.Person FROM [Subs] WHERE (((Subs.Person) Is Not Null)) ORDER BY Subs.Person

-- [~sq_fp_bardasht]
SELECT Subs.* FROM [Subs] WHERE (((Subs.Scode)=26)) ORDER BY Subs.Sdate

-- [~sq_cCheck_Print_single~sq_cBabatid]
SELECT Babs.Bid,Babs.Dp,Babs.GBid,Babs.Bdesc FROM [Babs] WHERE (((Babs.Dp)=2) And ((Babs.GBid)<>0)) 

-- [~sq_cCheck_Print_single~sq_cTransaccid]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Tafno,Bankacc.Accno FROM [Bankacc] 

-- [~sq_cCheck_Print_single~sq_cBaccno]
SELECT Bankacc.Baccno,Bankacc.Bank,Bankacc.Branch,Bankacc.Curid,Bankacc.Acctype,Bankacc.Iscur,Bankacc.Accno,Bankacc.Tafno FROM [Bankacc] WHERE (((Bankacc.Acctype)=0)) 

-- [~sq_cCheck_Print_single~sq_cComboBundle]
SELECT Bundle.Baccno,Bundle.Fsheet,Bundle.Lsheet,Bundle.Sheets,Bundle.Lastused,Bundle.Active,Bundle.Printcode FROM [Bundle] WHERE (((Bundle.Baccno)=forms!check_print_single!Baccno) And ((Bundle.Active)=True)) 

-- [~sq_cCheck_Print_single~sq_cComboP]
SELECT Check.Person FROM [Check] WHERE (((Check.Person) Is Not Null)) ORDER BY Check.Person

-- [~sq_cCheck_Print_single~sq_cCombocontno]
SELECT Check.Contract FROM [Check] ORDER BY Check.Contract

-- [~sq_cCheck_Print_single~sq_cComboinvno]
SELECT Check.Invno FROM [Check] ORDER BY Check.Invno

-- [~sq_cCheck_Print_single~sq_cBabat]
SELECT Check.Babat FROM [Check] 

-- [~sq_fCheck_s]
SELECT Check.*,Switch([Checktype]=0,"چك نقدي",[Checktype]=1,"چك انتظامي",[Checktype]=2,"چك مدت دار",[Checktype]=9,"پرداخت نقدي") FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Payit)=True) AND ((Check.Paied)=False) AND ((Check.Checktype) In (0,1,2))) ORDER BY Check.Rfpdate

-- [~sq_fCheck_Print_single]
SELECT Check.*,IIf([Hazfhavale]=True,'******','') FROM [Check] WHERE (((Check.Payit)=True) And ((Check.Dp)=2)) ORDER BY Check.Rfpdate

