Attribute VB_Name = "Form_Check_Print_single"
Attribute VB_Base = "0{D70C7EF7-6FB7-4BF3-98FA-DC5E8A32F6BA}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
    Dim rst As Recordset
    Dim mystr As String, mydesc As String
    Dim stDocName As String
    Dim stLinkCriteria As String
    Dim Mybundfrom As Long
    Dim MyAsnadnazdsandogh As String, MyChecknazdsandogh As String, Myjaryanvosool As String, Mybacked As String
    Dim Myaccentezami As String, Myacctentezami As String, hasnotacc As String
    Dim sideacc As String, sidetaf As String, acclongent As String, taflongent As String
   
Private Sub Accno_AfterUpdate()
    If Me.Accno.Column(3) = True Then
        Me.Tafno.Enabled = True
        Me.Tafno.SetFocus
    Else
        Me.Tafno = Null
        Me.Tafno.Enabled = False
    End If
End Sub
Private Sub Accno_BeforeUpdate(Cancel As Integer)
    If Me.Accno.Column(4) = True Then
        MsgBox "حساب داراي سطح معين در معين است"
        Cancel = True
    End If
End Sub

Private Sub Accno_DblClick(Cancel As Integer)
    Openaccs 1
End Sub

Private Sub Accno_GotFocus()
    Me.Accno.Requery
    If Me.Accno.Column(3) = True And IsNull(Me.Tafno) Then
        MsgBox "حساب تفصيل نامشخص است"
        Me.Tafno.Enabled = True
        Me.Tafno.SetFocus
    End If
End Sub

Private Sub Accnotcash_AfterUpdate()
    If Me.Accnotcash.Column(3) = True Then
        Me.Tafnotcash.Enabled = True
        Me.Tafnotcash.SetFocus
    Else
        Me.Tafnotcash = Null
        Me.Tafnotcash.Enabled = False
    End If
End Sub

Private Sub Accnotcash_BeforeUpdate(Cancel As Integer)
    If Me.Accnotcash.Column(4) = True Then
        MsgBox "حساب داراي سطح معين در معين است"
        Cancel = True
    End If
End Sub

Private Sub Accnotcash_DblClick(Cancel As Integer)
  Openaccs 3
End Sub

Private Sub Amount_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Amount) Or Me.Amount = 0 Then
        MsgBox "مبلغ نامشخص است"
        Cancel = True
    End If
End Sub

Private Sub Calcit_AfterUpdate()
    If Me.Calcit = True Then
        Me.D5 = CurrentDateFarsi(Date)
        Me.D5.Enabled = True
        Me.D5.SetFocus
    Else
        Me.D5 = Null
        Me.D5.Enabled = False
    End If

End Sub

Private Sub Calcit_BeforeUpdate(Cancel As Integer)
    If Me.Calcit = True Then
        If Me.EbtorKharj = True Or (Me.Checktype <> 0 And Me.Ctocash = False) Then
            MsgBox "چك ابطالي ويا انتظامي /مدت دار است"
            Cancel = True
        End If
        
    End If
End Sub

Private Sub Checkno_AfterUpdate()
    If IsNull(Me.ComboBundle) Then
        MsgBox "دسته چك نامشخص است"
        Me.Checkno = Null
    End If

End Sub

Private Sub Checktype_AfterUpdate()
    If Me.Checktype <> 0 And Me.Ctocash = False Then
        Me.Calcit = False
    End If
    If Me.Checktype <> 0 Then
        Me.Transaccid = Null
        Me.Transaccid.Enabled = False
    Else
        Me.Transaccid.Enabled = True
    End If
    Me.Checktypen = Me.Checktype.Column(1)
    
    If Me.Checktype = 1 Or Me.Checktype = 2 Then
        Me.Accnotcash.Enabled = True
        If Me.Checktype = 2 Then
            Me.Accno = Null
            Me.Accnotcash = Nz(DLookup("Asnadpardakhtani", "Globals"), "")
        Else
            Me.Accno = Nz(DLookup("Accentezami", "Globals"), "")
            Me.Accnotcash = Nz(DLookup("Acctentezami", "Globals"), "")
        End If
    Else
        Me.Accno = Null
        Me.Accnotcash = Null
        Me.Accnotcash.Enabled = False
        Me.Tafnotcash = Null
        Me.Tafnotcash.Enabled = False
    End If
    If IsNull(Me.Checktype) Then
        Me.Checktype = 0
    End If
    If Me.Checktype <> 0 Then
        Me.Transaccid = Null
        Me.Transaccid.Enabled = False
    End If
