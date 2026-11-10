Attribute VB_Name = "Form_Date_card"
Attribute VB_Base = "0{1040E846-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command5_Click()
    Dim stDocName As String
    If Me.Iscur = True Then
        stDocName = "Cardcur"
    Else
        stDocName = "Card"
    End If
    DoCmd.OpenReport stDocName, acPreview
End Sub
Private Sub Command9_Click()
On Error GoTo Err_Command9_Click


    DoCmd.Close

Exit_Command9_Click:
    Exit Sub

Err_Command9_Click:
    MsgBox Err.Description
    Resume Exit_Command9_Click
    
End Sub

Private Sub Form_Open(Cancel As Integer)
    updatecard
End Sub
