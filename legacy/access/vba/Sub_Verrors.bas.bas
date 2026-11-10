Attribute VB_Name = "Sub_Verrors"
Option Compare Database
Dim rst As Recordset

Public Sub Verrors(Vno As Long, Vdate As String, Accvno As Long, ErTaf As String, Eracc As String, Erbed As Double, Erbes As Double, Ercode As Byte, erdesc As String)
    Set rst = CurrentDb.OpenRecordset("Voucher_Errors", dbOpenDynaset, dbSeeChanges)
    With rst
        .AddNew
            !Voucher_No = Vno
            !Voucher_Date = Vdate
            !Accvno = Accno
            !Tafno = ErTaf
            !Accno = Eracc
            !bed = Erbed
            !bes = Erbes
            !Tcode = Ercode
            !Errordesc = erdesc
        .Update
        .Close
    End With
End Sub