End Sub
Private Sub Sel_AfterUpdate()
    Me.Recalc
End Sub
Private Sub Checkno_BeforeUpdate(Cancel As Integer)
    If Val(Me.Checkno) < Me.ComboBundle.Column(1) Or Val(Me.Checkno) > Me.ComboBundle.Column(2) Then
        MsgBox "چك متعلق به دسته چك انتخابي نيست"
        Cancel = True
        Exit Sub
    End If
    'If Len(Me.Checkno) < 4 Then
    '    MsgBox "چك متعلق به دسته چك انتخابي نيست"
     '   Cancel = True
     '   Exit Sub
    'End If
'    If Mid(Me.Checkno, 1, 4) <> Mid(Me.ComboBundle, 1, 4) Then
'        MsgBox "چك متعلق به دسته چك انتخابي نيست"
'        Cancel = True
'        Exit Sub
'    End If
    mystr = "SELECT * FROM Check WHERE Dp=2 AND bundleid =" & Me.ComboBundle & " And Checkno= " & "'" & CStr(Me.Checkno) & "'"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            Exit Sub
        Else
            MsgBox "شماره چك تكراري است"
            Cancel = True
        End If
    .Close
    End With

End Sub

Private Sub Baccno_AfterUpdate()
    Set rst = CurrentDb.OpenRecordset("Globals", dbOpenDynaset, dbSeeChanges)
    With rst
        Me.Hazfhavale = !Hazfhavale
    .Close
    End With
    Me.Curid = Me.Baccno.Column(3)
    Me.Iscur = Me.Baccno.Column(5)
    If Me.Baccno.Column(5) = True Then
        Me.Amount.DecimalPlaces = 2
    Else
        Me.Amount.DecimalPlaces = 0
    End If
    Me.ComboBundle.Requery
    Me.Tafnotcash.Enabled = True
    Me.Accnotcash.Enabled = True
    If IsNull(Me.Baccno) Then
        Me.Tafnotcash = Null
        Me.Accnotcash = Null
        Me.Iscur = 0
        Me.Curid = "ريال"
    Else
        If Me.Checktype = 0 Then
            Me.Tafnotcash = Me.Baccno.Column(7)
            Me.Accnotcash = Me.Baccno.Column(6)
            Me.Iscur = Me.Baccno.Column(5)
            Me.Curid = Me.Baccno.Column(3)
        End If
    End If
    Me.Accnotcash.Requery
    Me.Tafnotcash.Requery
    Me.Tafnotcash.Enabled = False
    Me.Accnotcash.Enabled = False
End Sub

Private Sub Baccno_GotFocus()
        If IsNull(Me.D1) Then
            MsgBox "تاريخ صدورنامشخص است"
            Me.D1.SetFocus
            Exit Sub
        End If
        If IsNull(Me.Checktype) Then
            MsgBox "نوع چك نامشخص است"
            Me.Checktype.SetFocus
            Exit Sub
        End If
        If IsNull(Me.ComboP) Then
            MsgBox "ذينفع نامشخص است"
            Me.ComboP.SetFocus
            Exit Sub
        End If
        If IsNull(Me.Babat) Then
            MsgBox "بابت نامشخص است"
            Me.Babat.SetFocus
            Exit Sub
        End If
        If IsNull(Me.Amount) Then
            MsgBox "مبلغ نامشخص است"
            Me.Amount.SetFocus
            Exit Sub
        End If
End Sub

Private Sub Combo60_GotFocus()
    
End Sub

