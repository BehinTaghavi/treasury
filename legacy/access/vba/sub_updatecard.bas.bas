Attribute VB_Name = "sub_updatecard"
Option Compare Database
Dim mystr As String, Mydate As String, subdate As String

Public Sub updatecard()
    'Mydate = Forms!Sal!Tsal & "0101"
    Mydate = Nz(DLookup("Vfrom", "Globals"), 0)
    subdate = CurrentDateFarsi(Date)
    DoCmd.SetWarnings False
        mystr = "Delete * from Check where Checkid=0"
        DoCmd.RunSQL mystr
            
        mystr = "Delete * from Subs where Sid=0"
        DoCmd.RunSQL mystr
        
        mystr = "UPDATE Subs SET Subs.Sdate = " & "'" & CStr(subdate) & "'WHERE (((Subs.Sdate) Is Null));"
        DoCmd.RunSQL mystr
        
        mystr = "UPDATE Subs SET Subs.Bed = 0 WHERE (((Subs.Bed) Is Null));"
        DoCmd.RunSQL mystr
        
        mystr = "UPDATE Subs SET Subs.Bes = 0 WHERE (((Subs.Bes) Is Null));"
        DoCmd.RunSQL mystr
        
        mystr = "Delete * from Subs where card= true and sal=" & Forms!Sal!Tsal
        DoCmd.RunSQL mystr
        
        mystr = "UPDATE [Check] SET [Check].D4 = [Maturity] WHERE (((Check.D4) Is Null) AND ((Check.Dp)=1) AND ((Check.Stcode)=13));"
        DoCmd.RunSQL mystr

        mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal ,Babatid,Currate,tafno) SELECT Check.Checkid, Check.Checkno, Check.D4, 13 , 'وصول چكهاي واگذارشده' , Check.Vagno, Check.Curid, Check.Iscur, Check.Amount, 0 AS B, Check.Person, Check.Babat, True AS ca, forms!sal![tsal] ,Check.Babatid,Check.Currate,Check.tafno FROM [Check] WHERE (((Check.D4)>=" & "'" & CStr(Mydate) & "') AND ((Check.Dp)=1) AND ((Check.Stcode)=13));"
        DoCmd.RunSQL mystr
        
        mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal,Babatid ,Currate,tafno) SELECT Check.Checkid, Check.Checkno, Check.D5, 21 , 'صدور چك', Check.Baccno, Check.Curid, Check.Iscur, 0 AS a, Check.Amount AS B, IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]), Check.Babat, Check.Calcit, forms!sal![tsal] ,Check.Babatid,Check.Currate,Check.tafno  FROM [Check] WHERE (((Check.D5)>=" & "'" & CStr(Mydate) & "') AND ((Check.Dp)=2) AND ((Check.Calcit)=True)AND ((Check.Ctocash)=false));"
        DoCmd.RunSQL mystr
        
        'Start Checks Voided
            '1-Naghdi
            'sodoor
             mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal,Babatid ,Currate,Ebt) SELECT Check.Checkid, Check.Checkno, Check.D1, 21 , 'صدور چك', Check.Baccno, Check.Curid, Check.Iscur, 0 AS a, Check.Amount AS B, IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]), Check.Babat, True, forms!sal![tsal] ,Check.Babatid,Check.Currate,True  FROM [Check] WHERE Check.D1>='" & Mydate & "' AND Check.Dp=2 AND Check.Ebtorkharj=True AND Check.Checktype=0 ;"
             DoCmd.RunSQL mystr
            'Ebtal
             mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal,Babatid ,Currate,Ebt) SELECT Check.Checkid, Check.Checkno, Check.D2, 22 , 'ابطال چک', Check.Baccno, Check.Curid, Check.Iscur,  Check.Amount ,0, IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]), Check.Babat, True, forms!sal![tsal] ,Check.Babatid,Check.Currate,True  FROM [Check] WHERE Check.D2>='" & Mydate & "' AND Check.Dp=2 AND Check.Ebtorkharj=True AND Check.Checktype=0 ;"
             DoCmd.RunSQL mystr
        
            '2-Longs
            'sodoor
             mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal,Babatid ,Currate,Ebt) SELECT Check.Checkid, Check.Checkno, Check.D3, 21 , 'تبديل به نقد اقلام مدت دار/انتظامي', Check.Baccno, Check.Curid, Check.Iscur, 0 AS a, Check.Amount AS B, IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]), Check.Babat, True, forms!sal![tsal] ,Check.Babatid,Check.Currate,True  FROM [Check] WHERE Check.D3>='" & Mydate & "' AND Check.Dp=2 AND Check.Ebtorkharj=True AND Check.Checktype=2 AND Check.Ctocash=true ;"
             DoCmd.RunSQL mystr
            'Ebtal
             mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal,Babatid ,Currate,Ebt) SELECT Check.Checkid, Check.Checkno, Check.D2, 22 , 'ابطال چک', Check.Baccno, Check.Curid, Check.Iscur,  Check.Amount ,0, IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]), Check.Babat, True, forms!sal![tsal] ,Check.Babatid,Check.Currate,True  FROM [Check] WHERE Check.D2>='" & Mydate & "' AND Check.Dp=2 AND Check.Ebtorkharj=True AND Check.Checktype=2 AND Check.Ctocash=true ;"
             DoCmd.RunSQL mystr
  
        
        
