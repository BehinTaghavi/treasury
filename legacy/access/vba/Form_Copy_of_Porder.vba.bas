Attribute VB_Name = "Form_Copy of Porder"
Attribute VB_Base = "0{86DEA72C-25AD-477A-BFFF-E3FD0D24DA7C}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mys As String, rst As Recordset
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
    Openaccs 101
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
    Openaccs 103
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

Private Sub babat_DblClick(Cancel As Integer)
    Openaccs 48
End Sub

Private Sub Baccno_AfterUpdate()
    Me.Tafnotcash.Enabled = True
    Me.Accnotcash.Enabled = True
    If IsNull(Me.Baccno) Then
        Me.Tafnotcash = Null
        Me.Accnotcash = Null
        Me.Iscur = 0
        Me.Combocur = "ريال"
    Else
        If Me.Checktype = 0 Then
            Me.Tafnotcash = Me.Baccno.Column(7)
            Me.Accnotcash = Me.Baccno.Column(6)
            Me.Iscur = Me.Baccno.Column(4)
            Me.Combocur = Me.Baccno.Column(5)
        End If
    End If
    Set rst = CurrentDb.OpenRecordset("Globals", dbOpenDynaset, dbSeeChanges)
    With rst
        Me.Hazfhavale = Nz(!Hazfhavale)
    .Close
    End With

    Me.Accnotcash.Requery
    Me.Tafnotcash.Requery
    Me.Combocur.Requery
    Me.Tafnotcash.Enabled = False
    Me.Accnotcash.Enabled = False

End Sub

Private Sub Checktype_AfterUpdate()
    If Me.Checktype = 1 Or Me.Checktype = 2 Then
        Me.Accnotcash.Enabled = True
        If Me.Checktype = 2 Then
            Me.Accno = Null
            Me.Accnotcash = Nz(DLookup("Asnadpardakhtani", "Globals"), "")
            Me.Tafnotcash.Enabled = True
            Me.Tafnotcash.SetFocus
        Else
            Me.Accno = Nz(DLookup("Accentezami", "Globals"), "")
            Me.Accnotcash = Nz(DLookup("Acctentezami", "Globals"), "")
            Me.Tafnotcash.Enabled = True
            Me.Tafnotcash = Null
            Me.Tafno.Enabled = True
            Me.Tafno.SetFocus
        End If
    Else
        Me.Accno = Null
        Me.Accnotcash = Null
        Me.Accnotcash.Enabled = False
        Me.Tafnotcash = Null
        Me.Tafnotcash.Enabled = False
    End If
    If Me.Checktype <> 0 Then
        Me.Transaccid = Null
        Me.Transaccid.Enabled = False
    Else
        Me.Transaccid.Enabled = True
    End If
    
    If IsNull(Me.Checktype) Then
        Me.Checktype = 0
    End If
    Me.Checktypen = Me.Checktype.Column(1)
    If Me.Checktype <> 0 Then
        Me.Transaccid = Null
        Me.Transaccid.Enabled = False
    End If
    'صندوق نقدي
'    If Me.Checktype = 9 Then
'        Me.Baccno = "صندوق نقدي"
'    Else
'        Me.Baccno = Null
'    End If
End Sub

Private Sub ComboP_DblClick(Cancel As Integer)
    Openaccs 47
End Sub

Private Sub Command1_Click()
On Error GoTo Err_Command1_Click
    mys = "INSERT INTO [Check] ( Rfpid, Rfpdate, Rfptime, Empid, Checktype, Checktypen, Amount, Iscur, Curid, Currate, Person, Babat, Babatid, Sal, Tafno, Accno, Accnotcash, Tafnotcash,depid )SELECT Check.Rfpid, Check.Rfpdate, Check.Rfptime, Check.Empid, Check.Checktype, Check.Checktypen, Check.Amount, Check.Iscur, Check.Curid, Check.Currate, Check.Person, Check.Babat, Check.Babatid, Check.Sal, Check.Tafno, Check.Accno, Check.Accnotcash, Check.Tafnotcash, Check.Depid FROM [Check] WHERE (((Check.Checkid)=" & Me.Checkid & "));"
    docommand mys
    Me.Requery
    If Me.RecordsetClone.RecordCount > 0 Then
        DoCmd.GoToRecord , , acLast
    End If
    Me.Rfpid = Nz(DMax("Rfpid", "Check")) + 1
    Me.Rfptime = Time()
    Me.Empid = [Forms]![Sal]![Tempid]
    Me.Sal = [Forms]![Sal]![Tsal]
    Me.Payit = True