Private Sub ComboBundle_AfterUpdate()
    If IsNull(Me.Checkno) = False Then Exit Sub
    If IsNull(ComboBundle) Then
        Exit Sub
    End If
    mystr = "SELECT Max(Checkno) as Maxno FROM Check WHERE Dp=2 AND bundleid =" & Me.ComboBundle
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If IsNull(!Maxno) Then
            Me.Checkno = Me.ComboBundle.Column(1)
        Else
            Me.Checkno = !Maxno + 1
        End If
        
    .Close
    End With
    
    mystr = "select * from Bundle where fsheet=" & Me.ComboBundle
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If Me.Checkno = CStr(!Lsheet) Then
            .Edit
            !Active = False
            .Update
            Me.ComboBundle.Requery
            MsgBox "برگهاي دسته چك تمام شد.دسته چك غيرفعال خواهد شد"
            .Close
        End If
    End With

End Sub

Private Sub ComboBundle_GotFocus()
    Me.ComboBundle.Requery
End Sub

Private Sub Command122_Click()
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Ebtit"
    
    stLinkCriteria = "[Checkid]=" & Me![Checkid]
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub

Private Sub Command42_Click()
On Error GoTo Err_Command42_Click


    DoCmd.Close

Exit_Command42_Click:
    Exit Sub

Err_Command42_Click:
    MsgBox Err.Description
    Resume Exit_Command42_Click
    
End Sub
Private Sub Command43_Click()
On Error GoTo Err_Command43_Click

    Dim myform As Form

    stDocName = "Accamount"
    Set myform = Screen.ActiveForm
    DoCmd.SelectObject acForm, stDocName, True
    DoCmd.PrintOut
    DoCmd.SelectObject acForm, myform.Name, False

Exit_Command43_Click:
    Exit Sub

Err_Command43_Click:
    MsgBox Err.Description
    Resume Exit_Command43_Click
    
End Sub


Private Sub Command50_Click()
    Dim isitnew As Boolean, isarzi As Boolean, isnewest As Boolean
    If Me.Paied = False Then
        MsgBox "پرداخت تاييد نشده است"
        Exit Sub
    End If
    
    Me.Recalc

    Dim myprintcode As Long
    mystr = "Select * from Bundle Where Fsheet=" & Me.ComboBundle
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
        Else
            isnewest = IIf(IsNull(!Newest), False, !Newest)
            isitnew = IIf(IsNull(!Isnew), False, !Isnew)
            myprintcode = !Printcode
        End If
    .Close
    End With
    
    mystr = "Select * from Bankacc Where Baccno= '" & CStr(Me.Baccno) & "'"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        isarzi = IIf(IsNull(!Iscur), False, !Iscur)
    .Close
    End With
   
    Select Case myprintcode
        Case 11100
        stDocName = "CH_MELLI100"
        Case 1150
            If isitnew = True Then
                stDocName = "CH_MELLI50_new"
            Else
                stDocName = "CH_MELLI50"
            End If
        Case 1125
            If isitnew = True Then
                stDocName = "CH_MELLI25_new"
            Else
                stDocName = "CH_MELLI25"
            End If
        
        Case 12100
        stDocName = "CH_MELLAT100"
        Case 1250
            If isitnew = False Then
                If isarzi = False Then
                    stDocName = "CH_MELLAT50"
                Else
                    stDocName = "CH_MELLAT50_Z"
                End If
            Else
                stDocName = "CH_MELLAT50_new"
            End If
            
            If isnewest = True Then
                stDocName = "CH_MELLAT50_newest"
            End If
            
        Case 1225
        stDocName = "CH_MELLAT25"
        
        Case 22100
        stDocName = "CH_KAR100"
        Case 2250
        stDocName = "CH_KAR50"
        Case 2225
        stDocName = "CH_KAR25"
        
        Case 13100
        stDocName = "CH_SEPAH100"
        Case 1350
            If isitnew = True Then
                stDocName = "CH_SEPAH50carbon"
            Else
                stDocName = "CH_SEPAH50"
            End If
        
        Case 1325
        stDocName = "CH_SEPAH25"
        
        Case 14100
        stDocName = "CH_SADERAT100"
        Case 1450
        stDocName = "CH_SADERAT50"
        Case 1425
        stDocName = "CH_SADERAT25"
        
        Case 15100
        stDocName = "CH_TEJARAT100"
        Case 1550
            If isitnew = False Then
                stDocName = "CH_TEJARAT50"
            Else
                stDocName = "CH_TEJARAT50new"
            End If
        Case 1525
        stDocName = "CH_TEJARAT25"
        
        Case 16100
        
            'If Me.newformat = True Then
                stDocName = "CH_REFAH100"
            'Else
            '    stDocName = "CH_REFAHnew100"
            'End If
        Case 1650
            'If Me.newformat = True Then
                stDocName = "CH_REFAH50"
            'Else
            '    stDocName = "CH_REFAHnew50"
            'End If
        Case 1625
            'If Me.newformat = True Then
                stDocName = "CH_REFAH25"
            'Else
            '    stDocName = "CH_REFAHnew25"
            'End If
        Case 17100
        stDocName = "CH_TOSE100"
        Case 1750
        stDocName = "CH_TOSE50"
        Case 1725
        stDocName = "CH_TOSE25"
        
        Case 3125
        stDocName = "CH_Parsian25"
        Case 3150
        stDocName = "CH_Parsian50"
        Case 31100
        stDocName = "CH_Parsian100"
        
        Case 1850
        stDocName = "CH_maadan50"
        
        Case 1910
        stDocName = "CH_Kesh10"
        Case 1925
        stDocName = "CH_Kesh25"
        Case 1950
        stDocName = "CH_Kesh50"
        
        Case 2010
        stDocName = "CH_Maskan10"
        Case 2025
        stDocName = "CH_Maskan25"
        Case 2050
        stDocName = "CH_Maskan50"
        
        
        Case 4110
        stDocName = "CH_Pasargaad10"
        Case 4125
        stDocName = "CH_Pasargaad10"
        Case 4150
        stDocName = "CH_Pasargaad10"
        Case 2525
        stDocName = "CH_Saman25"
        Case 4325
        stDocName = "CH_Sina25"
        
        Case 3225
        stDocName = "CH_Novin25"
        
    End Select
    
    stLinkCriteria = "[Checkid]=" & Me.Checkid
    DoCmd.OpenReport stDocName, acPreview, , stLinkCriteria


