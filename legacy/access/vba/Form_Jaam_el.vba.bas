Attribute VB_Name = "Form_Jaam_el"
Attribute VB_Base = "0{040A7A73-2FD6-4CD3-9E05-77B238F3FBF9}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command23_Click()
On Error GoTo Err_Command23_Click
    Me.Recalc
    Dim stDocName As String

    stDocName = "D_Check_Jaam_el"
    DoCmd.OpenReport stDocName, acPreview

Exit_Command23_Click:
    Exit Sub

Err_Command23_Click:
    MsgBox Err.Description
    Resume Exit_Command23_Click
    
End Sub

Private Sub Form_Close()
    Form_DATE_DCHECK.Visible = True
End Sub

Private Sub Form_Open(Cancel As Integer)
    Form_DATE_DCHECK.Visible = False
End Sub