'        mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bes, Bed, Person, Babat, Card, Sal,Babatid ,Currate,Ebt) SELECT Check.Checkid, Check.Checkno, Check.D2, 22 , 'ابطال چك صادره', Check.Baccno, Check.Curid, Check.Iscur, 0 AS a, Check.Amount AS B, IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]), Check.Babat, True, forms!sal![tsal] ,Check.Babatid,Check.Currate,True  FROM [Check] WHERE (((Check.D2)>='" & CStr(Mydate) & "') AND ((Check.Dp)=2) AND ((Check.Ebtorkharj)=True)AND ((Check.Checktype)=0));"
'        DoCmd.RunSQL mystr
'        'Longs Ctocash
'
'        'mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal,Babatid ,Currate,tafno) SELECT Check.Checkid, Check.Checkno, Check.D2, 21 , 'تبديل به نقد اقلام مدت دار/انتظامي', Check.Baccno, Check.Curid, Check.Iscur, 0 , Check.Amount , IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]), Check.Babat, true, forms!sal![tsal] ,Check.Babatid,Check.Currate,Check.tafno  FROM [Check] WHERE (((Check.D2)>=" & "'" & CStr(Mydate) & "') AND ((Check.Dp)=2) AND ((Check.Ebtorkharj)=True)AND ((Check.Ctocash)=True));"
'        mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal,Babatid ,Currate,tafno) SELECT Check.Checkid, Check.Checkno, Check.D2, 21 , 'تبديل به نقد اقلام مدت دار/انتظامي', Check.Baccno, Check.Curid, Check.Iscur, 0 , Check.Amount , IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]), Check.Babat, true, forms!sal![tsal] ,Check.Babatid,Check.Currate,Check.tafno  FROM [Check] WHERE Check.D2>='" & Mydate & "' AND Check.Dp=2 AND Check.Ebtorkharj=True AND Check.Ctocash=True;"
'        DoCmd.RunSQL mystr
'        mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bes, Bed, Person, Babat, Card, Sal,Babatid ,Currate,Ebt) SELECT Check.Checkid, Check.Checkno, Check.D2, 22 , 'ابطال چك صادره', Check.Baccno, Check.Curid, Check.Iscur, 0 AS a, Check.Amount AS B, IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]), Check.Babat, True, forms!sal![tsal] ,Check.Babatid,Check.Currate,True  FROM [Check] WHERE Check.D2>='" & Mydate & "' AND Check.Dp=2 AND Check.Ebtorkharj=True AND Check.Checktype=2 AND Check.Ctocash=true ;"
'        DoCmd.RunSQL mystr
        
        'End Checks Voided
        
        
        
        
        
        mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal ,Babatid,Currate,Tafno) SELECT Check.Checkid, Check.Checkno, Check.D5, 14 , ' وجوه انتقالي از ساير حسابها' , Check.Transaccid, Check.Curid, Check.Iscur, Check.Amount, 0 AS B,  'جاري-' & Check.baccno, Check.Babat, True AS ca, forms!sal![tsal] ,Check.Babatid,Check.Currate,Check.tafno  FROM [Check] WHERE (((Check.D5)>=" & "'" & CStr(Mydate) & "') AND ((Check.Dp)=2) And ((Check.Transaccid)<>null)AND ((Check.Calcit)=True));"
        DoCmd.RunSQL mystr

        mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal ,Babatid,Currate,tafno) SELECT Subs.Checkid,Subs.Checkno, Subs.Sdate,14, ' انتقالي از ساير حسابها',Subs.Transaccid,Subs.Curid, Subs.iscur ,IIf([iscur]=False,[Bes],[Bes]*[Currate]),0,Subs.Person,Subs.Babat, True , forms!sal![tsal] ,Subs.Babatid,Subs.Currate,Subs.tafno  FROM Subs WHERE (((Subs.Sdate)>='" & CStr(Mydate) & "') AND ((Subs.Transaccid) Is Not Null));"
        DoCmd.RunSQL mystr
        
        mystr = "INSERT INTO Subs ( Checkid, Checkno, Sdate, Scode, Transtype, Baccno, Curid, Iscur, Bed, Bes, Person, Babat, Card, Sal,Babatid ,Currate,tafno) SELECT Check.Checkid, Check.Checkno, Check.D5, 25 , 'پ اقساط /چ مدت دار/چ انتظامي ', Check.Baccno, Check.Curid, Check.Iscur, 0 , Check.Amount , IIf(IsNull([Pbycheckno]),[Person],[Pbycheckno]), Check.Babat, Check.Calcit, forms!sal![tsal] ,Check.Babatid,Check.Currate,Check.tafno  FROM [Check] WHERE (((Check.D5)>=" & "'" & CStr(Mydate) & "') AND ((Check.Dp)=2) AND ((Check.Calcit)=True)AND ((Check.Ctocash)=True));"
        DoCmd.RunSQL mystr
        
        S = "UPDATE (([Check] INNER JOIN Bankacc ON Check.Baccno = Bankacc.Baccno) INNER JOIN Bankacc AS Bankacc_1 ON Check.Transaccid = Bankacc_1.Baccno) INNER JOIN Subs ON Check.Checkid = Subs.Checkid SET Subs.Bed = [Amount]*[Check].[Currate]WHERE (((Check.D5)>='" & CStr(Mydate) & "') AND ((Check.Transaccid) Is Not Null) AND ((Check.Dp)=2) AND ((Check.Calcit)=True) AND ((Bankacc_1.Curid)<>[Bankacc].[Curid]) AND ((Subs.Bes)=0));"
        docommand S
    DoCmd.SetWarnings True
End Sub
