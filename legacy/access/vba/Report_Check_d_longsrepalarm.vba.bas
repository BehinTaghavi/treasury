Attribute VB_Name = "Report_Check_d_longsrepalarm"
Attribute VB_Base = "0{17D8687E-EDAE-4F70-A319-8CA185FACD68}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Private Sub Report_Close()
    Form_Check_d_longsalarm.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Form_Check_d_longsalarm.Visible = False
End Sub
