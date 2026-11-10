Attribute VB_Name = "Report_Hepcomainnag"
Attribute VB_Base = "0{81361833-BEC5-4681-95E3-41988F3E7320}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Report_Close()
    Form_HepcoBrief.Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Form_HepcoBrief.Visible = False
End Sub
