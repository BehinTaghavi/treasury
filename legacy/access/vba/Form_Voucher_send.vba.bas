Attribute VB_Name = "Form_Voucher_send"
Attribute VB_Base = "0{277B8ABF-D2C3-472C-9B7E-62EE1A29AB4F}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
    Dim mystr As String, mystr1 As String, mystr2 As String, mystrv As String
    Dim rst As Recordset, rst1 As Recordset, rst2 As Recordset, rstv As Recordset
    'Dim D1 As String, D2 As String
    Dim T As Long, Trecs As Long, Accvno As Long, A As String
    Dim sbed As Double, sbes As Double, Myvfrom As String, Myvup As String
    Dim haserror As Boolean, itsum As Double, Onebyone As Byte
    Dim Myvno As Long, myvdate As String, Mybed As Double, Mybes As Double
    Dim tvouch As Integer, Mydbase As String, Curdate As String, Terrors As Long


Private Sub Command0_Click()
    'GoTo Line21
    'Sendonebyone
    'Exit Sub
    mystr = "DELETE Voucher.* FROM Voucher WHERE (((Voucher.vid) Is Null) Or ((Voucher.vid)=0));"
    docommand mystr
    'S = "DELETE Voucher_row.Vid FROM Voucher_row WHERE (((Voucher_row.Vid) Not In (Select Vid from Voucher)));"
    S = "DELETE Voucher_row.*, Voucher.Vid FROM Voucher RIGHT JOIN Voucher_row ON Voucher.Vid = Voucher_row.Vid WHERE (((Voucher.Vid) Is Null));"
    docommand S
    mystr = "DELETE * FROM Voucher_errors ;"
    docommand mystr
    Onebyone = IIf(DLookup("Editlcv", "Globals") = True, 1, 0)
    Mydbase = Me.LastCreatec
    Curdate = CurrentDateFarsi(Date)
    If Onebyone = 0 Then
        mystr = "Select * from Voucher where Vdate Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "' and Sended= false Order by Vdate,Vid;"
    Else
        mystr = "Select * from Voucher where Vdate Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "'  Order by Vdate,Vid;"
    End If
    
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            MsgBox "درمحدوده مورد نظر سندي وجود ندارد"
            Exit Sub
        Else
            .MoveLast
            .MoveFirst
            tvouch = .RecordCount
            Me.Label12.Visible = True
            Me.Label12.Caption = tvouch
            Me.Label3.Visible = True
            DoEvents
        End If
    .Close
    End With
    
    Set rst = CurrentDb.OpenRecordset("Globals", dbOpenDynaset, dbSeeChanges)
    With rst
         Myvfrom = Nz(!Vfrom)
         Myvup = Nz(!Vup)
        .Close
    End With
    
    S = "SELECT Voucher_row.Vid, Voucher_row.Recno, Count(1) AS a, Voucher.Vno, Voucher.Vdate FROM Voucher_row INNER JOIN Voucher ON Voucher_row.Vid = Voucher.Vid GROUP BY Voucher_row.Vid, Voucher_row.Recno, Voucher.Vno, Voucher.Vdate HAVING (((Count(1))>1) AND ((Voucher.Vdate)>=' & CStr(Mydbase) & '));"
    Set rst = CurrentDb.OpenRecordset(S, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            GoTo Linejafarsoli
        Else
            S = "INSERT INTO Voucher_errors ( Vdate, Errordesc, Vno, Vid )SELECT Voucher.Vdate, 'رديف سند تكراري است' AS 1, Voucher.Vno, Voucher_row.Vid FROM Voucher_row INNER JOIN Voucher ON Voucher_row.Vid = Voucher.Vid GROUP BY Voucher.Vdate, Voucher.Vno, Voucher_row.Vid, Voucher_row.Recno HAVING (((Voucher.Vdate)>=' & CStr(Mydbase) & ') AND ((Count(1))>1));"
            docommand S
        End If
    .Close
    End With
    
Linejafarsoli:
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc, Vno )SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'تاريخ سند نادرست است' AS T, Voucher.Vno FROM Voucher WHERE (((Len(Trim([Vdate])))<>8));"
    docommand S
    
   ' S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc, Vno )SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'تاريخ سند نادرست است' AS T, Voucher.Vno FROM Voucher WHERE (((Voucher.Vdate) Not Between '" & CStr(Myvfrom) & "' And '" & CStr(Myvup) & "'));"
   ' docommand S
    
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc )SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'تاريخ سند نادرست است' AS T FROM Voucher INNER JOIN Voucher_row ON Voucher.Vid = Voucher_row.Vid WHERE Voucher.Vdate Not Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "' and Sended= false ;"
    docommand S
    
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Bed, Bes, Vdate, Errordesc )SELECT Voucher.Vid, Voucher.Vgroup, Sum(Voucher_row.bed) AS Sbed, Sum(Voucher_row.bes) AS Sbes, Voucher.Vdate, 'سند موازنه نيست' AS T FROM Voucher INNER JOIN Voucher_row ON Voucher.Vid = Voucher_row.Vid GROUP BY Voucher.Vid, Voucher.Vgroup, Voucher.Vdate HAVING (((Voucher.Vdate)>='" & CStr(Mydbase) & "') AND ((Sum([bed])-Sum([bes]))<>0));"
    docommand S
    Me.Label5.Visible = True
    Me.Label5.Caption = "1- كنترل تاريخ وموازنه اسناد حسابداري انجام شد"
    Me.Label5.ForeColor = RGB(255, 0, 155)
    DoEvents
    
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc, Bed, Bes, Accno, Tafno )SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'رديف سند فاقد مبلغ است' AS T, Voucher_row.bed, Voucher_row.bes, Voucher_row.accno, Voucher_row.tafno FROM Voucher INNER JOIN Voucher_row ON Voucher.Vid = Voucher_row.Vid WHERE (((Voucher.Vdate) Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "') AND ((Voucher_row.bed)=0) AND ((Voucher_row.bes)=0));"
    docommand S
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc, Bed, Bes, Accno, Tafno ) SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'رديف سند فاقد حساب معين است' AS T, Voucher_row.bed, Voucher_row.bes, Voucher_row.accno, Voucher_row.tafno FROM Voucher INNER JOIN Voucher_row ON Voucher.Vid = Voucher_row.Vid WHERE (((Voucher.Vdate) Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "') AND ((Voucher_row.accno) Is Null));"
    docommand S
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc, Bed, Bes, Accno, Tafno )SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'معادل حساب معين در سيستم دفترداري وجود ندارد' AS T, Voucher_row.bed, Voucher_row.bes, Voucher_row.accno, Voucher_row.tafno FROM (Voucher INNER JOIN Voucher_row ON Voucher.Vid = Voucher_row.Vid) LEFT JOIN t_accounts ON Voucher_row.accno = t_accounts.Account_Code WHERE (((Voucher.Vdate) Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "') AND ((t_accounts.Account_Code) Is Null));"
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc, Bed, Bes, Accno, Tafno )SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'سطح حساب نادرست است' AS T, Voucher_row.bed, Voucher_row.bes, Voucher_row.accno, Voucher_row.tafno FROM Voucher INNER JOIN (Voucher_row LEFT JOIN t_accounts ON Voucher_row.accno = t_accounts.Account_Code) ON Voucher.Vid = Voucher_row.Vid WHERE (((Voucher.Vdate) Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "') AND ((t_accounts.Has_Sub_Subsidery)=True));"
    docommand S
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc, Bed, Bes, Accno, Tafno ) SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'رديف سند فاقد حساب تفصيل است' AS T, Voucher_row.bed, Voucher_row.bes, Voucher_row.accno, Voucher_row.tafno FROM Voucher INNER JOIN (Voucher_row LEFT JOIN t_accounts ON Voucher_row.accno = t_accounts.Account_Code) ON Voucher.Vid = Voucher_row.Vid WHERE (((Voucher.Vdate) Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "') AND ((Voucher_row.tafno) Is Null) AND ((t_accounts.Has_Unique_Account)=True));"
    docommand S
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc, Bed, Bes, Accno, Tafno ) SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'معادل حساب تفصيل در سيستم دفترداري وجود ندارد' AS T, Voucher_row.bed, Voucher_row.bes, Voucher_row.accno, Voucher_row.tafno FROM (Voucher INNER JOIN (Voucher_row LEFT JOIN t_accounts ON Voucher_row.accno = t_accounts.Account_Code) ON Voucher.Vid = Voucher_row.Vid) LEFT JOIN t_unique_accounts ON Voucher_row.tafno = t_unique_accounts.Account_Code WHERE (((Voucher.Vdate) Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "') AND ((t_accounts.Has_Unique_Account)=True) AND ((t_unique_accounts.Account_Code) Is Null));"
    docommand S
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc, Bed, Bes, Accno, Tafno ) SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'رديف سند فاقدمقدار است' AS T, Voucher_row.bed, Voucher_row.bes, Voucher_row.accno, Voucher_row.tafno FROM Voucher INNER JOIN (Voucher_row LEFT JOIN t_accounts ON Voucher_row.accno = t_accounts.Account_Code) ON Voucher.Vid = Voucher_row.Vid WHERE (((Voucher.Vdate) Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "') AND ((t_accounts.Has_Value_Field)=True) AND ((Voucher_row.ValueField) Is Null));"
    docommand S
    S = "INSERT INTO Voucher_errors ( Vid, Tcode, Vdate, Errordesc, Bed, Bes, Accno, Tafno )SELECT Voucher.Vid, Voucher.Vgroup, Voucher.Vdate, 'رديف سند فاقدزمان است' AS T, Voucher_row.bed, Voucher_row.bes, Voucher_row.accno, Voucher_row.tafno FROM Voucher INNER JOIN (Voucher_row LEFT JOIN t_accounts ON Voucher_row.accno = t_accounts.Account_Code) ON Voucher.Vid = Voucher_row.Vid WHERE (((Voucher.Vdate) Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "') AND ((t_accounts.Has_Date_Field)=True) AND ((Voucher_row.DateField) Is Null));"
    Terrors = DCount("*", "Voucher_errors")
    Me.Label6.Visible = True
    Me.Label6.Caption = "2- كنترل صحت اسناد حسابداري انجام شد"
    Me.Label6.ForeColor = RGB(255, 0, 255)
    DoEvents
Line21:

    If Terrors = 0 Then
        If MsgBox("اسناد محدوده انتخابي فاقد خطاست .اسناد انتقال يابد؟", vbYesNo) = vbYes Then
             If Onebyone = 0 Then
                Sendvoucher
             Else
                Sendonebyone
             End If
           
        Else
            Exit Sub
        End If
    Else
        MsgBox "خطاي اسناد مورد نظر را مشاهده نماييد"
        DoCmd.OpenForm "V_errors"
    End If
    
End Sub

Private Sub Command2_Click()
On Error GoTo Err_Command2_Click


    DoCmd.Close

Exit_Command2_Click:
    Exit Sub

Err_Command2_Click:
    MsgBox Err.Description
    Resume Exit_Command2_Click
    
End Sub

Private Sub Sendvoucher()
    'Curdate = "13831230"
    mystr = "Select * from Voucher where Vdate Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "' And Sended=false Order by Vdate,Vid;"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            MsgBox "درمحدوده مورد نظر سندي وجود ندارد"
            Exit Sub
        Else
            .MoveLast
            .MoveFirst
            T = 1
            Trecs = rst.RecordCount
            Me.ProgressBar3.Value = 0
            Me.Label9.Caption = 0
            Me.Label14.Visible = True
            Me.Label17.Visible = True
            Me.Label20.Visible = True
            Me.Label9.Visible = True
            DoEvents
        End If
        
        S = "DELETE t_voucher_row.*, t_voucher.Is_Updated, t_voucher.source_type, t_voucher.Voucher_Date FROM t_voucher INNER JOIN t_voucher_row ON t_voucher.Voucher_No = t_voucher_row.Voucher_No WHERE (((t_voucher.Is_Updated)=0) AND ((t_voucher.source_type)=3) AND ((t_voucher.Voucher_Date) Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "'));"
        docommand S
        
        S = "DELETE t_voucher.* FROM t_voucher WHERE (((t_voucher.Is_Updated)=0) AND ((t_voucher.Source_Type)=3) AND ((t_voucher.Voucher_Date) Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "'));"
        docommand S

        S = "DELETE t_voucher_row.* FROM t_voucher_row LEFT JOIN t_voucher ON t_voucher_row.Voucher_No=t_voucher.Voucher_No WHERE (((t_voucher.Voucher_No) Is Null));"
        docommand S
        
        While Not .EOF
            Myvno = !Vid
            myvdate = !Vdate
            Me.ProgressBar3.Value = (T / Trecs) * 100
            Label9.Caption = Int(ProgressBar3.Value) & " %"
            Me.Label20.Caption = !Vno
            Me.Label17.Caption = Format(!Vdate, "@@@@/@@/@@")
            DoEvents
            
            S = "INSERT INTO t_voucher ( Voucher_Date, Source_Type, Modification_Type, Voucher_Type, Is_Updated, Sumation, Description ) SELECT Vcreate.Vdate, Vcreate.Stype, Vcreate.Fictype, Vcreate.Vtype, Vcreate.Isupdated, Vcreate.S, Vcreate.Sh  FROM Vcreate WHERE (((Vcreate.Vid)=" & Myvno & "));"
            'S = "INSERT INTO t_voucher ( Voucher_Date, Source_Type, Modification_Type, Voucher_Type, Is_Updated, Sumation, Description,St_temp ) SELECT Vcreate.Vdate, Vcreate.Stype, Vcreate.Fictype, Vcreate.Vtype, Vcreate.Isupdated, Vcreate.S, Vcreate.Sh,3 FROM Vcreate WHERE (((Vcreate.Vid)=" & Myvno & "));"
            docommand S
            Accvno = Nz(DMax("Voucher_no", "t_voucher"))
            
            S = "INSERT INTO t_voucher_row ( Voucher_No, Account_No, Unique_Account_No, Debit, Credit, Description,Exchange_Ammount, Date_Field, Value_Field,Record_no )SELECT " & Accvno & ", Voucher_row.accno, Voucher_row.tafno, Int(Voucher_row.bed), Int(Voucher_row.bes), Voucher_row.rowdesc,  Voucher_row.Curam,Voucher_row.DateField, Voucher_row.ValueField,Voucher_row.Recno FROM Voucher_row WHERE (((Voucher_row.Vid)=" & Myvno & "))ORDER BY Voucher_row.Checkid, Voucher_row.bed DESC , Voucher_row.bes DESC;"
            docommand S
                    
            S = "UPDATE Voucher SET Voucher.Sended = True, Voucher.Accvno = " & Accvno & " WHERE (((Voucher.Vid)=" & Myvno & "));"
            docommand S

            '*********
         T = T + 1
        .MoveNext
        Wend
    .Close
    End With
        Delorphans
        S = "UPDATE t_voucher LEFT JOIN t_voucher_row ON t_voucher.Voucher_No = t_voucher_row.Voucher_No SET t_voucher.source_type = 99 WHERE (((t_voucher.source_type)=3 Or (t_voucher.source_type)=6) AND ((t_voucher_row.Voucher_No) Is Null));"
        docommand S
        S = "DELETE t_voucher.source_type FROM t_voucher WHERE (((t_voucher.source_type)=99));"
        docommand S
        S = "UPDATE t_voucher_row INNER JOIN t_voucher ON t_voucher_row.Voucher_No = t_voucher.Voucher_No SET t_voucher_row.Value_Field = 1 WHERE (((t_voucher_row.Value_Field) Is Null) AND ((t_voucher.Source_Type)=3));"
        docommand S
        Me.Label22.Visible = True
        Me.Label22.Caption = "4- اسناد با موفقيت به سيستم دفتر داري ارسال شد"
        Me.Label22.ForeColor = RGB(255, 0, 255)
        DoEvents
End Sub

Private Sub Sendonebyone()
    mystr = "Select * from Voucher where Vdate Between '" & CStr(Mydbase) & "' And '" & CStr(Curdate) & "' Order by Vdate,Vid;"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            MsgBox "در محدوده مورد نظر سندي وجود ندارد"
            Exit Sub
        End If
         S = "UPDATE Voucher INNER JOIN t_voucher ON Voucher.Accvno = t_voucher.Voucher_No SET t_voucher.Voucher_Date = [Voucher].[Vdate] WHERE (((Voucher.Vdate)>='" & CStr(Mydbase) & "') AND ((Voucher.Sended)=True) AND ((t_voucher.Is_Updated)=0));"
         docommand S
         
         S = "DELETE t_voucher_row.*, t_voucher.Voucher_Date, t_voucher.Is_Updated, t_voucher.source_type FROM t_voucher INNER JOIN t_voucher_row ON t_voucher.Voucher_No = t_voucher_row.Voucher_No WHERE (((t_voucher.Voucher_Date)>='" & CStr(Mydbase) & "') AND ((t_voucher.Is_Updated)=0) AND ((t_voucher.source_type)=3));"
         docommand S

        .MoveLast
        .MoveFirst
        While Not .EOF
        Myvno = !Vid
        myvdate = !Vdate
        Accvno = Nz(!Accvno)
        A = "Voucher_no= " & Accvno
        If !Sended = False Or DCount("Voucher_no", "t_voucher", A) = 0 Then
            S = "INSERT INTO t_voucher ( Voucher_Date, Source_Type, Modification_Type, Voucher_Type, Is_Updated, Sumation, Description ) SELECT Vcreate.Vdate, Vcreate.Stype, Vcreate.Fictype, Vcreate.Vtype, Vcreate.Isupdated, Vcreate.S, Vcreate.Sh  FROM Vcreate WHERE (((Vcreate.Vid)=" & Myvno & "));"
            docommand S
            
            Accvno = Nz(DMax("Voucher_no", "t_voucher"))
                    
            S = "UPDATE Voucher SET Voucher.Sended = True, Voucher.Accvno = " & Accvno & " WHERE (((Voucher.Vid)=" & Myvno & "));"
            docommand S
        End If
        
            S = "INSERT INTO t_voucher_row ( Voucher_No, Account_No, Unique_Account_No, Debit, Credit, Description,Exchange_Ammount, Date_Field, Value_Field,Record_no )SELECT " & Accvno & ", Voucher_row.accno, Voucher_row.tafno, Int(Voucher_row.bed), Int(Voucher_row.bes), Voucher_row.rowdesc,  Voucher_row.Curam,Voucher_row.DateField, Voucher_row.ValueField,Voucher_row.Recno FROM Voucher_row WHERE (((Voucher_row.Vid)=" & Myvno & "))ORDER BY Voucher_row.Checkid, Voucher_row.bed DESC , Voucher_row.bes DESC;"
            docommand S
        .MoveNext
        Wend
    End With
    
Lineolds:
        Delorphans
        Me.Label22.Visible = True
        Me.Label22.Caption = "4- اسناد با موفقيت به سيستم دفتر داري ارسال شد"
        Me.Label22.ForeColor = RGB(255, 0, 255)
        DoEvents

End Sub
Public Sub Delorphans()
    S = "DELETE t_voucher_row.*, t_voucher.Voucher_No FROM t_voucher RIGHT JOIN t_voucher_row ON t_voucher.Voucher_No = t_voucher_row.Voucher_No WHERE (((t_voucher.Voucher_No) Is Null));"
    docommand S
End Sub
