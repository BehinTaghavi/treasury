Attribute VB_Name = "Report_Card"
Attribute VB_Base = "0{5C1DEDA2-D469-4315-8CC2-5F7F157678D3}"
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