End Sub

Private Sub Command58_Click()
    If Me.Paied = False Then
        MsgBox "پرداخت تاييد نشده است"
        Exit Sub
    End If

    Me.Recalc
    S = "Delete * from voucher_row_tmp"
    docommand S
    
    V_tmp Me.Checkid

    stLinkCriteria = "[Checkid]=" & Me![Checkid]

    stDocName = "B_P_print"
    DoCmd.OpenReport stDocName, acPreview, , stLinkCriteria
End Sub

Private Sub Command59_Click()

    stDocName = "Pordersub_vouch"
    
    stLinkCriteria = "[Checkid]=" & Me.Checkid
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub

Private Sub Ctocash_AfterUpdate()
    If Me.Ctocash = True Then
        Me.D3.Enabled = True
        Me.D3 = CurrentDateFarsi(Date)
        Me.Calcit = True
        Me.D5 = IIf(IsNull(Me.D3), 0, Me.D3)
        Me.D3.SetFocus
    Else
        Me.D3 = Null
        Me.D3.Enabled = False
        Me.Calcit = False
        Me.D5 = Null
        Me.D5.Enabled = False
    End If
End Sub
Private Sub Ctocash_BeforeUpdate(Cancel As Integer)
    If Me.Ctocash = True Then
    updatecard
        S = "Baccno='" & CStr(Nz(Me.Baccno, 0)) & "'"
        Mybalance = Nz(DSum("Bed", "Subs", S), 0) - Nz(DSum("Bes", "Subs", S), 0)
        If Me.Amount > Mybalance Then
        MsgBox "موجودي منفي ميشود.پرداخت امكان پذير نيست"
        Cancel = True
        End If
    End If
End Sub
Private Sub D1_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.D1) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Nz(Me.D1, 0), 0) = False Then Cancel = True
End Sub

