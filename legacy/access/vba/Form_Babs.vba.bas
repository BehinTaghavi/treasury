Attribute VB_Name = "Form_Babs"
Attribute VB_Base = "0{E365540C-8D1D-471B-A367-9548738F2F8D}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mys As String

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Dp = Me.Frame2
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.bdesc) Then
        MsgBox "شرح بابت نامشخص است"
        Cancel = True
        Me.bdesc.SetFocus
    End If
End Sub

Private Sub Frame2_AfterUpdate()
    If Me.Frame2 = 3 Then
        mys = "Select * from Babs where GBid=0 Order by Dp,Bdesc"
    Else
        mys = "Select * from Babs Where GBid=0 and Dp=" & Me.Frame2 & " order by Dp,Bdesc"
    End If
    Me.RecordSource = mys
End Sub
