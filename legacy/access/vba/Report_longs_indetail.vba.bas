Attribute VB_Name = "Report_longs_indetail"
Attribute VB_Base = "0{3D0CC7A5-DFB5-4C1A-BC6C-A62F48D1EC3D}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Private Sub Report_Close()
    Forms![Bank_rep].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Bank_rep].Visible = False
End Sub
