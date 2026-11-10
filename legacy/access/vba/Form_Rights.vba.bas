Attribute VB_Name = "Form_Rights"
Attribute VB_Base = "0{0F3BB7A0-74F7-426E-A288-DA8660007A77}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Private Sub Form_Current()
    Dim mycontrol As Control
'    For Each mycontro In Me
'        If mycontrol.Name <> "Mcome" Then
'            If Me.Mcome = True Then
'                mycontrol.Object.Enabled = True
'            Else
'                mycontrol.Object.Enabled = True
'            End If
'        End If
'
'    Next
End Sub

Private Sub Mcome_AfterUpdate()
    If Me.Mcome = True Then
        Me.C1 = True
        Me.C2 = True
        Me.C3 = True
        Me.C4 = True
        Me.C5 = True
        Me.C6 = True
        Me.C7 = True
        Me.C8 = True
        Me.C9 = True
        Me.C10 = True
        Me.C11 = True
        Me.C12 = True
    Else
        Me.C1 = False
        Me.C2 = False
        Me.C3 = False
        Me.C4 = False
        Me.C5 = False
        Me.C6 = False
        Me.C7 = False
        Me.C8 = False
        Me.C9 = False
        Me.C10 = False
        Me.C11 = False
        Me.C12 = False
    End If
End Sub
