Attribute VB_Name = "Form_Branch"
Attribute VB_Base = "0{1040E779-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Combo0_AfterUpdate()
    Me.RecordSource = "select * from branch where bankid=" & Me.Combo0
End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Brid = tellmelast(6)
    Me.Bankid = Me.Combo0
End Sub

Private Sub Form_Open(Cancel As Integer)
    Me.Combo0 = IIf(IsNull(Me.Combo0.ItemData(0)), 0, Me.Combo0.ItemData(0))
    Me.RecordSource = "select * from branch where bankid=" & Me.Combo0
End Sub
