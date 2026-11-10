Attribute VB_Name = "Report_Bankrep_d_1"
Attribute VB_Base = "0{1040E987-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mys As String, mybacc As String

Private Sub Report_Activate()
    Me.Textco = Tellconame
    Select Case Forms![Bank_rep]!Frame2
        Case 1
            Repname = "تعهدات پرداختني سررسيد گذشته(نسبت به):"
            Dfilter = Forms![Bank_rep]!Text0
        Case 2
            Repname = "تعهدات پرداختني امروز:"
            Dfilter = Forms![Bank_rep]!Text0
        Case 3
            Repname = "تعهدات پرداختني ازتاريخ:"
            Dfilter = Forms![Bank_rep]!Daz
            T3 = "تا:"
            T4 = Forms![Bank_rep]!Dta
    End Select
    If Forms![Bank_rep]!Frame3 = 1 Then
        Me.Tbacc = "همه حسابها"
    Else
        Me.Tbacc = Forms!Bank_rep!Bacc
    End If
End Sub

Private Sub Report_Close()
    Forms![Bank_rep].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Bank_rep].Visible = False
    mys = "SELECT Check.Baccno, Check.Amount, Check.Checktypen, Check.Rfpdate, Check.Checkno, Check.Maturity, Check.Iscur, Check.Curid, Check.Person, Check.Babat FROM Check WHERE Check.Dp=2 And (Check.Checktype<>1 And Check.Checktype<>6) And Check.Paied=True And Check.Ebtorkharj=false And Check.Calcit=False "

    Select Case Forms![Bank_rep]!Frame2
        Case 1
            If Forms![Bank_rep]!Frame3 = 1 Then
                mybacc = " And Check.Maturity <Forms!Bank_rep!Text0;"
            Else
                mybacc = " And Check.Maturity <Forms!Bank_rep!Text0 And Baccno=Forms!Bank_rep!Bacc;"
            End If
        Case 2
            If Forms![Bank_rep]!Frame3 = 1 Then
                mybacc = " And Check.Maturity =Forms!Bank_rep!Text0;"
            Else
                mybacc = " And Check.Maturity =Forms!Bank_rep!Text0 And Baccno=Forms!Bank_rep!Bacc;"
            End If
        Case 3
            If Forms![Bank_rep]!Frame3 = 1 Then
                mybacc = " And Check.Maturity Between Forms!Bank_rep!Daz And Forms!Bank_rep!Dta;"
            Else
                mybacc = " And Check.Maturity Between Forms!Bank_rep!Daz And Forms!Bank_rep!Dta And Baccno=Forms!Bank_rep!Bacc;"
            End If
    End Select
    Me.RecordSource = mys & mybacc
End Sub
