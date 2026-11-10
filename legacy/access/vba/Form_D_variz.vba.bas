Attribute VB_Name = "Form_D_variz"
Attribute VB_Base = "0{C958B9F3-D280-4056-A949-53B32A8D1B36}"
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
     Openaccs 13
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

Private Sub babat_DblClick(Cancel As Integer)
    Openaccs 46
End Sub

Private Sub babat_GotFocus()
    Me.Babat.Requery
End Sub

Private Sub Basetype_GotFocus()
    Me.Basetype.Requery
End Sub
Private Sub ComboBacc_AfterUpdate()
    Me.Curid = ComboBacc.Column(3)
    If Me.ComboBacc.Column(3) <> "ريال" Then
        Me.Iscur = True
        Me.L‍cur.Visible = True
        Me.Currate.Visible = True
        Me.bed.DecimalPlaces = 2
    Else
        Me.Iscur = False
        Me.L‍cur.Visible = False
        Me.Currate.Visible = False
        Me.Curid = "ريال"
        Me.bed.DecimalPlaces = 0
    End If
End Sub

Private Sub ComboP_DblClick(Cancel As Integer)
    Openaccs 45
End Sub

Private Sub ComboP_GotFocus()
    Me.ComboP.Requery
End Sub

Private Sub Command28_Click()
    Dim FilterStr As String
    FilterStr = ""
    
    If Not IsNull(Me!T12) Then
       FilterStr = "InStr(1,Sdate,'" & Me!T12 & "',0) > 0"
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
       FilterStr = "InStr(1,Bed,'" & Me!T5 & "',0) > 0"
    End If
'    If Not IsNull(Me!T6) Then
'       FilterStr = "InStr(1,Maturity,'" & Me!T6 & "',0) > 0"
'    End If
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

Private Sub Command42_Click()
    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 2, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 5, , acMenuVer70 'Paste Append
    Me.Checkno = Null
    Me.Checkno.SetFocus
End Sub

Private Sub Command58_Click()
    If Me.Vno = 0 Or IsNull(Me.Vno) Then
        MsgBox "سند صادرنشده است"
        Exit Sub
    Else
        Dim stDocName As String
        Dim stLinkCriteria As String
    
        stDocName = "Voucher"
        
        stLinkCriteria = "[Voucher.Vid]=" & Me![Vno]
        DoCmd.OpenForm stDocName, , , stLinkCriteria
    End If
End Sub

Private Sub Command59_Click()
    stLinkCriteria = "[Sid]=" & Me![Sid]

    stDocName = "B_D_printvariz"
    DoCmd.OpenReport stDocName, acPreview, , stLinkCriteria

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
    Me.bes = 0
    Me.Transtype = "واريز به حساب"
    Me.Scode = 16
    Me.Stime = Time()
    Me.Sal = Forms!Sal!Tsal
    Me.Currate = 1
    S = "Scode=16 and Sal=" & Me.Sal
    Me.Sno = Nz(DMax("Sno", "Subs", S)) + 1
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Sdate) Then
        MsgBox "تاريخ نامشخص است"
        Me.Sdate.SetFocus
        Cancel = True
        Exit Sub
    End If

    If IsNull(Me.Basetype) Then
        MsgBox "نوع واريز نامشخص است"
        Me.Basetype.SetFocus
        Cancel = True
        Exit Sub
    End If

    If IsNull(Me.Baccno) Then
        MsgBox "شماره حساب نامشخص است"
        Me.ComboBacc.SetFocus
        Cancel = True
        Exit Sub
    End If
    
    If IsNull(Me.bed) Or Me.bed = 0 Then
        MsgBox "مبلغ نامشخص است"
        Me.bed.SetFocus
        Cancel = True
        Exit Sub

    End If

    
    If IsNull(Me.Person) Then
        MsgBox "پرداخت كننده نامشخص است"
        Me.ComboP.SetFocus
        Cancel = True
        Exit Sub
    End If



    If IsNull(Me.Babat) Then
        MsgBox "بابت نامشخص است"
        Me.Babat.SetFocus
        Cancel = True
        Exit Sub
    End If

End Sub

