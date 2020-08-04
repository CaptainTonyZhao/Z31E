Imports boDIProxy
Imports boUIExt

Public Class Z31E010
    Inherits FormExt

    Private WithEvents ioBtn_OK As Button
    Private WithEvents ioBtn_Copy As Button
    Private WithEvents ioBtn_Exec As Button

    Private WithEvents ioMtx_10 As Matrix

    Private ioDbds_Z31E010 As DBDataSource
    Private ioDbds_Z31E011 As DBDataSource

#Region "系统事件"

    Private Sub Z31E010_On_Form_Create(BeforeAction As Boolean, CreationParam As FormCreationParams, ByRef BubbleEvent As Boolean) Handles Me.On_Form_Create
        If Not BeforeAction Then
            ioBtn_OK = GetItemSpecific("1")
            ioBtn_Copy = GetItemSpecific("Copy")
            ioBtn_Exec = GetItemSpecific("Exec")

            ioMtx_10 = GetItemSpecific("Mtx_10")
            ioMtx_10.SetSelectionModeEx(BoMatrixSelect.ms_Auto, False)
            ioMtx_10.AutoSelectRow = True


            ioDbds_Z31E010 = GetDBDataSource("@Z31E010")
            ioDbds_Z31E011 = GetDBDataSource("@Z31E011")

            ResetSeriesVvs(MyForm.Mode)
            If MyForm.Mode = BoFormMode.fm_ADD_MODE Then
                InitFormForAdd()
            End If
            SetItemEnabled()


            '初始化代理连接
            Proxy.GetProxyCompany(MyApplication.Company.ServerName, MyApplication.Company.DatabaseName, MyApplication.Company.UserName)
        End If
    End Sub

    Private Sub Z31E010_On_FormData_Load(pVal As BusinessObjectInfo, ByRef BubbleEvent As Boolean) Handles Me.On_FormData_Load
        If Not pVal.BeforeAction Then
            SetItemEnabled()
        End If
    End Sub

    Private Sub Z31E010_On_Form_ModeChange(BeforeAction As Boolean, Mode As BoFormMode, ByRef BubbleEvent As Boolean) Handles Me.On_Form_ModeChange
        If BeforeAction Then
            ResetSeriesVvs(MyForm.Mode)
        Else
            If Mode = BoFormMode.fm_ADD_MODE Then
                InitFormForAdd()
            ElseIf Mode = BoFormMode.fm_FIND_MODE Then
                ioDbds_Z31E010.SetValue("Status", 0, "O")
            End If
            SetItemEnabled()
        End If
    End Sub

    Private Sub ioBtn_Copy_On_ChooseFromList(FormUID As String, pVal As ChooseFromListEvent, ByRef BubbleEvent As Boolean) Handles ioBtn_Copy.On_ChooseFromList
        If Not pVal.BeforeAction AndAlso Not pVal.SelectedObjects Is Nothing Then
            Dim loDt As DataTable = pVal.SelectedObjects
            Dim lsClear As String = pVal.SelectedForm.DataSources.UserDataSources.Item("Clear").ValueEx

            MyForm.FreezeForm(
             Sub()
                 If lsClear = "Y" Then
                     ioDbds_Z31E011.Clear()
                 ElseIf ioDbds_Z31E011.Size = 1 AndAlso String.IsNullOrEmpty(ioDbds_Z31E011.GetValue("U_SoNum", 0)) Then
                     ioDbds_Z31E011.Clear()
                 End If

                 If loDt.Rows.Count > 0 AndAlso Not loDt.IsRowEmpty(0) Then
                     Dim lsColumnName As String
                     Dim liMaxLine As Integer = 0

                     liMaxLine = ioDbds_Z31E011.GetMaxId("LineId") + 1
                     For i As Integer = 0 To loDt.Rows.Count - 1
                         loDt.Rows.Offset = i
                         If loDt.GetValue("U_Select") = "Y" Then
                             ioDbds_Z31E011.InsertRecord(ioDbds_Z31E011.Size, True)
                             ioDbds_Z31E011.SetValue("LineId", liMaxLine.ToString())
                             For j As Integer = 2 To loDt.Columns.Count - 1
                                 lsColumnName = loDt.Columns(j).Name
                                 ioDbds_Z31E011.SetValue(lsColumnName, loDt.GetValueStr(lsColumnName))
                             Next
                             ioDbds_Z31E011.SetValue("U_ChangeType", "1")
                             liMaxLine += 1
                         End If
                     Next
                 End If
                 ioMtx_10.LoadFromDataSource()

                 If MyForm.Mode = BoFormMode.fm_OK_MODE Then
                     MyForm.Mode = BoFormMode.fm_UPDATE_MODE
                 End If
             End Sub)
        End If
    End Sub

    Private Sub ioBtn_Exec_On_Pressed(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles ioBtn_Exec.On_Pressed
        If Not pVal.BeforeAction Then
            ioMtx_10.FlushToDataSource()
            If MyForm.Mode <> BoFormMode.fm_OK_MODE Then
                Throw New UIException("Z31", "Z31E010", "单据未确认状态，不可执行变更", Nothing)
            End If

            If MessageBox("Z31E010", "是否确定执行变更？", Nothing, 1, "是", "否") <> 1 Then
                Throw New UIException("")
            End If

            Dim liCount As Integer = 0

            SetStatusBarMessage("Z31E010", "正在执行变更，请稍后", Nothing, BoMessageTime.bmt_Long, BoStatusBarMessageType.smt_Success)
            Proxy.RunScript(MyApplication.Company.ServerName, MyApplication.Company.DatabaseName, MyApplication.Company.UserName,
            Sub(loCompany)
                UpdateSalesOrder(loCompany)
            End Sub)

            'ioDbds_Z31E010.SetValue("U_UpType", 0, "Exec")
            If MyForm.Mode = BoFormMode.fm_OK_MODE Then
                MyForm.Mode = BoFormMode.fm_UPDATE_MODE
            End If

            ioBtn_OK.Item.Click(BoCellClickType.ct_Regular)
        End If
    End Sub

    Private Sub ioBtn_OK_On_Pressed(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles ioBtn_OK.On_Pressed
        If Not pVal.BeforeAction Then
            SetItemEnabled()
        End If
    End Sub

    Private Sub ioMtx_10_On_ComboSelect(FormUID As String, pVal As ItemEvent, ByRef BubbleEvent As Boolean) Handles ioMtx_10.On_ComboSelect
        'TODO: 暂时不做新增行，新增行需要考虑价格
        'If pVal.BeforeAction AndAlso pVal.ItemChanged AndAlso pVal.ColUID = "ChangeType" Then
        '    If pVal.PopUpIndicator = 0 Then
        '        BubbleEvent = False
        '        SetStatusBarMessage("Z31E010", "请通过""添加行""操作新增物料!", Nothing, BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Warning)
        '        Return
        '    End If
        'End If
        If Not pVal.BeforeAction AndAlso pVal.ItemChanged AndAlso pVal.ColUID = "ChangeType" Then
            Dim lsChangeType As String
            Dim liSoEntry, liSoLine As Integer

            ioMtx_10.FlushToDataSource()
            ioDbds_Z31E011.Offset = pVal.Row - 1
            lsChangeType = ioDbds_Z31E011.GetValue("U_ChangeType")
            If lsChangeType = "1" OrElse lsChangeType = "4" OrElse lsChangeType = "5" Then
                Integer.TryParse(ioDbds_Z31E011.GetValue("U_SoEntry"), liSoEntry)
                For Each liOffset In ioDbds_Z31E011.SelectRecords("[U_SoEntry]=" + liSoEntry.ToString())
                    ioDbds_Z31E011.Offset = liOffset
                    ioDbds_Z31E011.SetValue("U_ChangeType", lsChangeType)
                    ioMtx_10.SetLineData(liOffset + 1)
                    SetMtx_10CellEditable(liOffset + 1)
                Next
            End If
        End If
    End Sub

    Private Sub ioMtx_10_On_RowDelete(BeforeAction As Boolean, FormUID As String, ItemUID As String, StartRow As Integer, RowCount As Integer, ByRef BubbleEvent As Boolean) Handles ioMtx_10.On_RowDelete
        If Not BeforeAction Then
            ioMtx_10.FlushToDataSource()
            For i = 0 To ioDbds_Z31E011.Size - 1
                ioDbds_Z31E011.Offset = i
                ioDbds_Z31E011.SetValue("LineId", Convert.ToString(i + 1))
                ioMtx_10.SetLineData(i + 1)
            Next
            SetMtx_10CellEditable()
        End If
    End Sub

#End Region

#Region "私有方法"

    ''' <summary>
    ''' 根据不同的窗口模式，设置序列选项值
    ''' </summary>
    ''' <param name="loFormMode">窗口模式</param>
    Private Sub ResetSeriesVvs(ByVal loFormMode As BoSeriesMode)
        Dim loCmb_Series As ComboBox

        loCmb_Series = GetItemSpecific("Series")
        loCmb_Series.ValidValues.LoadSeries(MyForm.BusinessObject.Type, loFormMode)
    End Sub

    ''' <summary>
    ''' 当窗体变成添加状态时，初始化一些基本参数
    ''' </summary>
    Private Sub InitFormForAdd()
        Dim lsDefaultSeries As String

        lsDefaultSeries = BusinessObject.GetDefaultSeries(MyForm.BusinessObject.Type)
        ioDbds_Z31E010.SetValue("Series", 0, lsDefaultSeries)
        ioDbds_Z31E010.SetValue("DocNum", 0, MyForm.BusinessObject.GetNextSerialNumber(lsDefaultSeries, MyForm.BusinessObject.Type).ToString)
        ioDbds_Z31E010.SetValue("U_DocDate", 0, Today.ToString("yyyyMMdd"))
        ioDbds_Z31E010.SetValue("Status", 0, "O")
    End Sub

    ''' <summary>
    ''' 设置Item状态
    ''' </summary>
    Private Sub SetItemEnabled()
        Dim lsCanCeled, lsStatus As String

        lsCanCeled = ioDbds_Z31E010.GetValue("Canceled", 0)
        lsStatus = ioDbds_Z31E010.GetValue("Status", 0)
        If lsCanCeled = "Y" Then
            If MyForm.Mode = BoFormMode.fm_OK_MODE Then
                MyForm.Mode = BoFormMode.fm_VIEW_MODE
            End If
        ElseIf MyForm.FormPermission <> BoPermission.ReadOnly Then
            If MyForm.Mode = BoFormMode.fm_VIEW_MODE Then
                MyForm.Mode = BoFormMode.fm_EDIT_MODE
                MyForm.Mode = BoFormMode.fm_OK_MODE
            End If
        End If

        If lsStatus = "O" Then
            ioMtx_10.Item.SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, 7, BoModeVisualBehavior.mvb_True)
            ioBtn_Copy.Item.SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, 7, BoModeVisualBehavior.mvb_True)
            ioBtn_Exec.Item.SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, 1, BoModeVisualBehavior.mvb_True)
            GetItem("DocDate").SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, 7, BoModeVisualBehavior.mvb_True)
        Else
            ioMtx_10.Item.SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, 7, BoModeVisualBehavior.mvb_False)
            ioBtn_Copy.Item.SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, 7, BoModeVisualBehavior.mvb_False)
            ioBtn_Exec.Item.SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, 1, BoModeVisualBehavior.mvb_False)
            GetItem("DocDate").SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, 7, BoModeVisualBehavior.mvb_False)
        End If

        SetMtx_10CellEditable()
    End Sub

    Private Sub SetMtx_10CellEditable(Optional ByVal liRow As Integer = 0)
        'Dim lsChangeType As String
        'Dim liSoLine As Integer
        'MyForm.FreezeForm(
        'Sub()
        '    If liRow > 0 AndAlso liRow <= ioMtx_10.VisualRowCount Then
        '        lsChangeType = ioMtx_10.GetValue("ChangeType", liRow)
        '        Integer.TryParse(ioMtx_10.GetValue("SoLine", liRow), liSoLine)
        '        ioMtx_10.CommonSetting.SetCellEditable(liRow, ioMtx_10.Columns("ItemCode").Index, lsChangeType = "A")
        '        ioMtx_10.CommonSetting.SetCellEditable(liRow, ioMtx_10.Columns("ChangeType").Index, liSoLine > -1)
        '    Else
        '        For i = 1 To ioMtx_10.VisualRowCount
        '            lsChangeType = ioMtx_10.GetValue("ChangeType", i)
        '            Integer.TryParse(ioMtx_10.GetValue("SoLine", i), liSoLine)
        '            ioMtx_10.CommonSetting.SetCellEditable(i, ioMtx_10.Columns("ItemCode").Index, lsChangeType = "A")
        '            ioMtx_10.CommonSetting.SetCellEditable(i, ioMtx_10.Columns("ChangeType").Index, liSoLine > -1)
        '        Next
        '    End If
        'End Sub)
    End Sub
    'Private Sub UpdateSalesOrder(ByRef loCompany As ProxyCompany)
    '    Dim liSoEntry, liSoLine, liErrorId, liAddLine As Integer
    '    Dim ldQuantity As Decimal
    '    Dim ldPrice, ldOPrice, ldBDPayRto, ldADPayRto, ldTsPayRto, ldQGRto, ldLGRto As Double
    '    Dim ldShipDate As Date
    '    Dim lsChangeType, lsItemCode, lsCardUnit, lsCardUnitN As String
    '    Dim loSoList As List(Of Integer) = New List(Of Integer)
    '    Dim loBoSalesOrder As SAPbobsCOM.Documents


    '    '先取消订单
    '    For Each liOffset In ioDbds_Z31E011.SelectRecords("[U_ChangeType]=""C""")
    '        Integer.TryParse(ioDbds_Z31E011.GetValue("U_SoEntry", liOffset), liSoEntry)
    '        If Not loSoList.Contains(liSoEntry) Then
    '            loBoSalesOrder = loCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders)
    '            If loBoSalesOrder.GetByKey(liSoEntry) Then
    '                liErrorId = loBoSalesOrder.Cancel()
    '                If liErrorId = 0 Then
    '                    SetStatusBarMessage("Z31E010", "取消销售订单：" + liSoEntry.ToString + " 成功", Nothing, BoMessageTime.bmt_Long, BoStatusBarMessageType.smt_Success)
    '                Else
    '                    Throw New UIException("Z31", "Z31E010", loCompany.GetLastErrorDescription(), Nothing)
    '                End If
    '            End If
    '            loSoList.Add(liSoEntry)
    '        End If
    '    Next

    '    '更新订单
    '    loSoList.Clear()
    '    For Each liOffset In ioDbds_Z31E011.SelectRecords("[U_ChangeType]<>""C""")
    '        Integer.TryParse(ioDbds_Z31E011.GetValue("U_SoEntry"， liOffset), liSoEntry)
    '        If Not loSoList.Contains(liSoEntry) Then
    '            loSoList.Add(liSoEntry)
    '        End If
    '    Next
    '    For Each liSoEntry In loSoList
    '        loBoSalesOrder = loCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders)
    '        If loBoSalesOrder.GetByKey(liSoEntry) Then
    '            For Each liOffset In ioDbds_Z31E011.SelectRecords("[U_SoEntry]=" + liSoEntry.ToString())
    '                ioDbds_Z31E011.Offset = liOffset
    '                lsChangeType = ioDbds_Z31E011.GetValue("U_ChangeType")
    '                Integer.TryParse(ioDbds_Z31E011.GetValue("U_SoLine"), liSoLine)
    '                If lsChangeType = "U" Then
    '                    liAddLine = loBoSalesOrder.Lines.Count
    '                    For i As Integer = loBoSalesOrder.Lines.Count - 1 To 0 Step -1
    '                        loBoSalesOrder.Lines.SetCurrentLine(i)
    '                        If loBoSalesOrder.Lines.LineNum = liSoLine Then
    '                            lsItemCode = ioDbds_Z31E011.GetValue("U_ItemCode")
    '                            lsCardUnit = ioDbds_Z31E011.GetValue("U_Z31_CardCode")
    '                            lsCardUnitN = ioDbds_Z31E011.GetValue("U_Z31_CardName")
    '                            Decimal.TryParse(ioDbds_Z31E011.GetValue("U_Quantity"), ldQuantity)
    '                            Double.TryParse(ioDbds_Z31E011.GetValue("U_Price"), ldPrice)
    '                            Double.TryParse(ioDbds_Z31E011.GetValue("U_OPrice"), ldOPrice)
    '                            Double.TryParse(ioDbds_Z31E011.GetValue("U_Z31_BDPayRto"), ldBDPayRto)
    '                            Double.TryParse(ioDbds_Z31E011.GetValue("U_Z31_ADPayRto"), ldADPayRto)
    '                            Double.TryParse(ioDbds_Z31E011.GetValue("U_Z31_TsPayRto"), ldTsPayRto)
    '                            Double.TryParse(ioDbds_Z31E011.GetValue("U_Z31_QGRto"), ldQGRto)
    '                            Double.TryParse(ioDbds_Z31E011.GetValue("U_Z31_LGRto"), ldLGRto)
    '                            ldShipDate = ioDbds_Z31E011.GetValue("U_ShipDate").ToDate()
    '                            loBoSalesOrder.Lines.ShipDate = ldShipDate
    '                            loBoSalesOrder.UserFields.Fields.Item("U_Z31_CardCode").Value = lsCardUnit
    '                            loBoSalesOrder.UserFields.Fields.Item("U_Z31_CardName").Value = lsCardUnitN
    '                            loBoSalesOrder.UserFields.Fields.Item("U_Z31_BDPayRto").Value = ldBDPayRto
    '                            loBoSalesOrder.UserFields.Fields.Item("U_Z31_ADPayRto").Value = ldADPayRto
    '                            loBoSalesOrder.UserFields.Fields.Item("U_Z31_TsPayRto").Value = ldTsPayRto
    '                            loBoSalesOrder.UserFields.Fields.Item("U_Z31_QGRto").Value = ldQGRto
    '                            loBoSalesOrder.UserFields.Fields.Item("U_Z31_LGRto").Value = ldLGRto
    '                            If ldPrice > 0 Then
    '                                If ldPrice > ldOPrice Then
    '                                    loBoSalesOrder.Lines.Add()
    '                                    loBoSalesOrder.Lines.SetCurrentLine(liAddLine)
    '                                    loBoSalesOrder.Lines.ItemCode = lsItemCode
    '                                    loBoSalesOrder.Lines.Quantity = ldQuantity
    '                                    loBoSalesOrder.Lines.Price = ldPrice - ldOPrice
    '                                    loBoSalesOrder.Lines.ShipDate = ldShipDate
    '                                ElseIf ldPrice < ldOPrice Then
    '                                    loBoSalesOrder.UserFields.Fields.Item("U_Z31_DiffPrice").Value = ldPrice
    '                                End If
    '                            End If
    '                        End If
    '                    Next
    '                End If
    '            Next

    '            liErrorId = loBoSalesOrder.Update()
    '            If liErrorId = 0 Then
    '                SetStatusBarMessage("Z31E010", "更新销售订单：" + liSoEntry.ToString + " 成功", Nothing, BoMessageTime.bmt_Long, BoStatusBarMessageType.smt_Success)
    '            Else
    '                Throw New UIException("M0", "Z31E010", loCompany.GetLastErrorDescription(), Nothing)
    '            End If
    '        End If
    '    Next
    '    ioDbds_Z31E010.SetValue("Status", "C")
    'End Sub
    Private Sub UpdateSalesOrder(ByRef loCompany As ProxyCompany)
        Dim liSoEntry, liSoLine, liErrorId, liAddLine As Integer
        Dim ldQuantity As Decimal
        Dim ldPrice, ldOPrice, ldBDPayRto, ldADPayRto, ldTsPayRto, ldQGRto, ldLGRto As Double
        Dim ldShipDate As Date
        Dim lsChangeType, lsItemCode, lsCardUnit, lsCardUnitN As String
        Dim loSoList As List(Of Integer) = New List(Of Integer)
        Dim loBoSalesOrder As SAPbobsCOM.Documents


        '先取消订单
        For Each liOffset In ioDbds_Z31E011.SelectRecords("[U_ChangeType]=""5""")
            Integer.TryParse(ioDbds_Z31E011.GetValue("U_SoEntry", liOffset), liSoEntry)
            If Not loSoList.Contains(liSoEntry) Then
                loBoSalesOrder = loCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders)
                If loBoSalesOrder.GetByKey(liSoEntry) Then
                    liErrorId = loBoSalesOrder.Cancel()
                    If liErrorId = 0 Then
                        SetStatusBarMessage("Z31E010", "取消销售订单：" + liSoEntry.ToString + " 成功", Nothing, BoMessageTime.bmt_Long, BoStatusBarMessageType.smt_Success)
                    Else
                        Throw New UIException("Z31", "Z31E010", loCompany.GetLastErrorDescription(), Nothing)
                    End If
                End If
                loSoList.Add(liSoEntry)
            End If
        Next

        '更新订单
        loSoList.Clear()
        For Each liOffset In ioDbds_Z31E011.SelectRecords("[U_ChangeType]<>""5""")
            Integer.TryParse(ioDbds_Z31E011.GetValue("U_SoEntry"， liOffset), liSoEntry)
            If Not loSoList.Contains(liSoEntry) Then
                loSoList.Add(liSoEntry)
            End If
        Next
        For Each liSoEntry In loSoList
            loBoSalesOrder = loCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders)
            If loBoSalesOrder.GetByKey(liSoEntry) Then
                For Each liOffset In ioDbds_Z31E011.SelectRecords("[U_SoEntry]=" + liSoEntry.ToString())
                    ioDbds_Z31E011.Offset = liOffset
                    lsChangeType = ioDbds_Z31E011.GetValue("U_ChangeType")
                    Integer.TryParse(ioDbds_Z31E011.GetValue("U_SoLine"), liSoLine)
                    If lsChangeType = "1" Then
                        Double.TryParse(ioDbds_Z31E011.GetValue("U_Z31_BDPayRto"), ldBDPayRto)
                        Double.TryParse(ioDbds_Z31E011.GetValue("U_Z31_ADPayRto"), ldADPayRto)
                        Double.TryParse(ioDbds_Z31E011.GetValue("U_Z31_TsPayRto"), ldTsPayRto)
                        Double.TryParse(ioDbds_Z31E011.GetValue("U_Z31_QGRto"), ldQGRto)
                        Double.TryParse(ioDbds_Z31E011.GetValue("U_Z31_LGRto"), ldLGRto)


                        loBoSalesOrder.UserFields.Fields.Item("U_Z31_BDPayRto").Value = ldBDPayRto
                        loBoSalesOrder.UserFields.Fields.Item("U_Z31_ADPayRto").Value = ldADPayRto
                        loBoSalesOrder.UserFields.Fields.Item("U_Z31_TsPayRto").Value = ldTsPayRto
                        loBoSalesOrder.UserFields.Fields.Item("U_Z31_QGRto").Value = ldQGRto
                        loBoSalesOrder.UserFields.Fields.Item("U_Z31_LGRto").Value = ldLGRto

                    ElseIf lsChangeType = "2" Then
                        For i As Integer = loBoSalesOrder.Lines.Count - 1 To 0 Step -1
                            loBoSalesOrder.Lines.SetCurrentLine(i)
                            If loBoSalesOrder.Lines.LineNum = liSoLine Then
                                ldShipDate = ioDbds_Z31E011.GetValue("U_ShipDate").ToDate()
                                loBoSalesOrder.Lines.ShipDate = ldShipDate
                            End If
                        Next
                    ElseIf lsChangeType = "3" Then
                        liAddLine = loBoSalesOrder.Lines.Count
                        For i As Integer = loBoSalesOrder.Lines.Count - 1 To 0 Step -1
                            loBoSalesOrder.Lines.SetCurrentLine(i)
                            If loBoSalesOrder.Lines.LineNum = liSoLine Then
                                lsItemCode = ioDbds_Z31E011.GetValue("U_ItemCode")
                                Decimal.TryParse(ioDbds_Z31E011.GetValue("U_Quantity"), ldQuantity)
                                Double.TryParse(ioDbds_Z31E011.GetValue("U_Price"), ldPrice)
                                Double.TryParse(ioDbds_Z31E011.GetValue("U_OPrice"), ldOPrice)

                                If ldPrice > 0 Then
                                    If ldPrice > ldOPrice Then
                                        loBoSalesOrder.Lines.Add()
                                        loBoSalesOrder.Lines.SetCurrentLine(liAddLine)
                                        loBoSalesOrder.Lines.ItemCode = lsItemCode
                                        loBoSalesOrder.Lines.Quantity = ldQuantity
                                        loBoSalesOrder.Lines.Price = ldPrice - ldOPrice
                                        loBoSalesOrder.Lines.ShipDate = ldShipDate
                                    ElseIf ldPrice < ldOPrice Then
                                        loBoSalesOrder.UserFields.Fields.Item("U_Z31_DiffPrice").Value = ldPrice
                                    End If
                                End If
                            End If
                        Next
                    ElseIf lsChangeType = "4" Then
                        lsCardUnit = ioDbds_Z31E011.GetValue("U_Z31_CardCode")
                        lsCardUnitN = ioDbds_Z31E011.GetValue("U_Z31_CardName")

                        loBoSalesOrder.UserFields.Fields.Item("U_Z31_CardCode").Value = lsCardUnit
                        loBoSalesOrder.UserFields.Fields.Item("U_Z31_CardName").Value = lsCardUnitN
                    End If
                Next

                liErrorId = loBoSalesOrder.Update()
                If liErrorId = 0 Then
                    SetStatusBarMessage("Z31E010", "更新销售订单：" + liSoEntry.ToString + " 成功", Nothing, BoMessageTime.bmt_Long, BoStatusBarMessageType.smt_Success)
                Else
                    Throw New UIException("Z31", "Z31E010", loCompany.GetLastErrorDescription(), Nothing)
                End If
            End If
        Next
        ioDbds_Z31E010.SetValue("Status", "C")
    End Sub

#End Region

End Class
