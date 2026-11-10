Attribute VB_Name = "Report_Resid_check0"
Attribute VB_Base = "0{A0E559BD-7F11-41FD-A3A3-DED75C8A0DD9}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Private Sub Report_Close()
   Forms![D_check].Visible = True
End Sub
Private Sub Report_Open(Cancel As Integer)
    Forms![D_check].Visible = False
End Sub
