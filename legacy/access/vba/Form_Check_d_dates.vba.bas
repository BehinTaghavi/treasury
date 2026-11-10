Attribute VB_Name = "Form_Check_d_dates"
Attribute VB_Base = "0{D53D765D-BB65-4E06-8C2C-BB9580E8A302}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Checkbacked_Enter()
    Me.Status.Requery
    If Me.Stcode = 14 Then
        Me.Checkbacked.Enabled = True
    Else
        Me.Command18.SetFocus
        Me.Checkbacked.Enabled = False
    End If
End Sub

Private Sub Combos_AfterUpdate()
    Me.Status = Me.Combos.Column(0)
    Select Case Me.Combos
        Case 11
            Me.D1.SetFocus
            Me.D3 = Null
            Me.ComboBacc = Null
            Me.D3.Enabled = False
            Me.D4.Enabled = False
            Me.D5.Enabled = False
        Case 12, 15, 16
            Me.D3.Enabled = True
            Me.D3.SetFocus
            Me.D4.Enabled = False
            Me.D5.Enabled = False
            Me.Vos = False
        Case 13, 14
            Me.D3.Enabled = True
            Me.D4.Enabled = True
            Me.D4.SetFocus
            Me.D5.Enabled = False
        Case 19
            Me.D3.Enabled = True
            Me.D4.Enabled = True
            Me.D5.Enabled = True
            Me.D5.SetFocus
    End Select
    Select Case Me.Combos
        Case 16
            Me.Acckharj.Enabled = True
            Me.TafKharj.Enabled = True
        Case Else
            Me.Acckharj = Null
            Me.TafKharj = Null
            Me.Acckharj.Enabled = False
            Me.TafKharj.Enabled = False
    End Select
    If Me.Stcode = 13 Then
        Me.Comeel.Enabled = True
    Else
        Me.Comeel = False
        Me.Comeel.Enabled = False
    End If
End Sub

Private Sub Command18_Click()
On Error GoTo Err_Command18_Click


    DoCmd.Close

Exit_Command18_Click:
    Exit Sub

Err_Command18_Click:
    MsgBox Err.Description
    Resume Exit_Command18_Click
    
End Sub
Private Sub D3_BeforeUpdate(Cancel As Integer)
    If Me.D3 < Me.D1 Then
        MsgBox "تاريخ واگذاري از تاريخ دريافت كوچكتر است"
        Cancel = True
    End If
End Sub
Private Sub D4_BeforeUpdate(Cancel As Integer)
    If Me.D4 < Me.D3 Then
        MsgBox "تاريخ وصول از تاريخ واگذاري كوچكتر است"
        Cancel = True
    End If
End Sub
Private Sub Form_Close()
    If Me.Stcode = 12 Then
        Isbankicheck Me.Checkid, Nz(Me.D3, CurrentDateFarsi(Date))
    End If
    myopenform = ""
End Sub

Private Sub Form_Current()
    Me.AllowEdits = Not (Isfinal)
    Me.AllowDeletions = Not (Isfinal)
    If Me.Checktype = 1 Or Me.Checktype = 6 Then
        Me.D2.Enabled = True
        Me.Ctocash.Enabled = True
        Me.V2.Enabled = True
    Else
        Me.D2.Enabled = False
        Me.Ctocash.Enabled = False
        Me.V2.Enabled = False
    End If
    Select Case Me.Combos
        Case 11
            Me.D1.SetFocus
            Me.D3.Enabled = False
            Me.D4.Enabled = False
            Me.D5.Enabled = False
        Case 12, 15, 16
            Me.D3.Enabled = True
            Me.D3.SetFocus
            Me.D4.Enabled = False
            Me.D5.Enabled = False
        Case 13, 14
            Me.D3.Enabled = True
            Me.D4.Enabled = True
            Me.D4.SetFocus
            Me.D5.Enabled = False
        Case 19
            Me.D3.Enabled = True
            Me.D4.Enabled = True
            Me.D5.Enabled = ytue
            Me.D5.SetFocus
    End Select
    Select Case Me.Combos
        Case 16
            Me.Acckharj.Enabled = True
            Me.TafKharj.Enabled = True
        Case Else
            Me.Acckharj.Enabled = False
            Me.TafKharj.Enabled = False
    End Select

    If Me.Stcode = 13 Then
        Me.Comeel.Enabled = True
    Else
        Me.Comeel.Enabled = False
    End If

End Sub

Private Sub Form_Open(Cancel As Integer)
    myopenform = Me.Name
End Sub
Private Sub TafKharj_AfterUpdate()
    Me.ComboBacc = Me.TafKharj.Column(2)
End Sub

Private Sub V1_DblClick(Cancel As Integer)
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

Private Sub V4_DblClick(Cancel As Integer)
    If Me.V4 = 0 Or IsNull(Me.V4) Then
        MsgBox "سند صادرنشده است"
        Exit Sub
    Else
        Dim stDocName As String
        Dim stLinkCriteria As String
    
        stDocName = "Voucher"
        
        stLinkCriteria = "[Voucher.Vid]=" & Me![V4]
        DoCmd.OpenForm stDocName, , , stLinkCriteria
    End If

End Sub

Private Sub V5_DblClick(Cancel As Integer)
    If Me.V5 = 0 Or IsNull(Me.V5) Then
        MsgBox "سند صادرنشده است"
        Exit Sub
    Else
        Dim stDocName As String
        Dim stLinkCriteria As String
    
        stDocName = "Voucher"
        
        stLinkCriteria = "[Voucher.Vid]=" & Me![V5]
        DoCmd.OpenForm stDocName, , , stLinkCriteria
    End If

End Sub
