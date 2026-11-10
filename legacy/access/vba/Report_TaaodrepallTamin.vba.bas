Attribute VB_Name = "Report_TaaodrepallTamin"
Attribute VB_Base = "0{68309C86-212C-44CF-9C3C-4519D41063FD}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Report_Activate()
    Me.Textco = Tellconame
    Select Case Forms!Taaodtamin.Frame0
        Case 1
            Me.Text1 = "(نزدصندوق)"
        Case 2
            Me.Text1 = "(نزدبانك)"
        Case 3
            Me.Text1 = "(نزدصندوق ونزدبانك)"
    End Select
End Sub

Private Sub Report_Close()
    Forms!Taaodtamin.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms!Taaodtamin.Visible = False
End Sub
