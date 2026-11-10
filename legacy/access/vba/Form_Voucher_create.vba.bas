Attribute VB_Name = "Form_Voucher_create"
Attribute VB_Base = "0{E7F4AA38-6F06-4E0E-A6E0-E3272B2988E7}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim Mydbase As String, mys As String, mys0 As String, mys1 As String, mys2 As String, rst As Recordset, rst0 As Recordset, rst1 As Recordset, rst2 As Recordset, Pbaccno As String, Myd As String
Dim Mycode As Byte, Mytbl As String, myfld As String, Myshart As String, Mybabid As Long, Babfld As Long
Dim mydesc As String, Mab As Double, Myvno As Long, Myaccvno As Long, Myvdesc As String
Dim Hasuservoucher As Boolean, Mybed As Double, Mybes As Double, Vrowuser As Long
Dim Praccno As String, Prtafno As String, Lvno As Long, Myvfld As String, Mydfld As String
Dim T As Long, Trecs As Long, Mycheckid As Long, Terrors As Long, Myrecno As Integer
Dim Tarafacc As String, Taraftaf As String, Lastvno As Long, Versedvno As Long, Mycuram As Double
Dim MyChecknazdsandogh As String, MyAsnadnazdsandogh As String, Myjaryanvosool As String
Dim Mybacked As String, Myasnadpardakhtani As String, Myaccentezami As String, Myacctentezami As String
Dim Finaldateacc As String

Private Sub Check1_AfterUpdate()
    If Me.Check1 = True Then
        Me.LastCreatev.Enabled = True
    Else
        Me.LastCreatev.Enabled = False
    End If
End Sub

Private Sub Command19_Click()
    DoCmd.Close
End Sub

Private Sub Command20_Click()
    DoCmd.OpenForm "V_search_m"
End Sub

Private Sub Command8_Click()
    If IsNull(Me.D1) Then
        MsgBox "تاريخ نامشخص است"
        Exit Sub
    End If
    
    If Me.D1 <= Me.LastCreatev Or Me.D1 <= Finaldateacc Then
        MsgBox "اسناد محدوده انتخابي قطعي شده اند"
        Exit Sub
    End If
    
    If MsgBox("اسناد بازسازي شوند؟", vbYesNo) = vbNo Then
        Exit Sub
    Else
        Mydbase = Me.D1
        Me.ProgressBar4.SetFocus
        Me.Command8.Enabled = False
        Me.Command19.Enabled = False
        Me.Command20.Enabled = False
        'updatecard
    End If
    If Controlvouchers(Mydbase) <> 0 Then
        MsgBox "خطاي تشخيص حساب وجود دارد"
        Exit Sub
    End If
    If Nz(DLookup("Editlcv", "Globals"), 0) = 0 Then
        Createvoucher Mydbase
    Else
        CreatevoucherNg Mydbase
    End If
    
    'S = "UPDATE Voucher_row SET Voucher_row.tafno = Null WHERE (((Voucher_row.accno)='1601300002' Or (Voucher_row.accno)='1601300001'));"
    'docommand S
    
    S = "DELETE Voucher.Vid FROM Voucher WHERE (((Voucher.Vdate) Not Between DLookUp('Vfrom','Globals') And DLookUp('Vup','Globals')));"
    docommand S

    S = "DELETE Voucher.Vid FROM Voucher WHERE (((Voucher.Vid) Not In (Select Distinct vid from Voucher_row)));"
    docommand S

    S = "UPDATE Voucher_row INNER JOIN t_accounts ON Voucher_row.accno = t_accounts.Account_Code SET Voucher_row.ValueField = '1' WHERE (((t_accounts.Has_Value_Field)=True) AND ((Voucher_row.ValueField) Is Null));"
    docommand S

    S = "UPDATE Voucher_row INNER JOIN t_accounts ON Voucher_row.accno = t_accounts.Account_Code SET Voucher_row.tafno = Null WHERE (((t_accounts.Has_Unique_Account)=0));"
    docommand S

    S = "UPDATE Voucher_row INNER JOIN t_accounts ON Voucher_row.accno = t_accounts.Account_Code SET Voucher_row.ValueField = '1'WHERE (((Voucher_row.ValueField) Is Null Or (Voucher_row.ValueField)='0')) OR (((Voucher_row.ValueField)=' '));"
    docommand S
    
    Me.Label10.Caption = "درحال مرتب سازي اسناد.لطفامنتظر بمانيد!"
    DoEvents
    
    Orderingvno
    Me.Label10.Caption = "مرتب سازي اسناد انجام شد"
    DoEvents

    S = "UPDATE Voucher_row INNER JOIN [Check] ON Voucher_row.Checkid = Check.Checkid SET Voucher_row.DateField = Nz([Check].[Maturity],'00000000'), Voucher_row.ValueField = Nz([Check].[Maturity])WHERE (((Voucher_row.DateField) Is Null) AND ((Voucher_row.ValueField) Is Null));"
    docommand S
    MsgBox "پايان بازسازي اسناد"
    Me.Command8.Enabled = True
    Me.Command19.Enabled = True
    Me.Command20.Enabled = True
End Sub

Private Sub Createvoucher(Dvoucher As String)
    S = "Delete * from Voucher Where Vdate>= '" & CStr(Dvoucher) & "'"
    docommand S
    
    Set rst = CurrentDb.OpenRecordset("Globals", dbOpenDynaset, dbSeeChanges)
    With rst
        MyChecknazdsandogh = Nz(!Checknazdsandogh)
        MyAsnadnazdsandogh = Nz(!Asnadnazdsandogh)
        Myjaryanvosool = Nz(!Jaryanvosool)
        Mybacked = Nz(!Backedchecks)
        Myasnadpardakhtani = Nz(!asnadpardakhtani)
        Myaccentezami = Nz(!Accentezami)
        Myacctentezami = Nz(!Acctentezami)
    .Close
    End With
    Me.TD.Visible = True
    Me.LD.Visible = True
    Me.T31.Visible = True
    Me.L31.Visible = True
    Me.T32.Visible = True
    Me.L32.Visible = True
    Me.T33.Visible = True
    Me.L33.Visible = True
    Me.T34.Visible = True
    Me.L34.Visible = True
    Me.T35.Visible = True
    Me.L35.Visible = True
    'Lastvno = Nz(DMax("VNo", "Voucher"))
     Vnotchecks
     Vchecksd
     Vchecksp
