Attribute VB_Name = "func_Endofmonth"
Option Compare Database
Option Explicit

Public Function Endofmonth(Mydate As String) As String
    Dim y As String, M As String, d As String, endm As String
    y = Mid(Mydate, 1, 4)
    M = Mid(Mydate, 5, 2)
    d = Mid(Mydate, 7, 2)
    
    Select Case M
        Case "01" To "06"
            Endofmonth = Mid(Mydate, 1, 6) & "31"
        Case "07" To "12"
            Endofmonth = Mid(Mydate, 1, 6) & "30"
    End Select
End Function
