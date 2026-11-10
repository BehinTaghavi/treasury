Attribute VB_Name = "Report_Resid_check"
Attribute VB_Base = "0{BFD88A8B-3DD6-48B3-9BB0-829F8E573F53}"
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
