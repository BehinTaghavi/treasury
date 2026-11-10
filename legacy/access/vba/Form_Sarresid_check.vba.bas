Attribute VB_Name = "Form_Sarresid_check"
Attribute VB_Base = "0{1040E8FE-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit


Private Sub Command31_Click()
DoCmd.Close
End Sub


Private Sub Command35_Click()
    Dim stDocName As String

    Select Case Me.Frame1
        Case 1
            stDocName = "Sarresid_d"
        Case 2
            stDocName = "Sarresid_d_byperson"
        Case 3
            stDocName = "Sarresid_P"
        Case 4
            stDocName = "Sarresid_P_byperson"
    End Select
    DoCmd.OpenReport stDocName, acPreview

End Sub

Private Sub Command36_Click()
    Dim stDocName As String
    stDocName = "SARRESID_P"
    DoCmd.OpenReport stDocName, acPreview

End Sub
