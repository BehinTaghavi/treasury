Attribute VB_Name = "Form_Findtext"
Attribute VB_Base = "0{474CBA25-91FF-4769-A9F6-DCB7E0BE50B3}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Form_AfterUpdate()
    Form_DATE_DCHECK.Findtext = True
End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Dp = 1
End Sub
