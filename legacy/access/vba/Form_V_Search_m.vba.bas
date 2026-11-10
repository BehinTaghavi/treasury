Attribute VB_Name = "Form_V_Search_m"
Attribute VB_Base = "0{1040E936-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim rst As Recordset
Dim mystr As String

Private Sub ComboBacc_AfterUpdate()
    ComboBundle.Requery
End Sub


Private Sub Command4_Click()
On Error GoTo Err_Command4_Click


    DoCmd.Close

Exit_Command4_Click:
    Exit Sub

Err_Command4_Click:
    MsgBox Err.Description
    Resume Exit_Command4_Click
    
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

Private Sub Form_Open(Cancel As Integer)
    DoCmd.Maximize
End Sub
