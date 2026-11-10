Attribute VB_Name = "Report_B_D_printvariz"
Attribute VB_Base = "0{99EFFB87-4C95-454E-8677-CC127E1478FB}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Private Sub Report_Close()
    Forms![D_variz].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![D_variz].Visible = False
End Sub
