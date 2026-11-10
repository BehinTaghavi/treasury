Attribute VB_Name = "Report_Taaodrepcompactonerow"
Attribute VB_Base = "0{177EBE13-2C6F-4D4E-B746-64704C46005B}"
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
