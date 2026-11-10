Attribute VB_Name = "Report_B_P_print_Safteh"
Attribute VB_Base = "0{528EF206-D420-47FC-BD3E-C07CDB1D28E5}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit

Private Sub Report_Activate()
    Me.Textco = Tellconame
End Sub

Private Sub Report_Close()
    'Forms![check_print_single].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    'Forms![check_print_single].Visible = False
End Sub
