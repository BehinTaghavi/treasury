Attribute VB_Name = "Report_Cardcur"
Attribute VB_Base = "0{708F2CAA-B5C2-4EF6-B2B6-5C1D9711B391}"
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


