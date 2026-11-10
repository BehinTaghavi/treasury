Attribute VB_Name = "Report_CH_TEJARAT100"
Attribute VB_Base = "0{61FC9C37-251D-48C5-8C69-CA1EBC30DB71}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False


Option Compare Database
Option Explicit

Private Sub Report_Close()
    Forms.Check_Print_single.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms.Check_Print_single.Visible = False
End Sub
