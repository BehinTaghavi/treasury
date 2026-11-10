Attribute VB_Name = "Form_Check_p_vos"
Attribute VB_Base = "0{1040E7BF-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Backed_AfterUpdate()
     If Me.Backed = True Then
        If Me.Vos = True Then
            Me.Vos = False
        End If
     End If
     Me.Recalc
End Sub

Private Sub Command28_Click()
    Dim FilterStr As String
    FilterStr = ""
    
    If Not IsNull(Me!T1) Then
       FilterStr = "InStr(1,Gno,'" & Me!T1 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T2) Then
       FilterStr = "InStr(1,D1,'" & Me!T2 & "',0) > 0"
    End If
    If Not IsNull(Me!T3) Then
       FilterStr = "InStr(1,Checkno,'" & Me!T3 & "',0) > 0"
    End If
    If Not IsNull(Me!T4) Then
       FilterStr = "InStr(1,Checktypen,'" & Me!T4 & "',0) > 0"
    End If
    If Not IsNull(Me!T5) Then
       FilterStr = "InStr(1,Amount,'" & Me!T5 & "',0) > 0"
    End If
    If Not IsNull(Me!T6) Then
       FilterStr = "InStr(1,Person,'" & Me!T6 & "',0) > 0"
    End If
    
    If Not IsNull(Me!T7) Then
       FilterStr = "InStr(1,Babat,'" & Me!T7 & "',0) > 0"
    End If

    If Not IsNull(Me!T10) Then
       FilterStr = "InStr(1,Maturity,'" & Me!T10 & "',0) > 0 "
    End If
    
    If Not IsNull(Me!T12) Then
       FilterStr = "InStr(1,ٍEmpid,'" & Me!T12 & "',0) > 0 "
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

Private Sub Vos_AfterUpdate()
     If Me.Vos = True Then
        If Me.Backed = True Then
            Me.Backed = False
        End If
     End If
    Me.Recalc
End Sub