Private Sub D2_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.D2) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Nz(Me.D2, 0), 0) = False Then Cancel = True
End Sub

Private Sub D3_AfterUpdate()
    Me.D5 = Me.D3
End Sub

Private Sub D3_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.D3) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Nz(Me.D3, 0), 0) = False Then Cancel = True
End Sub

Private Sub D5_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.D5) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Nz(Me.D5, 0), 0) = False Then Cancel = True
End Sub

Private Sub EbtorKharj_AfterUpdate()
    If Me.EbtorKharj = True Then
        Me.D2 = CurrentDateFarsi(Date)
        Me.Calcit = False
        Me.D5 = Null
        Me.D2.Enabled = True
        Me.D2.SetFocus
        Me.Label0.Visible = True
        Me.Stcode = 26
        Me.Status = "ابطال شده"
    Else
        Me.D2 = Null
        Me.D2.Enabled = False
        Me.Label0.Visible = False
        Me.Stcode = 20
        Me.Status = "درجريان صدور"
    End If
End Sub
Private Sub Final_AfterUpdate()
    Me.Refresh
    Me.AllowEdits = Not Nz(Me.Final)
    Me.AllowDeletions = Not Nz(Me.Final)
End Sub
Private Sub Final_BeforeUpdate(Cancel As Integer)
    If Me.Paied = False Then
        MsgBox ("پرداخت تاييد نشده است")
        Cancel = True
        Exit Sub
    Else
        Varasi
    End If
    If MsgBox("اطمينان داريد؟", vbYesNo) = vbNo Then
        Cancel = True
    End If
End Sub

Private Sub Form_Close()
    If Spfilter = 1 Then
        Exit Sub
    Else
        Forms![Check_pay_m]![Check_s].Form.Requery
    End If
End Sub

Private Sub Form_Current()
    Isfinal = Nz(Me.Final, False)
    Me.AllowEdits = Not Nz(Me.Final)
    Me.AllowDeletions = Not Nz(Me.Final)
    
    If Me.D1 <= Nochangedate Then
        Me.AllowEdits = False
        Me.AllowDeletions = False
    Else
        Me.AllowEdits = True
        Me.AllowDeletions = True
    End If
    
    If Me.Paied = True Then
        Me.EbtorKharj.Enabled = True
        If Me.Checktype <> 0 Then
            Me.Ctocash.Enabled = True
        End If
        Me.Calcit.Enabled = True
    Else
        Me.EbtorKharj.Enabled = False
        Me.Ctocash.Enabled = False
        Me.Calcit.Enabled = False
    End If
    If Me.EbtorKharj = True Then
        Me.Label0.Visible = True
        Me.D2.Enabled = True
    Else
        Me.Label0.Visible = False
        Me.D2.Enabled = False
    End If
    If IsNull(Me.D1) Then
        Me.D1 = CurrentDateFarsi(Date)
    End If
    
    If Me.Checktype = 1 Or Me.Checktype = 2 Then
        Me.Accnotcash.Enabled = True
        If Me.Accnotcash.Column(3) = True Then
            Me.Tafnotcash.Enabled = True
        Else
            Me.Tafnotcash.Enabled = False
        End If
    Else
        Me.Accnotcash.Enabled = False
        Me.Tafnotcash.Enabled = False
    End If
    
    If Me.Accno.Column(3) = True Then
        Me.Tafno.Enabled = True
    Else
        Me.Tafno.Enabled = False
    End If
    If Me.Checktype <> 0 Then
        Me.Transaccid.Enabled = False
    End If
    If Me.Baccno.Column(5) = True Then
        Me.Amount.DecimalPlaces = 2
    Else
        Me.Amount.DecimalPlaces = 0
    End If

    
'    If Forms!Sal!Arzifarsi = False Then
'        Me.Checkl.Enabled = True
'    Else
'        Me.Checkl.Enabled = False
'    End If
    
    Me.D1.SetFocus
End Sub

Private Sub Maturity_BeforeUpdate(Cancel As Integer)
    If Me.Checktype <> 1 And Controldate(Nz(Me.Maturity, 0), 1) = False Then Cancel = True