End Sub
Private Sub Vchecksp()
    'mys0 = "SELECT Check.D1 FROM [Check] WHERE (((Check.Dp) = 2) And ((Check.Checktype) <> 9) And ((Check.Paied) = True)And checkno='100101')GROUP BY Check.D1 HAVING (((Check.D1) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D1;"
    'mys0 = "SELECT Check.D1 FROM [Check] WHERE (((Check.Dp) = 2) And ((Check.Checktype) <> 9) And ((Check.Paied) = True))GROUP BY Check.D1 HAVING (((Check.D1) = '13820120')) ORDER BY Check.D1;"
    If Nz(DLookup("Vforvoids", "Globals"), True) = True Then
        mys0 = "SELECT Check.D1 FROM [Check] WHERE (((Check.Dp) = 2) And ((Check.Checktype) <> 9) And ((Check.Paied) = True))GROUP BY Check.D1 HAVING (((Check.D1) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D1;"
    Else
        mys0 = "SELECT Check.D1 FROM [Check] WHERE (((Check.Dp) = 2) And ((Check.Checktype) <> 9) And ((Check.Paied) = True)  And ((Check.Ebtorkharj) = false))GROUP BY Check.D1 HAVING (((Check.D1) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D1;"
    End If
    Set rst0 = CurrentDb.OpenRecordset(mys0, dbOpenDynaset, dbSeeChanges)
    With rst0
        If .RecordCount = 0 Then
            GoTo Linelongtocash
        Else
            .MoveLast
            .MoveFirst
            T = 0
            Trecs = .RecordCount
            Me.ProgressBar4.Value = 0
            Me.Label10.Visible = True
            Me.Label10.Caption = "درحال ايجاد اسناد حسابداري چكها/اسنادپرداختني صادره"
            Me.Label11.Visible = True
            Me.Label11.Caption = 0
        End If
     While Not .EOF
         Myd = !D1
         Me.TD = Myd
         Mycode = 21
         Me.ProgressBar4.Value = (T / Trecs) * 100
         Label11.Caption = Int(ProgressBar4.Value) + 1 & " %"
         DoEvents
         
         Myvno = Newvoucher(Myd, Mycode)
         'mys1 = "SELECT * FROM Check WHERE D1 ='" & CStr(Myd) & "' And Dp = 2 And Paied = True And Checktype<>9 And checkno='100101'ORDER BY D1;"
         'mys1 = "SELECT * FROM Check WHERE D1 ='" & CStr(Myd) & "' And Dp = 2 And Paied = True And Checktype<>9 And d1='13820120'ORDER BY Checkid;"
         If Nz(DLookup("Vforvoids", "Globals"), True) = True Then
            mys1 = "SELECT * FROM Check WHERE D1 ='" & CStr(Myd) & "' And Dp = 2 And Paied = True And Checktype<>9 ORDER BY Checkid;"
         Else
            mys1 = "SELECT * FROM Check WHERE D1 ='" & CStr(Myd) & "' And Dp = 2 And Paied = True And Checktype<>9 And Ebtorkharj=false ORDER BY Checkid;"
         End If
         
         Set rst1 = CurrentDb.OpenRecordset(mys1, dbOpenDynaset, dbSeeChanges)
         With rst1
              If .RecordCount = 0 Then
                  GoTo Line10
              Else
                .MoveLast
                .MoveFirst
             End If
                While Not .EOF
                Me.T31 = !Checkno
                Me.T32 = !Person
                Me.T33 = !Amount
                Me.T34 = !Maturity
                Me.T35 = !Babat
                Mybabid = Nz(!Babatid)
                DoEvents
                
                    mys = "Select * from Bankacc where Baccno='" & CStr(Nz(!Baccno)) & "'"
                    Set rst = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                    With rst
                        If .RecordCount = 0 Then
                            Praccno = 0
                            Prtafno = 0
                        Else
                            Praccno = Nz(!Accno)
                            Prtafno = Nz(!Tafno)
                        End If
                    .Close
                    End With
                    
                    If !Checktype = 0 Then
                        Tarafacc = Praccno
                        Taraftaf = Prtafno
                    Else
                        Tarafacc = Nz(!Accnotcash)
                        Taraftaf = Nz(!Tafnotcash)
                    End If
                    
                    If !Iscur = True Then
                        Mab = Round(Nz(!Amount) * Nz(!Currate))
                        Myvfld = Nz(!Checkno)
                        Mycuram = Nz(!Amount)
                    Else
                        Mab = Nz(!Amount)
                        Myvfld = Nz(!Checkno, 0)
                        Mycuram = 0
                    End If
                    '***************8
                    Mycheckid = !Checkid
                    
                    Select Case !Checktype
                        Case 0
                            mychecktype = " چك"
                        Case 1
                            mychecktype = " چك انتظامي"
                        Case 2
                            mychecktype = " چك مدت دار"
                        Case 3
                            mychecktype = " اقساط پرداختني"
                        Case 4
                            mychecktype = " سفته پرداختني"
                        Case 5
                            mychecktype = " تعهدات پرداختني"
                        Case 6
                            mychecktype = " سفته انتظامي"
                    End Select
                    If !Checktype <= 2 Then
                        mydesc = mychecktype & Nz(!Checkno) & " دروجه " & Nz(!Person, "") & " بابت " & Nz(!Babat, "")
                        Createvoucherrows 0, Mycode, !Checkid, Myvno, Nz(!Accno), Nz(!Tafno), mydesc, Mab, Mycuram, Nz(!Maturity, "0000/00/00"), Myvfld, Mybabid, Tarafacc, Taraftaf, False
                    Else
                        If Nz(!GoInlaw, False) = False Then
                            mydesc = mychecktype & Nz(!Checkno) & " دروجه " & Nz(!Person, "") & " بابت " & Nz(!Babat, "")
                            Createvoucherrows 0, Mycode, !Checkid, Myvno, Nz(!Accno), Nz(!Tafno), mydesc, Mab, Mycuram, Nz(!Maturity, "0000/00/00"), Myvfld, Mybabid, Tarafacc, Taraftaf, False
                        End If
                    End If
                    .Edit
                    !V1 = Myvno
                    .Update
                     Lastvno = 0
                    .MoveNext
                    Wend
                 End With
Line10:
     .MoveNext
      T = T + 1
     Wend
    .Close
    End With

 'Change long checks to Paiedchecks
