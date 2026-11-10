Attribute VB_Name = "Report_B_P_printnaghd"
Attribute VB_Base = "0{D39E2C95-C92F-418E-ABA9-FFE43309EEFF}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Private Sub Report_Close()
    Forms![P_Naghd].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![P_Naghd].Visible = False
End Sub
