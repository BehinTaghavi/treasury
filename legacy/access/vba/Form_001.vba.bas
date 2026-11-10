Attribute VB_Name = "Form_001"
Attribute VB_Base = "0{4BAF7D49-EA92-4636-8F68-0B4E09BA213A}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database

Private Sub Command0_Click()
    Dim rst As Recordset, myno As String, Myname As String
    S = "Select * from t_unique_accounts Order by Account_code"
    Set rst = CurrentDb.OpenRecordset(S)
    With rst
        .MoveLast
        .MoveFirst
        While Not .EOF
        myno = !account_code
        Myname = !Account_Name
         S = "INSERT INTO Tafs ( Tafno, Tafname, Accno, Accname, Rowdesc )SELECT t_voucher_row.Unique_Account_No,'" & CStr(Myname) & "', t_voucher_row.Account_No, t_accounts.Account_Name, t_voucher_row.Description FROM t_voucher_row INNER JOIN t_accounts ON t_voucher_row.Account_No = t_accounts.account_code WHERE (((t_voucher_row.Unique_Account_No)='" & CStr(myno) & "'));"
         docommand S
        .MoveNext
        Wend
    End With
End Sub
