Attribute VB_Name = "Form_HepcoBrief"
Attribute VB_Base = "0{F1B37908-102A-4E08-98E6-B0519E7EED0B}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command10_Click()

Select Case Val(Me.Mah1)
    Case 1
        Me.M2 = [Y1] & "02"
        Me.M3 = [Y1] & "03"
        Me.M4 = [Y1] & "04"
        Me.M5 = [Y1] & "05"
        Me.M6 = [Y1] & "06"
        Me.M7 = [Y1] & "07"
        Me.M8 = [Y1] & "08"
        Me.M9 = [Y1] & "09"
        Me.M10 = [Y1] & "10"
        Me.M11 = [Y1] & "11"
        Me.M12 = [Y1] & "12"
    Case 2
        Me.M2 = [Y1] & "03"
        Me.M3 = [Y1] & "04"
        Me.M4 = [Y1] & "05"
        Me.M5 = [Y1] & "06"
        Me.M6 = [Y1] & "07"
        Me.M7 = [Y1] & "08"
        Me.M8 = [Y1] & "09"
        Me.M9 = [Y1] & "10"
        Me.M10 = [Y1] & "11"
        Me.M11 = [Y1] & "12"
        Me.M12 = [Y1] + 1 & "01"
    Case 3
        Me.M2 = [Y1] & "04"
        Me.M3 = [Y1] & "05"
        Me.M4 = [Y1] & "06"
        Me.M5 = [Y1] & "07"
        Me.M6 = [Y1] & "08"
        Me.M7 = [Y1] & "09"
        Me.M8 = [Y1] & "10"
        Me.M9 = [Y1] & "11"
        Me.M10 = [Y1] & "12"
        Me.M11 = [Y1] + 1 & "01"
        Me.M12 = [Y1] + 1 & "02"
    Case 4
        Me.M2 = [Y1] & "05"
        Me.M3 = [Y1] & "06"
        Me.M4 = [Y1] & "07"
        Me.M5 = [Y1] & "08"
        Me.M6 = [Y1] & "09"
        Me.M7 = [Y1] & "10"
        Me.M8 = [Y1] & "11"
        Me.M9 = [Y1] & "12"
        Me.M10 = [Y1] + 1 & "01"
        Me.M11 = [Y1] + 1 & "02"
        Me.M12 = [Y1] + 1 & "03"
    Case 5
        Me.M2 = [Y1] & "06"
        Me.M3 = [Y1] & "07"
        Me.M4 = [Y1] & "08"
        Me.M5 = [Y1] & "09"
        Me.M6 = [Y1] & "10"
        Me.M7 = [Y1] & "11"
        Me.M8 = [Y1] & "12"
        Me.M9 = [Y1] + 1 & "01"
        Me.M10 = [Y1] + 1 & "02"
        Me.M11 = [Y1] + 1 & "03"
        Me.M12 = [Y1] + 1 & "04"
    Case 6
        Me.M2 = [Y1] & "07"
        Me.M3 = [Y1] & "08"
        Me.M4 = [Y1] & "09"
        Me.M5 = [Y1] & "10"
        Me.M6 = [Y1] & "11"
        Me.M7 = [Y1] & "12"
        Me.M8 = [Y1] + 1 & "01"
        Me.M9 = [Y1] + 1 & "02"
        Me.M10 = [Y1] + 1 & "03"
        Me.M11 = [Y1] + 1 & "04"
        Me.M12 = [Y1] + 1 & "05"
    Case 7
        Me.M2 = [Y1] & "08"
        Me.M3 = [Y1] & "09"
        Me.M4 = [Y1] & "10"
        Me.M5 = [Y1] & "11"
        Me.M6 = [Y1] & "12"
        Me.M7 = [Y1] + 1 & "01"
        Me.M8 = [Y1] + 1 & "02"
        Me.M9 = [Y1] + 1 & "03"
        Me.M10 = [Y1] + 1 & "04"
        Me.M11 = [Y1] + 1 & "05"
        Me.M12 = [Y1] + 1 & "06"
    Case 8
        Me.M2 = [Y1] & "09"
        Me.M3 = [Y1] & "10"
        Me.M4 = [Y1] & "11"
        Me.M5 = [Y1] & "12"
        Me.M6 = [Y1] + 1 & "01"
        Me.M7 = [Y1] + 1 & "02"
        Me.M8 = [Y1] + 1 & "03"
        Me.M9 = [Y1] + 1 & "04"
        Me.M10 = [Y1] + 1 & "05"
        Me.M11 = [Y1] + 1 & "06"
        Me.M12 = [Y1] + 1 & "07"
    Case 9
        Me.M2 = [Y1] & "10"
        Me.M3 = [Y1] & "11"
        Me.M4 = [Y1] & "12"
        Me.M5 = [Y1] + 1 & "01"
        Me.M6 = [Y1] + 1 & "02"
        Me.M7 = [Y1] + 1 & "03"
        Me.M8 = [Y1] + 1 & "04"
        Me.M9 = [Y1] + 1 & "05"
        Me.M10 = [Y1] + 1 & "06"
        Me.M11 = [Y1] + 1 & "07"
        Me.M12 = [Y1] + 1 & "08"
    Case 10
        Me.M2 = [Y1] & "11"
        Me.M3 = [Y1] & "12"
        Me.M4 = [Y1] + 1 & "01"
        Me.M5 = [Y1] + 1 & "02"
        Me.M6 = [Y1] + 1 & "03"
        Me.M7 = [Y1] + 1 & "04"
        Me.M8 = [Y1] + 1 & "05"
        Me.M9 = [Y1] + 1 & "06"
        Me.M10 = [Y1] + 1 & "07"
        Me.M11 = [Y1] + 1 & "08"
        Me.M12 = [Y1] + 1 & "09"
    Case 11
        Me.M2 = [Y1] & "12"
        Me.M3 = [Y1] + 1 & "01"
        Me.M4 = [Y1] + 1 & "02"
        Me.M5 = [Y1] + 1 & "03"
        Me.M6 = [Y1] + 1 & "04"
        Me.M7 = [Y1] + 1 & "05"
        Me.M8 = [Y1] + 1 & "06"
        Me.M9 = [Y1] + 1 & "07"
        Me.M10 = [Y1] + 1 & "08"
        Me.M11 = [Y1] + 1 & "09"
        Me.M12 = [Y1] + 1 & "10"
    Case 12
        Me.M2 = [Y1] + 1 & "01"
        Me.M3 = [Y1] + 1 & "02"
        Me.M4 = [Y1] + 1 & "03"
        Me.M5 = [Y1] + 1 & "04"
        Me.M6 = [Y1] + 1 & "05"
        Me.M7 = [Y1] + 1 & "06"
        Me.M8 = [Y1] + 1 & "07"
        Me.M9 = [Y1] + 1 & "08"
        Me.M10 = [Y1] + 1 & "09"
        Me.M11 = [Y1] + 1 & "10"
        Me.M12 = [Y1] + 1 & "11"