Private Sub Form_Current()
    Isfinal = Nz(Me.Final, False)
    Me.AllowEdits = Not Nz(Me.Final)
    Me.AllowDeletions = Not Nz(Me.Final)
    
    If Me.Sdate <= Nochangedate Then
        lockcontrol = 1
        Me.AllowEdits = False
        Me.AllowDeletions = False
        Me.Po_vouch.Enabled = False
    Else
        lockcontrol = 0
        Me.AllowEdits = True
        Me.AllowDeletions = True
        Me.Po_vouch.Enabled = True
    End If
    
    If Me.Accno.Column(3) = True Then
        Me.Tafno.Enabled = True
    Else
        Me.Tafno.Enabled = False
    End If
    If Me.ComboBacc.Column(3) <> "ريال" Then
        Me.bed.DecimalPlaces = 2
    Else
        Me.bed.DecimalPlaces = 0
    End If

End Sub

Private Sub Form_Delete(Cancel As Integer)
    If Nz(DLookup("Editlcv", "Globals"), 0) <> 0 Then
         Cancel = DelonebyoneCancel(Nz(Me.Sid))
    End If
End Sub

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Maximize
    If Me.RecordsetClone.RecordCount > 0 Then
        DoCmd.GoToRecord , , acLast
    End If

    Mainformint = 3
End Sub

Private Sub Frame1_AfterUpdate()
    Select Case Me.Frame1
        Case 1
            Me.Accno.RowSource = "SELECT Acc.Accno0, Acc.Accno, Acc.Accname, Acc.Has_Unique_Account FROM Acc WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen) And ((Acc.Has_Sub_Subsidery)=False)) ORDER BY Acc.Accno;"
            Me.Tafno.RowSource = "SELECT Taf.Tafno0, Taf.Tafno, Taf.Tafname FROM Taf ORDER BY Taf.Tafno; "
        Case 2
            Me.Accno.RowSource = " SELECT Acc.Accno0, Acc.Accname, Acc.Accno, Acc.Has_Unique_Account FROM Acc WHERE (((Len(nz(Trim([Accno0]))))>=forms!sal!Lmoeen) And ((Acc.Has_Sub_Subsidery)=False)) ORDER BY Acc.Accname;"
            Me.Tafno.RowSource = " SELECT Taf.Tafno0, Taf.Tafname, Taf.Tafno FROM Taf ORDER BY Taf.Tafname; "
    End Select
    
    Me.Accno.Requery
    Me.Tafno.Requery
End Sub
Private Sub Command49_Click()
On Error GoTo Err_Command49_Click
    If MsgBox("ركوردحذف شود؟", vbYesNo) = vbNo Then
        Exit Sub
    End If

    Delpovouch Me.Sid, 2
    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70

Exit_Command49_Click:
    Exit Sub

Err_Command49_Click:
    MsgBox Err.Description
    Resume Exit_Command49_Click
    
End Sub
Private Sub Command50_Click()
On Error GoTo Err_Command50_Click


    DoCmd.Close

Exit_Command50_Click:
    Exit Sub

Err_Command50_Click:
    MsgBox Err.Description
    Resume Exit_Command50_Click
    
End Sub

Private Sub Po_vouch_Exit(Cancel As Integer)
    Me.Recalc
    If Form_Po_vouch.Form.RecordsetClone.RecordCount > 0 Then
            If Form_Po_vouch.Tsumbes <> Form_Po_vouch.Tsumbed + Nz(Me.bed) Then
                MsgBox "سند موازنه نيست"
                Cancel = True
            End If
    End If
End Sub

Private Sub Sdate_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.Sdate) Then
        MsgBox "تاريخ نامشخص است"
        Cancel = True
    End If
    If Controldate(Me.Sdate, 0) = False Then Cancel = True
End Sub

Private Sub Tafno_BeforeUpdate(Cancel As Integer)
    If Me.Accno.Column(3) = True And IsNull(Me.Tafno) Then
        MsgBox "حساب تفصيل نامشخص است"
        Cancel = True
    End If
End Sub
Private Sub Tafno_DblClick(Cancel As Integer)
  Openaccs 14
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

Private Sub Vno_DblClick(Cancel As Integer)
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Voucher"
    
    stLinkCriteria = "[Voucher.Vid]=" & Me![Vno]
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub
