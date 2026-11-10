Attribute VB_Name = "Form_D_Check"
Attribute VB_Base = "0{C0C39B7F-AC95-4062-89DE-98C9251F8F4D}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mystr As String, mystr1 As String, mybankname As String
Dim rst As Recordset, rst1 As Recordset, mydesc As String, vu As Boolean
Dim Myperson As String, Mydate As String

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
    Openaccs 17
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
    Openaccs 19
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
    If IsNull(Me.Amount) Then
        Me.Amount = 0
    End If
    Me.D1 = Me.Rfpdate
    If Me.Checktype = 0 Or Me.Checktype = 2 Then
        Me.Signed = True
    Else
        Me.Signed = False
    End If
End Sub
Private Sub Babat_AfterUpdate()
    Me.Babat.Requery
End Sub

Private Sub babat_DblClick(Cancel As Integer)
    Openaccs 36
End Sub

Private Sub babat_GotFocus()
    Me.Babat.Requery
End Sub

Private Sub Baccno_GotFocus()
Me.Baccno.Requery
End Sub

Private Sub Checkno_BeforeUpdate(Cancel As Integer)
    If dupit = True Then
        dupit = False
        Exit Sub
    End If

    If IsNull(Me.Checkno) Then
        MsgBox "شماره چك نامشخص است"
        Cancel = True
        Exit Sub
    End If
    
    
    mystr = "select * from Check where Dp=1 and Checkno= " & "'" & CStr(Me.Checkno) & "'"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            Exit Sub
        Else
            Dim stDocName As String
            Dim stLinkCriteria As String
        
            stDocName = "Ch_dups"
            
            stLinkCriteria = "[Checkno]=" & "'" & Me![Checkno] & "'"
            DoCmd.OpenForm stDocName, , , stLinkCriteria
        End If
    End With
    
End Sub

Private Sub Checktype_AfterUpdate()
    If IsNull(Me.Checktype) Then
        Me.Checktype = 0
    End If
    If Me.Checktype = 0 Or Me.Checktype = 2 Then
        Me.Signed = True
        Me.ComboBacc.Enabled = True
    Else
        Me.Signed = False
        Me.ComboBacc.Enabled = False
    End If
    Me.Checktypen = Me.Checktype.Column(1)
End Sub

Private Sub Cityid_GotFocus()
    Me.Cityid.Requery
End Sub

Private Sub Combo59_GotFocus()
    Me.Combo59.Requery
End Sub

Private Sub ComboBacc_AfterUpdate()
    If Me.ComboBacc.Column(3) <> "ريال" Then
        Me.Iscur = True
        'Me.Lcur.Visible = True
    Else
        Me.Iscur = False
        'Me.Lcur.Visible = False
    End If

End Sub

Private Sub combobankid_AfterUpdate()

'    mybankname = Me.combobankid
'    mystr = "SELECT Bankacc.* FROM Bankacc WHERE (((Bankacc.Ispool)=True) AND ((Bankacc.Bank)=" & "'" & CStr(mybankname) & "'));"
'    Set rst = CurrentDb.OpenRecordset(mystr)
'    With rst
'        If .RecordCount = 0 Then
'            mystr1 = "SELECT * FROM Bankacc WHERE Mainpool=True"
'            Set rst1 = CurrentDb.OpenRecordset(mystr1)
'            With rst1
'                If .RecordCount = 0 Then
'                    GoTo line10
'                Else
'                    Me.Vagno = !Baccno
'                End If
'            End With
'        Else
'            Me.Vagno = !Baccno
'        End If
'    .Close
'    End With
'line10:
'    Me.ComboBacc.Requery
End Sub

Private Sub combobankid_BeforeUpdate(Cancel As Integer)
    If dupit = True Then
        'dupit = False
        Exit Sub
    End If

'    mystr = "Select * from Check Where Dp=1 and Checkno=" & "'" & CStr(Me.Checkno) & "' And  Bankid=" & "'" & CStr(Me.combobankid) & "'"
'    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
'    With rst
'        If .RecordCount = 0 Then
'            Exit Sub
'        Else
'            MsgBox "شماره چك تكراري است"
'            Cancel = True
'            Exit Sub
'        End If
'    End With

End Sub

Private Sub combobankid_GotFocus()
    Me.combobankid.Requery
