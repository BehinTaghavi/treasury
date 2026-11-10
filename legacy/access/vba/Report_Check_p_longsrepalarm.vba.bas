Attribute VB_Name = "Report_Check_p_longsrepalarm"
Attribute VB_Base = "0{6A64000E-3D14-4015-A30F-98FDC71441FB}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Private Sub Report_Close()
    Form_Check_p_longsalarm.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Form_Check_p_longsalarm.Visible = False
End Sub