Linelongtocash:
    If Nz(DLookup("Vforvoids", "Globals"), True) = True Then
        mys0 = "SELECT Check.D3 FROM [Check] WHERE (((Check.Dp) = 2) And ((Check.Checktype) not in(0, 9)) And ((Check.Paied) = True)And ((Check.Ctocash) = True))GROUP BY Check.D3 HAVING (((Check.D3) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D3;"
    Else
        mys0 = "SELECT Check.D3 FROM [Check] WHERE (((Check.Dp) = 2) And ((Check.Checktype) not in(0, 9)) And ((Check.Paied) = True)And ((Check.Ctocash) = True)And ((Check.Ebtorkharj) = false))GROUP BY Check.D3 HAVING (((Check.D3) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D3;"
    End If
    
    Set rst0 = CurrentDb.OpenRecordset(mys0, dbOpenDynaset, dbSeeChanges)
    With rst0
        If .RecordCount = 0 Then
            GoTo Linevoidingcheck
        Else
            .MoveLast
            .MoveFirst
            T = 0
            Trecs = .RecordCount
            Me.ProgressBar4.Value = 0
            Me.Label10.Visible = True
            Me.Label10.Caption = "درحال ايجاد اسناد حسابداري چكها/اسنادپرداختني تبديل شده به نقد"
            Me.Label11.Visible = True
            Me.Label11.Caption = 0
        End If
     While Not .EOF
         Myd = Nz(!D3)
         Me.TD = Myd
         Mycode = 25
         Me.ProgressBar4.Value = (T / Trecs) * 100
         Label11.Caption = Int(ProgressBar4.Value) + 1 & " %"
         DoEvents
         
         Myvno = Newvoucher(Myd, Mycode)
         If Nz(DLookup("Vforvoids", "Globals"), True) = True Then
            mys1 = "SELECT * FROM Check WHERE D3 ='" & CStr(Myd) & "' And Dp = 2 And Paied = True And Checktype not in (0,9) and Ctocash= true ORDER BY Checkid;"
         Else
            mys1 = "SELECT * FROM Check WHERE D3 ='" & CStr(Myd) & "' And Dp = 2 And Paied = True And Checktype not in (0,9) and Ebtprkharj= false and Ctocash= true ORDER BY Checkid;"
         End If
         Set rst1 = CurrentDb.OpenRecordset(mys1, dbOpenDynaset, dbSeeChanges)
         With rst1
              If .RecordCount = 0 Then
                  GoTo Line20
              Else
                .MoveLast
                .MoveFirst
             End If
                While Not .EOF
                Me.T31 = !Checkno
                Me.T32 = !Person
                Me.T33 = !Amount
                Me.T34 = !Maturity
                Me.T35 = !Babat
                Mybabid = Nz(!Babatid)
                DoEvents
                
                    mys = "Select * from Bankacc where Baccno='" & CStr(Nz(!Baccno)) & "'"
                    Set rst = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                    With rst
                        If .RecordCount = 0 Then
                            Praccno = 0
                            Prtafno = 0
                        Else
                            Praccno = Nz(!Accno)
                            Prtafno = Nz(!Tafno)
                        End If
                    .Close
                    End With
                    
                    If !Checktype = 0 Then
                        Tarafacc = Praccno
                        Taraftaf = Prtafno
                    Else
                        Tarafacc = Nz(!Accnotcash)
                        Taraftaf = Nz(!Tafnotcash)
                    End If
                    
                  If !Iscur = True Then
                        Mab = Round(Nz(!Amount) * Nz(!Currate))
                        Myvfld = Nz(!Checkno)
                        Mycuram = Nz(!Amount)
                    Else
                        Mab = Nz(!Amount)
                        Myvfld = Nz(!Checkno)
                        Mycuram = 0

                    End If
                    '***************8
                    Mycheckid = !Checkid
                    
                    Select Case !Checktype
                        Case 0
                            mychecktype = " چك"
                        Case 1
                            mychecktype = " چك انتظامي"
                        Case 2
                            mychecktype = " چك مدت دار"
                        Case 3
                            mychecktype = " اقساط پرداختني"
                        Case 4
                            mychecktype = " سفته پرداختني"
                        Case 5
                            mychecktype = " تعهدات پرداختني"
                        Case 6
                            mychecktype = " سفته انتظامي"
                    End Select
                    
                    mydesc = "تبديل " & mychecktype & Nz(!Checkno) & " دروجه " & Nz(!Person, "") & " به چك نقدي "
                    Createvoucherrows 0, Mycode, !Checkid, Myvno, Tarafacc, Taraftaf, mydesc, Mab, Mycuram, Nz(!Maturity, "00000000"), Myvfld, Mybabid, Praccno, Prtafno, True
                     
                    .Edit
                    !V3 = Myvno
                    .Update
                     Lastvno = 0
                    .MoveNext
                    Wend
                 End With
Line20:
     .MoveNext
      T = T + 1
     Wend
.Close
End With
    
 'Voiding Checks
Linevoidingcheck:
    If Nz(DLookup("Vforvoids", "Globals"), True) = True Then
        mys0 = "SELECT Check.D2 FROM [Check] WHERE (((Check.Dp) = 2) And ((Check.Checktype) <>9) And ((Check.Paied) = True)And ((Check.Ebtorkharj) = True))GROUP BY Check.D2 HAVING (((Check.D2) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D2;"
    Else
        mys0 = "SELECT Check.D2 FROM [Check] WHERE (((Check.Dp) = 2) And ((Check.Checktype) <>9) And ((Check.Paied) = True)And ((Check.Ebtorkharj) = false))GROUP BY Check.D2 HAVING (((Check.D2) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D2;"
    End If
    Set rst0 = CurrentDb.OpenRecordset(mys0, dbOpenDynaset, dbSeeChanges)
    With rst0
        If .RecordCount = 0 Then
            Exit Sub
        Else
            .MoveLast
            .MoveFirst
            T = 0
            Trecs = .RecordCount
            Me.ProgressBar4.Value = 0
            Me.Label10.Visible = True
            Me.Label10.Caption = "درحال ايجاد اسناد حسابداري ابطال چكها/اسنادپرداختني "
            Me.Label11.Visible = True
            Me.Label11.Caption = 0
        End If
     While Not .EOF
         Myd = Nz(!D2)
         Me.TD = Myd
         Mycode = 22
         Me.ProgressBar4.Value = (T / Trecs) * 100
         Label11.Caption = Int(ProgressBar4.Value) + 1 & " %"
         DoEvents
         
         Myvno = Newvoucher(Myd, Mycode)
         If Nz(DLookup("Vforvoids", "Globals"), True) = True Then
           mys1 = "SELECT * FROM Check WHERE D2 ='" & CStr(Myd) & "' And Dp = 2 And Paied = True And Checktype <>9 and Ebtorkharj= True ORDER BY Checkid;"
         Else
           mys1 = "SELECT * FROM Check WHERE D2 ='" & CStr(Myd) & "' And Dp = 2 And Paied = True And Checktype <>9 and Ebtorkharj= false ORDER BY Checkid;"
         End If
         Set rst1 = CurrentDb.OpenRecordset(mys1, dbOpenDynaset, dbSeeChanges)
         With rst1
              If .RecordCount = 0 Then
                  GoTo Line30
              Else
                .MoveLast
                .MoveFirst
             End If
                While Not .EOF
                Me.T31 = !Checkno
                Me.T32 = !Person
                Me.T33 = !Amount
                Me.T34 = !Maturity
                Me.T35 = !Babat
                Mybabid = Nz(!Babatid)
                DoEvents
                    mys = "Select * from Bankacc where Baccno='" & CStr(Nz(!Baccno)) & "'"
                    Set rst = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                    With rst
                        If .RecordCount = 0 Then
                            Praccno = 0
                            Prtafno = 0
                        Else
                            Praccno = Nz(!Accno)
                            Prtafno = Nz(!Tafno)
                        End If
                    .Close
                    End With
                    
                    If !Checktype = 0 Then
                        Tarafacc = Praccno
                        Taraftaf = Prtafno
                    Else
                        If !Ctocash = True Then
                            Tarafacc = Praccno
                            Taraftaf = Prtafno
                        Else
                            Tarafacc = Nz(!Accnotcash)
                            Taraftaf = Nz(!Tafnotcash)
                        End If
                    End If
                    
                    If !Iscur = True Then
                        Mab = Round(Nz(!Amount) * Nz(!Currate))
                        Myvfld = Nz(!Checkno)
                        Mycuram = Nz(!Amount)
                    Else
                        Mab = Nz(!Amount)
                        Myvfld = Nz(!Checkno)
                        Mycuram = 0
                    End If
                    '***************8
                    Mycheckid = !Checkid
                    Select Case !Checktype
                        Case 0
                            mychecktype = " چك"
                        Case 1
                            mychecktype = " چك انتظامي"
                        Case 2
                            mychecktype = " چك مدت دار"
                        Case 3
                            mychecktype = " اقساط پرداختني"
                        Case 4
                            mychecktype = " سفته پرداختني"
                        Case 5
                            mychecktype = " تعهدات پرداختني"
                        Case 6
                            mychecktype = " سفته انتظامي"
                    End Select
                    
                        mydesc = "ابطال " & mychecktype & Nz(!Checkno) & " صادره دروجه " & Nz(!Person)
                        Createvoucherrows 0, Mycode, !Checkid, Myvno, Tarafacc, Taraftaf, mydesc, Mab, Mycuram, Nz(!Maturity, "00000000"), Myvfld, Mybabid, Nz(!Accno), Nz(!Tafno), False
                    .Edit
                    !V2 = Myvno
                    .Update
                     Lastvno = 0
                    .MoveNext
                    Wend
                 End With