End Sub
Private Sub Combocontno_AfterUpdate()
'    If IsNull(Me.Comboinvno) Then
'        Me.Babat = Forms!Porder_m![ComboP].Column(1) & " بابت " & " قرارداد: " & Me.Combocontno
'    Else
'        Me.Babat = Forms!Porder_m![ComboP].Column(1) & " بابت " & "صورتحساب: " & Me.Comboinvno & "ازقرارداد: " & Me.Combocontno
'    End If
End Sub

Private Sub Combocur_AfterUpdate()
    If IsNull(Combocur) Then
        Combocur = 0
    End If
End Sub

Private Sub Combosubg_AfterUpdate()
'    Me.ComboA = Me.Combosubg.Column(3)
End Sub

Private Sub Command24_Click()
On Error GoTo Err_Command24_Click

    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70

Exit_Command24_Click:
    Exit Sub

Err_Command24_Click:
    MsgBox Err.Description
    Resume Exit_Command24_Click
    
End Sub

Private Sub Command30_Click()
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "People"
    DoCmd.OpenForm stDocName, , , stLinkCriteria
End Sub

Private Sub Command53_Click()
    MsgBox Reporder(0)
    MsgBox Repcondition(1)
End Sub

Private Sub Command58_Click()
    Searchtype = 0
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Shart_m"
    DoCmd.OpenForm stDocName, , , stLinkCriteria
End Sub

Private Sub Command130_Click()
    DoCmd.Close
End Sub

Private Sub Command132_Click()
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Check_d_dates"
    
    stLinkCriteria = "[Checkid]=" & Me![Checkid]
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub

Private Sub Command147_Click()
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Frmbabsub"
    
    stLinkCriteria = "[Checkid]=" & Nz(Me![Checkid], 0)
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub

Private Sub Command28_Click()
    Dim FilterStr As String
    FilterStr = ""
    
    If Not IsNull(Me!T1) Then
       FilterStr = "InStr(1,Rfpid,'" & Me!T1 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T2) Then
       FilterStr = "InStr(1,Rfpdate,'" & Me!T2 & "',0) > 0"
    End If
    If Not IsNull(Me!T3) Then
       FilterStr = "InStr(1,Checkno,'" & Me!T3 & "',0) > 0"
    End If
    If Not IsNull(Me!T4) Then
       FilterStr = "InStr(1,Rfpid,'" & Me!T4 & "',0) > 0"
    End If
    If Not IsNull(Me!T5) Then
       FilterStr = "InStr(1,Amount,'" & Me!T5 & "',0) > 0"
    End If
    If Not IsNull(Me!T6) Then
       FilterStr = "InStr(1,Maturity,'" & Me!T6 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T7) Then
       FilterStr = "InStr(1,Babat,'" & Me!T7 & "',0) > 0"
    End If
    If Not IsNull(Me!T8) Then
       FilterStr = "InStr(1,Pbycheckno,'" & Me!T8 & "',0) > 0"
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

Private Sub Final_AfterUpdate()
    Me.Refresh
    Me.AllowEdits = Not Nz(Me.Final)
    Me.AllowDeletions = Not Nz(Me.Final)
End Sub
Private Sub Final_BeforeUpdate(Cancel As Integer)
    If MsgBox("اطمينان داريد؟", vbYesNo) = vbNo Then
        Cancel = True
    End If
