Attribute VB_Name = "Sub_Checktypename"
Option Compare Database

Public Sub Checktypename()
    S = "UPDATE [Check] INNER JOIN Checktype ON (Check.Dp = Checktype.Dp) AND (Check.Checktype = Checktype.Checktype) SET [Check].Checktypen = [Checktype].[Checktypen];"

End Sub