End Sub
Private Sub Maturity_GotFocus()
    If IsNull(Me.Maturity) And Me.Checktype = 0 Then
        Me.Maturity = CurrentDateFarsi(Date)
    End If
End Sub
Private Sub Paied_AfterUpdate()
    If Me.Paied = True Then
        Set rst = CurrentDb.OpenRecordset("Globals", dbOpenDynaset, dbSeeChanges)
        With rst
            Mykasrtime = !Kasrtime
            If !Regsigning = True Then
                Regs = True
            Else
                Regs = False
            End If
        .Close
        End With
        
        If IsNull(Me.Maturity) And Me.Checktype <> 1 Then
            Me.Maturity = Me.D1
        End If
        
        If Regs = False Then
            Me.Signed = True
            Me.Status = "چك صادره"
            Me.Stcode = 21
        Else
            Me.Status = "درجريان صدور"
            Me.Stcode = 20
        End If
        If Mykasrtime = 0 And Me.Checktype = 0 Then
            Me.Calcit = True
            Me.D5 = Me.D1
        End If
        Me.Bpno = tellmelastbp()
        Me.EbtorKharj.Enabled = True
        If Me.Checktype <> 0 Then
            Me.Ctocash.Enabled = True
        End If
        Me.Calcit.Enabled = True
    Else
            'Me.D1 = Null
            'Me.Maturity = Null
            Me.bundleid = 0
            Me.ComboBundle.Requery
            'Me.Baccno = Null
            Me.Checkno = Null
            Me.Status = "درجريان صدور"
            Me.Stcode = 20
            Me.EbtorKharj = False
            Me.D2 = Null
            DelVoucher Me.V1, Me.Checkid
            Me.V1 = 0
            DelVoucher Me.V2, Me.Checkid
            Me.V2 = 0
            DelVoucher Me.V3, Me.Checkid
            Me.V3 = 0
            Me.Signed = False
            Me.Calcit = False
            Me.Ctocash = False
            Me.D3 = Null
            Me.D5 = Null
    End If
End Sub

Private Sub Paied_BeforeUpdate(Cancel As Integer)
    If Me.Paied = True Then
        If Varasi = False Then
            Cancel = True
            Exit Sub
        End If
'        If Me.AMOUNT > Nz(Form_Balance.Balance) And Me.Checktype = 0 Then
''            If MsgBox("موجودي حساب منفي ميشود.پرداخت صورت گيرد؟", vbYesNo) = vbNo Then
''                Cancel = True
''                Exit Sub
''            End If
'        MsgBox "موجودي منفي ميشود.پرداخت امكان پذير نيست"
'        Cancel = True
'        End If

        S = "Baccno='" & CStr(Nz(Me.Baccno, 0)) & "'"
        Mybalance = Nz(DSum("Bed", "Subs", S), 0) - Nz(DSum("Bes", "Subs", S), 0)
        If Me.Amount > Mybalance And Me.Checktype = 0 Then
        MsgBox "موجودي منفي ميشود.پرداخت امكان پذير نيست"
        Cancel = True
        End If
    Else
        If MsgBox("ازلغوپرداخت اطمينان داريد؟", vbYesNo) = vbNo Then
            Cancel = True
            Exit Sub
        End If
    End If
End Sub

Private Sub PbyCheckno_GotFocus()
    Me.PbyCheckno.Requery
End Sub

Private Sub Podate_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Nz(Me.Rfpdate, 0), 0) = False Then Cancel = True
End Sub

Private Sub Tafno_BeforeUpdate(Cancel As Integer)
    If Me.Accno.Column(3) = True And IsNull(Me.Tafno) Then
        MsgBox "حساب تفصيل نامشخص است"
        Cancel = True
    End If
End Sub

Private Sub Tafno_DblClick(Cancel As Integer)
  Openaccs 2
End Sub

Private Sub Tafno_LostFocus()
    If Me.Accno.Column(3) = True And IsNull(Me.Tafno) Then
        MsgBox "حساب تفصيل نامشخص است"
    End If
End Sub