Line30:
     .MoveNext
      T = T + 1
     Wend
    .Close
    End With
End Sub
Private Sub Vchecksd()
    'mys0 = "SELECT Check.D1 FROM [Check] WHERE (((Check.Dp) = 1)And Stcode=14)GROUP BY Check.D1 HAVING (((Check.D1) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D1;"
     mys0 = "SELECT Check.D1 FROM [Check] WHERE (((Check.Dp) = 1))GROUP BY Check.D1 HAVING (((Check.D1) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D1;"
    Set rst0 = CurrentDb.OpenRecordset(mys0, dbOpenDynaset, dbSeeChanges)
    With rst0
        If .RecordCount = 0 Then
            GoTo Linevag
        Else
            .MoveLast
            .MoveFirst
            T = 0
            Trecs = .RecordCount
            Me.ProgressBar4.Value = 0
            Me.Label10.Visible = True
            Me.Label10.Caption = "درحال ايجاد اسناد حسابداري چكها/اسناددريافتني"
            Me.Label11.Visible = True
            Me.Label11.Caption = 0
        End If
     While Not .EOF
         Myd = !D1
         Mycode = 11
         Me.TD = Myd
         Me.ProgressBar4.Value = (T / Trecs) * 100
         Label11.Caption = Int(ProgressBar4.Value) + 1 & " %"
         DoEvents
         
         Myvno = Newvoucher(Myd, Mycode)
         'mys1 = "SELECT * FROM Check WHERE D1 ='" & CStr(Myd) & "' And Dp = 1  and stcode=14 ORDER BY Checkid;"
         mys1 = "SELECT * FROM Check WHERE D1 ='" & CStr(Myd) & "' And Dp = 1  ORDER BY Checkid;"
         Set rst1 = CurrentDb.OpenRecordset(mys1, dbOpenDynaset, dbSeeChanges)
         With rst1
              If .RecordCount = 0 Then
                  GoTo Line10
              Else
                .MoveLast
                .MoveFirst
             End If
                While Not .EOF
                    Me.T31 = !Checkno
                    Me.T32 = !Person
                    Me.T33 = !Amount
                    Me.T34 = !Maturity
                    Me.T35 = !Babat
                    Mybabid = Nz(!Babatid)
                    DoEvents
                    If !Iscur = True Then
                        Mab = Round(Nz(!Amount) * Nz(!Currate))
                        Myvfld = Nz(!Checkno)
                        Mycuram = Nz(!Amount)
                    Else
                        Mab = Nz(!Amount)
                        Myvfld = Nz(!Checkno)
                        Mycuram = 0
                    End If
                    '***************8
                    Mycheckid = !Checkid
                    Select Case !Checktype
                        Case 0
                            mychecktype = "چك عادي "
                        Case 1
                            mychecktype = " چك انتظامي"
                        Case 2
                            mychecktype = "(چك رمزدار(بانكي"
                        Case 3
                            mychecktype = " اقساط دريافتني"
                        Case 4
                            mychecktype = " سفته دريافتني"
                        Case 5
                            mychecktype = " تعهدات دريافتني"
                        Case 6
                            mychecktype = " سفته انتظامي"
                    End Select
                    
                    mydesc = mychecktype & Nz(!Checkno) & " س " & Format(Nz(!Maturity, "00000000"), "@@@@/@@/@@") & " از " & Nz(!Person) & " بابت " & Nz(!Babat)
                     
                    Createvoucherrows 0, Mycode, !Checkid, Myvno, Nz(!Accno), Nz(!Tafno), mydesc, Mab, Mycuram, Nz(!Maturity, "0000/00/00"), Myvfld, Mybabid, Nz(!Accnotcash), Nz(!Tafnotcash), False
                     
                    .Edit
                    !V1 = Myvno
                    .Update
                     Lastvno = 0
                    .MoveNext
                    Wend
                 End With
Line10:
     .MoveNext
      T = T + 1
     Wend
    .Close
    End With
    
'********************************************8
' Vagozari Checks
Linevag:
    mys0 = "SELECT Check.D3 FROM [Check] WHERE (((Check.Dp) = 1))GROUP BY Check.D3 HAVING (((Check.D3) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D3;"
    Set rst0 = CurrentDb.OpenRecordset(mys0, dbOpenDynaset, dbSeeChanges)
    With rst0
        If .RecordCount = 0 Then
            GoTo LineVos
        Else
            .MoveLast
            .MoveFirst
            T = 0
            Trecs = .RecordCount
            Me.ProgressBar4.Value = 0
            Me.Label10.Visible = True
            Me.Label10.Caption = "درحال ايجاد اسناد حسابداري چكها/اسناددريافتني واگذارشده/خرج شده/عودت شده"
            Me.Label11.Visible = True
            Me.Label11.Caption = 0
        End If
     While Not .EOF
         Myd = !D3
         Mycode = 12
         Me.TD = Myd
         Me.ProgressBar4.Value = (T / Trecs) * 100
         Label11.Caption = Int(ProgressBar4.Value) + 1 & " %"
         DoEvents
         
         Myvno = Newvoucher(Myd, Mycode)
         mys1 = "SELECT * FROM Check WHERE D3 ='" & CStr(Myd) & "' And Dp = 1 ORDER BY Checkid;"
         Set rst1 = CurrentDb.OpenRecordset(mys1, dbOpenDynaset, dbSeeChanges)
         With rst1
              If .RecordCount = 0 Then
                  GoTo Line100
              Else
                .MoveLast
                .MoveFirst
             End If
                While Not .EOF
                    Me.T31 = !Checkno
                    Me.T32 = !Person
                    Me.T33 = !Amount
                    Me.T34 = !Maturity
                    Me.T35 = !Babat
                    Mybabid = 0
                    DoEvents
                    If !Iscur = True Then
                        Mab = Round(Nz(!Amount) * Nz(!Currate))
                        Myvfld = Nz(!Checkno)
                        Mycuram = Nz(!Amount)
                    Else
                        Mab = Nz(!Amount)
                        Myvfld = Nz(!Checkno)
                        Mycuram = 0
                    End If
                    '***************8
                    Mycheckid = !Checkid
                    Select Case !Checktype
                        Case 0
                            mychecktype = "چك عادي "
                        Case 1
                            mychecktype = " چك انتظامي"
                        Case 2
                            mychecktype = "(چك رمزدار(بانكي"
                        Case 3
                            mychecktype = " اقساط دريافتني"
                        Case 4
                            mychecktype = " سفته دريافتني"
                        Case 5
                            mychecktype = " تعهدات دريافتني"
                        Case 6
                            mychecktype = " سفته انتظامي"
                   End Select
                    
                    
                    Select Case !Stcode
                        Case 12, 13, 14
                        mys = "Select * from Bankacc where Baccno='" & CStr(Nz(!Vagno)) & "'"
                        Set rst = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                        With rst
                            If .RecordCount = 0 Then
                                Praccno = 0
                                Prtafno = 0
                            Else
                                Praccno = Nz(!Accno)
                                Prtafno = Nz(!Tafno)
                                Myjaryanvosool = Nz(!Vagno, Myjaryanvosool)
                            End If
                        .Close
                        End With
                        
                            mydesc = "واگذاري چك " & !Checkno & " به حساب"
                            Createvoucherrows 0, 12, !Checkid, Myvno, Nz(!Accnotcash), Nz(!Tafnotcash), mydesc, Mab, Mycuram, Nz(!Maturity, "0000/00/00"), Myvfld, Mybabid, Myjaryanvosool, Prtafno, True
                        Case 15
                            mydesc = "عودت " & mychecktype & Nz(!Checkno) & " س " & Format(Nz(!Maturity, "00000000"), "@@@@/@@/@@") & " به " & Nz(!Person)
                            Createvoucherrows 0, 18, !Checkid, Myvno, Nz(!Accnotcash), Nz(!Tafnotcash), mydesc, Mab, Mycuram, Nz(!Maturity, "0000/00/00"), Myvfld, Mybabid, Nz(!Accno), Nz(!Tafno), True
                        Case 16
                            mys = "Select * from t_unique_accounts where Account_Code='" & CStr(Nz(!TafKharj)) & "'"
                            Set rst = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                            With rst
                                If .RecordCount = 0 Then
                                    Prtafno = 0
                                Else
                                    Prtafno = Nz(!Account_Name)
                                End If
                            .Close
                            End With
                            mydesc = "واگذاري(خرج) " & mychecktype & Nz(!Checkno) & " س " & Format(Nz(!Maturity, "00000000"), "@@@@/@@/@@") & " به " & Prtafno
                            Createvoucherrows 0, 19, !Checkid, Myvno, Nz(!Accnotcash), Nz(!Tafnotcash), mydesc, Mab, Mycuram, Nz(!Maturity, "0000/00/00"), Myvfld, Mybabid, Nz(!Acckharj), Nz(!TafKharj), False
                    End Select
                    .Edit
                    !V3 = Myvno
                    .Update
                     Lastvno = 0
                    .MoveNext
                    Wend
                 End With
