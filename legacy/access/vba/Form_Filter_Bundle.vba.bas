Attribute VB_Name = "Form_Filter_Bundle"
Attribute VB_Base = "0{1040E88C-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit

Private Sub Combo01_AfterUpdate()
    Me.Combo02.Requery
End Sub

Private Sub Command26_Click()
    Dim stDocName As String
    stDocName = "Check_Bundle"
    DoCmd.OpenReport stDocName, acPreview
End Sub

Private Sub Command28_Click()
DoCmd.Close
End Sub
