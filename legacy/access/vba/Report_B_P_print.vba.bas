Attribute VB_Name = "Report_B_P_print"
Attribute VB_Base = "0{1040E979-F05E-11D7-BC94-8154B80D7F75}"
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
    Forms![Check_Print_single].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Check_Print_single].Visible = False
End Sub
