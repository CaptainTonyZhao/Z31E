Imports boUIExt

Public Class Z31E011
    Inherits boUIExt.FormExt

    Private WithEvents ioBtn_OK As Button
    Private WithEvents ioBtn_ClearCond As Button

    Private WithEvents ioFld_10 As Folder
    Private WithEvents ioFld_20 As Folder
    Private WithEvents ioFld_30 As Folder

    Private WithEvents ioMtx_10 As Matrix
    Private WithEvents ioMtx_20 As Matrix

    Private ioRec_10 As Rectangle

    Private ioDt_Doc As DataTable
    Private ioDt_Result As DataTable

    Private ioUds_Clear As UserDataSource
    Private ioUds_SoNumF As UserDataSource
    Private ioUds_SoNumT As UserDataSource
    Private ioUds_ItemCodeF As UserDataSource
    Private ioUds_ItemCodeT As UserDataSource
    Private ioUds_CardCodeF As UserDataSource
    Private ioUds_CardCodeT As UserDataSource
    Private ioUds_NumAtCardF As UserDataSource
    Private ioUds_NumAtCardT As UserDataSource
    Private ioUds_ShipDateF As UserDataSource
    Private ioUds_ShipDateT As UserDataSource

    Private iiBPLId As String = 0
    Private iiPane As Integer = 1
    Private ibConditionChanged As Boolean = True   '标识条件是否发生改变
    Private ibSelectedNumChanged As Boolean = True   '标识勾选的生产订单编号是否发生变化
    Private ibUpdateData As Boolean = False          '标识是否返回记录

