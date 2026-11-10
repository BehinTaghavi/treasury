Attribute VB_Name = "Report_D_Check_entez"
Attribute VB_Base = "0{07CD4E51-4808-48AF-93AB-D5E5F7F0E79D}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit
Dim S As String, mainstr As String, typestr As String, datestr As String
Dim datemstr As String, Ordstr As String, perstr As String, babstr As String, perbab As String

Private Sub Report_Activate()
    Me.Textco = Tellconame
    Select Case Forms!Date_dcheck_entez!Frame1
        Case 2
            Select Case Forms!Date_dcheck_entez!Frame2
                Case 0
                    Repname = "همه اقلام پرداختني و انتظامي صادره"
                Case 1
                    Repname = "چكهاي انتظامي صادره"
                Case 2
                    Repname = "چكهاي مدت دارصادره"
                Case 3
                    Repname = "اقساط پرداختني"
                Case 4
                    Repname = "سفته هاي پرداختني"
                Case 5
                    Repname = "ساير اقلام پرداختني"
                Case 6
                    Repname = "سفته انتظامي"
             End Select
        Case 1
            Case 0
                Repname = "همه اقلام دريافتني و انتظامي"
            Case 1
                Repname = "چكهاي انتظامي دريافتي"
            Case 3
                Repname = "اقساط دريافتني"
            Case 4
                Repname = "سفته هاي دريافتني"
            Case 5
                Repname = "ساير اقلام دريافتني"
            Case 6
                Repname = "سفته انتظامي"
    End Select
    
    Select Case Forms!Date_dcheck_entez!Frame2
        Case 0
        Case 13
            Repname = "چكهاي وصول شده"
        Case 14
            Repname = "چكهاي برگشت شده"
        Case 15
            Repname = "چكهاي عودت شده"
        Case 16
            Repname = "چكهاي خرج شده"
        Case 19
            Repname = "چكهاي حقوقي شده"
    End Select

    Select Case Forms!Date_dcheck_entez!Frame2
        Case 0
            Dfilter = "بدون محدوده تاريخي"
        Case 1
            Dfilter = "تاريخ صدور/دريافت"
        Case 2
            Dfilter = "سررسيد"
    End Select
    
End Sub

Private Sub Report_Close()
    Forms![Date_dcheck_entez].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Date_dcheck_entez].Visible = False
    
    Select Case Forms!Date_dcheck_entez!Frame1
        Case 1
            mainstr = "SELECT Check.* ,Trim(Check.checktypen) as checkname FROM Check WHERE ((Check.Dp)=1) And "
            Select Case Forms!Date_dcheck_entez!Frame5
            Case 0
                typestr = " 1=1 and "
            Case 4
                If Forms!Date_dcheck_entez!Andchecks = 0 Then
                typestr = "Checktype=" & Forms!Date_dcheck_entez!Frame5 & " And "
                Else
                typestr = "Checktype In (0,2,4) And "
                End If
            Case Else
                typestr = "Checktype=" & Forms!Date_dcheck_entez!Frame5 & " And "
            End Select
         Select Case Forms!Date_dcheck_entez!Framestatus
            Case 0
                typestr = typestr
            Case Else
                typestr = typestr & "  Stcode=" & Forms!Date_dcheck_entez!Framestatus & " And "
         End Select
        Case 2
            mainstr = "SELECT Check.* FROM Check WHERE Check.Dp=2 And Paied=true And "
            Select Case Forms!Date_dcheck_entez!Frame4
            Case 0
                typestr = " 1=1 and "
            Case Else
                typestr = "Checktype=" & Forms!Date_dcheck_entez!Frame4 & " And "
            End Select
    End Select

    Select Case Forms!Date_dcheck_entez!Frame2
        Case 0
            datestr = "1=1"
        Case 1
            datestr = "Rfpdate"
        Case 2
            datestr = "Maturity"
        Case 3, 5
            datestr = "D3"
        Case 4
            datestr = "D4"
    End Select
    
    If Forms!Date_dcheck_entez!Frame2 = 0 Then
        datemstr = datestr
    Else
        datemstr = datestr & " Between Forms!DATE_DCHECK_entez![D1] And Forms!DATE_DCHECK_entez![D2] "
    End If
    
   Select Case Forms!Date_dcheck_entez!Frame3
        Case 1
            Ordstr = ""
        Case 2
            Ordstr = " Desc "
    End Select
    
    If Forms!Date_dcheck_entez!ChPerson = True Then
        If IsNull(Forms!Date_dcheck_entez!Person) Then
            perstr = ""
        Else
            perstr = BSPACE(Forms!Date_dcheck_entez!Person)
            perstr = " and IIf(IsNull([Person]), Null, BSPACE([Person])) Like '*" & Trim(perstr) & "*'"
        End If
    End If

    If Forms!Date_dcheck_entez!ChBabat = True Then
        If IsNull(Forms!Date_dcheck_entez!Babat) Then
            babstr = ""
        Else
            babstr = BSPACE(Forms!Date_dcheck_entez!Babat)
            babstr = " And IIf(IsNull([Babat]), Null, BSPACE([Babat])) Like '*" & Trim(babstr) & "*'"
        End If
    End If
    perbab = perstr & babstr

    If Forms!Date_dcheck_entez!Frame2 = 0 Then
       Ordstr = "Maturity"
    Else
       Ordstr = datestr & Ordstr
    End If

    S = mainstr & typestr & datemstr & perbab
    Me.RecordSource = S
    Me.OrderBy = Ordstr

End Sub
