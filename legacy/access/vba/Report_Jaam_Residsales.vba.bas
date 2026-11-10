Attribute VB_Name = "Report_Jaam_Residsales"
Attribute VB_Base = "0{77AFECAC-CFD2-454B-AD55-B5A58BB69FDC}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Report_Close()
    Form_D_Check.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Form_D_Check.Visible = False
End Sub
