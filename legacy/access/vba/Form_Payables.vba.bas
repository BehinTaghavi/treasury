Attribute VB_Name = "Form_Payables"
Attribute VB_Base = "0{2A39F2D2-7FB3-4EA6-A200-C209A9923559}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Accno_AfterUpdate()
    If Me.Accno.Column(3) = True Then
        Me.Tafno.Enabled = True
        Me.Tafno.SetFocus
    Else
        Me.Tafno.Enabled = True
        Me.Tafno = Null
        Me.Tafno.Enabled = False
    End If
End Sub

Private Sub Accno_DblClick(Cancel As Integer)
     Openaccs 5
End Sub

Private Sub Accno_GotFocus()
    Me.Accno.Requery
    If Me.Accno.Column(3) = True And IsNull(Me.Tafno) Then
        MsgBox "حساب تفصيل نامشخص است"
        Me.Tafno.Enabled = True
        Me.Tafno.SetFocus
    End If
    If IsNull(Me.Accno) Or Me.Accno = "" Then
        Me.Tafno = Null
    End If
End Sub

Private Sub Accnotcash_AfterUpdate()
    If Me.Accnotcash.Column(3) = True Then
        Me.Tafnotcash.Enabled = True
        Me.Tafnotcash.SetFocus
    Else
        Me.Tafnotcash.Enabled = True
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
     Openaccs 7
End Sub

Private Sub Accnotcash_GotFocus()
    Me.Accnotcash.Requery
    If IsNull(Me.Accnotcash) Or Me.Accnotcash = "" Then
        Me.Tafnotcash = Null
    End If
    If Me.Accnotcash.Column(3) = True And IsNull(Me.Tafnotcash) Then
        MsgBox "حساب تفصيل نامشخص است"
        Me.Tafnotcash.Enabled = True
        Me.Tafnotcash.SetFocus
    End If
End Sub

Private Sub Amount_AfterUpdate()
    Me.D1 = Me.Rfpdate
End Sub

Private Sub Amount_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Amount) Or Me.Amount = 0 Then
        MsgBox "مبلغ نامشخص است"
        Cancel = True
    End If
End Sub

Private Sub Amount_GotFocus()
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Me.Rfpdate.SetFocus
    End If

    Me.Amount.Requery
End Sub

Private Sub Babat_AfterUpdate()
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Me.Rfpdate.SetFocus
    End If
End Sub

Private Sub babat_DblClick(Cancel As Integer)
 Openaccs 42
End Sub

Private Sub babat_GotFocus()
    Me.Babat.Requery
End Sub

Private Sub Combo20_AfterUpdate()
    If IsNull(Me.Combo20) Then
        Me.Combo20 = 3
    End If
    Select Case Me.Combo20
        Case 4, 6
        Me.Mt = 1
        Case 3, 5
        Me.Mt = 9
    End Select
    Me.Checktypen = Me.Combo20.Column(1)

End Sub

Private Sub ComboP_DblClick(Cancel As Integer)
 Openaccs 41
End Sub

Private Sub Command102_Click()
    If Me.Paied = False Then
        MsgBox "پرداخت تاييد نشده است"
        Exit Sub
    End If
    If Nz(Me.Checkid) = 0 Then Exit Sub

    Me.Recalc
    S = "Delete * from voucher_row_tmp"
    docommand S
    
    V_tmp Nz(Me.Checkid)

    stLinkCriteria = "[Checkid]=" & Me![Checkid]

    stDocName = "B_P_print_Safteh"
    DoCmd.OpenReport stDocName, acPreview, , stLinkCriteria

End Sub

Private Sub Command28_Click()
    Dim FilterStr As String
    FilterStr = ""
    
    If Not IsNull(Me!T12) Then
       FilterStr = "InStr(1,Rfpdate,'" & Me!T12 & "',0) > 0"
    End If
    
