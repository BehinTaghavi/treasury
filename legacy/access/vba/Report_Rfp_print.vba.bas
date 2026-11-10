Attribute VB_Name = "Report_Rfp_print"
Attribute VB_Base = "0{5BCC0D28-7C58-4E27-9B49-72FB2EB40848}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit



Private Sub Report_Close()
    Forms![Porder].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Porder].Visible = False
    DoCmd.Maximize
End Sub
