Attribute VB_Name = "Report_B_D_printnaghd"
Attribute VB_Base = "0{B9558E62-C688-4AB2-90B1-E2C2052513E2}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit

Private Sub Report_Activate()
    'Me.Textco = Tellconame
End Sub

Private Sub Report_Close()
    Forms![D_Naghd].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![D_Naghd].Visible = False
End Sub