'    If Not IsNull(Me!T2) Then
'       FilterStr = "InStr(1,D1,'" & Me!T2 & "',0) > 0"
'    End If
    If Not IsNull(Me!T3) Then
       FilterStr = "InStr(1,Checkno,'" & Me!T3 & "',0) > 0"
    End If
    If Not IsNull(Me!T4) Then
       FilterStr = "InStr(1,PT,'" & Me!T4 & "',0) > 0"
    End If
    If Not IsNull(Me!T5) Then
       FilterStr = "InStr(1,Amount,'" & Me!T5 & "',0) > 0"
    End If
    If Not IsNull(Me!T6) Then
       FilterStr = "InStr(1,Maturity,'" & Me!T6 & "',0) > 0"
    End If
    '
    If Not IsNull(Me!T7) Then
       FilterStr = "InStr(1,Baccno,'" & Me!T7 & "',0) > 0"
    End If

    If Not IsNull(Me!T1) Then
       FilterStr = "InStr(1,Babat,'" & Me!T1 & "',0) > 0"
    End If
    If Not IsNull(Me!T9) Then
       FilterStr = "InStr(1,Curid,'" & Me!T9 & "',0) > 0"
    End If
    If Not IsNull(Me!T10) Then
       FilterStr = "InStr(1,Person,'" & Me!T10 & "',0) > 0"
    End If

    If FilterStr = "" Then
       Me.FilterOn = False
    Else
       Me.Filter = FilterStr
       Me.FilterOn = True
    End If
End Sub

Private Sub Command31_Click()
       Me.FilterOn = False
End Sub

Private Sub Command34_Click()
    DoCmd.Close
End Sub

Private Sub Command43_Click()
    If Me.Calcit = True Then
        MsgBox "پرداخت صورت گرفته و امكان حذف وجود ندارد"
        Exit Sub
    End If
    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70
End Sub

Private Sub Command44_Click()
    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 2, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 5, , acMenuVer70 'Paste Append
    Me.Ctocash = False
    Me.Calcit = False
    Me.Rfpdate = CurrentDateFarsi(Date)
    Me.D1 = CurrentDateFarsi(Date)
    Me.D3 = Null
    Me.D5 = Null
End Sub

Private Sub Command58_Click()
    Searchtype = 0
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Shart_m"
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub

Private Sub Ctocash_AfterUpdate()
    If Me.Ctocash = True Then
        Me.D3.Enabled = True
        Me.D3.SetFocus
        Me.Calcit = True
    Else
        Me.D3 = Null
        Me.D5 = Null
        Me.D3.Enabled = False
        Me.Calcit = False
    End If
End Sub

Private Sub Ctocash_BeforeUpdate(Cancel As Integer)
    If Me.Ctocash = False Then
        If MsgBox("برداشت از حساب لغو شود؟", vbYesNo) = vbNo Then
            Cancel = True
        End If
    End If

End Sub

Private Sub D3_AfterUpdate()
    Me.D5 = Me.D3
End Sub

Private Sub EbtorKharj_AfterUpdate()
    If Me.EbtorKharj = True Then
        Me.D2 = CurrentDateFarsi(Date)
        Me.Calcit = False
        Me.D5 = Null
        Me.D2.Enabled = True
        Me.D2.SetFocus
        Me.Stcode = 26
        Me.Status = "ابطال شده"
    Else
       'Me.D2 = Null
        Me.D2.Enabled = False
        Me.Stcode = 21
        Me.Status = "درجريان صدور"
    End If
    
End Sub

Private Sub Final_AfterUpdate()
    Me.Refresh
    Me.AllowEdits = Not Nz(Me.Final)
    Me.AllowDeletions = Not Nz(Me.Final)
End Sub

Private Sub Final_Click()

End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Dp = 2
    Me.Rfptime = Time()
    Me.Empid = [Forms]![Sal]![Tempid]
    Me.Sal = [Forms]![Sal]![Tsal]
    Me.Payit = True
    Me.Paied = True
    Me.Signed = True
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    If Me.Iscur = True And IsNull(Me.Curid) Then
        MsgBox "نوع ارز نامشخص است"
        Me.Combocur.SetFocus
        Cancel = True
    End If
End Sub

Private Sub Form_Current()
    Isfinal = Nz(Me.Final, False)
