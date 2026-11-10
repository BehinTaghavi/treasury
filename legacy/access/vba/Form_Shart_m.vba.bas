Attribute VB_Name = "Form_Shart_m"
Attribute VB_Base = "0{1040E90C-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command5_Click()
On Error GoTo Err_Command5_Click
    Dim stDocName As String
    Select Case Forms!Sal!Ttype
        Case 0
            stDocName = "Porder_search_print"
        Case 1
            stDocName = "D_search_Print"
        Case 2
            stDocName = "P_search_print"
    End Select

    DoCmd.OpenReport stDocName, acPreview

Exit_Command5_Click:
    Exit Sub

Err_Command5_Click:
    MsgBox Err.Description
    Resume Exit_Command5_Click
    
End Sub
Private Sub Command6_Click()
On Error GoTo Err_Command6_Click


    DoCmd.Close

Exit_Command6_Click:
    Exit Sub

Err_Command6_Click:
    MsgBox Err.Description
    Resume Exit_Command6_Click
    
End Sub

Private Sub Form_Open(Cancel As Integer)
    Me.Text0 = Forms!Sal!Ttype
End Sub
