Attribute VB_Name = "Report_Dailyrep0"
Attribute VB_Base = "0{BBB7310E-E6FF-42D6-942B-2A5CB92768E2}"
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
    Forms![Deaily_rep].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Deaily_rep].Visible = False
End Sub


