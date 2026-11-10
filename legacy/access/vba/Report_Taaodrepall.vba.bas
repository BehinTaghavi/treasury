Attribute VB_Name = "Report_Taaodrepall"
Attribute VB_Base = "0{83F91AF0-12B2-4083-AA6F-A1850944312C}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Report_Close()
    Forms!Taaod.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms!Taaod.Visible = False
End Sub
