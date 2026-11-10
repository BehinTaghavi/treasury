Attribute VB_Name = "Form_Babss"
Attribute VB_Base = "0{D9A4CD37-9647-4030-9AA0-BE8C8D90B6A6}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mys As String
Private Sub Bdesc_GotFocus()
    If IsNull(Me.GBid) Or Me.GBid = 0 Then
        MsgBox "گروه عمومي بابت نامشخص است"
        Me.GBid.SetFocus
    End If
End Sub

Private Sub Form_BeforeInsert(Cancel As Integer)
    Me.Dp = Me.Frame2
End Sub

Private Sub Form_BeforeUpdate(Cancel As Integer)
    If IsNull(Me.GBid) Or Me.GBid = 0 Then
        MsgBox "گروه عمومي بابت نامشخص است"
        Cancel = True
        Me.GBid.SetFocus
    End If

    If IsNull(Me.bdesc) Then
        MsgBox "شرح بابت نامشخص است"
        Cancel = True
        Me.bdesc.SetFocus
    End If

End Sub

Private Sub Frame2_AfterUpdate()
    If Me.Frame2 = 3 Then
        mys = "Select * from Babs where Gbid<>0 Order by Ord,Dp,Bdesc"
    Else
        mys = "Select * from Babs Where Gbid<>0 And Dp=" & Me.Frame2 & " order by Ord,Dp,Bdesc"
    End If
    Me.GBid.Requery
    Me.RecordSource = mys
    
End Sub
