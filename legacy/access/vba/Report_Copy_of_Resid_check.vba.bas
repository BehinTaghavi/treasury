Attribute VB_Name = "Report_Copy of Resid_check"
Attribute VB_Base = "0{F6F55EAD-0062-473E-99C6-5BA8B48B834C}"
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
