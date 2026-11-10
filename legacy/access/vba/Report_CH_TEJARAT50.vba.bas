Attribute VB_Name = "Report_CH_TEJARAT50"
Attribute VB_Base = "0{8D5414DE-FBA8-4F3E-AFEE-FBF78748F6C1}"
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
