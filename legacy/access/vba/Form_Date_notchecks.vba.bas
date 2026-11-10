Attribute VB_Name = "Form_Date_notchecks"
Attribute VB_Base = "0{1040E85B-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Dim stDocName As String

Private Sub Command26_Click()
    Select Case Me.Frame1
        Case 1
            stDocName = "D_CHECK"
        Case 2
            stDocName = "D_CHECK_bymat"
    End Select
    DoCmd.OpenReport stDocName, acPreview
End Sub

Private Sub ChBabat_AfterUpdate()
    If Me.ChBabat = True Then
        Me.Babat.Enabled = True
    Else
        Me.Babat.Enabled = False
    End If

End Sub

Private Sub ChBacc_AfterUpdate()
    If Me.ChBacc = True Then
        Me.Bacc.Enabled = True
    Else
        Me.Bacc.Enabled = False
    End If
End Sub

Private Sub ChPerson_AfterUpdate()
    If Me.ChPerson = True Then
        Me.Person.Enabled = True
    Else
        Me.Person.Enabled = False
    End If
End Sub

Private Sub Command31_Click()
    DoCmd.Close
End Sub

Private Sub Command32_Click()
    Select Case Me.Frame1
        Case 1
        stDocName = "D_Check_vagozar"
        Case 2
        stDocName = "D_CHECK_vagozar_bymat"
    End Select
    DoCmd.OpenReport stDocName, acPreview
End Sub

Private Sub Command33_Click()
    stDocName = "D_CHECK_VOSOOL"
    DoCmd.OpenReport stDocName, acPreview

End Sub

Private Sub Command35_Click()
    Select Case Me.Frame1
        Case 1
        stDocName = "D_CHECK_nazd"
        Case 2
        stDocName = "D_CHECK_nazd"
    End Select
    DoCmd.OpenReport stDocName, acPreview
End Sub

Private Sub Command36_Click()
    stDocName = "D_CHECK_BACKED"
    DoCmd.OpenReport stDocName, acPreview
End Sub

Private Sub Command37_Click()

    stDocName = "D_CHECK_KHARJ"
    DoCmd.OpenReport stDocName, acPreview

End Sub

Private Sub Command38_Click()
    stDocName = "D_CHECK_LONG"
    DoCmd.OpenReport stDocName, acPreview
End Sub


Private Sub Command62_Click()
On Error GoTo Err_Command62_Click
    Me.Recalc
    Dim stDocName As String

    stDocName = "Dailyrep_notcheck"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command62_Click:
    Exit Sub

Err_Command62_Click:
    MsgBox Err.Description
    Resume Exit_Command62_Click
    
End Sub



Private Sub Frame2_AfterUpdate()
    If Me.Frame2 = 0 Then
        Me.D1 = Null
        Me.D2 = Null
        Me.D1.Enabled = False
        Me.D2.Enabled = False
    Else
        Me.D1.Enabled = True
        Me.D2.Enabled = True
        Me.D1 = Mid(CurrentDateFarsi(Date), 1, 4) & "0101"
        Me.D2 = CurrentDateFarsi(Date)
    End If
End Sub