Line100:
     .MoveNext
      T = T + 1
     Wend
    .Close
    End With
'**********************************************
' Vosool Checks
LineVos:
    mys0 = "SELECT Check.D4 FROM [Check] WHERE (((Check.Dp) = 1))GROUP BY Check.D4 HAVING (((Check.D4) >= '" & CStr(Mydbase) & "')) ORDER BY Check.D4;"
    Set rst0 = CurrentDb.OpenRecordset(mys0, dbOpenDynaset, dbSeeChanges)
    With rst0
        If .RecordCount = 0 Then
            Exit Sub
        Else
            .MoveLast
            .MoveFirst
            T = 0
            Trecs = .RecordCount
            Me.ProgressBar4.Value = 0
            Me.Label10.Visible = True
            Me.Label10.Caption = "درحال ايجاد اسناد حسابداري وصول/برگشت چكها/اسناددريافتني "
            Me.Label11.Visible = True
            Me.Label11.Caption = 0
        End If
     While Not .EOF
         Myd = !D4
         Mycode = 13
         Me.TD = Myd
         Me.ProgressBar4.Value = (T / Trecs) * 100
         Label11.Caption = Int(ProgressBar4.Value) + 1 & " %"
         DoEvents
         
         Myvno = Newvoucher(Myd, Mycode)
         mys1 = "SELECT * FROM Check WHERE D4 ='" & CStr(Myd) & "' And Dp = 1  ORDER BY Checkid;"
         Set rst1 = CurrentDb.OpenRecordset(mys1, dbOpenDynaset, dbSeeChanges)
         With rst1
              If .RecordCount = 0 Then
                  GoTo Line200
              Else
                .MoveLast
                .MoveFirst
             End If
                While Not .EOF
                    Me.T31 = !Checkno
                    Me.T32 = !Person
                    Me.T33 = !Amount
                    Me.T34 = !Maturity
                    Me.T35 = !Babat
                    Mybabid = 0
                    DoEvents
                    
                    If !Iscur = True Then
                        Mab = Round(Nz(!Amount) * Nz(!Currate))
                        Myvfld = Nz(!Checkno)
                        Mycuram = Nz(!Amount)
                    Else
                        Mab = Nz(!Amount)
                        Myvfld = Nz(!Checkno)
                        Mycuram = 0
                    End If
                    '***************8
                    
                    Mycheckid = !Checkid
                    Select Case !Checktype
                        Case 0
                            mychecktype = "چك عادي "
                        Case 1
                            mychecktype = " چك انتظامي"
                        Case 2
                            mychecktype = "(چك رمزدار(بانكي"
                        Case 3
                            mychecktype = " اقساط دريافتني"
                        Case 4
                            mychecktype = " سفته دريافتني"
                        Case 5
                            mychecktype = " تعهدات دريافتني"
                        Case 6
                            mychecktype = " سفته انتظامي"
                   End Select
                    mys2 = !Checkno & "دريافتي از " & Nz(!Person)
                    
                    mys = "Select * from Bankacc where Baccno='" & CStr(Nz(!Vagno)) & "'"
                    Set rst = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                    With rst
                        If .RecordCount = 0 Then
                            Praccno = 0
                            Prtafno = 0
                        Else
                            Praccno = Nz(!Accno)
                            Prtafno = Nz(!Tafno)
                        End If
                    .Close
                    End With
                        
                    Select Case !Stcode
                        Case 13
                                mydesc = "وصول چك " & mys2
                                Createvoucherrows 0, 13, !Checkid, Myvno, Myjaryanvosool, Prtafno, mydesc, Mab, Mycuram, Nz(!Maturity, "00000000"), Myvfld, Mybabid, Praccno, Prtafno, True
                        Case 14
                                mydesc = "برگشت چك " & mys2
                                Createvoucherrows 0, 13, !Checkid, Myvno, Myjaryanvosool, Prtafno, mydesc, Mab, Mycuram, Nz(!Maturity, "00000000"), Myvfld, Mybabid, Mybacked, Nz(!Tafnotcash), False
                    End Select
                    .Edit
                    !V4 = Myvno
                    .Update
                     Lastvno = 0
                    .MoveNext
                    Wend
                 End With
Line200:
     .MoveNext
      T = T + 1
     Wend
    .Close
    End With
    
End Sub

