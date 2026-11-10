Attribute VB_Name = "Report_CH_Pasargaad25"
Attribute VB_Base = "0{08092092-A16A-4EFD-A5A5-00407E0EEC52}"
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
