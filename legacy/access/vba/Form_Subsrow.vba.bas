Attribute VB_Name = "Form_Subsrow"
Attribute VB_Base = "0{308FD73B-D011-480D-A807-973A1E79DA92}"
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