Private Sub Vnotchecks()
    mys0 = "SELECT Subs.Sdate FROM Subs WHERE (((Subs.Scode) <> 10) And ((Subs.Card) = False))GROUP BY Subs.Sdate HAVING (((Subs.Sdate) >= '" & CStr(Mydbase) & "'))ORDER BY Subs.Sdate;"
    Set rst0 = CurrentDb.OpenRecordset(mys0, dbOpenDynaset, dbSeeChanges)
    With rst0
    If .RecordCount = 0 Then
        Exit Sub
    Else
        .MoveLast
        .MoveFirst
        T = 0
        Trecs = .RecordCount
        Me.ProgressBar4.Value = 0
        Me.Label10.Visible = True
        Me.Label10.Caption = "درحال ايجاد اسناد حسابداري واريزوبرداشت از حسابها/دريافت و پرداخت نقدي"
        Me.Label11.Visible = True
        Me.Label11.Caption = 0
    End If
     While Not .EOF
        Myd = !Sdate
        Me.TD = Myd
        Me.ProgressBar4.Value = (T / Trecs) * 100
        Label11.Caption = Int(ProgressBar4.Value) + 1 & " %"
        DoEvents
        
         mys1 = "SELECT Subs.Scode FROM Subs WHERE (((Subs.Sdate)='" & CStr(Myd) & "') AND ((Subs.Scode)<>10) AND ((Subs.Card)=False))GROUP BY Subs.Scode ORDER BY Subs.Scode;"
         Set rst1 = CurrentDb.OpenRecordset(mys1, dbOpenDynaset, dbSeeChanges)
         With rst1
              If .RecordCount = 0 Then
                  GoTo Line10
              Else
                .MoveLast
                .MoveFirst
             End If
                While Not .EOF
                    Mycode = !Scode
                    Myvno = Newvoucher(Myd, Mycode)
                    mys2 = "SELECT * FROM Subs WHERE Sdate = '" & CStr(Myd) & "' And Scode= " & Mycode & " ORDER BY Scode;"
                    Set rst2 = CurrentDb.OpenRecordset(mys2, dbOpenDynaset, dbSeeChanges)
                    With rst2
                        If .RecordCount = 0 Then
                            GoTo line5
                        Else
                            .MoveLast
                            .MoveFirst
                        End If
                    While Not .EOF
                        Me.T31 = !Checkno
                        Me.T32 = !Person
                        Me.T33 = IIf(!bed = 0, !bes, !bed)
                        Me.T34 = !Basetype
                        Me.T35 = !Babat
                        Mydfld = Nz(!Sdate)
                        Myvfld = Nz(!Checkno, 1)
                        Mybabid = Nz(!Babatid)
                        DoEvents

                        mys = "Select * from Bankacc where Baccno='" & CStr(Nz(!Baccno)) & "'"
                        Set rst = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
                        With rst
                            If .RecordCount = 0 Then
                                Praccno = 0
                                Prtafno = 0
                            Else
                                Praccno = Nz(!Accno)
                                Prtafno = Nz(!Tafno)
                            End If
                        .Close
                        End With
                        
                    Select Case Mycode
                        Case 16
                            mydesc = "واريز وجه توسط  " & !Person & " بابت " & !Babat
                        Case 17
                            mydesc = "دريافت نقدي از " & !Person & " بابت " & !Babat
                        Case 26
                            mydesc = " برداشت ازحساب توسط  " & !Person & " بابت " & !Babat
                        Case 27
                            mydesc = " پرداخت نقدي به " & !Person & " بابت " & !Babat
                    End Select
                    
                    If !Iscur = True Then
                        If Mycode = 16 Or Mycode = 17 Then
                            Mab = Round(Nz(!bed) * Nz(!Currate))
                            Mycuram = Nz(!bed)
                        Else
                            Mab = Round(Nz(!bes) * Nz(!Currate))
                            Mycuram = Nz(!bes)
                        End If
                    Else
                        If Mycode = 16 Or Mycode = 17 Then
                            Mab = Nz(!bed)
                        Else
                            Mab = Nz(!bes)
                        End If
                    Mycuram = 0
                    End If
                    If IsNull(Myvfld) Or Myvfld = "" Then Myvfld = 1
                    
                    Createvoucherrows 1, !Scode, !Sid, Myvno, Nz(!Accno), Nz(!Tafno), mydesc, Mab, Mycuram, Mydfld, Myvfld, Mybabid, Praccno, Prtafno, False
                    
                    .Edit
                    !Vno = Myvno
                    .Update
                    .MoveNext
                    Wend
                 End With
line5:
                .MoveNext
                Wend
        .Close
        End With
Line10:
     .MoveNext
      T = T + 1
     Wend
    .Close
    End With
End Sub

'Private Sub Createvoucherrows(Cors As Byte, Itid As Byte, Csid As Long, Vno As Long, acc As String, taf As String, mydesc As String, mymab As Double, Curfld As Double, Dfld As String, Vfld As String, Bfld As Long, tacc As String, ttaf As String, Noconsidervuser As Boolean)
'    If Cors = 0 Then
'        myfld = "[Checkid]"
'        Myshart = "[Checkid]=" & Csid
'    Else
'        myfld = "[Sid]"
'        Myshart = "[Sid]=" & Csid
'    End If
'
'    Select Case Itid
'        Case 11, 12, 13, 16, 17, 18, 19
'            Mybed = mymab
'            Mybes = 0
'            S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc,Curam, DateField, ValueField,Bid) SELECT 0," & Vno & " ," & Csid & ",'" & CStr(tacc) & "','" & CStr(ttaf) & "'," & Mybed & ", " & Mybes & ",'" & CStr(mydesc) & "'," & Curfld & " ,'" & CStr(Dfld) & "','" & CStr(Vfld) & "'," & Bfld & ";"
'            docommand S
'        Case 21, 22, 25, 26, 27
'            Mybed = 0
'            Mybes = mymab
'    End Select
'    Vrowuser = DCount(myfld, "Povouch", Myshart)
'    If Vrowuser = 0 Or Noconsidervuser = True Then
'
'        S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc, Curam,DateField, ValueField,Bid) SELECT 0," & Vno & " ," & Csid & ",'" & CStr(acc) & "','" & CStr(taf) & "'," & Mybes & " , " & Mybed & ",'" & CStr(mydesc) & "'," & Curfld & " ,'" & CStr(Dfld) & "','" & CStr(Vfld) & "'," & Bfld & ";"
'        docommand S
'    Else
'
'       Insfrompovouch Cors, Csid, Vno
'    End If
'    Select Case Itid
'        Case 21, 22, 25, 26, 27
'        S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc,Curam, DateField, ValueField,Bid) SELECT 0," & Vno & " ," & Csid & ",'" & CStr(tacc) & "','" & CStr(ttaf) & "'," & Mybed & ", " & Mybes & ",'" & CStr(mydesc) & "'," & Curfld & " ,'" & CStr(Dfld) & "','" & CStr(Vfld) & "'," & Bfld & ";"
'        docommand S
'    End Select
'End Sub

'Private Sub Insfrompovouch(Cors As Byte, Csid As Long, Vno As Long)
'
'    If Cors = 0 Then
'        S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc,Bid ) SELECT 0 ," & Vno & ", Povouch.Checkid, Povouch.accno, Povouch.tafno, Povouch.bed, Povouch.bes, Povouch.rowdesc, Povouch.Bid FROM Povouch WHERE (((Povouch.Checkid)=" & Csid & "));"
'    Else
'        S = "INSERT INTO Voucher_row ( Recno,Vid, Checkid, accno, tafno, bed, bes, rowdesc,Bid ) SELECT 0 ," & Vno & ", Povouch.Sid, Povouch.accno, Povouch.tafno, Povouch.bed, Povouch.bes, Povouch.rowdesc, Povouch.Bid FROM Povouch WHERE (((Povouch.Sid)=" & Csid & "));"
'    End If
'    docommand S
'End Sub

