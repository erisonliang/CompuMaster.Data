Option Explicit On 
Option Strict On

Imports System.IO

Namespace CompuMaster.Data

    ''' <summary>
    '''     Provides simplified access to CSV files
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    Friend Class CsvTools

#Region "Read data"

#Region "Fixed columns"
        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Read from a CSV table
        ''' </summary>
        ''' <param name="reader">A stream reader targetting CSV data</param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="columnWidths">An array of column widths in their order</param>
        ''' <param name="convertEmptyStringsToDBNull">Convert values with empty strings automatically to DbNull</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' In case of duplicate column names, all additional occurances of the same column name will be modified to use a unique column name
        ''' </remarks>
        ''' <history>
        ''' 	[adminsupport]	19.04.2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Shared Function ReadDataTableFromCsvReader(ByVal reader As StreamReader, ByVal includesColumnHeaders As Boolean, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal columnWidths As Integer(), ByVal convertEmptyStringsToDBNull As Boolean) As DataTable

            If cultureFormatProvider Is Nothing Then
                cultureFormatProvider = System.Globalization.CultureInfo.InvariantCulture
            End If
            If columnWidths Is Nothing Then
                columnWidths = New Integer() {Integer.MaxValue}
            End If

            Dim Result As New DataTable
            Dim rdStr As String
            Dim RowCounter As Integer

            'Read file content
            rdStr = reader.ReadToEnd
            If rdStr = Nothing Then
                'simply return the empty table when there is no input data
                Return Result
            End If

            'Read the file char by char and add row by row
            Dim CharPosition As Integer = 0
            While CharPosition < rdStr.Length

                'Read the next csv row
                Dim ColValues As New ArrayList
                SplitFixedCsvLineIntoCellValues(rdStr, ColValues, CharPosition, columnWidths)

                'Add it as a new data row (respectively add the columns definition)
                RowCounter += 1
                If RowCounter = 1 AndAlso includesColumnHeaders Then
                    'Read first line as column names
                    For ColCounter As Integer = 0 To ColValues.Count - 1
                        Dim colName As String = Trim(CType(ColValues(ColCounter), String))
                        If Result.Columns.Contains(colName) Then
                            colName = String.Empty
                        End If
                        If Result.Columns.Contains(colName) = False Then
                            Result.Columns.Add(New DataColumn(colName, GetType(String)))
                        Else
                            Result.Columns.Add(New DataColumn(DataTables.LookupUniqueColumnName(Result, colName), GetType(String)))
                        End If
                    Next
                Else
                    'Read line as data and automatically add required additional columns on the fly
                    Dim MyRow As DataRow = Result.NewRow
                    For ColCounter As Integer = 0 To ColValues.Count - 1
                        Dim colValue As String = Trim(CType(ColValues(ColCounter), String))
                        If Result.Columns.Count <= ColCounter Then
                            Result.Columns.Add(New DataColumn(Nothing, GetType(String)))
                        End If
                        MyRow(ColCounter) = colValue
                    Next
                    Result.Rows.Add(MyRow)
                End If

            End While

            If convertEmptyStringsToDBNull Then
                ConvertEmptyStringsToDBNullValue(Result)
            Else
                ConvertDBNullValuesToEmptyStrings(Result)
            End If

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Read from a CSV file
        ''' </summary>
        ''' <param name="path">The path of the file</param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="columnWidths">An array of column widths in their order</param>
        ''' <param name="encoding">The text encoding of the file</param>
        ''' <param name="convertEmptyStringsToDBNull">Convert values with empty strings automatically to DbNull</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' In case of duplicate column names, all additional occurances of the same column name will be modified to use a unique column name
        ''' </remarks>
        ''' <history>
        ''' 	[adminwezel]	03.07.2004	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function ReadDataTableFromCsvFile(ByVal path As String, ByVal includesColumnHeaders As Boolean, ByVal columnWidths As Integer(), ByVal encoding As String, ByVal convertEmptyStringsToDBNull As Boolean) As DataTable

            Dim Result As New DataTable
            Dim fi As FileInfo

            If File.Exists(path) Then
                fi = New FileInfo(path)
            ElseIf path.ToLower.StartsWith("http://") OrElse path.ToLower.StartsWith("https://") Then
                Dim LocalCopyOfFileContentFromRemoteUri As String = Utils.ReadStringDataFromUri(path, encoding)
                Return ReadDataTableFromCsvString(LocalCopyOfFileContentFromRemoteUri, includesColumnHeaders, columnWidths, convertEmptyStringsToDBNull)
            Else
                Throw New System.IO.FileNotFoundException("File not found", path)
            End If
            fi = Nothing

            Dim reader As StreamReader = Nothing
            Try
                If encoding = "" Then
                    reader = New StreamReader(path, System.Text.Encoding.Default)
                Else
                    reader = New StreamReader(path, System.Text.Encoding.GetEncoding(encoding))
                End If
                Result = ReadDataTableFromCsvReader(reader, includesColumnHeaders, System.Globalization.CultureInfo.CurrentCulture, columnWidths, convertEmptyStringsToDBNull)
            Finally
                If Not reader Is Nothing Then
                    reader.Close()
                End If
            End Try

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Read from a CSV file
        ''' </summary>
        ''' <param name="path">The path of the file</param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="columnWidths">An array of column widths in their order</param>
        ''' <param name="encoding">The text encoding of the file</param>
        ''' <param name="cultureFormatProvider"></param>
        ''' <param name="convertEmptyStringsToDBNull">Convert values with empty strings automatically to DbNull</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' In case of duplicate column names, all additional occurances of the same column name will be modified to use a unique column name
        ''' </remarks>
        ''' <history>
        ''' 	[adminwezel]	03.07.2004	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function ReadDataTableFromCsvFile(ByVal path As String, ByVal includesColumnHeaders As Boolean, ByVal columnWidths As Integer(), ByVal encoding As System.Text.Encoding, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal convertEmptyStringsToDBNull As Boolean) As DataTable

            Dim Result As New DataTable
            Dim fi As FileInfo

            If File.Exists(path) Then
                fi = New FileInfo(path)
            ElseIf path.ToLower.StartsWith("http://") OrElse path.ToLower.StartsWith("https://") Then
                Dim LocalCopyOfFileContentFromRemoteUri As String = Utils.ReadStringDataFromUri(path, encoding.WebName)
                Return ReadDataTableFromCsvString(LocalCopyOfFileContentFromRemoteUri, includesColumnHeaders, columnWidths, convertEmptyStringsToDBNull)
            Else
                Throw New System.IO.FileNotFoundException("File not found", path)
            End If
            fi = Nothing

            Dim reader As StreamReader = Nothing
            Try
                If encoding Is Nothing Then
                    reader = New StreamReader(path, System.Text.Encoding.Default)
                Else
                    reader = New StreamReader(path, (encoding))
                End If
                Result = ReadDataTableFromCsvReader(reader, includesColumnHeaders, cultureFormatProvider, columnWidths, convertEmptyStringsToDBNull)
            Finally
                If Not reader Is Nothing Then
                    reader.Close()
                End If
            End Try

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Read from a CSV table
        ''' </summary>
        ''' <param name="data">The content of a CSV file</param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="columnWidths">An array of column widths in their order</param>
        ''' <param name="convertEmptyStringsToDBNull">Convert values with empty strings automatically to DbNull</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' In case of duplicate column names, all additional occurances of the same column name will be modified to use a unique column name
        ''' </remarks>
        ''' <history>
        ''' 	[adminwezel]	03.07.2004	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function ReadDataTableFromCsvString(ByVal data As String, ByVal includesColumnHeaders As Boolean, ByVal columnWidths As Integer(), ByVal convertEmptyStringsToDBNull As Boolean) As DataTable

            Dim Result As New DataTable
            Dim reader As StreamReader = Nothing
            Try
                reader = New StreamReader(New MemoryStream(System.Text.Encoding.Unicode.GetBytes(data)), System.Text.Encoding.Unicode, False)
                Result = ReadDataTableFromCsvReader(reader, includesColumnHeaders, System.Globalization.CultureInfo.CurrentCulture, columnWidths, convertEmptyStringsToDBNull)
            Finally
                If Not reader Is Nothing Then
                    reader.Close()
                End If
            End Try

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Read from a CSV table
        ''' </summary>
        ''' <param name="data">The content of a CSV file</param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="cultureFormatProvider"></param>
        ''' <param name="columnWidths">An array of column widths in their order</param>
        ''' <param name="convertEmptyStringsToDBNull">Convert values with empty strings automatically to DbNull</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' In case of duplicate column names, all additional occurances of the same column name will be modified to use a unique column name
        ''' </remarks>
        ''' <history>
        ''' 	[adminwezel]	03.07.2004	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function ReadDataTableFromCsvString(ByVal data As String, ByVal includesColumnHeaders As Boolean, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal columnWidths As Integer(), ByVal convertEmptyStringsToDBNull As Boolean) As DataTable

            Dim Result As New DataTable
            Dim reader As StreamReader = Nothing
            Try
                reader = New StreamReader(New MemoryStream(System.Text.Encoding.Unicode.GetBytes(data)), System.Text.Encoding.Unicode, False)
                Result = ReadDataTableFromCsvReader(reader, includesColumnHeaders, cultureFormatProvider, columnWidths, convertEmptyStringsToDBNull)
            Finally
                If Not reader Is Nothing Then
                    reader.Close()
                End If
            End Try

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Split a line content into separate column values and add them to the output list
        ''' </summary>
        ''' <param name="lineContent">The line content as it has been read from the CSV file</param>
        ''' <param name="outputList">An array list which shall hold the separated column values</param>
        ''' <param name="startPosition">The start position to which the columnWidhts are related to</param>
        ''' <param name="columnWidths">An array of column widths in their order</param>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[AdminSupport]	29.08.2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Shared Sub SplitFixedCsvLineIntoCellValues(ByRef lineContent As String, ByVal outputList As ArrayList, ByRef startposition As Integer, ByVal columnWidths As Integer())

            Dim CurrentColumnValue As System.Text.StringBuilder = Nothing
            Dim CharPositionCounter As Integer = 0

            For CharPositionCounter = startposition To lineContent.Length - 1
                If CharPositionCounter = startposition Then
                    'Prepare the new value for the first column
                    CurrentColumnValue = New System.Text.StringBuilder
                ElseIf SplitFixedCsvLineIntoCellValuesIsNewColumnPosition(CharPositionCounter, startposition, columnWidths) Then
                    'A new column has been found
                    'Save the previous column value 
                    outputList.Add(CurrentColumnValue.ToString)
                    'Prepare the new value for  the next column
                    CurrentColumnValue = New System.Text.StringBuilder
                End If
                Select Case lineContent.Chars(CharPositionCounter)
                    Case ControlChars.Lf
                        'now it's a line separator
                        Exit For
                    Case ControlChars.Cr
                        'now it's a line separator
                        If CharPositionCounter + 1 < lineContent.Length AndAlso lineContent.Chars(CharPositionCounter + 1) = ControlChars.Lf Then
                            'Found a CrLf occurance; handle it as one line break!
                            CharPositionCounter += 1
                        End If
                        Exit For
                    Case Else
                        'just add the character as it is because it's inside of a cell text
                        CurrentColumnValue.Append(lineContent.Chars(CharPositionCounter))
                End Select
            Next

            'Add the last column value to the collection
            If Not CurrentColumnValue Is Nothing AndAlso CurrentColumnValue.Length <> 0 Then
                outputList.Add(CurrentColumnValue.ToString)
            End If

            'Next start position is the next char after the last read one
            startposition = CharPositionCounter + 1

        End Sub

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Calculate if the current position is the first position of a new column
        ''' </summary>
        ''' <param name="currentPosition">The current position in the whole document</param>
        ''' <param name="startPosition">The start position to which the columnWidhts are related to</param>
        ''' <param name="columnWidths">An array containing the definitions of the column widths</param>
        ''' <returns>True if the current position identifies a new column value, otherwise False</returns>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[adminsupport]	07.03.2006	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Shared Function SplitFixedCsvLineIntoCellValuesIsNewColumnPosition(ByVal currentPosition As Integer, ByVal startPosition As Integer, ByVal columnWidths As Integer()) As Boolean
            Dim positionDifference As Integer = currentPosition - startPosition
            For MyCounter As Integer = 0 To columnWidths.Length - 1
                Dim ColumnStartPosition As Integer
                ColumnStartPosition += columnWidths(MyCounter)
                If positionDifference = ColumnStartPosition Then
                    Return True
                End If
            Next
            Return False
        End Function

        Private Shared Function SumOfIntegerValues(ByVal array As Integer(), ByVal sumUpToElementIndex As Integer) As Integer
            Dim Result As Integer
            For MyCounter As Integer = 0 To sumUpToElementIndex
                Result += array(MyCounter)
            Next
            Return Result
        End Function
