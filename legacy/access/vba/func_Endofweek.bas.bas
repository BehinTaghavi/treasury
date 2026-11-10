Attribute VB_Name = "func_Endofweek"
Option Compare Database
Option Explicit

Public Function Endofweek(Mydate As String) As String
    Dim myweekday As Integer, adding As Integer
    Dim Mysal As String, mymah As String, Myday As String
    Mysal = Mid(Mydate, 1, 4)
    mymah = Mid(Mydate, 5, 2)
    Myday = Mid(Mydate, 7, 2)
    'If myday = 31 and mymah between "01" and "06" Then
    
    
    myweekday = Weekday(ConvetFarsiDateToEnglandDate(Mydate))
    Select Case myweekday
        Case 1
            adding = 5
        Case 2
            adding = 4
        Case 3
            adding = 3
        Case 4
            adding = 2
        Case 5
            adding = 1
        Case 6
            adding = 0
        Case 7
            adding = 6
    End Select
    Endofweek = "13" & AddNumberAndConvertToNewDate(Mydate, adding, True)
End Function
