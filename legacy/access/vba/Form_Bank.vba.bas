Attribute VB_Name = "Form_Bank"
Attribute VB_Base = "0{1040E764-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database


Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Bankid = tellmelast(8)
End Sub
