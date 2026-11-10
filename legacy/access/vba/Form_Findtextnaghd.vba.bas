Attribute VB_Name = "Form_Findtextnaghd"
Attribute VB_Base = "0{61E9860E-6B85-4400-B28B-D1156ECABEB3}"
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
    Me.Dp = 11
End Sub
