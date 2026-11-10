Attribute VB_Name = "Form_Ch_dups"
Attribute VB_Base = "0{1040E795-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command17_Click()
    DoCmd.Close
End Sub

Private Sub Command20_DblClick(Cancel As Integer)
    Forms![D_Check_m]![Check_s].Form![Checkno] = Null
    DoCmd.Close
End Sub

Private Sub Command21_Click()
    DoCmd.Close
End Sub
