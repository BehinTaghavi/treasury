Attribute VB_Name = "Report_Bankrep_d_jv"
Attribute VB_Base = "0{1040E98E-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim mys As String, mybacc As String, statusstr As String, datestr As String
Private Sub Report_Activate()
    Me.Textco = Tellconame
    Select Case Forms![Bank_rep]!Frame2
        Case 4
            Repname = "كليه چك هاي در جريان وصول"
        Case 5
            Repname = "چكهاي در جريان وصول مركز"
        Case 6
            Repname = "چكهاي در جريان وصول خارج از مركز"
        Case 7
            Repname = "كليه چك هاي نزد صندوق"
        Case 8
            Repname = "چكهاي نزد صندوق مركز"
        Case 9
            Repname = "چكهاي نزد صندوق خارج از مركز"
    End Select
    If Forms![Bank_rep]!Frame3 = 1 Then
        Tbacc = "همه حسابها"
    Else
        Tbacc = Forms![Bank_rep]!Bacc
    End If
    If Forms![Bank_rep]!Frame4 = 1 Then
        S0 = "بدون محدوده تاريخي"
        S1 = ""
        S2 = ""
    Else
        S0 = "سررسيد"
        S1 = Forms![Bank_rep]!S1
        S2 = Forms![Bank_rep]!S2
    End If

End Sub

Private Sub Report_Close()
    Forms![Bank_rep].Visible = True
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Bank_rep].Visible = False
    mys = "SELECT Check.Faragir,Check.Vagno, Check.Amount, IIf([Far]=True,'شهرستان','تهران') AS F, Check.Rfpdate, Check.Checktypen, Check.Checkno, Check.Maturity, Check.Curid, Check.Person, Check.Babat, Check.Bankid,Check.D3 FROM [Check] WHERE Check.Dp=1 AND (Check.Checktype=0 Or Check.Checktype=2) " 'AND ((Check.Stcode)=12));
    
    Select Case Forms![Bank_rep]!Frame2
        Case 4
            statusstr = " And Stcode=12"
        Case 5
            statusstr = " And Stcode=12 And Far=false"
        Case 6
            statusstr = " And Stcode=12 And Far=true"
        Case 7
            statusstr = " And Stcode=11"
        Case 8
            statusstr = " And Stcode=11 And Far=false"
        Case 9
            statusstr = " And Stcode=11 And Far=true"
    End Select

    If Forms![Bank_rep]!Frame3 = 1 Then
        mybacc = ""
    Else
        mybacc = " And Vagno='" & Form_Bank_rep.Bacc & "'"
    End If

'    If Forms![Bank_rep]!Frame3 = 1 Then
'        mybacc = ""
'    Else
'        mybacc = " And Baccno=Forms!Bank_rep!Bacc"
'    End If
    
    If Forms![Bank_rep]!Frame4 = 1 Then
        datestr = ";"
    Else
        datestr = " And Maturity between Forms!Bank_rep!S1 And Forms!Bank_rep!S2;"
    End If
    S = mys & statusstr & mybacc & datestr
    Me.RecordSource = S
End Sub
