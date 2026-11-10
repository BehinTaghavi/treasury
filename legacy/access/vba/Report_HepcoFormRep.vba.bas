Attribute VB_Name = "Report_HepcoFormRep"
Attribute VB_Base = "0{C2277E71-CD2D-4366-85B9-68426BB7C352}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Frametype_AfterUpdate()
    Select Case Me.Frametype
        Case 0, 2, 10, 13, 20, 22
            Me.Check1.Enabled = False
            Me.Check2.Enabled = False
            Me.Check6.Enabled = False
            Me.Check4.Enabled = False
        Case Else
            Me.Check1.Enabled = True
            Me.Check2.Enabled = True
            Me.Check6.Enabled = True
            Me.Check4.Enabled = True
    End Select
End Sub
