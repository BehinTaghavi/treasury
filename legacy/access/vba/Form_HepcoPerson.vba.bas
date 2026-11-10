Attribute VB_Name = "Form_HepcoPerson"
Attribute VB_Base = "0{D44F9BFB-2D0E-4550-8F1D-EED086C09799}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Form_Close()
    Forms!Date_dcheck_entez.Visible = True
End Sub

Private Sub Form_Open(Cancel As Integer)
    Forms!Date_dcheck_entez.Visible = False
End Sub

Private Sub Command71_Click()
On Error GoTo Err_Command71_Click
    Me.Recalc
    Dim stDocName As String
    
    stDocName = "Hepcobyperson"
    
    DoCmd.OpenReport stDocName, acPreview

Exit_Command71_Click:
    Exit Sub

Err_Command71_Click:
    MsgBox Err.Description
    Resume Exit_Command71_Click
    
End Sub
