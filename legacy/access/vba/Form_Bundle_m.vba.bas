Attribute VB_Name = "Form_Bundle_m"
Attribute VB_Base = "0{1040E787-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Form_Open(Cancel As Integer)
    Me.Combo0 = Me.Combo0.ItemData(0)
    Me.Combo0.Requery
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