Private Sub Orderingvno()
    mys = "Vdate<'" & CStr(Mydbase) & "'"
    Lvno = Nz(DMax("Vno", "Voucher", mys))
    mys = "Select * from Voucher where vdate>='" & CStr(Mydbase) & "'Order by Vdate,Vgroup"
    Set rst = CurrentDb.OpenRecordset(mys, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            Exit Sub
        Else
            .MoveLast
            .MoveFirst
            While Not .EOF
            .Edit
                !Vno = Lvno + 1
            .Update
            Myrecno = 0
             S = "SELECT Voucher_row.* FROM Voucher_row WHERE (((Voucher_row.Vid) = " & Nz(!Vid) & "))ORDER BY Voucher_row.Sqlid;"
                Set rst1 = CurrentDb.OpenRecordset(S, dbOpenDynaset, dbSeeChanges)
                With rst1
                    If .RecordCount = 0 Then
                        GoTo lineA
                    Else
                        .MoveLast
                        .MoveFirst
                        While Not .EOF
                            .Edit
                            !Recno = Myrecno + 1
                            .Update
                             Myrecno = Myrecno + 1
                            .MoveNext
                        Wend
                    End If
                .Close
                End With
lineA:
             Lvno = Lvno + 1
            .MoveNext
            Wend
        End If
    .Close
    End With
End Sub
Private Function Versevoucher(Newdate As String, Itcode As Byte, Myvid As Long, Cid As Long, Chno As String, Similiarvid As Long) As Long
    If Similiarvid = 0 Then
        Versevoucher = Newvoucher(Newdate, Itcode)
    Else
        Versevoucher = Similiarvid
    End If
    Select Case Itcode
        Case 18
            mydesc = "عودت چك شماره " & Chno & " به صادر كننده "
        Case 22
            mydesc = "ابطال چك شماره " & Chno
    End Select
    S = "INSERT INTO Voucher_row (Recno, Vid, Checkid, accno, tafno, bes, bed, rowdesc, DateField, ValueField)SELECT " & Vrowno(Versevoucher) & "," & Versevoucher & ", Voucher_row.Checkid, Voucher_row.accno, Voucher_row.tafno, Voucher_row.bed, Voucher_row.bes, '" & CStr(mydesc) & "', Voucher_row.DateField, Voucher_row.ValueField FROM Voucher_row WHERE (((Voucher_row.Checkid)=" & Mycheckid & ") AND ((Voucher_row.Vid)=" & Myvid & "));"
    docommand S
End Function

Private Function Controlvouchers(Controldate As String) As Long
        S = "Delete * from Voucher_notok"
        docommand S
        
        Me.Label10.Caption = "درحال كنترل وجود حساب معين و تفصيل حسابهاي بانكي و صندوق ها"
        DoEvents
        S = "INSERT INTO Voucher_notok ( Baccno, Errordesc, Notname )SELECT Bankacc.Baccno, 'حساب معين نامشخص است' AS 1, 'حسابهاي بانكي و يا صندوق' AS 2 FROM Bankacc WHERE (((Bankacc.Accno) Is Null));"
        docommand S
        S = "INSERT INTO Voucher_notok ( Baccno, Tafno, Errordesc, Notname )SELECT Bankacc.Baccno, Bankacc.Tafno, 'حساب تفصيل نامشخص است' AS 1, 'حسابهاي بانكي و يا صندوق' AS 2 FROM Bankacc INNER JOIN t_accounts ON Bankacc.Accno = t_accounts.Account_Code WHERE (((Bankacc.Tafno) Is Null) AND ((t_accounts.Has_Unique_Account)=True));"
        docommand S
'Investigating badacc's in Subs
        Me.Label10.Caption = "درحال كنترل صحت تشخيص حساب برداشت از/ واريز به حسابها - دريافتها و پرداختهاي نقدي"
        DoEvents
        S = "INSERT INTO Voucher_notok ( Itid,Checkno,Notdate, Baccno,Notname, Accno, Tafno, Errordesc ) SELECT Subs.Sid,Subs.Checkno,Subs.Sdate, Subs.Baccno, Trans.Tname, Subs.Accno, Subs.Tafno, 'شماره حساب نامشخص است' AS 1 FROM Subs LEFT  JOIN Trans ON Subs.Scode = Trans.Tid WHERE (((Subs.Sdate) >= '" & CStr(Controldate) & " ') And ((Subs.Baccno) Is Null) And ((Subs.Scode) <> 10) And ((Subs.Card) = False))ORDER BY Subs.Sdate;"
        docommand S
        S = "INSERT INTO Voucher_notok ( Notdate, Itid, Checkno, Baccno, Notname, Errordesc, Accno, Tafno, Nottype ) SELECT Subs.Sdate, Subs.Sid, Subs.Checkno, Subs.Baccno, Trans.Tname, 'حساب معين نامشخص است' AS 1, Subs.Accno, Subs.Tafno, Subs.Scode FROM (Subs LEFT JOIN Trans ON Subs.Scode = Trans.Tid) LEFT JOIN Povouch ON Subs.Sid = Povouch.Sid WHERE (((Subs.Sdate)>='" & CStr(Controldate) & " ') AND ((Subs.Accno) Is Null) AND ((Subs.Scode)<>10) AND ((Subs.Card)=False) AND ((Povouch.Sid) Is Null))ORDER BY Subs.Sdate;"
        docommand S
        S = "INSERT INTO Voucher_notok ( Notdate, Itid, Checkno, Baccno, Notname, Errordesc, Accno, Tafno, Nottype ) SELECT Subs.Sdate, Subs.Sid, Subs.Checkno, Subs.Baccno, Trans.Tname, 'حساب تفصيل نامشخص است' AS 1, Subs.Accno, Subs.Tafno, Subs.Scode FROM ((Subs LEFT JOIN Trans ON Subs.Scode = Trans.Tid) LEFT JOIN Povouch ON Subs.Sid = Povouch.Sid) LEFT JOIN t_accounts ON Subs.Accno = t_accounts.Account_Code WHERE (((Subs.Sdate) >= '" & CStr(Controldate) & " ') And ((Subs.Tafno) Is Null) And ((Subs.Scode) <> 10) And ((Subs.Card) = False) And ((Povouch.Sid) Is Null) And ((t_accounts.Has_Unique_Account) = True))ORDER BY Subs.Sdate;"
        docommand S
'Investigating badacc's in D_checks
        Me.Label10.Caption = "درحال كنترل صحت تشخيص حساب چكهاواسناد دريافتي"
        DoEvents
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Checktype, Checkno, Accno, Tafno )SELECT Check.D1, 'حساب معين نامشخص است' AS 1, Check.Checkid, Check.Dp, Check.Checktypen, Check.Checktype, Check.Checkno, Check.Accno, Check.Tafno FROM [Check] LEFT JOIN Povouch ON Check.Checkid = Povouch.Checkid WHERE (((Check.D1) >= '" & CStr(Controldate) & " ') And ((Check.Dp) = 1) And ((Check.Accno) Is Null) And ((Povouch.Checkid) Is Null))ORDER BY Check.D1;"
        docommand S
        
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Checktype, Checkno, Accno, Tafno )SELECT Check.D1, 'حساب تفصيل نامشخص است' AS 1, Check.Checkid, Check.Dp, Check.Checktypen, Check.Checktype, Check.Checkno, Check.Accno, Check.Tafno FROM ([Check] LEFT JOIN Povouch ON Check.Checkid = Povouch.Checkid) LEFT JOIN t_accounts ON Check.Accno = t_accounts.Account_Code WHERE (((Check.D1) >= '" & CStr(Controldate) & " ') And ((Check.Dp) = 1) And ((Check.Tafno) Is Null) And ((Povouch.Checkid) Is Null) And ((t_accounts.Has_Unique_Account) = True))ORDER BY Check.D1;"
        docommand S
        
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Checktype, Checkno, Accno, Tafno )SELECT Check.D1, 'حساب معين نامشخص است' AS 1, Check.Checkid, Check.Dp, Check.Checktypen, Check.Checktype, Check.Checkno, Check.Accnotcash, Check.Tafnotcash FROM [Check] LEFT JOIN Povouch ON Check.Checkid = Povouch.Checkid WHERE (((Check.D1) >= '" & CStr(Controldate) & " ') And ((Check.Dp) = 1) And ((Check.Accnotcash) Is Null) And ((Povouch.Checkid) Is Null))ORDER BY Check.D1;"
        docommand S
        
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Checktype, Checkno, Accno, Tafno )SELECT Check.D1, 'حساب تفصيل نامشخص است' AS 1, Check.Checkid, Check.Dp, Check.Checktypen, Check.Checktype, Check.Checkno, Check.Accnotcash, Check.Tafnotcash FROM ([Check] LEFT JOIN Povouch ON Check.Checkid = Povouch.Checkid) LEFT JOIN t_accounts ON Check.Accnotcash = t_accounts.Account_Code WHERE (((Check.D1) >= '" & CStr(Controldate) & " ') And ((Check.Dp) = 1) And ((Check.Tafnotcash) Is Null) And ((Povouch.Checkid) Is Null) And ((t_accounts.Has_Unique_Account) = True))ORDER BY Check.D1;"
        docommand S
