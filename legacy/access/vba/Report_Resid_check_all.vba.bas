Attribute VB_Name = "Report_Resid_check_all"
Attribute VB_Base = "0{7B0C8301-6EB5-4294-BC0E-4E3B53F5D43D}"
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
   Forms![D_check].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
       Forms![D_check].Visible = False
End Sub
