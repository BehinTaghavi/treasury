Attribute VB_Name = "Report_Subsrowrep"
Attribute VB_Base = "0{413CC052-4BC4-4CEC-9F24-87F4B3E3F25F}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Itquant = 1
End Sub