Exit_Command1_Click:
    Exit Sub
Err_Command1_Click:
    MsgBox Err.Description
    Resume Exit_Command1_Click
End Sub
Private Sub Command2_Click()
On Error GoTo Err_Command2_Click
    If MsgBox("ركوردحذف شود؟", vbYesNo) = vbNo Then
        Exit Sub
    End If
    Delpovouch Me.Checkid, 1

    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70

Exit_Command2_Click:
    Exit Sub

Err_Command2_Click:
    MsgBox Err.Description
    Resume Exit_Command2_Click

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
    
    If IsNull(Me.Person) Then
        MsgBox "ذينفع نامشخص است"
        Me.ComboP.SetFocus
        Cancel = True
        Exit Sub
    End If

    Me.Amount.Requery
End Sub

Private Sub Babat_AfterUpdate()
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Me.Rfpdate.SetFocus
    End If
End Sub

Private Sub babat_GotFocus()
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Me.Rfpdate.SetFocus
    End If
    
    If IsNull(Me.Person) Then
        MsgBox "ذينفع نامشخص است"
        Me.ComboP.SetFocus
        Cancel = True
        Exit Sub
    End If

    If IsNull(Me.Amount) Then
        MsgBox "مبلغ نامشخص است"
        Me.Amount.SetFocus
        Cancel = True
        Exit Sub
    End If

    Me.Babat.Requery
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
       FilterStr = "InStr(1,Rfpref,'" & Me!T3 & "',0) > 0"
    End If
    If Not IsNull(Me!T5) Then
       FilterStr = "InStr(1,Amount,'" & Me!T5 & "',0) > 0"
    End If
    If Not IsNull(Me!T6) Then
       FilterStr = "InStr(1,Person,'" & Me!T6 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T7) Then
       FilterStr = "InStr(1,Babat,'" & Me!T7 & "',0) > 0"
    End If
    If Not IsNull(Me!T8) Then
       FilterStr = "InStr(1,Checkno,'" & Me!T8 & "',0) > 0"
    End If
    If Not IsNull(Me!T12) Then
       FilterStr = "InStr(1,Empid,'" & Me!T12 & "',0) > 0"
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

Private Sub Command45_Click()
    Me.Recalc

    stLinkCriteria = "[Checkid]=" & Me![Checkid]

    stDocName = "Rfp_print"
    DoCmd.OpenReport stDocName, acPreview, , stLinkCriteria
End Sub

Private Sub Form_Delete(Cancel As Integer)
    If Nz(DLookup("Editlcv", "Globals"), 0) <> 0 Then
         Cancel = DelonebyoneCancel(Nz(Me.Checkid))
    End If
End Sub

Private Sub Frame0_AfterUpdate()
    Select Case Me.Frame0
        Case 1
            mys = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype)=0 Or (Check.Checktype)=1 Or (Check.Checktype)=2 Or (Check.Checktype)=9)) ORDER BY Check.Rfpdate, Check.Checkid; "
        Case 2
            mys = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype)=0 Or (Check.Checktype)=1 Or (Check.Checktype)=2)) ORDER BY Check.Rfpdate, Check.Checkid; "
        Case 3
            mys = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype)=9 )) ORDER BY Check.Rfpdate, Check.Checkid; "
    End Select
    Me.RecordSource = mys
End Sub

