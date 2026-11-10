Attribute VB_Name = "Report_longs_indetailmonth"
Attribute VB_Base = "0{DD49793C-0B94-4647-B338-F271EE0D0CCB}"
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

