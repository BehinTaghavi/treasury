Attribute VB_Name = "Report_CH_MELLI50_new"
Attribute VB_Base = "0{0EACEDE8-E656-4B00-A8CF-2682DC3CB652}"
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