Private Sub Tafnotcash_BeforeUpdate(Cancel As Integer)
    If Me.Accnotcash.Column(3) = True And IsNull(Me.Tafnotcash) Then
        MsgBox "حساب تفصيل نامشخص است"
        Cancel = True
    End If
End Sub

Private Sub Tafnotcash_DblClick(Cancel As Integer)
     Openaccs 4
End Sub

Private Sub Tafnotcash_LostFocus()
    If Me.Accnotcash.Column(3) = True And IsNull(Me.Tafnotcash) Then
        MsgBox "حساب تفصيل نامشخص است"
    End If
End Sub

Private Sub Transaccid_AfterUpdate()
    If IsNull(Me.Transaccid) Then
        Me.Tafno = Null
        Me.Accno = Null
        Me.Tafno.Enabled = False
        Me.Accno.SetFocus
    Else
        Me.Tafno = Me.Transaccid.Column(5)
        Me.Accno = Me.Transaccid.Column(6)
    End If
    Me.Tafno.Requery
    Me.Accno.Requery
End Sub

Private Sub V1_DblClick(Cancel As Integer)
    If Me.Paied = False Then
        MsgBox "پرداخت تاييد نشده است"
        Exit Sub
    End If

    If Me.V1 = 0 Or IsNull(Me.V1) Then
        MsgBox "سند صادرنشده است"
        Exit Sub
    Else
        Dim stDocName As String
        Dim stLinkCriteria As String
    
        stDocName = "Voucher"
        
        stLinkCriteria = "[Voucher.Vid]=" & Me![V1]
        DoCmd.OpenForm stDocName, , , stLinkCriteria
    End If

End Sub

Private Sub V2_DblClick(Cancel As Integer)
    If Me.Paied = False Then
        MsgBox "پرداخت تاييد نشده است"
        Exit Sub
    End If

    If Me.V2 = 0 Or IsNull(Me.V2) Then
        MsgBox "سند صادرنشده است"
        Exit Sub
    Else
        Dim stDocName As String
        Dim stLinkCriteria As String
    
        stDocName = "Voucher"
        
        stLinkCriteria = "[Voucher.Vid]=" & Me![V2]
        DoCmd.OpenForm stDocName, , , stLinkCriteria
    End If

End Sub

Private Sub V3_DblClick(Cancel As Integer)
    If Me.Paied = False Then
        MsgBox "پرداخت تاييد نشده است"
        Exit Sub
    End If

    If Me.V3 = 0 Or IsNull(Me.V3) Then
        MsgBox "سند صادرنشده است"
        Exit Sub
    Else
        Dim stDocName As String
        Dim stLinkCriteria As String
    
        stDocName = "Voucher"
        
        stLinkCriteria = "[Voucher.Vid]=" & Me![V3]
        DoCmd.OpenForm stDocName, , , stLinkCriteria
    End If
End Sub

Public Sub V_tmp(Cid As Long)
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
    hasnotacc = ""

    mystr = "Select * from check where checkid=" & Cid
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            Exit Sub
        End If
        
        
        mybacc = !Baccno
        mystr1 = "select * from Bankacc where baccno=" & "'" & CStr(mybacc) & "'"
        Set rst1 = CurrentDb.OpenRecordset(mystr1, dbOpenDynaset, dbSeeChanges)
        With rst1
            sideacc = IIf(IsNull(!Accno), "", !Accno)
            sidetaf = IIf(IsNull(!Tafno), "", !Tafno)
            isitcur = Iscur
        .Close
        End With
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
            
            mydesc = mychecktype & IIf(IsNull(!Checkno), "00000", !Checkno) & " دروجه " & IIf(IsNull(!Person), "", !Person) & " بابت " & IIf(IsNull(!Babat), "", !Babat)
            
           '*************
            strv = "Select * from Povouch where Checkid=" & Cid
            Set rstv = CurrentDb.OpenRecordset(strv, dbOpenDynaset, dbSeeChanges)
            With rstv
                If .RecordCount = 0 Then
                    vu = False
                    GoTo linex
                Else
                     vu = True
                    .MoveLast
                    .MoveFirst
                End If
                
                While Not .EOF
                    Newvrow_tmp Cid, IIf(IsNull(!Accno), "", !Accno), IIf(IsNull(!Tafno), "", !Tafno), !bed, !bes, IIf(IsNull(!rowdesc), "", !rowdesc), "", ""
                .MoveNext
                Wend
            .Close
            End With

           '*************
