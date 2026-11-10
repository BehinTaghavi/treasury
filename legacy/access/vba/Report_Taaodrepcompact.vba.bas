Attribute VB_Name = "Report_Taaodrepcompact"
Attribute VB_Base = "0{DE1D0101-B2D3-40CA-AC8C-3F8E35CD7F82}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Private Sub Report_Close()
    Forms!Taaod.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms!Taaod.Visible = False
End Sub