#End Region

#Region "Separator separation"
        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Read from a CSV file
        ''' </summary>
        ''' <param name="path">The path of the file</param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="encoding">The text encoding of the file</param>
        ''' <param name="columnSeparator">Choose the required character for splitting the columns. Set to null (Nothing in VisualBasic) to enable fixed column widths mode</param>
        ''' <param name="recognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <param name="recognizeMultipleColumnSeparatorCharsAsOne">Specifies whether we should treat multiple column seperators as one</param>
        ''' <param name="convertEmptyStringsToDBNull">Convert values with empty strings automatically to DbNull</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' In case of duplicate column names, all additional occurances of the same column name will be modified to use a unique column name
        ''' </remarks>
        ''' <history>
        ''' 	[adminwezel]	03.07.2004	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function ReadDataTableFromCsvFile(ByVal path As String, ByVal includesColumnHeaders As Boolean, ByVal encoding As String, ByVal columnSeparator As Char, ByVal recognizeTextBy As Char, ByVal recognizeMultipleColumnSeparatorCharsAsOne As Boolean, ByVal convertEmptyStringsToDBNull As Boolean) As DataTable

            Dim Result As New DataTable
            Dim fi As FileInfo

            If File.Exists(path) Then
                fi = New FileInfo(path)
            ElseIf path.ToLower.StartsWith("http://") OrElse path.ToLower.StartsWith("https://") Then
                Dim LocalCopyOfFileContentFromRemoteUri As String = Utils.ReadStringDataFromUri(path, encoding)
                Return ReadDataTableFromCsvString(LocalCopyOfFileContentFromRemoteUri, includesColumnHeaders, columnSeparator, recognizeTextBy, recognizeMultipleColumnSeparatorCharsAsOne, convertEmptyStringsToDBNull)
            Else
                Throw New System.IO.FileNotFoundException("File not found", path)
            End If
            fi = Nothing

            Dim reader As StreamReader = Nothing
            Try
                If encoding = "" Then
                    reader = New StreamReader(path, System.Text.Encoding.Default)
                Else
                    reader = New StreamReader(path, System.Text.Encoding.GetEncoding(encoding))
                End If
                Result = ReadDataTableFromCsvReader(reader, includesColumnHeaders, System.Globalization.CultureInfo.CurrentCulture, columnSeparator, recognizeTextBy, recognizeMultipleColumnSeparatorCharsAsOne, convertEmptyStringsToDBNull)
            Finally
                If Not reader Is Nothing Then
                    reader.Close()
                End If
            End Try

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Read from a CSV file
        ''' </summary>
        ''' <param name="Path">The path of the file</param>
        ''' <param name="IncludesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="Encoding">The text encoding of the file</param>
        ''' <param name="cultureFormatProvider"></param>
        ''' <param name="RecognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <param name="recognizeMultipleColumnSeparatorCharsAsOne">Specifies whether we should treat multiple column seperators as one</param>
        ''' <param name="convertEmptyStringsToDBNull">Convert values with empty strings automatically to DbNull</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' In case of duplicate column names, all additional occurances of the same column name will be modified to use a unique column name
        ''' </remarks>
        ''' <history>
        ''' 	[adminwezel]	03.07.2004	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function ReadDataTableFromCsvFile(ByVal path As String, ByVal includesColumnHeaders As Boolean, ByVal encoding As System.Text.Encoding, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal recognizeTextBy As Char, ByVal recognizeMultipleColumnSeparatorCharsAsOne As Boolean, ByVal convertEmptyStringsToDBNull As Boolean) As DataTable

            Dim Result As New DataTable
            Dim fi As FileInfo

            If File.Exists(path) Then
                fi = New FileInfo(path)
            ElseIf path.ToLower.StartsWith("http://") OrElse path.ToLower.StartsWith("https://") Then
                Dim LocalCopyOfFileContentFromRemoteUri As String = Utils.ReadStringDataFromUri(path, encoding.WebName)
                Return ReadDataTableFromCsvString(LocalCopyOfFileContentFromRemoteUri, includesColumnHeaders, cultureFormatProvider, recognizeTextBy, recognizeMultipleColumnSeparatorCharsAsOne, convertEmptyStringsToDBNull)
            Else
                Throw New System.IO.FileNotFoundException("File not found", path)
            End If
            fi = Nothing

            Dim reader As StreamReader = Nothing
            Try
                If encoding Is Nothing Then
                    reader = New StreamReader(path, System.Text.Encoding.Default)
                Else
                    reader = New StreamReader(path, encoding)
                End If
                Result = ReadDataTableFromCsvReader(reader, includesColumnHeaders, cultureFormatProvider, Nothing, recognizeTextBy, recognizeMultipleColumnSeparatorCharsAsOne, convertEmptyStringsToDBNull)
            Finally
                If Not reader Is Nothing Then
                    reader.Close()
                End If
            End Try

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Read from a CSV table
        ''' </summary>
        ''' <param name="reader">A stream reader targetting CSV data</param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="columnSeparator">Choose the required character for splitting the columns. Set to null (Nothing in VisualBasic) to enable fixed column widths mode</param>
        ''' <param name="recognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <param name="recognizeMultipleColumnSeparatorCharsAsOne">Specifies whether we should treat multiple column seperators as one</param>
        ''' <param name="convertEmptyStringsToDBNull">Convert values with empty strings automatically to DbNull</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' In case of duplicate column names, all additional occurances of the same column name will be modified to use a unique column name
        ''' </remarks>
        ''' <history>
        ''' 	[adminsupport]	19.04.2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Shared Function ReadDataTableFromCsvReader(ByVal reader As StreamReader, ByVal includesColumnHeaders As Boolean, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal columnSeparator As Char, ByVal recognizeTextBy As Char, ByVal recognizeMultipleColumnSeparatorCharsAsOne As Boolean, ByVal convertEmptyStringsToDBNull As Boolean) As DataTable

            If cultureFormatProvider Is Nothing Then
                cultureFormatProvider = System.Globalization.CultureInfo.InvariantCulture
            End If

            If columnSeparator = Nothing OrElse columnSeparator = vbNullChar Then
                'Attention: list separator is a string, but columnSeparator is implemented as char! Might be a bug in some specal cultures
                If cultureFormatProvider.TextInfo.ListSeparator.Length > 1 Then
                    Throw New NotSupportedException("No column separator has been defined and the current culture declares a list separator with more than 1 character. Column separators with more than 1 characters are currenlty not supported.")
                End If
                columnSeparator = cultureFormatProvider.TextInfo.ListSeparator.Chars(0)
            End If

            Dim Result As New DataTable
            Dim rdStr As String
            Dim RowCounter As Integer

            'Read file content
            rdStr = reader.ReadToEnd
            If rdStr = Nothing Then
                'simply return the empty table when there is no input data
                Return Result
            End If

            'Read the file char by char and add row by row
            Dim CharPosition As Integer = 0
            While CharPosition < rdStr.Length

                'Read the next csv row
                Dim ColValues As New ArrayList
                SplitCsvLineIntoCellValues(rdStr, ColValues, CharPosition, columnSeparator, recognizeTextBy, recognizeMultipleColumnSeparatorCharsAsOne)

                'Add it as a new data row (respectively add the columns definition)
                RowCounter += 1
                If RowCounter = 1 AndAlso includesColumnHeaders Then
                    'Read first line as column names
                    For ColCounter As Integer = 0 To ColValues.Count - 1
                        Dim colName As String = Trim(CType(ColValues(ColCounter), String))
                        If Result.Columns.Contains(colName) = False Then
                            Result.Columns.Add(New DataColumn(colName, GetType(String)))
                        Else
                            Result.Columns.Add(New DataColumn(DataTables.LookupUniqueColumnName(Result, colName), GetType(String)))
                        End If
                    Next
                Else
                    'Read line as data and automatically add required additional columns on the fly
                    Dim MyRow As DataRow = Result.NewRow
                    For ColCounter As Integer = 0 To ColValues.Count - 1
                        Dim colValue As String = Trim(CType(ColValues(ColCounter), String))
                        If Result.Columns.Count <= ColCounter Then
                            Result.Columns.Add(New DataColumn(Nothing, GetType(String)))
                        End If
                        MyRow(ColCounter) = colValue
                    Next
                    Result.Rows.Add(MyRow)
                End If

            End While

            If convertEmptyStringsToDBNull Then
                ConvertEmptyStringsToDBNullValue(Result)
            Else
                ConvertDBNullValuesToEmptyStrings(Result)
            End If

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Split a line content into separate column values and add them to the output list
        ''' </summary>
        ''' <param name="lineContent">The line content as it has been read from the CSV file</param>
        ''' <param name="outputList">An array list which shall hold the separated column values</param>
        ''' <param name="startposition"></param>
        ''' <param name="columnSeparator">Choose the required character for splitting the columns. Set to null (Nothing in VisualBasic) to enable fixed column widths mode</param>
        ''' <param name="recognizeTextBy">A character indicating the start and end of text string</param>
        ''' <param name="recognizeMultipleColumnSeparatorCharsAsOne">Specifies whether we should treat multiple column seperators as one</param>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[AdminSupport]	29.08.2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Shared Sub SplitCsvLineIntoCellValues(ByRef lineContent As String, ByVal outputList As ArrayList, ByRef startposition As Integer, ByVal columnSeparator As Char, ByVal recognizeTextBy As Char, ByVal recognizeMultipleColumnSeparatorCharsAsOne As Boolean)

            Dim CurrentColumnValue As New System.Text.StringBuilder
            Dim InQuotationMarks As Boolean
            Dim CharPositionCounter As Integer

            For CharPositionCounter = startposition To lineContent.Length - 1
                Select Case lineContent.Chars(CharPositionCounter)
                    Case columnSeparator
                        If InQuotationMarks Then
                            'just add the character as it is because it's inside of a cell text
                            CurrentColumnValue.Append(lineContent.Chars(CharPositionCounter))
                        Else
                            'now it's a column separator
                            'implementation follows to the handling of recognizeDoubledColumnSeparatorCharAsOne as Excel does
                            If Not (recognizeMultipleColumnSeparatorCharsAsOne = True AndAlso lineContent.Chars(CharPositionCounter - 1) = columnSeparator) Then
                                outputList.Add(CurrentColumnValue.ToString)
                                CurrentColumnValue = New System.Text.StringBuilder
                            End If
                        End If
                    Case recognizeTextBy
                        If InQuotationMarks = False Then
                            InQuotationMarks = Not InQuotationMarks
                        Else
                            'Switch between state of in- our out-of quotation marks
                            If CharPositionCounter + 1 < lineContent.Length AndAlso lineContent.Chars(CharPositionCounter + 1) = recognizeTextBy Then
                                'doubled quotation marks lead to one single quotation mark
                                CurrentColumnValue.Append("""")
                                'fix the position to be now after the second quotation marks
                                CharPositionCounter += 1
                            Else
                                InQuotationMarks = Not InQuotationMarks
                            End If
                        End If
                    Case ControlChars.Lf
                        If InQuotationMarks Then
                            'just add the line-break because it's inside of a cell text
                            'but add the line break in the format of the curren platform
                            CurrentColumnValue.Append(System.Environment.NewLine)
                        Else
                            'now it's a line separator
                            'Add previously collected data as column value
                            outputList.Add(CurrentColumnValue.ToString)
                            CurrentColumnValue = New System.Text.StringBuilder
                            'Leave this method because the reading of one csv row has been completed
                            Exit For
                        End If
                    Case ControlChars.Cr
                        If InQuotationMarks Then
                            'just add the character as it is because it's inside of a cell text
                            CurrentColumnValue.Append(lineContent.Chars(CharPositionCounter))
                        Else
                            'now it's a line separator
                            If CharPositionCounter + 1 < lineContent.Length AndAlso lineContent.Chars(CharPositionCounter + 1) = ControlChars.Lf Then
                                'Found a CrLf occurance; handle it as one line break!
                                CharPositionCounter += 1
                            End If
                            'Add previously collected data as column value
                            outputList.Add(CurrentColumnValue.ToString)
                            CurrentColumnValue = New System.Text.StringBuilder
                            'Leave this method because the reading of one csv row has been completed
                            Exit For
                        End If
                    Case Else
                        'just add the character as it is because it's inside of a cell text
                        CurrentColumnValue.Append(lineContent.Chars(CharPositionCounter))
                End Select
            Next

            'Add the last column value to the collection
            If CurrentColumnValue.Length <> 0 Then
                outputList.Add(CurrentColumnValue.ToString)
            End If

            'Next start position is the next char after the last read one
            startposition = CharPositionCounter + 1

        End Sub

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Read from a CSV table
        ''' </summary>
        ''' <param name="data">The content of a CSV file</param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="columnSeparator">Choose the required character for splitting the columns. Set to null (Nothing in VisualBasic) to enable fixed column widths mode</param>
        ''' <param name="recognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <param name="recognizeMultipleColumnSeparatorCharsAsOne">Currently without purpose</param>
        ''' <param name="convertEmptyStringsToDBNull">Convert values with empty strings automatically to DbNull</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' In case of duplicate column names, all additional occurances of the same column name will be modified to use a unique column name
        ''' </remarks>
        ''' <history>
        ''' 	[adminwezel]	03.07.2004	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function ReadDataTableFromCsvString(ByVal data As String, ByVal includesColumnHeaders As Boolean, ByVal columnSeparator As Char, ByVal recognizeTextBy As Char, ByVal recognizeMultipleColumnSeparatorCharsAsOne As Boolean, ByVal convertEmptyStringsToDBNull As Boolean) As DataTable

            Dim Result As New DataTable
            Dim reader As StreamReader = Nothing
            Try
                reader = New StreamReader(New MemoryStream(System.Text.Encoding.Unicode.GetBytes(data)), System.Text.Encoding.Unicode, False)
                Result = ReadDataTableFromCsvReader(reader, includesColumnHeaders, System.Globalization.CultureInfo.CurrentCulture, columnSeparator, recognizeTextBy, recognizeMultipleColumnSeparatorCharsAsOne, convertEmptyStringsToDBNull)
            Finally
                If Not reader Is Nothing Then
                    reader.Close()
                End If
            End Try

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Read from a CSV table
        ''' </summary>
        ''' <param name="data">The content of a CSV file</param>
        ''' <param name="IncludesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="cultureFormatProvider"></param>
        ''' <param name="RecognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <param name="recognizeMultipleColumnSeparatorCharsAsOne">Currently without purpose</param>
        ''' <param name="convertEmptyStringsToDBNull">Convert values with empty strings automatically to DbNull</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' In case of duplicate column names, all additional occurances of the same column name will be modified to use a unique column name
        ''' </remarks>
        ''' <history>
        ''' 	[adminwezel]	03.07.2004	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function ReadDataTableFromCsvString(ByVal data As String, ByVal includesColumnHeaders As Boolean, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal recognizeTextBy As Char, ByVal recognizeMultipleColumnSeparatorCharsAsOne As Boolean, ByVal convertEmptyStringsToDBNull As Boolean) As DataTable

            Dim Result As New DataTable
            Dim reader As StreamReader = Nothing
            Try
                reader = New StreamReader(New MemoryStream(System.Text.Encoding.Unicode.GetBytes(data)), System.Text.Encoding.Unicode, False)
                Result = ReadDataTableFromCsvReader(reader, includesColumnHeaders, cultureFormatProvider, Nothing, recognizeTextBy, recognizeMultipleColumnSeparatorCharsAsOne, convertEmptyStringsToDBNull)
            Finally
                If Not reader Is Nothing Then
                    reader.Close()
                End If
            End Try

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Convert DBNull values to empty strings
        ''' </summary>
        ''' <param name="data">The data which might contain DBNull values</param>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[wezel]	14.04.2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Shared Sub ConvertDBNullValuesToEmptyStrings(ByVal data As DataTable)

            'Parameter validation
            If data Is Nothing Then
                Throw New ArgumentNullException("data")
            End If

            'Ensure that only string columns are here
            For ColCounter As Integer = 0 To data.Columns.Count - 1
                If Not data.Columns(ColCounter).DataType Is GetType(String) Then
                    Throw New Exception("All columns must be of data type System.String")
                End If
            Next

            'Update content
            For RowCounter As Integer = 0 To data.Rows.Count - 1
                Dim MyRow As DataRow = data.Rows(RowCounter)
                For ColCounter As Integer = 0 To data.Columns.Count - 1
                    If MyRow(ColCounter).GetType Is GetType(DBNull) Then
                        MyRow(ColCounter) = ""
                    End If
                Next
            Next

        End Sub

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Convert empty string values to DBNull
        ''' </summary>
        ''' <param name="data">The data which might contain empty strings</param>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[wezel]	14.04.2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Shared Sub ConvertEmptyStringsToDBNullValue(ByVal data As DataTable)

            'Parameter validation
            If data Is Nothing Then
                Throw New ArgumentNullException("data")
            End If

            'Ensure that only string columns are here
            For ColCounter As Integer = 0 To data.Columns.Count - 1
                If Not data.Columns(ColCounter).DataType Is GetType(String) Then
                    Throw New Exception("All columns must be of data type System.String")
                End If
            Next

            'Update content
            For RowCounter As Integer = 0 To data.Rows.Count - 1
                Dim MyRow As DataRow = data.Rows(RowCounter)
                For ColCounter As Integer = 0 To data.Columns.Count - 1
                    Try
                        If MyRow(ColCounter).GetType Is GetType(String) AndAlso CType(MyRow(ColCounter), String) = "" Then
                            MyRow(ColCounter) = DBNull.Value
                        End If
                    Catch
                        'Ignore any conversion errors since we only want to change string columns
                    End Try
                Next
            Next

        End Sub