End Select


    S = "UPDATE Hepcomain SET Hepcomain.Hepcost = 10 WHERE (((Hepcomain.ItMat)<[forms]![hepcobrief]![M1]) AND ((Hepcomain.Stcode)=13));"
    docommand S
    
    S = "UPDATE Hepcomain SET Hepcomain.Hepcost = 11 WHERE (((Hepcomain.ItMat)<[forms]![hepcobrief]![M1]) AND ((Hepcomain.Stcode)=11));"
    docommand S
    
    S = "UPDATE Hepcomain SET Hepcomain.Hepcost = 12 WHERE (((Hepcomain.ItMat)<[forms]![hepcobrief]![M1]) AND ((Hepcomain.Stcode)=12));"
    docommand S
    
    S = "UPDATE Hepcomain SET Hepcomain.Hepcost = 13 WHERE (((Hepcomain.ItMat) Between [forms]![hepcobrief]![M1] And [forms]![hepcobrief]![M12]));"
    docommand S
    
    S = "UPDATE Hepcomain SET Hepcomain.Hepcost = 14 WHERE ((Hepcomain.ItMat)>[forms]![hepcobrief]![M12]) ;"
    docommand S
    
    Dim stDocName As String

    stDocName = "Hepcomainnag"
    DoCmd.OpenReport stDocName, acPreview
    
End Sub

Private Sub Form_Close()
    Forms!Date_dcheck_entez.Visible = True
End Sub

Private Sub Form_Open(Cancel As Integer)
    Forms!Date_dcheck_entez.Visible = False
End Sub

Private Sub Mah1_AfterUpdate()
    Me.Mah2 = Me.Mah1
End Sub

Private Sub Y1_AfterUpdate()
    Me.Y2 = Me.Y1 + 1
End Sub

