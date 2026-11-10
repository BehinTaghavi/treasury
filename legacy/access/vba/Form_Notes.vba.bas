Attribute VB_Name = "Form_Notes"
Attribute VB_Base = "0{1040E8B6-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command28_Click()
    Dim FilterStr As String
    FilterStr = ""
    
    If Not IsNull(Me!T1) Then
       FilterStr = "InStr(1,notedate,'" & Me!T1 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T2) Then
       FilterStr = "InStr(1,note,'" & Me!T2 & "',0) > 0"
    End If
    If Not IsNull(Me!T3) Then
       FilterStr = "InStr(1,did,'" & Me!T3 & "',0) > 0"
    End If
    If FilterStr = "" Then
       Me.FilterOn = False
    Else
       Me.Filter = FilterStr
       Me.FilterOn = True
    End If
End Sub

Private Sub Command31_Click()
       Me.FilterOn = False
End Sub

Private Sub Form_Open(Cancel As Integer)
    If Me.RecordsetClone.RecordCount > 0 Then
        DoCmd.GoToRecord , , acLast
    End If
End Sub