#End Region

#End Region

#Region "Write data"
        Friend Shared Sub WriteDataTableToCsvFile(ByVal path As String, ByVal dataTable As System.Data.DataTable)
            WriteDataTableToCsvFile(path, dataTable, True, System.Globalization.CultureInfo.InvariantCulture, "UTF-8", vbNullChar, """"c)
        End Sub

        Friend Shared Sub WriteDataTableToCsvFile(ByVal path As String, ByVal dataTable As System.Data.DataTable, ByVal includesColumnHeaders As Boolean, ByVal columnWidths As Integer(), ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal encoding As String)

            'Create stream writer
            Dim writer As StreamWriter = Nothing
            Try
                writer = New StreamWriter(path, False, System.Text.Encoding.GetEncoding(encoding))
                writer.Write(ConvertDataTableToCsv(dataTable, includesColumnHeaders, cultureFormatProvider, columnWidths))
            Finally
                If Not writer Is Nothing Then
                    writer.Close()
                End If
            End Try

        End Sub

        Friend Shared Sub WriteDataTableToCsvFile(ByVal path As String, ByVal dataTable As System.Data.DataTable, ByVal includesColumnHeaders As Boolean, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal encoding As String, ByVal columnSeparator As String, ByVal recognizeTextBy As Char)

            'Create stream writer
            Dim writer As StreamWriter = Nothing
            Try
                writer = New StreamWriter(path, False, System.Text.Encoding.GetEncoding(encoding))
                writer.Write(ConvertDataTableToCsv(dataTable, includesColumnHeaders, cultureFormatProvider, columnSeparator, recognizeTextBy))
            Finally
                If Not writer Is Nothing Then
                    writer.Close()
                End If
            End Try

        End Sub

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Trims a string to exactly the required fix size
        ''' </summary>
        ''' <param name="text"></param>
        ''' <param name="fixedLengthSize"></param>
        ''' <param name="alignedRight">Add additionally required spaces on the left (True) or on the right (False)</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[adminsupport]	09.03.2006	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Shared Function FixedLengthText(ByVal text As String, ByVal fixedLengthSize As Integer, ByVal alignedRight As Boolean) As String
            Dim Result As String = Mid(text, 1, fixedLengthSize)
            If Result.Length < fixedLengthSize Then
                'Add some spaces to the string
                If alignedRight = False Then
                    Result &= Strings.Space(fixedLengthSize - Result.Length)
                Else
                    Result = Strings.Space(fixedLengthSize - Result.Length) & Result
                End If
            End If
            Return Result
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Convert the datatable to a string based, comma-separated format
        ''' </summary>
        ''' <param name="dataTable"></param>
        ''' <param name="includesColumnHeaders"></param>
        ''' <param name="cultureFormatProvider"></param>
        ''' <param name="columnWidths"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[adminsupport]	09.03.2006	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function ConvertDataTableToCsv(ByVal dataTable As System.Data.DataTable, ByVal includesColumnHeaders As Boolean, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal columnWidths As Integer()) As String

            If cultureFormatProvider Is Nothing Then
                cultureFormatProvider = System.Globalization.CultureInfo.InvariantCulture
            End If

            Dim writer As New System.Text.StringBuilder

            'Column headers
            If includesColumnHeaders Then
                For ColCounter As Integer = 0 To System.Math.Min(columnWidths.Length, dataTable.Columns.Count) - 1
                    writer.Append(FixedLengthText(dataTable.Columns(ColCounter).ColumnName, columnWidths(ColCounter), False))
                Next
                writer.Append(vbNewLine)
            End If

            'Data values
            For RowCounter As Integer = 0 To dataTable.Rows.Count - 1
                For ColCounter As Integer = 0 To System.Math.Min(columnWidths.Length, dataTable.Columns.Count) - 1
                    If dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                        writer.Append(FixedLengthText(String.Empty, columnWidths(ColCounter), False))
                    ElseIf dataTable.Columns(ColCounter).DataType Is GetType(String) Then
                        'Strings
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            writer.Append(FixedLengthText(CType(dataTable.Rows(RowCounter)(ColCounter), String), columnWidths(ColCounter), False))
                        End If
                    ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.Double) Then
                        'Doubles
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            'Other data types which do not require textual handling
                            writer.Append(FixedLengthText(CType(dataTable.Rows(RowCounter)(ColCounter), Double).ToString(cultureFormatProvider), columnWidths(ColCounter), True))
                        End If
                    ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.Decimal) Then
                        'Decimals
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            'Other data types which do not require textual handling
                            writer.Append(FixedLengthText(CType(dataTable.Rows(RowCounter)(ColCounter), Decimal).ToString(cultureFormatProvider), columnWidths(ColCounter), True))
                        End If
                    ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.DateTime) Then
                        'Datetime
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            'Other data types which do not require textual handling
                            writer.Append(FixedLengthText(CType(dataTable.Rows(RowCounter)(ColCounter), DateTime).ToString(cultureFormatProvider), columnWidths(ColCounter), False))
                        End If
                    ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.Int16) OrElse dataTable.Columns(ColCounter).DataType Is GetType(System.Int32) OrElse dataTable.Columns(ColCounter).DataType Is GetType(System.Int64) Then
                        'Intxx
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            'Other data types which do not require textual handling
                            writer.Append(FixedLengthText(CType(dataTable.Rows(RowCounter)(ColCounter), System.Int64).ToString(cultureFormatProvider), columnWidths(ColCounter), True))
                        End If
                    ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.UInt16) OrElse dataTable.Columns(ColCounter).DataType Is GetType(System.UInt32) OrElse dataTable.Columns(ColCounter).DataType Is GetType(System.UInt64) Then
                        'UIntxx
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            'Other data types which do not require textual handling
                            writer.Append(FixedLengthText(CType(dataTable.Rows(RowCounter)(ColCounter), System.UInt64).ToString(cultureFormatProvider), columnWidths(ColCounter), True))
                        End If
                    Else
                        'Other data types
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            'Other data types which do not require textual handling
                            writer.Append(FixedLengthText(CType(dataTable.Rows(RowCounter)(ColCounter), String), columnWidths(ColCounter), False))
                        End If
                    End If
                Next
                writer.Append(vbNewLine)
            Next
            Return writer.ToString

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Convert the datatable to a string based, comma-separated format
        ''' </summary>
        ''' <param name="dataTable"></param>
        ''' <param name="includesColumnHeaders"></param>
        ''' <param name="cultureFormatProvider"></param>
        ''' <param name="columnSeparator"></param>
        ''' <param name="recognizeTextBy"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[adminsupport]	09.03.2006	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function ConvertDataTableToCsv(ByVal dataTable As System.Data.DataTable, ByVal includesColumnHeaders As Boolean, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal columnSeparator As String, ByVal recognizeTextBy As Char) As String

            If cultureFormatProvider Is Nothing Then
                cultureFormatProvider = System.Globalization.CultureInfo.InvariantCulture
            End If

            If columnSeparator = Nothing OrElse columnSeparator = vbNullChar Then
                columnSeparator = cultureFormatProvider.TextInfo.ListSeparator
            End If

            Dim writer As New System.Text.StringBuilder

            'Column headers
            If includesColumnHeaders Then
                For ColCounter As Integer = 0 To dataTable.Columns.Count - 1
                    If ColCounter <> 0 Then
                        writer.Append(columnSeparator)
                    End If
                    writer.Append(recognizeTextBy & dataTable.Columns(ColCounter).ColumnName.Replace(recognizeTextBy, recognizeTextBy & recognizeTextBy) & recognizeTextBy)
                Next
                writer.Append(vbNewLine)
            End If

            'Data values
            For RowCounter As Integer = 0 To dataTable.Rows.Count - 1
                For ColCounter As Integer = 0 To dataTable.Columns.Count - 1
                    If ColCounter <> 0 Then
                        writer.Append(columnSeparator)
                    End If
                    If dataTable.Columns(ColCounter).DataType Is GetType(String) Then
                        'Strings
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            writer.Append(recognizeTextBy & CType(dataTable.Rows(RowCounter)(ColCounter), String).Replace(recognizeTextBy, recognizeTextBy & recognizeTextBy) & recognizeTextBy)
                        End If
                    ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.Double) Then
                        'Doubles
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            'Other data types which do not require textual handling
                            writer.Append(CType(dataTable.Rows(RowCounter)(ColCounter), Double).ToString(cultureFormatProvider))
                        End If
                    ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.Decimal) Then
                        'Decimals
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            'Other data types which do not require textual handling
                            writer.Append(CType(dataTable.Rows(RowCounter)(ColCounter), Decimal).ToString(cultureFormatProvider))
                        End If
                    ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.DateTime) Then
                        'Datetime
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            'Other data types which do not require textual handling
                            If cultureFormatProvider Is Globalization.CultureInfo.InvariantCulture Then
                                writer.Append(CType(dataTable.Rows(RowCounter)(ColCounter), DateTime).ToString("yyyy-MM-dd HH:mm:ss.fff"))
                            Else
                                writer.Append(CType(dataTable.Rows(RowCounter)(ColCounter), DateTime).ToString(cultureFormatProvider))
                            End If
                        End If
                    Else
                        'Other data types
                        If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                            'Other data types which do not require textual handling
                            writer.Append(dataTable.Rows(RowCounter)(ColCounter).ToString)
                        End If
                    End If
                Next
                writer.Append(vbNewLine)
            Next
            Return writer.ToString

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Write to a CSV file
        ''' </summary>
        ''' <param name="path">The path of the file</param>
        ''' <param name="dataTable"></param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="encoding">The text encoding of the file</param>
        ''' <param name="columnSeparator">Choose the required character for splitting the columns. Set to null (Nothing in VisualBasic) to enable fixed column widths mode</param>
        ''' <param name="recognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <param name="decimalSeparator">A character indicating the decimal separator in the text string</param>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[Wezel]	19.10.2004	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Sub WriteDataTableToCsvFile(ByVal path As String, ByVal dataTable As System.Data.DataTable, ByVal includesColumnHeaders As Boolean, ByVal encoding As String, ByVal columnSeparator As String, ByVal recognizeTextBy As Char, ByVal decimalSeparator As Char)

            Dim cultureFormatProvider As New System.Globalization.CultureInfo("")
            cultureFormatProvider.NumberFormat.CurrencyDecimalSeparator = decimalSeparator
            cultureFormatProvider.NumberFormat.NumberDecimalSeparator = decimalSeparator
            cultureFormatProvider.NumberFormat.PercentDecimalSeparator = decimalSeparator

            'Create stream writer
            Dim writer As StreamWriter = Nothing
            Try
                writer = New StreamWriter(path, False, System.Text.Encoding.GetEncoding(encoding))

                'Column headers
                If includesColumnHeaders Then
                    For ColCounter As Integer = 0 To dataTable.Columns.Count - 1
                        If ColCounter <> 0 Then
                            writer.Write(columnSeparator)
                        End If
                        writer.Write(recognizeTextBy & dataTable.Columns(ColCounter).ColumnName.Replace(recognizeTextBy, recognizeTextBy & recognizeTextBy) & recognizeTextBy)
                    Next
                    writer.WriteLine()
                End If

                'Data values
                For RowCounter As Integer = 0 To dataTable.Rows.Count - 1
                    For ColCounter As Integer = 0 To dataTable.Columns.Count - 1
                        If ColCounter <> 0 Then
                            writer.Write(columnSeparator)
                        End If
                        If dataTable.Columns(ColCounter).DataType Is GetType(String) Then
                            'Strings
                            If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                                writer.Write(recognizeTextBy & CType(dataTable.Rows(RowCounter)(ColCounter), String).Replace(recognizeTextBy, recognizeTextBy & recognizeTextBy) & recognizeTextBy)
                            End If
                        ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.Double) Then
                            'Doubles
                            If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataTable.Rows(RowCounter)(ColCounter), Double).ToString(cultureFormatProvider))
                            End If
                        ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.Decimal) Then
                            'Decimals
                            If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataTable.Rows(RowCounter)(ColCounter), Decimal).ToString(cultureFormatProvider))
                            End If
                        ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.DateTime) Then
                            'Datetime
                            If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                If cultureFormatProvider Is Globalization.CultureInfo.InvariantCulture Then
                                    writer.Write(CType(dataTable.Rows(RowCounter)(ColCounter), DateTime).ToString("yyyy-MM-dd HH:mm:ss.fff"))
                                Else
                                    writer.Write(CType(dataTable.Rows(RowCounter)(ColCounter), DateTime).ToString(cultureFormatProvider))
                                End If
                            End If
                        Else
                            'Other data types
                            If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(dataTable.Rows(RowCounter)(ColCounter).ToString)
                            End If
                        End If
                    Next
                    writer.WriteLine()
                Next

            Finally
                If Not writer Is Nothing Then
                    writer.Close()
                End If
            End Try

        End Sub

        ''' <summary>
        '''     Create a CSV table
        ''' </summary>
        ''' <param name="dataTable"></param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="encoding">The text encoding of the file</param>
        ''' <param name="columnSeparator">Choose the required character for splitting the columns. Set to null (Nothing in VisualBasic) to enable fixed column widths mode</param>
        ''' <param name="recognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <param name="decimalSeparator"></param>
        ''' <returns>A string containing the CSV table</returns>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[wezel]	19.04.2005	Created
        ''' </history>
        Friend Shared Function WriteDataTableToCsvBytes(ByVal dataTable As System.Data.DataTable, ByVal includesColumnHeaders As Boolean, ByVal encoding As String, ByVal columnSeparator As Char, ByVal recognizeTextBy As Char, ByVal decimalSeparator As Char) As Byte()
            Dim MyStream As MemoryStream = WriteDataTableToCsvMemoryStream(dataTable, includesColumnHeaders, encoding, columnSeparator, recognizeTextBy, decimalSeparator)
            Return MyStream.ToArray
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Create a CSV table
        ''' </summary>
        ''' <param name="dataTable"></param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="encoding">The text encoding of the file</param>
        ''' <param name="cultureFormatProvider">A globalization information object for the conversion of all data to strings</param>
        ''' <param name="columnSeparator">Choose the required character for splitting the columns. Set to null (Nothing in VisualBasic) to enable fixed column widths mode</param>
        ''' <param name="recognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <returns>A string containing the CSV table</returns>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[wezel]	19.04.2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function WriteDataTableToCsvBytes(ByVal dataTable As System.Data.DataTable, ByVal includesColumnHeaders As Boolean, ByVal encoding As System.Text.Encoding, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal columnSeparator As Char, ByVal recognizeTextBy As Char) As Byte()
            Dim MyStream As MemoryStream = WriteDataTableToCsvMemoryStream(dataTable, includesColumnHeaders, encoding, cultureFormatProvider, columnSeparator, recognizeTextBy)
            Return MyStream.ToArray
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Create a CSV table
        ''' </summary>
        ''' <param name="dataTable"></param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="encoding">The text encoding of the file</param>
        ''' <param name="columnSeparator">Choose the required character for splitting the columns. Set to null (Nothing in VisualBasic) to enable fixed column widths mode</param>
        ''' <param name="recognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <param name="decimalSeparator"></param>
        ''' <returns>A memory stream containing all texts as bytes in Unicode format</returns>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[wezel]	19.04.2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function WriteDataTableToCsvMemoryStream(ByVal dataTable As System.Data.DataTable, ByVal includesColumnHeaders As Boolean, ByVal encoding As String, ByVal columnSeparator As String, ByVal recognizeTextBy As Char, ByVal decimalSeparator As Char) As System.IO.MemoryStream
            Dim cultureFormatProvider As System.Globalization.CultureInfo = CType(System.Globalization.CultureInfo.InvariantCulture.Clone, System.Globalization.CultureInfo)
            cultureFormatProvider.NumberFormat.CurrencyDecimalSeparator = decimalSeparator
            cultureFormatProvider.NumberFormat.NumberDecimalSeparator = decimalSeparator
            cultureFormatProvider.NumberFormat.PercentDecimalSeparator = decimalSeparator
            Return WriteDataTableToCsvMemoryStream(dataTable, includesColumnHeaders, System.Text.Encoding.GetEncoding(encoding), cultureFormatProvider, columnSeparator, recognizeTextBy)
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Create a CSV table
        ''' </summary>
        ''' <param name="dataTable"></param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="encoding">The text encoding of the file</param>
        ''' <param name="cultureFormatProvider">A globalization information object for the conversion of all data to strings</param>
        ''' <param name="columnSeparator">Choose the required character for splitting the columns. Set to null (Nothing in VisualBasic) to enable fixed column widths mode</param>
        ''' <param name="recognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <returns>A memory stream containing all texts as bytes in Unicode format</returns>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[wezel]	19.04.2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Function WriteDataTableToCsvMemoryStream(ByVal dataTable As System.Data.DataTable, ByVal includesColumnHeaders As Boolean, ByVal encoding As System.Text.Encoding, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal columnSeparator As String, ByVal recognizeTextBy As Char) As System.IO.MemoryStream

            If cultureFormatProvider Is Nothing Then
                cultureFormatProvider = System.Globalization.CultureInfo.InvariantCulture
            End If

            If columnSeparator = Nothing OrElse columnSeparator = vbNullChar Then
                columnSeparator = cultureFormatProvider.TextInfo.ListSeparator
            End If

            'Create stream writer
            Dim Result As New MemoryStream
            Dim writer As StreamWriter = Nothing
            Try
                writer = New StreamWriter(Result, encoding)

                'Column headers
                If includesColumnHeaders Then
                    For ColCounter As Integer = 0 To dataTable.Columns.Count - 1
                        If ColCounter <> 0 Then
                            writer.Write(columnSeparator)
                        End If
                        writer.Write(recognizeTextBy & CsvEncode(dataTable.Columns(ColCounter).ColumnName, recognizeTextBy) & recognizeTextBy)
                    Next
                    writer.WriteLine()
                End If

                'Data values
                For RowCounter As Integer = 0 To dataTable.Rows.Count - 1
                    For ColCounter As Integer = 0 To dataTable.Columns.Count - 1
                        If ColCounter <> 0 Then
                            writer.Write(columnSeparator)
                        End If
                        If dataTable.Columns(ColCounter).DataType Is GetType(String) Then
                            'Strings
                            If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                                writer.Write(recognizeTextBy & CsvEncode(CType(dataTable.Rows(RowCounter)(ColCounter), String), recognizeTextBy) & recognizeTextBy)
                            End If
                        ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.Double) Then
                            'Doubles
                            If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataTable.Rows(RowCounter)(ColCounter), Double).ToString(cultureFormatProvider))
                            End If
                        ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.Decimal) Then
                            'Decimals
                            If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataTable.Rows(RowCounter)(ColCounter), Decimal).ToString(cultureFormatProvider))
                            End If
                        ElseIf dataTable.Columns(ColCounter).DataType Is GetType(System.DateTime) Then
                            'Datetime
                            If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                If cultureFormatProvider Is Globalization.CultureInfo.InvariantCulture Then
                                    writer.Write(CType(dataTable.Rows(RowCounter)(ColCounter), DateTime).ToString("yyyy-MM-dd HH:mm:ss.fff"))
                                Else
                                    writer.Write(CType(dataTable.Rows(RowCounter)(ColCounter), DateTime).ToString(cultureFormatProvider))
                                End If
                            End If
                        Else
                            'Other data types
                            If Not dataTable.Rows(RowCounter)(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataTable.Rows(RowCounter)(ColCounter), String))
                            End If
                        End If
                    Next
                    writer.WriteLine()
                Next

            Finally
                If Not writer Is Nothing Then
                    writer.Close()
                End If
            End Try

            Return Result

        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Encode a string into CSV encoding
        ''' </summary>
        ''' <param name="value">The unencoded text</param>
        ''' <param name="recognizeTextBy">The character to identify a string in the CSV file</param>
        ''' <returns>The encoded writing style of the given text</returns>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[AdminSupport]	29.08.2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Shared Function CsvEncode(ByVal value As String, ByVal recognizeTextBy As Char) As String
            Dim Result As String
            Result = Replace(value, recognizeTextBy, recognizeTextBy & recognizeTextBy)
            Result = Replace(value, ControlChars.CrLf, ControlChars.Lf)
            Result = Replace(value, ControlChars.Cr, ControlChars.Lf)
            Return Result
        End Function

        Friend Shared Sub WriteDataViewToCsvFile(ByVal path As String, ByVal dataview As System.Data.DataView)
            WriteDataViewToCsvFile(path, dataview, True, System.Globalization.CultureInfo.InvariantCulture, "UTF-8", vbNullChar, """"c)
        End Sub

        Friend Shared Sub WriteDataViewToCsvFile(ByVal path As String, ByVal dataView As System.Data.DataView, ByVal includesColumnHeaders As Boolean, ByVal cultureFormatProvider As System.Globalization.CultureInfo, ByVal encoding As String, ByVal columnSeparator As String, ByVal recognizeTextBy As Char)

            Dim DataTable As System.Data.DataTable = dataView.Table

            If cultureFormatProvider Is Nothing Then
                cultureFormatProvider = System.Globalization.CultureInfo.InvariantCulture
            End If

            If columnSeparator = Nothing OrElse columnSeparator = vbNullChar Then
                columnSeparator = cultureFormatProvider.TextInfo.ListSeparator
            End If

            'Create stream writer
            Dim writer As StreamWriter = Nothing
            Try
                writer = New StreamWriter(path, False, System.Text.Encoding.GetEncoding(encoding))

                'Column headers
                If includesColumnHeaders Then
                    For ColCounter As Integer = 0 To DataTable.Columns.Count - 1
                        If ColCounter <> 0 Then
                            writer.Write(columnSeparator)
                        End If
                        writer.Write(recognizeTextBy & DataTable.Columns(ColCounter).ColumnName.Replace(recognizeTextBy, recognizeTextBy & recognizeTextBy) & recognizeTextBy)
                    Next
                    writer.WriteLine()
                End If

                'Data values
                For RowCounter As Integer = 0 To dataView.Count - 1
                    For ColCounter As Integer = 0 To DataTable.Columns.Count - 1
                        If ColCounter <> 0 Then
                            writer.Write(columnSeparator)
                        End If
                        If DataTable.Columns(ColCounter).DataType Is GetType(String) Then
                            'Strings
                            If Not dataView.Item(RowCounter).Row(ColCounter) Is DBNull.Value Then
                                writer.Write(recognizeTextBy & CType(dataView.Item(RowCounter).Row(ColCounter), String).Replace(recognizeTextBy, recognizeTextBy & recognizeTextBy) & recognizeTextBy)
                            End If
                        ElseIf DataTable.Columns(ColCounter).DataType Is GetType(System.Double) Then
                            'Doubles
                            If Not dataView.Item(RowCounter).Row(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataView.Item(RowCounter).Row(ColCounter), Double).ToString(cultureFormatProvider))
                            End If
                        ElseIf DataTable.Columns(ColCounter).DataType Is GetType(System.Decimal) Then
                            'Decimals
                            If Not dataView.Item(RowCounter).Row(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataView.Item(RowCounter).Row(ColCounter), Decimal).ToString(cultureFormatProvider))
                            End If
                        ElseIf DataTable.Columns(ColCounter).DataType Is GetType(System.DateTime) Then
                            'Datetime
                            If Not dataView.Item(RowCounter).Row(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                If cultureFormatProvider Is Globalization.CultureInfo.InvariantCulture Then
                                    writer.Write(CType(DataTable.Rows(RowCounter)(ColCounter), DateTime).ToString("yyyy-MM-dd HH:mm:ss.fff"))
                                Else
                                    writer.Write(CType(DataTable.Rows(RowCounter)(ColCounter), DateTime).ToString(cultureFormatProvider))
                                End If
                            End If
                        Else
                            'Other data types
                            If Not dataView.Item(RowCounter).Row(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataView.Item(RowCounter).Row(ColCounter), String))
                            End If
                        End If
                    Next
                Next

            Finally
                If Not writer Is Nothing Then
                    writer.Close()
                End If
            End Try

        End Sub

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        '''     Write to a CSV file
        ''' </summary>
        ''' <param name="path">The path of the file</param>
        ''' <param name="dataView">A dataview object with the desired rows</param>
        ''' <param name="includesColumnHeaders">Indicates wether column headers are present</param>
        ''' <param name="encoding">The text encoding of the file</param>
        ''' <param name="columnSeparator">Choose the required character for splitting the columns. Set to null (Nothing in VisualBasic) to enable fixed column widths mode</param>
        ''' <param name="recognizeTextBy">A character indicating the start and end of text strings</param>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[Wezel]	19.10.2004	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Friend Shared Sub WriteDataViewToCsvFile(ByVal path As String, ByVal dataView As System.Data.DataView, ByVal includesColumnHeaders As Boolean, ByVal encoding As String, ByVal columnSeparator As String, ByVal recognizeTextBy As Char, ByVal decimalSeparator As Char)

            Dim DataTable As System.Data.DataTable = dataView.Table

            Dim cultureFormatProvider As New System.Globalization.CultureInfo("")
            cultureFormatProvider.NumberFormat.CurrencyDecimalSeparator = decimalSeparator
            cultureFormatProvider.NumberFormat.NumberDecimalSeparator = decimalSeparator
            cultureFormatProvider.NumberFormat.PercentDecimalSeparator = decimalSeparator

            'Create stream writer
            Dim writer As StreamWriter = Nothing
            Try
                writer = New StreamWriter(path, False, System.Text.Encoding.GetEncoding(encoding))

                'Column headers
                If includesColumnHeaders Then
                    For ColCounter As Integer = 0 To DataTable.Columns.Count - 1
                        If ColCounter <> 0 Then
                            writer.Write(columnSeparator)
                        End If
                        writer.Write(recognizeTextBy & DataTable.Columns(ColCounter).ColumnName.Replace(recognizeTextBy, recognizeTextBy & recognizeTextBy) & recognizeTextBy)
                    Next
                    writer.WriteLine()
                End If

                'Data values
                For RowCounter As Integer = 0 To dataView.Count - 1
                    For ColCounter As Integer = 0 To DataTable.Columns.Count - 1
                        If ColCounter <> 0 Then
                            writer.Write(columnSeparator)
                        End If
                        If DataTable.Columns(ColCounter).DataType Is GetType(String) Then
                            'Strings
                            If Not dataView.Item(RowCounter).Row(ColCounter) Is DBNull.Value Then
                                writer.Write(recognizeTextBy & CType(dataView.Item(RowCounter).Row(ColCounter), String).Replace(recognizeTextBy, recognizeTextBy & recognizeTextBy) & recognizeTextBy)
                            End If
                        ElseIf DataTable.Columns(ColCounter).DataType Is GetType(System.Double) Then
                            'Doubles
                            If Not dataView.Item(RowCounter).Row(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataView.Item(RowCounter).Row(ColCounter), Double).ToString(cultureFormatProvider))
                            End If
                        ElseIf DataTable.Columns(ColCounter).DataType Is GetType(System.Decimal) Then
                            'Decimals
                            If Not dataView.Item(RowCounter).Row(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataView.Item(RowCounter).Row(ColCounter), Decimal).ToString(cultureFormatProvider))
                            End If
                        ElseIf DataTable.Columns(ColCounter).DataType Is GetType(System.DateTime) Then
                            'Datetime
                            If Not dataView.Item(RowCounter).Row(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                If cultureFormatProvider Is Globalization.CultureInfo.InvariantCulture Then
                                    writer.Write(CType(DataTable.Rows(RowCounter)(ColCounter), DateTime).ToString("yyyy-MM-dd HH:mm:ss.fff"))
                                Else
                                    writer.Write(CType(DataTable.Rows(RowCounter)(ColCounter), DateTime).ToString(cultureFormatProvider))
                                End If
                            End If
                        Else
                            'Other data types
                            If Not dataView.Item(RowCounter).Row(ColCounter) Is DBNull.Value Then
                                'Other data types which do not require textual handling
                                writer.Write(CType(dataView.Item(RowCounter).Row(ColCounter), String))
                            End If
                        End If
                    Next
                    writer.WriteLine()
                Next

            Finally
                If Not writer Is Nothing Then
                    writer.Close()
                End If
            End Try

        End Sub

#End Region

    End Class

End Namespace