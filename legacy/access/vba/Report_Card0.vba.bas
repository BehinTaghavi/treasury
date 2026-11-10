Attribute VB_Name = "Report_Card0"
Attribute VB_Base = "0{1040E997-F05E-11D7-BC94-8154B80D7F75}"
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
    Forms!Date_card.Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms!Date_card.Visible = False
End Sub