linex:
        If vu = False Then
            Newvrow_tmp !Checkid, IIf(IsNull(!Accno), "", !Accno), IIf(IsNull(!Tafno), "", !Tafno), !Amount, 0, mydesc, IIf(IsNull(!Maturity), "00000000", !Maturity), IIf(IsNull(!Checkno), 0, !Checkno)
        End If
            Select Case !Checktype
                Case 0
                    Newvrow_tmp !Checkid, sideacc, sidetaf, 0, !Amount, mydesc, IIf(IsNull(!Maturity), "00000000", !Maturity), IIf(IsNull(!Checkno), 0, !Checkno)
                Case 2, 3, 4, 5 'longs
                    acclongent = IIf(IsNull(!Accnotcash), Myasnadpardakhtani, !Accnotcash)
                    taflongent = IIf(IsNull(!Tafnotcash), hasnotacc, !Tafnotcash)
                    Newvrow_tmp !Checkid, acclongent, taflongent, 0, !Amount, mydesc, IIf(IsNull(!Maturity), "00000000", !Maturity), IIf(IsNull(!Checkno), 0, !Checkno)
                Case 1, 6  'entz
                    acclongent = Myacctentezami
                    taflongent = IIf(IsNull(!Tafno), hasnotacc, !Tafno)
                    Newvrow_tmp !Checkid, acclongent, taflongent, 0, !Amount, mydesc, IIf(IsNull(!Maturity), "00000000", !Maturity), IIf(IsNull(!Checkno), 0, !Checkno)
            End Select
        End With
End Sub

Public Sub Newvrow_tmp(itcheckid As Long, itacc As String, ittaf As String, ItBed As Double, ItBes As Double, Itdesc As String, dfield As String, vfield As String)
    Set rst = CurrentDb.OpenRecordset("Voucher_row_tmp", dbOpenDynaset, dbSeeChanges)
    With rst
        .AddNew
            !Checkid = itcheckid
            !Accno = itacc
            !Tafno = ittaf
            !bed = ItBed
            !bes = ItBes
            !Recno = Accrowid
            !rowdesc = Mid(Itdesc, 1, 250)
            !DateField = dfield
            !ValueField = vfield
        .Update
        .Close
    End With
End Sub

Private Function Varasi() As Boolean
        Varasi = True
        If IsNull(Me.D1) Then
            MsgBox "تاريخ صدورنامشخص است"
            Varasi = False
        End If
        If IsNull(Me.Maturity) And (Me.Checktype = 0 Or Checktype = 2) Then
            MsgBox "سررسيدنامشخص است"
            Varasi = False
        End If
        If Me.Maturity < Me.D1 And (Me.Checktype = 0 Or Checktype = 2) Then
            MsgBox "سررسيدازتاريخ صدوركوچكتراست"
            Varasi = False
        End If
        If IsNull(Me.Checktype) Then
            MsgBox "نوع چك نامشخص است"
            Varasi = False
        End If
        If IsNull(Me.ComboP) Then
            MsgBox "ذينفع نامشخص است"
            Varasi = False
        End If
        If IsNull(Me.Babat) Then
            MsgBox "بابت نامشخص است"
            Varasi = False
        End If
        If IsNull(Me.Amount) Then
            MsgBox "مبلغ نامشخص است"
            Varasi = False
        End If
        If IsNull(Me.Baccno) Or Me.Baccno = 0 Then
            MsgBox "شماره حساب نامشخص است"
            Varasi = False
        End If
        If IsNull(Me.ComboBundle) Or Me.ComboBundle = 0 Then
            MsgBox "شماره دسته چك نامشخص است"
            Varasi = False
        End If
        If IsNull(Me.Checkno) Or Me.Checkno = 0 Then
            MsgBox "شماره چك نامشخص است"
            Varasi = False
        End If
End Function
