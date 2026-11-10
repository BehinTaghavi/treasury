Attribute VB_Name = "Report_CH_REFAH100"
Attribute VB_Base = "0{F78022DA-6896-489E-B27B-A1F2A7FF9AE9}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False


Option Compare Database
Option Explicit


Private Sub Report_Close()
   Forms.Check_Print_single.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms.Check_Print_single.Visible = False
End Sub