' Investigating badacc's in P_checks
        Me.Label10.Caption = "درحال كنترل صحت تشخيص حساب چكهاو اسناد پرداختي"
        DoEvents
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Checktype, Checkno, Accno, Tafno ) SELECT Check.D1, 'حساب معين نامشخص است' AS 1, Check.Checkid, Check.Dp, Check.Checktypen, Check.Checktype, Check.Checkno, Check.Accno, Check.Tafno FROM [Check] LEFT JOIN Povouch ON Check.Checkid = Povouch.Checkid WHERE (((Check.D1) >= '" & CStr(Controldate) & " ') And ((Check.Dp) = 2) And ((Check.Paied) = true) And ((Check.Checktype)<>9) And ((Check.Accno) Is Null) And ((Povouch.Checkid) Is Null)) ORDER BY Check.D1;"
        docommand S
        
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Checktype, Checkno, Accno, Tafno ) SELECT Check.D1, 'حساب تفصيل نامشخص است' AS 1, Check.Checkid, Check.Dp, Check.Checktypen, Check.Checktype, Check.Checkno, Check.Accno, Check.Tafno FROM ([Check] LEFT JOIN Povouch ON Check.Checkid = Povouch.Checkid) LEFT JOIN t_accounts ON Check.Accno = t_accounts.Account_Code WHERE (((Check.D1) >= '" & CStr(Controldate) & " ') And ((Check.Dp) = 2) And ((Check.Paied) = true) And ((Check.Checktype)<>9) And ((Check.Tafno) Is Null) And ((Povouch.Checkid) Is Null) And ((t_accounts.Has_Unique_Account) = True))ORDER BY Check.D1;"
        docommand S
        
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Checktype, Checkno, Accno, Tafno ) SELECT Check.D1, 'حساب معين نامشخص است' AS 1, Check.Checkid, Check.Dp, Check.Checktypen, Check.Checktype, Check.Checkno, Check.Accnotcash, Check.Tafnotcash FROM [Check] LEFT JOIN Povouch ON Check.Checkid = Povouch.Checkid WHERE (((Check.D1) >= '" & CStr(Controldate) & " ') And ((Check.Dp) = 2) And ((Check.Paied) = true) And ((Check.Checktype)<>9) And ((Check.Checktype) <> 0) And ((Check.Accnotcash) Is Null) And ((Povouch.Checkid) Is Null))ORDER BY Check.D1;"
        docommand S
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Checktype, Checkno, Accno, Tafno ) SELECT Check.D1, 'حساب تفصيل نامشخص است' AS 1, Check.Checkid, Check.Dp, Check.Checktypen, Check.Checktype, Check.Checkno, Check.Accnotcash, Check.Tafnotcash FROM ([Check] LEFT JOIN t_accounts ON Check.Accnotcash = t_accounts.Account_Code) LEFT JOIN Povouch ON Check.Checkid = Povouch.Checkid WHERE (((Check.D1) >= '" & CStr(Controldate) & " ') And ((Check.Dp) = 2And ((Check.Paied) = true) And ((Check.Checktype)<>9)) And ((Check.Checktype) <> 0) And ((Check.Tafnotcash) Is Null) And ((t_accounts.Has_Unique_Account) = True) And ((Povouch.Checkid) Is Null))ORDER BY Check.D1;"
        docommand S
' Investigating badacc's in Povouchs
        Me.Label10.Caption = "درحال كنترل صحت تشخيص حساب اسنادكاربر"
        DoEvents
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Checkno, Accno, Tafno ) SELECT Check.D1, 'حساب معين نامشخص است' AS 1, Check.Checkid, Check.Dp, [Checktypen] & ' اسنادكاربر' AS 2, Check.Checkno, Povouch.Accno, Povouch.tafno FROM [Check] INNER JOIN Povouch ON Check.Checkid = Povouch.Checkid WHERE (((Check.D1)>='" & CStr(Controldate) & " ') AND ((Povouch.Accno) Is Null))ORDER BY Check.D1;"
        docommand S
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Checkno, Accno, Tafno ) SELECT Check.D1, 'حساب معين نامشخص است' AS 1, Check.Checkid, Check.Dp, [Checktypen] & ' اسنادكاربر' AS 2, Check.Checkno, Check.Accno, Povouch.tafno FROM t_accounts INNER JOIN ([Check] INNER JOIN Povouch ON Check.Checkid = Povouch.Checkid) ON t_accounts.Account_Code = Povouch.accno WHERE (((Check.D1)>='" & CStr(Controldate) & " ') AND ((Povouch.tafno) Is Null) AND ((t_accounts.Has_Unique_Account)=True))ORDER BY Check.D1;"
        docommand S

        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Accno, Tafno )SELECT Subs.Sdate, 'حساب معين نامشخص است' AS 1, Subs.Sid, Subs.Scode, Trans.Tname, Povouch.accno, Povouch.tafno FROM (Povouch INNER JOIN Subs ON Povouch.Sid = Subs.Sid) LEFT JOIN Trans ON Subs.Scode = Trans.Tid WHERE (((Subs.Sdate)>='" & CStr(Controldate) & " ') AND ((Povouch.accno) Is Null))ORDER BY Subs.Sdate;"
        docommand S
        S = "INSERT INTO Voucher_notok ( Notdate, Errordesc, Itid, Nottype, Notname, Accno, Tafno )SELECT Subs.Sdate, 'حساب تفصيل نامشخص است' AS 1, Subs.Sid, Subs.Scode, Trans.Tname, Povouch.accno, Povouch.tafno FROM ((Povouch INNER JOIN Subs ON Povouch.Sid = Subs.Sid) LEFT JOIN Trans ON Subs.Scode = Trans.Tid) INNER JOIN t_accounts ON Povouch.accno = t_accounts.Account_Code WHERE (((Subs.Sdate)>='" & CStr(Controldate) & " ') AND ((Povouch.tafno) Is Null) AND ((t_accounts.Has_Unique_Account)=True)) ORDER BY Subs.Sdate;"
        docommand S

        Terrors = DCount("*", "Voucher_notok")
        Controlvouchers = Terrors
End Function

Private Sub Form_Open(Cancel As Integer)
    S = "Is_Updated=" & True
    Finaldateacc = Nz(DMax("Voucher_Date", "t_Voucher", S), "00000000")
    Me.Dacc = Nz(Format(Nz(DMax("Voucher_Date", "t_Voucher", S), "00000000"), "@@@@/@@/@@"), "13840101")
End Sub