End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Rfpid = tellmelastresid()
    Me.Rfptime = Time()
    Me.Dp = 1
    Me.Sal = [Forms]![Sal]![Tsal]
    Me.Empid = [Forms]![Sal]![Tempid]
    Me.Status = "نزدصندوق"
    Me.Stcode = 11
    Me.Accno = Bedtejari()
    Me.Accnotcash = Chnazdsand()
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)

    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ دريافت نامشخص است"
        Me.Rfpdate.SetFocus
        Cancel = True
        Exit Sub
    End If

    If IsNull(Me.Checktype) Then
        MsgBox "نوع چك نامشخص است"
        Me.Checktype.SetFocus
        Cancel = True
        Exit Sub
    End If
    
    If IsNull(Me.Person) Then
        MsgBox "صادركننده نامشخص است"
        Me.Person.SetFocus
        Cancel = True
        Exit Sub
    End If

    If IsNull(Me.Checkno) Then
        MsgBox "شماره چك نامشخص است"
        Me.Checkno.SetFocus
        Cancel = True
        Exit Sub
    End If

    If IsNull(Me.Amount) Or Me.Amount = 0 Then
        MsgBox "مبلغ نامشخص است"
        Me.Amount.SetFocus
        Cancel = True
        Exit Sub

    End If

    If (Me.Checktype = 0 Or Me.Checktype = 2) And IsNull(Me.Maturity) Then
        MsgBox "سررسيد نامشخص است"
        Me.Maturity.SetFocus
        Cancel = True
        Exit Sub
    End If

    If IsNull(Me.Babat) Then
        MsgBox "بابت نامشخص است"
        Me.Babat.SetFocus
        Cancel = True
        Exit Sub
    End If
    
    If (Me.Checktype = 0 Or Me.Checktype = 1 Or Me.Checktype = 2) And IsNull(Me.combobankid) Then
        MsgBox "بانك نامشخص است"
        Me.combobankid.SetFocus
        Cancel = True
        Exit Sub
    End If
    
    
'    If dupit = True Then
'        mystr = "Select * from Check Where Dp=1 and Checkno=" & "'" & CStr(Me.Checkno) & "' And  Bankid=" & "'" & CStr(Me.combobankid) & "'"
'        Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
'        With rst
'        If .RecordCount = 0 Then
'            dupit = False
'        Else
'            MsgBox "شماره چك تكراري است"
'            Cancel = True
'            Exit Sub
'        End If
'        End With
'    End If

    

    If IsNull(Me.Checktype) Then
        Me.Checktype = 0
    End If
    If Me.Checktype = 0 Or Me.Checktype = 2 Then
        Me.Signed = True
    Else
        Me.Signed = False
    End If

End Sub

Private Sub Form_Close()
  '  Checktypename
End Sub

Private Sub Form_Current()
    
    Isfinal = Nz(Me.Final, False)
    Me.AllowEdits = Not Nz(Me.Final)
    Me.AllowDeletions = Not Nz(Me.Final)
    
    Me.Rfpdate.SetFocus
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
        Me.Currate.Enabled = True
        Me.Amount.DecimalPlaces = 2
    Else
        Me.Combocur.Enabled = False
        Me.Currate.Enabled = False
        Me.Amount.DecimalPlaces = 0
    End If

    If Me.Checktype = 0 Or Me.Checktype = 2 Then
        Me.ComboBacc.Enabled = True
    Else
        Me.ComboBacc.Enabled = False
    End If

'    If Me.Checktype = 1 Or Me.Checktype = 6 Then
'        Me.ComboD.Enabled = True
'    Else
'        Me.ComboD.Enabled = False
'    End If


    If myopenform <> "" Then
        Dim stDocName As String
        Dim stLinkCriteria As String
    
        stDocName = myopenform
        
        DoCmd.Close acForm, stDocName
        
        stLinkCriteria = "[Checkid]=" & Me.Checkid
        DoCmd.OpenForm stDocName, , , stLinkCriteria
    End If
End Sub
Private Sub Form_Delete(Cancel As Integer)
    If Nz(DLookup("Editlcv", "Globals"), 0) <> 0 Then
         Cancel = DelonebyoneCancel(Nz(Me.Checkid))
    End If
End Sub

Private Sub Form_Open(Cancel As Integer)
    Babform = 1
    disableit = 1
    If Forms![Sal]![Tsal] = 0 Or IsNull(Forms![Sal]![Tsal]) Then
        MsgBox "سال مالي نامشخص است!"
        Cancel = True
        Exit Sub
    End If
    If IsNull([Forms]![Sal]![Tempid]) Then
        MsgBox "كاربر نامشخص است!"
        Cancel = True
        Exit Sub
    End If
    'DoCmd.Maximize
    DoCmd.GoToRecord , , acLast
    Mainformint = 5
End Sub

Private Sub Command63_Click()
On Error GoTo Err_Command63_Click
    dupit = True
    
    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 2, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 5, , acMenuVer70 'Paste Append
    Me.Rfpdate = CurrentDateFarsi(Date)
    Me.D1 = CurrentDateFarsi(Date)
    Me.Checkno = Null
    Me.Checkno.SetFocus
    
