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

