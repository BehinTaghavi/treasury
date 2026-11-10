Attribute VB_Name = "Report_Dailyrep_notcheck"
Attribute VB_Base = "0{1040EA66-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim S As String, mainstr As String, typestr As String, datestr As String
Dim datemstr As String, Ordstr As String, perstr As String
Dim Baccstr As String, babstr As String, perbab As String

Private Sub Report_Activate()
    Me.Textco = Tellconame
        Select Case Forms![Date_notchecks]!Frame1
        Case 10
            Repname = "واريز/برداشت حسابهاي بانكي -دريافت و پرداخت نقدي"
        Case 11
            Repname = "واريز به /برداشت از حسابهاي بانكي"
        Case 12
            Repname = "دريافت و پرداخت هاي نقدي"
        Case 16
            Repname = "واريز به حسابهاي بانكي"
        Case 17
            Repname = "دريافت هاي نقدي"
        Case 26
            Repname = "برداشت از حسابهاي بانكها"
        Case 27
            Repname = "پرداخت هاي نقدي"
    End Select
    
    Select Case Forms!Date_notchecks!Frame2
        Case 0
            Dfilter = "بدون محدوده تاريخي"
        Case 1
            Dfilter = "تاريخ دريافت/پرداخت"
    End Select

End Sub

Private Sub Report_Close()
    Forms![Date_notchecks].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Date_notchecks].Visible = False
    mainstr = "SELECT Check.* FROM [Check] WHERE Dp=1 And (Checktype=0 or Checktype=2) And "
    
    Select Case Forms!Date_notchecks!Frame1
        Case 10
            mainstr = "SELECT Subs.*, Subs.Scode FROM Subs WHERE Sdate Between Forms!Date_notchecks![D1] And Forms!Date_notchecks![D2] And Scode In (16,17,26,27) And "
        Case 11
            mainstr = "SELECT Subs.*, Subs.Scode FROM Subs WHERE Sdate Between Forms!Date_notchecks![D1] And Forms!Date_notchecks![D2] And Scode In (16,17) And "
        Case 12
            mainstr = "SELECT Subs.*, Subs.Scode FROM Subs WHERE Sdate Between Forms!Date_notchecks![D1] And Forms!Date_notchecks![D2] And Scode In (17,27) And "
        Case 16, 17, 26, 27
            mainstr = "SELECT Subs.*, Subs.Scode FROM Subs WHERE Sdate Between Forms!Date_notchecks![D1] And Forms!Date_notchecks![D2] And Scode=Forms!Date_notchecks!frame1 And "
    End Select

    Select Case Forms!Date_notchecks!Frame2
        Case 0
            datestr = "1=1"
        Case 1
            datestr = "Sdate"
    End Select
    
    If Forms!Date_notchecks!Frame2 = 0 Then
        datemstr = datestr
    Else
        datemstr = datestr & " Between Forms!Date_notchecks![D1] And Forms!Date_notchecks![D2] "
    End If
    
   Select Case Forms!Date_notchecks!Frame3
        Case 1
            Ordstr = ""
        Case 2
            Ordstr = " Desc"
    End Select
    
    If Forms!Date_notchecks!ChBacc = True Then
        If IsNull(Forms!Date_notchecks!Bacc) Then
            Baccstr = ""
        Else
            Baccstr = BSPACE(Forms!Date_notchecks!Bacc)
            Baccstr = " and IIf(IsNull([Baccno]), Null, BSPACE([Baccno])) Like '*" & Trim(Baccstr) & "*'"
        End If
    End If

    If Forms!Date_notchecks!ChPerson = True Then
        If IsNull(Forms!Date_notchecks!Person) Then
            perstr = ""
        Else
            perstr = BSPACE(Forms!Date_notchecks!Person)
            perstr = " and IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(perstr) & "*'"
        End If
    End If
    
    If Forms!Date_notchecks!ChBabat = True Then
        If IsNull(Forms!Date_notchecks!Babat) Then
            babstr = ""
        Else
            babstr = BSPACE(Forms!Date_notchecks!Babat)
            babstr = " And IIf(IsNull([Babat]), Null, BSPACE([Babat])) Like '*" & Trim(babstr) & "*'"
        End If
    End If
    
    '*************
    If Forms!Date_notchecks!Findtext = True And DCount("Rowid", "Findtext", "Dp = 11") > 0 Then
        S = "Select * from Findtext Where Dp=11"
        babstr = ""
        Myrow = 0
        Set rst = CurrentDb.OpenRecordset(S, dbOpenDynaset, dbSeeChanges)
        With rst
            While Not .EOF
             Hasor = IIf(Myrow = 0, "", " Or ")
             babstr = babstr & Hasor & " IIf(IsNull([Babat]),Null,BSPACE([Babat])) Like " & "'*" & CStr(BSPACE(!Itdesc)) & "*'"
             Myrow = Myrow + 1
            .MoveNext
            Wend
        .Close
        End With
        babstr = " And (" & babstr & ")"
    End If
    '***************
    perbab = Baccstr & perstr & babstr
       
    Ordstr = "Sdate" & Ordstr
    
    S = mainstr & typestr & datemstr & perbab
    Me.RecordSource = S
    Me.OrderBy = Ordstr
    
End Sub