#Region "系统事件"

    Private Sub M0_On_Form_Create(BeforeAction As Boolean, CreationParam As FormCreationParams, ByRef BubbleEvent As Boolean) Handles Me.On_Form_Create
        If Not BeforeAction Then
            ioBtn_OK = GetItemSpecific("1")
            ioBtn_ClearCond = GetItemSpecific("ClearCond")

            ioFld_10 = GetItemSpecific("Fld_10")
            ioFld_20 = GetItemSpecific("Fld_20")
            ioFld_30 = GetItemSpecific("Fld_30")

            ioMtx_10 = GetItemSpecific("Mtx_10")
            ioMtx_20 = GetItemSpecific("Mtx_20")

            ioRec_10 = GetItemSpecific("Rec_10")

            ioDt_Doc = GetDataTable("Doc")
            ioDt_Result = GetDataTable("Result")

            ioUds_Clear = GetUserDataSource("Clear")
            ioUds_SoNumF = GetUserDataSource("SoNumF")
            ioUds_SoNumT = GetUserDataSource("SoNumT")
            ioUds_ItemCodeF = GetUserDataSource("ItemCodeF")
            ioUds_ItemCodeT = GetUserDataSource("ItemCodeT")
            ioUds_CardCodeF = GetUserDataSource("CardCodeF")
            ioUds_CardCodeT = GetUserDataSource("CardCodeT")
            ioUds_NumAtCardF = GetUserDataSource("NumAtCardF")
            ioUds_NumAtCardT = GetUserDataSource("NumAtCardT")
            ioUds_ShipDateF = GetUserDataSource("ShipDateF")
            ioUds_ShipDateT = GetUserDataSource("ShipDateT")

            ioMtx_10.SetSelectionModeEx(BoMatrixSelect.ms_Auto, False)
            ioMtx_10.AutoSelectRow = True

            ioMtx_20.SetSelectionModeEx(BoMatrixSelect.ms_Auto, False)
            ioMtx_20.AutoSelectRow = True

            ioFld_10.SetPane(1, True)
            ioFld_20.SetPane(2, True)
            ioFld_30.SetPane(3, True)

            ioUds_Clear.ValueEx = "Y"
            If (MyForm.TriggerObject IsNot Nothing AndAlso TypeOf MyForm.TriggerObject Is ChooseFromList AndAlso CreationParam IsNot Nothing) Then
                Dim loCfl As ChooseFromList = MyForm.TriggerObject
                If loCfl.ParamRelations.Contains("[%0]") Then
                    Integer.TryParse(loCfl.ParamRelations.Item("[%0]").Value, iiBPLId)
                End If
            End If
            ioFld_10.Select(True)
        End If
    End Sub

    Private Sub M0_On_Form_Resize(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles Me.On_Form_Resize
        If Not pVal.BeforeAction AndAlso Not ioBtn_OK Is Nothing Then
            ioRec_10.Item.Width = ioMtx_10.Item.Width + 10
            ioRec_10.Item.Height = ioMtx_10.Item.Height + 20
        End If
    End Sub

    Private Sub M0_On_Form_PaneChange(BeforeAction As Boolean, PaneLevel As Integer, ByRef BubbleEvent As Boolean) Handles Me.On_Form_PaneChange
        If BeforeAction Then
            If iiPane = 1 AndAlso Not String.IsNullOrEmpty(MyForm.ActiveItem) Then
                Dim loItem As Item
                loItem = MyForm.Items.Item(MyForm.ActiveItem)
                If TypeOf (loItem.Specific) Is EditText Then
                    Dim loEdit_Item As EditText = loItem.Specific
                    loEdit_Item.Active = False
                End If
            End If
        End If
        If Not BeforeAction Then
            If PaneLevel = 1 Then
                ioFld_30.Item.SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, BoAutoFormMode.afm_All, BoModeVisualBehavior.mvb_False)
            ElseIf PaneLevel = 2 Then
                If iiPane = 1 Then
                    '从条件页签切换到生产订单明细页签，查询生产订单明细数据
                    If ibConditionChanged Then
                        RefreshMtx_10()
                    End If
                End If
                ioFld_30.Item.SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, BoAutoFormMode.afm_All, BoModeVisualBehavior.mvb_True)
            ElseIf PaneLevel = 3 Then
                If iiPane = 2 Then
                    If ibSelectedNumChanged Then
                        RefreshMtx_20()
                    End If
                End If
            End If
            iiPane = PaneLevel
        End If
    End Sub

    Private Sub M0_On_Form_Close(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles Me.On_Form_Close
        If pVal.BeforeAction Then
            If Not ibUpdateData Then
                '如果不需要更新父窗口数据，清除结果表中的数据
                ioDt_Result.Rows.Clear()
            End If
        End If
    End Sub

    Private Sub ioBtn_ClearCond_On_Pressed(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles ioBtn_ClearCond.On_Pressed
        If Not pVal.BeforeAction Then
            CType(GetItemSpecific("SoNumF"), EditText).Value = ""
            CType(GetItemSpecific("SoNumT"), EditText).Value = ""
            ioUds_ItemCodeF.ValueEx = String.Empty
            ioUds_ItemCodeT.ValueEx = String.Empty
            ioUds_CardCodeF.ValueEx = String.Empty
            ioUds_CardCodeT.ValueEx = String.Empty
            ioUds_NumAtCardF.ValueEx = String.Empty
            ioUds_NumAtCardT.ValueEx = String.Empty
            ioUds_ShipDateF.ValueEx = String.Empty
            ioUds_ShipDateT.ValueEx = String.Empty
        End If
    End Sub

    Private Sub ioBtn_OK_On_Pressed(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles ioBtn_OK.On_Pressed
        If pVal.BeforeAction Then
            If MyForm.PaneLevel = 2 Then
                Dim lsSql As String = GenDtlSql("Y")
                Dim loDt_Sql As DataTable = GetDataTable("M0_Sql")

                loDt_Sql.ExecuteQuery(lsSql)
                If Not loDt_Sql.IsRowEmpty(0) Then
                    Dim lsXml As String = loDt_Sql.SerializeAsXML(BoDataTableXmlSelect.dxs_DataOnly)
                    ioDt_Result.LoadSerializedXML(BoDataTableXmlSelect.dxs_DataOnly, lsXml)
                End If
            Else
                ioMtx_20.FlushToDataSource()
            End If
            ibUpdateData = True
        End If
    End Sub

    Private Sub M0_On_Item_Event(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles Me.On_Item_Event
        If Not pVal.BeforeAction AndAlso pVal.EventType = BoEventTypes.et_VALIDATE AndAlso pVal.ItemChanged Then
            If MyForm.DataSources.UserDataSources.Exists(pVal.ItemUID) AndAlso pVal.ItemUID <> "FolderDS" AndAlso pVal.ItemUID <> "Clear" Then
                ibConditionChanged = True
            End If
        End If
    End Sub

    Private Sub ioMtx_10_On_DoubleClick(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles ioMtx_10.On_DoubleClick
        If pVal.BeforeAction AndAlso pVal.ColUID = "Select" Then
            If pVal.Row = 0 Then
                MyForm.FreezeForm(
                Sub()
                    ioMtx_10.FlushToDataSource()
                    If ioDt_Doc.Rows.Count > 0 Then
                        Dim lsSelect As String = ioDt_Doc.GetValue("U_Select", 0)
                        If lsSelect = "Y" Then
                            lsSelect = "N"
                        Else
                            lsSelect = "Y"
                        End If

                        For i As Integer = 0 To ioDt_Doc.Rows.Count - 1
                            ioDt_Doc.Rows.Offset = i
                            ioDt_Doc.SetValue("U_Select", lsSelect)
                        Next
                        ioMtx_10.LoadFromDataSource()

                        Throw New UIException("")
                        ibSelectedNumChanged = True
                    End If
                End Sub)
            Else
                ibSelectedNumChanged = True
            End If
        End If
    End Sub

    Private Sub ioMtx_10_On_Pressed(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles ioMtx_10.On_Pressed
        If pVal.BeforeAction AndAlso pVal.ColUID = "Select" AndAlso pVal.Row > 0 Then
            ibSelectedNumChanged = True
        End If
    End Sub

    Private Sub ioMtx_20_On_DoubleClick(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles ioMtx_20.On_DoubleClick
        If Not pVal.BeforeAction AndAlso pVal.ColUID = "Select" AndAlso pVal.Row = 0 Then
            MyForm.FreezeForm(
            Sub()
                ioMtx_20.FlushToDataSource()
                If ioDt_Result.Rows.Count > 0 Then
                    Dim lsSelect As String = ioDt_Result.GetValue("U_Select", 0)
                    If lsSelect = "Y" Then
                        lsSelect = "N"
                    Else
                        lsSelect = "Y"
                    End If

                    For i As Integer = 0 To ioDt_Result.Rows.Count - 1
                        ioDt_Result.Rows.Offset = i
                        ioDt_Result.SetValue("U_Select", lsSelect)
                    Next
                    ioMtx_20.LoadFromDataSource()

                    Throw New UIException("")
                End If
            End Sub)
        End If
    End Sub

#End Region

#Region "私有方法"

    ''' <summary>
    ''' 重新加载Mtx_10的数据
    ''' </summary>
    Private Sub RefreshMtx_10()
        Dim lsSql As String = GenDocSql()
        Dim loDt_Sql As DataTable = GetDataTable("M0_Sql")
        MyForm.FreezeForm(
        Sub()
            ioDt_Doc.Rows.Clear()
            loDt_Sql.ExecuteQuery(lsSql)
            If Not loDt_Sql.IsRowEmpty(0) Then
                Dim lsXml As String = loDt_Sql.SerializeAsXML(BoDataTableXmlSelect.dxs_DataOnly)
                ioDt_Doc.LoadSerializedXML(BoDataTableXmlSelect.dxs_DataOnly, lsXml)
            End If
            ioMtx_10.LoadFromDataSource()
            ibConditionChanged = False
            ibSelectedNumChanged = True
        End Sub)
    End Sub

    ''' <summary>
    ''' 重新加载Mtx_20的数据
    ''' </summary>
    Private Sub RefreshMtx_20()
        Dim lsSql As String = GenDtlSql()
        Dim loDt_Sql As DataTable = GetDataTable("M0_Sql")
        MyForm.FreezeForm(
        Sub()
            ioDt_Result.Rows.Clear()
            loDt_Sql.ExecuteQuery(lsSql)
            If Not loDt_Sql.IsRowEmpty(0) Then
                Dim lsXml As String = loDt_Sql.SerializeAsXML(BoDataTableXmlSelect.dxs_DataOnly)
                ioDt_Result.LoadSerializedXML(BoDataTableXmlSelect.dxs_DataOnly, lsXml)
            End If
            ioMtx_20.LoadFromDataSource()
            ibSelectedNumChanged = False
        End Sub)
    End Sub

    Private Sub ioFld_20_On_Pressed(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles ioFld_20.On_Pressed
        If pVal.BeforeAction AndAlso MyForm.PaneLevel = 1 Then
            TryCast(GetItemSpecific("SoNumF"), EditText).Active = True
        End If
    End Sub

    ''' <summary>
    ''' 生成Mtx_10的SQL语句 
    ''' </summary>
    ''' <returns>SQL语句</returns>
    Private Function GenDocSql() As String
        Dim lsSql, lsSqlTemplate, lsConditionMain, lsConditionLine As String
        Dim liValue As Integer
        lsSqlTemplate = "
            select cast('N' as nvarchar(1)) U_Select, row_number() over (order by t10.DocEntry) LineId, t10.DocEntry, t10.DocNum,
                t10.CardCode, t10.CardName, t10.NumAtCard, t10.SupplCode, t10.DocDueDate, t10.CntctCode, t10.SlpCode,
                t10.OwnerCode, t10.DocTotal, t10.DocType, t10.Comments
            from ORDR t10
            where (t10.BPLId = {0} or {0} = 0) and t10.DocStatus = 'O' and t10.CANCELED = 'N'
                and exists(
                    select 'A' from RDR1 t20 where t10.DocEntry = t20.DocEntry and t20.LineStatus = 'O' {2}
                ) {1}"

        '拼接条件字符串   
        lsConditionMain = String.Empty
        lsConditionLine = String.Empty

        Integer.TryParse(ioUds_SoNumF.ValueEx, liValue)
        If liValue > 0 Then
            lsConditionMain += String.Format(" and t10.DocNum >= '{0}'", ioUds_SoNumF.ValueEx)
        End If
        Integer.TryParse(ioUds_SoNumT.ValueEx, liValue)
        If liValue > 0 Then
            lsConditionMain += String.Format(" and t10.DocNum <= '{0}'", ioUds_SoNumT.ValueEx)
        End If

        If Not String.IsNullOrEmpty(ioUds_CardCodeF.ValueEx) Then
            lsConditionMain += String.Format(" and t10.CardCode >= '{0}'", ioUds_CardCodeF.ValueEx)
        End If
        If Not String.IsNullOrEmpty(ioUds_CardCodeT.ValueEx) Then
            lsConditionMain += String.Format(" and t10.CardCode <= '{0}'", ioUds_CardCodeT.ValueEx)
        End If

        If Not String.IsNullOrEmpty(ioUds_NumAtCardF.ValueEx) Then
            lsConditionMain += String.Format(" and t10.NumAtCard >= '{0}'", ioUds_NumAtCardF.ValueEx)
        End If
        If Not String.IsNullOrEmpty(ioUds_NumAtCardT.ValueEx) Then
            lsConditionMain += String.Format(" and t10.NumAtCard <= '{0}'", ioUds_NumAtCardT.ValueEx)
        End If

        If Not String.IsNullOrEmpty(ioUds_ShipDateF.ValueEx) Then
            lsConditionMain += String.Format(" and t10.DocDueDate >= '{0}'", ioUds_ShipDateF.ValueEx)
        End If
        If Not String.IsNullOrEmpty(ioUds_ShipDateT.ValueEx) Then
            lsConditionMain += String.Format(" and t10.DocDueDate <= '{0}'", ioUds_ShipDateT.ValueEx)
        End If

        If Not String.IsNullOrEmpty(ioUds_ItemCodeF.ValueEx) Then
            lsConditionLine += String.Format(" and t20.ItemCode >= '{0}'", ioUds_ItemCodeF.ValueEx)
        End If
        If Not String.IsNullOrEmpty(ioUds_ItemCodeT.ValueEx) Then
            lsConditionLine += String.Format(" and t20.ItemCode <= '{0}'", ioUds_ItemCodeT.ValueEx)
        End If

        lsSql = String.Format(lsSqlTemplate, iiBPLId.ToString(), lsConditionMain, lsConditionLine)
        Return lsSql
    End Function

    ''' <summary>
    ''' 生成Mtx_20的SQL语句
    ''' </summary>
    ''' <param name="lsSelect">U_Select字段值，Y/N</param>
    ''' <returns>SQL语句</returns>   
    Private Function GenDtlSql(Optional ByVal lsSelect As String = "N") As String
        Dim lsSql, lsSqlTemplate, lsCondition, lsSelectedNum, lsValue As String
        Dim liValue As Integer
        lsSqlTemplate = "
            select cast('{0}' as nvarchar(1)) U_Select, row_number() over (order by t10.DocEntry, t11.LineNum) LineId,
                 t10.DocEntry U_SoEntry, t10.DocNum U_SoNum, t11.LineNum U_SoLine, t11.ItemCode U_ItemCode, t12.ItemName U_ItemName,
				  t11.Quantity U_Quantity,  t11.ShipDate U_ShipDate, t11.ShipDate U_OShipDate,t10.U_Z31_CardCode,t10.U_Z31_CardCode U_Z31_OCardCode,
				  t10.U_Z31_CardName,t10.U_Z31_CardName U_Z31_OCardName,t11.Price U_Price,t11.Price U_OPrice,t10.U_Z31_BDPayRto,t10.U_Z31_BDPayRto U_Z31_OBDPayRto,
				  t10.U_Z31_ADPayRto,t10.U_Z31_ADPayRto U_Z31_OADPayRto,t10.U_Z31_TsPayRto,t10.U_Z31_TsPayRto U_Z31_OTsPayRto,t10.U_Z31_QGRto,t10.U_Z31_QGRto U_Z31_OQGRto,
				  t10.U_Z31_LGRto,t10.U_Z31_LGRto U_Z31_OLGRto
            from ORDR t10
                inner join RDR1 t11 on t10.DocEntry = t11.DocEntry
                inner join OITM t12 on t11.ItemCode = t12.ItemCode
            where (t10.BPLId = {1} or {1} = 0) and t10.DocStatus = 'O' and t10.CANCELED = 'N' and t11.LineStatus = 'O'
                and t10.DocNum in ({2}) {3}"
        lsCondition = String.Empty

        Integer.TryParse(ioUds_SoNumF.ValueEx, liValue)
        If liValue > 0 Then
            lsCondition += String.Format(" and t10.DocNum >= '{0}'", ioUds_SoNumF.ValueEx)
        End If
        Integer.TryParse(ioUds_SoNumT.ValueEx, liValue)
        If liValue > 0 Then
            lsCondition += String.Format(" and t10.DocNum <= '{0}'", ioUds_SoNumT.ValueEx)
        End If

        If Not String.IsNullOrEmpty(ioUds_CardCodeF.ValueEx) Then
            lsCondition += String.Format(" and t10.CardCode >= '{0}'", ioUds_CardCodeF.ValueEx)
        End If
        If Not String.IsNullOrEmpty(ioUds_CardCodeT.ValueEx) Then
            lsCondition += String.Format(" and t10.CardCode <= '{0}'", ioUds_CardCodeT.ValueEx)
        End If

        If Not String.IsNullOrEmpty(ioUds_NumAtCardF.ValueEx) Then
            lsCondition += String.Format(" and t10.NumAtCard >= '{0}'", ioUds_NumAtCardF.ValueEx)
        End If
        If Not String.IsNullOrEmpty(ioUds_NumAtCardT.ValueEx) Then
            lsCondition += String.Format(" and t10.NumAtCard <= '{0}'", ioUds_NumAtCardT.ValueEx)
        End If

        If Not String.IsNullOrEmpty(ioUds_ShipDateF.ValueEx) Then
            lsCondition += String.Format(" and t10.DocDueDate >= '{0}'", ioUds_ShipDateF.ValueEx)
        End If
        If Not String.IsNullOrEmpty(ioUds_ShipDateT.ValueEx) Then
            lsCondition += String.Format(" and t10.DocDueDate <= '{0}'", ioUds_ShipDateT.ValueEx)
        End If

        If Not String.IsNullOrEmpty(ioUds_ItemCodeF.ValueEx) Then
            lsCondition += String.Format(" and t11.ItemCode >= '{0}'", ioUds_ItemCodeF.ValueEx)
        End If
        If Not String.IsNullOrEmpty(ioUds_ItemCodeT.ValueEx) Then
            lsCondition += String.Format(" and t11.ItemCode <= '{0}'", ioUds_ItemCodeT.ValueEx)
        End If

        lsSelectedNum = "0,"
        ioMtx_10.FlushToDataSource()
        For i As Integer = 0 To ioDt_Doc.Rows.Count - 1
            ioDt_Doc.Rows.Offset = i
            If ioDt_Doc.GetValue("U_Select") = "Y" Then
                lsSelectedNum += Convert.ToString(ioDt_Doc.GetValue("DocNum")) + ","
            End If
        Next
        lsSelectedNum = lsSelectedNum.Remove(lsSelectedNum.Length - 1)

        lsSql = String.Format(lsSqlTemplate, lsSelect, iiBPLId.ToString(), lsSelectedNum, lsCondition)
        Return lsSql
    End Function

#End Region

End Class
