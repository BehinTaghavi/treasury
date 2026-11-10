Attribute VB_Name = "Report_Bankrep"
Attribute VB_Base = "0{ECDD90A4-88D7-47D9-A15F-4419C3C78037}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Report_Activate()
    Me.Textco = Tellconame
End Sub

Private Sub Report_Close()
    Forms![Bank_rep].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Bank_rep].Visible = False
End Sub
