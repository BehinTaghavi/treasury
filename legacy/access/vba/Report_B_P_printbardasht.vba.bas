Attribute VB_Name = "Report_B_P_printbardasht"
Attribute VB_Base = "0{78C60CD4-BF60-4D57-AC5E-FEE7436957B0}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Private Sub Report_Close()
    Forms![P_Bardasht].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![P_Bardasht].Visible = False
End Sub