Exit_Command63_Click:
    Exit Sub

Err_Command63_Click:
    MsgBox Err.Description
    Resume Exit_Command63_Click
    
End Sub


Private Sub Command79_Click()
On Error GoTo Err_Command79_Click

    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Ch_dups"
    
    stLinkCriteria = "[Checkno]=" & "'" & Me![Checkno] & "'"
    DoCmd.OpenForm stDocName, , , stLinkCriteria

Exit_Command79_Click:
    Exit Sub

Err_Command79_Click:
    MsgBox Err.Description
    Resume Exit_Command79_Click
    
End Sub

Private Sub Iscur_AfterUpdate()
    If Me.Iscur = True Then
        Me.Combocur.Enabled = True
        Me.Currate.Enabled = True
        Me.Curid = "دلار"
        Me.Amount.DecimalPlaces = 2
    Else
        Me.Combocur.Enabled = False
        Me.Currate.Enabled = False
        Me.Curid = "ريال"
        Me.Amount.DecimalPlaces = 2
    End If
    'Me.Recalc
End Sub

Private Sub Label100_Click()
        MsgBox Babform
End Sub

Private Sub Maturity_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Maturity) Then Exit Sub
    If Me.Maturity <> Null And Controldate(Me.Maturity, 1) = False Then Cancel = True
End Sub

Private Sub Person_DblClick(Cancel As Integer)
    Openaccs 35
End Sub

Private Sub Person_GotFocus()
    Me.Person.Requery
End Sub

Private Sub Po_vouch_Exit(Cancel As Integer)
'    If [Po_vouch].Form![Tsumbed] <> [Po_vouch].Form![Tsumbes] Then
'        MsgBox "سند موازنه نيست"
'        Cancel = True
'    End If
End Sub

Private Sub Rfpdate_AfterUpdate()
    Me.D1 = Me.Rfpdate
End Sub
Private Sub Command101_Click()
On Error GoTo Err_Command101_Click
    Me.Recalc
    Dim stDocName As String
    Myperson = Nz(Me.Tafnotcash, 1)
    Mydate = Nz(Me.Rfpdate, 0)
    mystr = "SELECT * FROM [Check] WHERE Dp=1 and Tafnotcash= '" & Myperson & "' AND Rfpdate='" & Mydate & "';"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    With rst
        If .RecordCount = 0 Then
            GoTo Line10
        Else
            S = "Delete * from Voucher_row_tmp"
             docommand S
            .MoveLast
            .MoveFirst
        End If
        While Not .EOF
            V_tmp !Checkid
        .MoveNext
        Wend
    .Close
    End With
Line10:
    stDocName = "Resid_Check"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command101_Click:
    Exit Sub

Err_Command101_Click:
    MsgBox Err.Description
    Resume Exit_Command101_Click
    
End Sub
Private Sub Command1_Click()
On Error GoTo Err_Command1_Click
    If MsgBox("ركوردحذف شود؟", vbYesNo) = vbNo Then
        Exit Sub
    End If
    Delpovouch Me.Checkid, 1
    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70

Exit_Command1_Click:
    Exit Sub

Err_Command1_Click:
    MsgBox Err.Description
    Resume Exit_Command1_Click
    
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
    Openaccs 18
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

Private Sub Tafnotcash_AfterUpdate()
    Me.Tafno = Me.Tafnotcash
End Sub

Private Sub Tafnotcash_BeforeUpdate(Cancel As Integer)
    If Me.Accnotcash.Column(3) = True And IsNull(Me.Tafnotcash) Then
        MsgBox "حساب تفصيل نامشخص است"
        Cancel = True
    End If
End Sub

Private Sub Tafnotcash_DblClick(Cancel As Integer)
    Openaccs 20
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
Private Sub Command142_Click()
On Error GoTo Err_Command142_Click
    If IsNull(Me.Tafnotcash) Then
        MsgBox "حساب تفصيل پرداخت كننده نامشخص است"
        Exit Sub
    End If
    
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Invsamount"
    
    stLinkCriteria = "[Checkid]=" & Me![Checkid]
    DoCmd.OpenForm stDocName, , , stLinkCriteria

Exit_Command142_Click:
    Exit Sub

Err_Command142_Click:
    MsgBox Err.Description
    Resume Exit_Command142_Click
    
End Sub
