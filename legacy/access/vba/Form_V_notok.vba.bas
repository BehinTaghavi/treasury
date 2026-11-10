Attribute VB_Name = "Form_V_notok"
Attribute VB_Base = "0{1040E928-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database


Private Sub Accvno_DblClick(Cancel As Integer)
    Dim stDocName As String
    Dim stLinkCriteria As String
    Select Case Me.Nottype
        Case 2
            stDocName = "Check_Print_single"
            stLinkCriteria = "[Checkid]=" & Me![Itid]
        Case 16
            stDocName = "D_variz"
            stLinkCriteria = "[Sid]=" & Me![Itid]
        Case 17
            stDocName = "D_naghd"
            stLinkCriteria = "[Sid]=" & Me![Itid]
        Case 26
            stDocName = "P_Bardasht"
            stLinkCriteria = "[Sid]=" & Me![Itid]
        Case 27
            stDocName = "P_naghd"
            stLinkCriteria = "[Sid]=" & Me![Itid]
        Case 1
            stDocName = "D_Check"
            stLinkCriteria = "[Checkid]=" & Me![Itid]
        Case 98
            stDocName = "Pordersub_vouch"
            stLinkCriteria = "[Checkid]=" & Me![Itid]
        Case 99
            stDocName = "Pordersub_vouch"
            stLinkCriteria = "[Sid]=" & Me![Itid]
    End Select
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub

Private Sub Command28_Click()
    Dim FilterStr As String
    FilterStr = ""
    
    If Not IsNull(Me!T1) Then
       FilterStr = "InStr(1,Notdate,'" & Me!T1 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T2) Then
       FilterStr = "InStr(1,Notname,'" & Me!T2 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T3) Then
       FilterStr = "InStr(1,Tafno,'" & Me!T3 & "',0) > 0"
    End If
    If Not IsNull(Me!T4) Then
       FilterStr = "InStr(1,Accno,'" & Me!T4 & "',0) > 0"
    End If
    If Not IsNull(Me!T5) Then
       FilterStr = "InStr(1,errordesc,'" & Me!T5 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T6) Then
       FilterStr = "InStr(1,Checkno,'" & Me!T6 & "',0) > 0"
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

Private Sub Form_Close()
    Spfilter = 0
End Sub

Private Sub Form_Open(Cancel As Integer)
    Spfilter = 1
        DoCmd.Maximize
End Sub

Private Sub Vid_DblClick(Cancel As Integer)
    Dim stDocName As String
    Dim stLinkCriteria As String

    stDocName = "Voucher"
    
    stLinkCriteria = "[Voucher.Vid]=" & Me![Vid]
    DoCmd.OpenForm stDocName, , , stLinkCriteria

End Sub
Private Sub Command21_Click()
On Error GoTo Err_Command21_Click


    DoCmd.Close

Exit_Command21_Click:
    Exit Sub

Err_Command21_Click:
    MsgBox Err.Description
    Resume Exit_Command21_Click
    
End Sub
Private Sub Command22_Click()
On Error GoTo Err_Command22_Click

    Dim stDocName As String

    stDocName = "V_errors_rep"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command22_Click:
    Exit Sub

Err_Command22_Click:
    MsgBox Err.Description
    Resume Exit_Command22_Click
    
End Sub
