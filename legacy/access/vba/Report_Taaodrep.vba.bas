Attribute VB_Name = "Report_Taaodrep"
Attribute VB_Base = "0{C8C157B5-981E-418B-88A3-CA1D5464C8FF}"
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