'    Me.AllowEdits = Not Nz(Me.Final)
'    Me.AllowDeletions = Not Nz(Me.Final)
    
    If Me.Accno.Column(3) = True Then
        Me.Tafno.Enabled = True
    Else
        Me.Tafno.Enabled = False
    End If
    
    If Me.Accnotcash.Column(3) = True Then
        Me.Tafnotcash.Enabled = True
    Else
        Me.Tafnotcash.Enabled = False
    End If

    If Me.Iscur = True Then
        Me.Combocur.Enabled = True
    Else
        Me.Combocur.Enabled = False
    End If
    
    If Me.Ctocash = True Then
        Me.D3.Enabled = True
    Else
        Me.D3.Enabled = False
    End If
    If Me.EbtorKharj = True Then
        Me.D2.Enabled = True
    Else
        Me.D2.Enabled = False
    End If

    Me.Rfpdate.SetFocus
End Sub

Private Sub Form_Delete(Cancel As Integer)
    If Nz(DLookup("Editlcv", "Globals"), 0) <> 0 Then
         Cancel = DelonebyoneCancel(Nz(Me.Checkid))
    End If
End Sub

Private Sub Form_Open(Cancel As Integer)
On Error GoTo Err1
    
    DoCmd.Maximize
    If Me.RecordsetClone.RecordCount > 0 Then
        DoCmd.GoToRecord , , acLast
    End If
Err1:
If Err.number = 2450 Then
  MsgBox "كاربر و يا سال مالي نامشخص است"
  DoCmd.Close
  DoCmd.OpenForm "sal"
End If
End Sub

Private Sub Iscur_AfterUpdate()
    If Me.Iscur = False Then
        Me.Combocur = "ريال"
        Me.Combocur.Enabled = False
    Else
        Me.Combocur = Null
        Me.Combocur.Enabled = True
    End If
End Sub

Private Sub Po_vouch_Enter()
    If IsNull(Me.Checkid) Or Me.Checkid = 0 Then
        MsgBox "اطلاعات پرداخت كامل نيست"
        Me.Rfpdate.SetFocus
    End If
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Me.Rfpdate.SetFocus
    End If
End Sub

Private Sub Maturity_BeforeUpdate(Cancel As Integer)
    If Me.Maturity <> Null And Controldate(Me.Maturity, 1) = False Then Cancel = True
End Sub

Private Sub Rfpdate_AfterUpdate()
    Me.D1 = Me.Rfpdate
End Sub

Private Sub Rfpdate_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Me.Rfpdate, 0) = False Then Cancel = True
End Sub

Private Sub Tafno_BeforeUpdate(Cancel As Integer)
    If Me.Accno.Column(3) = True And IsNull(Me.Tafno) Then
        MsgBox "حساب تفصيل نامشخص است"
        Cancel = True
    End If
End Sub

Private Sub Tafno_DblClick(Cancel As Integer)
     Openaccs 6
End Sub

Private Sub Tafno_GotFocus()
    Me.Tafno.Requery
End Sub

Private Sub Tafno_LostFocus()
    If Me.Accno.Column(3) = True And IsNull(Me.Tafno) Then
        MsgBox "حساب تفصيل نامشخص است"
        Me.Tafno.SetFocus
    End If
End Sub

Private Sub Tafnotcash_BeforeUpdate(Cancel As Integer)
    If Me.Accnotcash.Column(3) = True And IsNull(Me.Tafnotcash) Then
        MsgBox "حساب تفصيل نامشخص است"
        Cancel = True
    End If
End Sub

Private Sub Tafnotcash_DblClick(Cancel As Integer)
     Openaccs 8
End Sub

Private Sub Tafnotcash_GotFocus()
    Me.Tafnotcash.Requery
End Sub

Private Sub Tafnotcash_LostFocus()
    If Me.Accnotcash.Column(3) = True And IsNull(Me.Tafnotcash) Then
        MsgBox "حساب تفصيل نامشخص است"
        Me.Tafnotcash.SetFocus
    End If
End Sub
