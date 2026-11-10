Attribute VB_Name = "Form_Invsamount"
Attribute VB_Base = "0{597CD223-022B-4968-BEAC-B9D8DBCA3D21}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Checkid = Forms!D_check!Checkid
End Sub
