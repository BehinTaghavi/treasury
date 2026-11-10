Attribute VB_Name = "Form_P_Bardasht"
Attribute VB_Base = "0{793E1664-71E2-4684-A567-99760A98FB15}"
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
     Openaccs 9
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
    Openaccs 44
End Sub

Private Sub babat_GotFocus()
    Me.Babat.Requery
End Sub

Private Sub Basetype_GotFocus()
    Me.Basetype.Requery
End Sub
Private Sub Bes_BeforeUpdate(Cancel As Integer)
    S = "Baccno='" & CStr(Nz(Me.Baccno, 0)) & "'"
    If IsNull(Me.bes.OldValue) Then
        Mybalance = Nz(DSum("Bed", "Subs", S), 0) - Nz(DSum("Bes", "Subs", S), 0)
    Else
        Mybalance = Nz(DSum("Bed", "Subs", S), 0) - Nz(DSum("Bes", "Subs", S), 0) + Me.bes.OldValue
    End If
    If Me.bes > Mybalance Then
        MsgBox "موجودي منفي ميشود.پرداخت امكان پذير نيست"
        Cancel = True
    End If
End Sub

Private Sub ComboBacc_AfterUpdate()
    Me.Curid = ComboBacc.Column(3)
    If Me.ComboBacc.Column(3) <> "ريال" Then
        Me.Iscur = True
        Me.bes.DecimalPlaces = 2
    Else
        Me.Iscur = False
        Me.Curid = "ريال"
        Me.bes.DecimalPlaces = 0
    End If
End Sub

Private Sub ComboP_DblClick(Cancel As Integer)
    Openaccs 43
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
       FilterStr = "InStr(1,bes,'" & Me!T5 & "',0) > 0"
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

    stDocName = "B_P_printbardasht"
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
    Me.bed = 0
    Me.Transtype = "برداشت از حساب"
    Me.Scode = 26
    Me.Stime = Time()
    Me.Currate = 1
    Me.Sal = Forms!Sal!Tsal
    S = "Scode=26 and Sal=" & Me.Sal
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
        MsgBox "نوع برداشت نامشخص است"
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
    
    If IsNull(Me.bes) Or Me.bes = 0 Then
        MsgBox "مبلغ نامشخص است"
        Me.bes.SetFocus
        Cancel = True
        Exit Sub

    End If

    
    If IsNull(Me.Person) Then
        MsgBox "دريافت كننده نامشخص است"
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
'    Me.Po_vouch.SourceObject = "Po_vouch"
'    A = Me.Po_vouch.Form.RecordsetClone.RecordCount
 
 
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
        Me.bes.DecimalPlaces = 2
    Else
        Me.bes.DecimalPlaces = 0
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
     
    Mainformint = 1
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
        If Form_Po_vouch.Tsumbes + Nz(Me.bes) <> Form_Po_vouch.Tsumbed Then
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
     Openaccs 10
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

Private Sub Transaccid_AfterUpdate()
    If IsNull(Me.Transaccid) Then
        Me.Tafno = Null
        Me.Accno = Null
    Else
        Me.Tafno = Me.Transaccid.Column(5)
        Me.Accno = Me.Transaccid.Column(6)
    End If
End Sub

Private Sub Vno_DblClick(Cancel As Integer)
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Voucher"
    
    stLinkCriteria = "[Voucher.Vid]=" & Me![Vno]
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub

Private Sub Command64_Click()
On Error GoTo Err_Command64_Click


    DoCmd.DoMenuItem acFormBar, acEditMenu, 8, , acMenuVer70
    DoCmd.DoMenuItem acFormBar, acEditMenu, 6, , acMenuVer70

Exit_Command64_Click:
    Exit Sub

Err_Command64_Click:
    MsgBox Err.Description
    Resume Exit_Command64_Click
    
End Sub