Private Sub FramePay_AfterUpdate()
    Select Case Me.FramePay
        Case 1
            mys = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype)=0 Or (Check.Checktype)=1 Or (Check.Checktype)=2 Or (Check.Checktype)=9)) ORDER BY Check.Rfpdate, Check.Checkid; "
        Case 2
            mys = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype)=0 Or (Check.Checktype)=1 Or (Check.Checktype)=2 Or (Check.Checktype)=9) AND ((Check.Paied)=False)) ORDER BY Check.Rfpdate, Check.Checkid;"
        Case 3
            mys = "SELECT Check.* FROM [Check] WHERE (((Check.Dp)=2) AND ((Check.Checktype)=0 Or (Check.Checktype)=1 Or (Check.Checktype)=2 Or (Check.Checktype)=9) AND ((Check.Paied)=True)) ORDER BY Check.Rfpdate, Check.Checkid;"
    End Select
    Me.RecordSource = mys

End Sub

Private Sub Maturity_BeforeUpdate(Cancel As Integer)
    If (Me.Checktype <> 1 And Me.Checktype <> 9) And Controldate(Nz(Me.Maturity, 0), 1) = False Then Cancel = True
End Sub

Private Sub Payit_GotFocus()
    If IsNull(Me.Baccno) Then
        MsgBox "شماره حساب نامشخص است"
        Me.Baccno.SetFocus
    End If
End Sub

Private Sub PbyCheckno_GotFocus()
    Me.PbyCheckno.Requery
End Sub

Private Sub Po_vouch_Exit(Cancel As Integer)
    Me.Recalc
    If Form_Po_vouch.Form.RecordsetClone.RecordCount > 0 And (Me.Checktype = 0 Or Me.Checktype = 9) Then
        If Form_Po_vouch.Tsumbes + Nz(Me.Amount, 0) <> Form_Po_vouch.Tsumbed Then
            MsgBox "سند موازنه نيست"
            Cancel = True
        End If
    End If

End Sub

Private Sub Rfpdate_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Me.Rfpdate, 0) = False Then Cancel = True
    
End Sub

Private Sub Tafno_AfterUpdate()
    If IsNull(Me.ComboP) Then Me.ComboP = Me.Tafno.Column(2)
End Sub

Private Sub Tafno_BeforeUpdate(Cancel As Integer)
    If Me.Accno.Column(3) = True And IsNull(Me.Tafno) Then
        MsgBox "حساب تفصيل نامشخص است"
        Cancel = True
    End If
End Sub

Private Sub Tafno_DblClick(Cancel As Integer)
    Openaccs 102
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
    Openaccs 104
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

Private Sub ComboA_AfterUpdate()
'    If Me.Accno.Column(3) = -1 Then
'        Me.Tafno.Enabled = True
'    Else
'        Me.Tafno = Null
'        Me.Tafno.Enabled = False
'    End If

End Sub

Private Sub ComboP_GotFocus()
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
        Me.Rfpdate.SetFocus
    End If
    Me.ComboP.Requery
End Sub

Private Sub Command34_Click()
'    If Spfilter = 1 Then
'        Spfilter = 0
'        Me.FilterOn = False
'    Else
'        DoCmd.Quit
'    End If
'    If Me.FilterOn = True Then
'        Me.FilterOn = False
'        DoCmd.OpenForm "Po_Sm"
'        'DoCmd.GoToRecord , , acLast
'    Else
'        DoCmd.Quit
'    End If
        'DoCmd.Quit
        DoCmd.Close
End Sub

Private Sub Command43_Click()
    If Me.Paied = True Then
        MsgBox "پرداخت صورت گرفته و امكان حذف وجود ندارد"
        Exit Sub
    End If
    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70
End Sub

Private Sub Command55_Click()
    stDocName = "Po_Sm"
    DoCmd.OpenForm stDocName, , , stLinkCriteria
End Sub

Private Sub Command58_Click()
    Searchtype = 0
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Shart_m"
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub



Private Sub Form_BeforeInsert(Cancel As Integer)
  '  Me.Checkid = tellmelast(4)
    Me.Rfpid = Nz(DMax("Rfpid", "Check")) + 1
    Me.Rfptime = Time()
    Me.Empid = [Forms]![Sal]![Tempid]
    Me.Sal = [Forms]![Sal]![Tsal]
    Me.Checktypen = "چك نقدي"
    Me.Payit = True
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Rfpdate) Then
        MsgBox "تاريخ نامشخص است"
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
        MsgBox "ذينفع نامشخص است"
        Me.ComboP.SetFocus
        Cancel = True
        Exit Sub
    End If

    If IsNull(Me.Amount) Or Me.Amount = 0 Then
        MsgBox "مبلغ نامشخص است"
        Me.Amount.SetFocus
        Cancel = True
        Exit Sub
    End If

    If IsNull(Me.Babat) Then
        MsgBox "بابت نامشخص است"
        Me.Babat.SetFocus
        Cancel = True
        Exit Sub
    End If

    If Me.Iscur = True And IsNull(Me.Curid) Then
        MsgBox "نوع ارز نامشخص است"
        Me.Combocur.SetFocus
        Cancel = True
    End If
    
    If Me.Accno.Column(3) = True And IsNull(Me.Tafno) Then
        MsgBox "حساب تفصيل نامشخص است"
        Me.Tafno.SetFocus
        Cancel = True
    End If
    
    If Me.Accnotcash.Column(3) = True And IsNull(Me.Tafnotcash) Then
        MsgBox "حساب تفصيل نامشخص است"
        Me.Tafnotcash.Enabled = True
        Me.Tafnotcash.SetFocus
        Cancel = True
    End If
End Sub

Private Sub Form_Current()
    Currecord = Me.CurrentRecord
    Pubid = Nz(Me.Checkid)
    Mainformint = 6
    Isfinal = Nz(Me.Final, False)
    
    Me.AllowEdits = Not Isfinal
    Me.AllowDeletions = Not Isfinal

    Me.Checktype.Enabled = Forms!Sal!Canvp
    Me.Baccno.Enabled = Forms!Sal!Canvp
    Me.Maturity.Enabled = Forms!Sal!Canvp
    Me.Payit.Enabled = Forms!Sal!Canvp
    
    If Me.Final = True Then
        Me.AllowEdits = False
        Me.AllowDeletions = False
    Else
        Me.AllowEdits = True
        Me.AllowDeletions = True
    End If
    If Me.Rfpdate <= Nochangedate Then
        Me.AllowEdits = False
        Me.AllowDeletions = False
        Me.Po_vouch.Enabled = False
    Else
        Me.AllowEdits = True
        Me.AllowDeletions = True
        Me.Po_vouch.Enabled = True
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

   
'    If Me.Checktype = 2 Then
'        Me.Accnotcash.Enabled = True
'    Else
'        Me.Accnotcash.Enabled = False
'    End If
'
'    If Me.Accno.Column(3) = -1 Then
'        Me.Tafno.Enabled = True
'    Else
'        Me.Tafno.Enabled = False
'    End If
'
'    If Me.Accnotcash.Column(3) = -1 Then
'        Me.Tafnotcash.Enabled = True
'    Else
'        Me.Tafnotcash.Enabled = False
'    End If

    If Me.Iscur = -1 Then
        Me.Combocur.Enabled = True
    Else
        Me.Combocur.Enabled = False
    End If
    If Me.Checktype <> 0 Then
        Me.Transaccid.Enabled = False
    Else
        Me.Transaccid.Enabled = True
    End If
    
'    If Me.Paied = True Then
'        Me.AllowEdits = False
'    Else
'        Me.AllowEdits = True
'    End If
    Me.Rfpdate.SetFocus
End Sub

Private Sub Form_Open(Cancel As Integer)

'    If Forms!Sal!Isadd = True Then
'        Me.RecordSource = "SELECT Check.* FROM Check WHERE Sal=Forms!sal!Tsal And Checktype in (0,1,2)And dp=2;"
'    Else
'        Me.RecordSource = "SELECT Check.* FROM Check WHERE Sal=Forms!sal!Tsal And Checktype in (0,1,2) And dp=2 And Empid=Forms!sal!Tempid;"
'    End If
    
    'DoCmd.Maximize
    If Me.RecordsetClone.RecordCount > 0 Then
        DoCmd.GoToRecord , , acLast
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

Private Sub Command49_Click()
On Error GoTo Err_Command49_Click

    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Sm"
    DoCmd.OpenForm stDocName, , , stLinkCriteria

Exit_Command49_Click:
    Exit Sub

Err_Command49_Click:
    MsgBox Err.Description
    Resume Exit_Command49_Click
End Sub
